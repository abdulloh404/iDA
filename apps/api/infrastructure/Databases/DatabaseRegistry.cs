using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Ida.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed record DatabaseEndpoint(string ConnectionKey, string Kind, string? HospitalId, string Host, int Port, string DatabaseName, string SchemaName, string Username, string PasswordEnvironment)
{
    public string SslMode { get; init; } = "Prefer";
}

public sealed class DatabaseRegistry : IDisposable
{
    private readonly IConfiguration configuration;
    private readonly ConcurrentDictionary<string, NpgsqlDataSource> sources = new(StringComparer.Ordinal);
    public DatabaseEndpoint Core { get; }
    public NpgsqlDataSource CoreSource => GetSource(Core);

    public DatabaseRegistry(IConfiguration configuration)
    {
        this.configuration = configuration;
        var value = configuration["CORE_DB_CONNECTION"] ?? configuration.GetConnectionString("Core") ?? configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Set CORE_DB_CONNECTION and CORE_DB_PASSWORD before starting iDA.");
        Core = ReadEndpoint("CORE", value, "core");
    }

    public NpgsqlDataSource GetSource(DatabaseEndpoint endpoint)
    {
        var connectionString = ConnectionString(endpoint);
        return sources.GetOrAdd(connectionString, value => new NpgsqlDataSourceBuilder(value).MapIdaEnums(Core.SchemaName).Build());
    }

    public string ConnectionString(DatabaseEndpoint endpoint, bool administrator = false)
    {
        Validate(endpoint);
        var connectionName = endpoint.ConnectionKey + (administrator ? "Migration" : "");
        var explicitValue = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(explicitValue) && endpoint.Kind == "core")
            explicitValue = configuration.GetConnectionString(administrator ? "PostgresMigration" : "Postgres");
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            var explicitConnection = new NpgsqlConnectionStringBuilder(explicitValue);
            if (explicitConnection.Database != endpoint.DatabaseName || (!administrator && explicitConnection.Username != endpoint.Username))
                throw new InvalidOperationException($"ConnectionStrings:{connectionName} must identify the registered database and runtime user for {endpoint.ConnectionKey}.");
            if (string.IsNullOrEmpty(explicitConnection.Password))
                explicitConnection.Password = configuration[administrator ? $"{endpoint.ConnectionKey}_DB_ADMIN_PASSWORD" : endpoint.PasswordEnvironment];
            if (string.IsNullOrEmpty(explicitConnection.Password))
                throw new InvalidOperationException($"Set the password for ConnectionStrings:{connectionName}.");
            explicitConnection.SearchPath = SearchPath(endpoint);
            explicitConnection.ApplicationName = "iDA";
            explicitConnection.Timeout = 15;
            explicitConnection.CommandTimeout = 60;
            explicitConnection.IncludeErrorDetail = false;
            return explicitConnection.ConnectionString;
        }
        var configured = configuration[$"{endpoint.ConnectionKey}_DB_CONNECTION"];
        var original = string.IsNullOrWhiteSpace(configured) ? null : new NpgsqlConnectionStringBuilder(configured);
        var password = configuration[endpoint.PasswordEnvironment] ?? original?.Password;
        var user = endpoint.Username;
        if (administrator)
        {
            var admin = configuration[$"{endpoint.ConnectionKey}_DB_ADMIN_CONNECTION"];
            if (!string.IsNullOrWhiteSpace(admin))
            {
                var explicitAdmin = new NpgsqlConnectionStringBuilder(admin);
                if (explicitAdmin.Host != endpoint.Host || explicitAdmin.Port != endpoint.Port || explicitAdmin.Database != endpoint.DatabaseName)
                    throw new InvalidOperationException($"{endpoint.ConnectionKey}_DB_ADMIN_CONNECTION must identify the same database as the registered endpoint.");
                explicitAdmin.SearchPath = SearchPath(endpoint);
                return explicitAdmin.ConnectionString;
            }
            user = configuration[$"{endpoint.ConnectionKey}_DB_ADMIN_USER"] ?? "postgres";
            password = configuration[$"{endpoint.ConnectionKey}_DB_ADMIN_PASSWORD"];
        }
        if (string.IsNullOrEmpty(password)) throw new InvalidOperationException($"Set {(administrator ? endpoint.ConnectionKey + "_DB_ADMIN_PASSWORD" : endpoint.PasswordEnvironment)}.");
        if (!Enum.TryParse<Npgsql.SslMode>(endpoint.SslMode, true, out var sslMode)) throw new InvalidOperationException($"Invalid SSL mode for {endpoint.ConnectionKey}.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = endpoint.Host,
            Port = endpoint.Port,
            Database = endpoint.DatabaseName,
            Username = user,
            Password = password,
            SslMode = sslMode,
            SearchPath = SearchPath(endpoint),
            ApplicationName = "iDA",
            Timeout = 15,
            CommandTimeout = 60,
            IncludeErrorDetail = false
        }.ConnectionString;
    }

    private string SearchPath(DatabaseEndpoint endpoint) => endpoint.Kind == "bu" ? $"{endpoint.SchemaName},{Core.SchemaName}" : $"bu,{Core.SchemaName}";

    public async Task<NpgsqlConnection> OpenAsync(DatabaseEndpoint endpoint, CancellationToken ct = default, bool administrator = false)
    {
        var connection = new NpgsqlConnection(ConnectionString(endpoint, administrator));
        try
        {
            await connection.OpenAsync(ct);
            if (!administrator && endpoint.Kind == "bu")
            {
                await using var command = new NpgsqlCommand("SELECT set_config('app.hospital_id', $1, false)", connection);
                command.Parameters.AddWithValue(endpoint.HospitalId!);
                await command.ExecuteNonQueryAsync(ct);
            }
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<DatabaseEndpoint>> InitializeAsync(CancellationToken ct = default)
    {
        var endpoints = new List<DatabaseEndpoint> { Core };
        var branchKeys = configuration.AsEnumerable()
            .Where(pair => Regex.IsMatch(pair.Key, "^(BU[0-9]+_DB_CONNECTION|ConnectionStrings:BU[0-9]+)$", RegexOptions.IgnoreCase))
            .Select(pair => pair.Key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase) ? pair.Key.Split(':')[1] : pair.Key[..^"_DB_CONNECTION".Length])
            .Select(key => key.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);
        foreach (var connectionKey in branchKeys)
        {
            var value = configuration[$"{connectionKey}_DB_CONNECTION"] ?? configuration.GetConnectionString(connectionKey);
            if (string.IsNullOrWhiteSpace(value)) continue;
            var endpoint = ReadEndpoint(connectionKey, value, "bu");
            if (endpoint.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{connectionKey} must use a schema name distinct from the Core lookup schema.");
            if (endpoints.Any(other => other.Host == endpoint.Host && other.Port == endpoint.Port && other.DatabaseName == endpoint.DatabaseName))
                throw new InvalidOperationException($"{connectionKey} must have a separate database from Core and other BUs.");
            if (endpoints.Any(other => other.HospitalId == endpoint.HospitalId)) throw new InvalidOperationException($"Duplicate hospital ID for {connectionKey}.");
            endpoints.Add(endpoint);
        }
        await using var connection = await OpenAsync(Core, ct, administrator: true);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = new NpgsqlCommand("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname='registry') THEN
                    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname='branch') THEN
                        RAISE EXCEPTION 'Both registry and branch schemas exist. Resolve the schema conflict before migrating.';
                    END IF;
                    ALTER SCHEMA registry RENAME TO branch;
                END IF;
            END $$;
            CREATE SCHEMA IF NOT EXISTS branch;
            CREATE TABLE IF NOT EXISTS branch.database_connections (
                id uuid PRIMARY KEY DEFAULT pg_catalog.gen_random_uuid(),
                connection_key varchar(40) NOT NULL,
                kind varchar(4) NOT NULL CHECK (kind IN ('core','bu')),
                hospital_id varchar(20) UNIQUE,
                host text NOT NULL,
                port integer NOT NULL CHECK (port BETWEEN 1 AND 65535),
                database_name text NOT NULL,
                schema_name varchar(63) NOT NULL,
                username text NOT NULL,
                password_environment text NOT NULL,
                ssl_mode text NOT NULL DEFAULT 'Prefer',
                is_active boolean NOT NULL DEFAULT true,
                updated_at timestamptz NOT NULL DEFAULT now(),
                CHECK ((kind='core' AND hospital_id IS NULL) OR (kind='bu' AND hospital_id IS NOT NULL))
            );
            ALTER TABLE branch.database_connections ADD COLUMN IF NOT EXISTS id uuid DEFAULT pg_catalog.gen_random_uuid();
            UPDATE branch.database_connections SET id=pg_catalog.gen_random_uuid() WHERE id IS NULL;
            ALTER TABLE branch.database_connections ALTER COLUMN id SET DEFAULT pg_catalog.gen_random_uuid(), ALTER COLUMN id SET NOT NULL;
            DO $$
            DECLARE
                previous_primary_key name;
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_attribute WHERE attrelid='branch.database_connections'::regclass AND attname='code' AND NOT attisdropped) THEN
                    IF EXISTS (SELECT 1 FROM pg_attribute WHERE attrelid='branch.database_connections'::regclass AND attname='connection_key' AND NOT attisdropped) THEN
                        RAISE EXCEPTION 'Both code and connection_key columns exist in branch.database_connections. Resolve the column conflict before migrating.';
                    END IF;
                    ALTER TABLE branch.database_connections RENAME COLUMN code TO connection_key;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_attribute WHERE attrelid='branch.database_connections'::regclass AND attname='enabled' AND NOT attisdropped) THEN
                    ALTER TABLE branch.database_connections RENAME COLUMN enabled TO is_active;
                END IF;
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint c
                    JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attname='id'
                    WHERE c.conrelid='branch.database_connections'::regclass AND c.contype='p' AND c.conkey=ARRAY[a.attnum]
                ) THEN
                    SELECT conname INTO previous_primary_key FROM pg_constraint
                    WHERE conrelid='branch.database_connections'::regclass AND contype='p';
                    IF previous_primary_key IS NOT NULL THEN
                        EXECUTE format('ALTER TABLE branch.database_connections DROP CONSTRAINT %I', previous_primary_key);
                    END IF;
                    ALTER TABLE branch.database_connections ADD PRIMARY KEY (id);
                END IF;
            END $$;
            ALTER INDEX IF EXISTS branch.database_connections_code_key RENAME TO database_connections_connection_key_key;
            CREATE UNIQUE INDEX IF NOT EXISTS database_connections_connection_key_key ON branch.database_connections(connection_key);
            CREATE UNIQUE INDEX IF NOT EXISTS one_core_database ON branch.database_connections(kind) WHERE kind='core';
            """, connection, transaction)) await command.ExecuteNonQueryAsync(ct);
        foreach (var endpoint in endpoints)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO branch.database_connections (connection_key,kind,hospital_id,host,port,database_name,schema_name,username,password_environment,ssl_mode)
                VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
                ON CONFLICT (connection_key) DO UPDATE SET
                    kind=EXCLUDED.kind,
                    hospital_id=EXCLUDED.hospital_id,
                    host=EXCLUDED.host,
                    port=EXCLUDED.port,
                    database_name=EXCLUDED.database_name,
                    schema_name=EXCLUDED.schema_name,
                    username=EXCLUDED.username,
                    password_environment=EXCLUDED.password_environment,
                    ssl_mode=EXCLUDED.ssl_mode,
                    updated_at=now()
                """, connection, transaction);
            command.Parameters.AddWithValue(endpoint.ConnectionKey);
            command.Parameters.AddWithValue(endpoint.Kind);
            command.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Varchar, (object?)endpoint.HospitalId ?? DBNull.Value);
            command.Parameters.AddWithValue(endpoint.Host);
            command.Parameters.AddWithValue(endpoint.Port);
            command.Parameters.AddWithValue(endpoint.DatabaseName);
            command.Parameters.AddWithValue(endpoint.SchemaName);
            command.Parameters.AddWithValue(endpoint.Username);
            command.Parameters.AddWithValue(endpoint.PasswordEnvironment);
            command.Parameters.AddWithValue(endpoint.SslMode);
            await command.ExecuteNonQueryAsync(ct);
        }
        await using (var command = new NpgsqlCommand($"GRANT USAGE ON SCHEMA branch TO {Quote(Core.Username)}; GRANT SELECT ON branch.database_connections TO {Quote(Core.Username)};", connection, transaction)) await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return endpoints;
    }

    public async Task<IReadOnlyList<DatabaseEndpoint>> ListBranchesAsync(CancellationToken ct = default)
    {
        await using var connection = await CoreSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT connection_key,kind,hospital_id,host,port,database_name,schema_name,username,password_environment,ssl_mode FROM branch.database_connections WHERE is_active AND kind='bu' ORDER BY connection_key", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var entries = new List<DatabaseEndpoint>();
        while (await reader.ReadAsync(ct))
        {
            var entry = new DatabaseEndpoint(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)) { SslMode = reader.GetString(9) };
            Validate(entry);
            if (entry.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{entry.ConnectionKey} conflicts with the Core lookup schema.");
            if (entry.Host == Core.Host && entry.Port == Core.Port && entry.DatabaseName == Core.DatabaseName) throw new InvalidOperationException($"{entry.ConnectionKey} points to the Core database.");
            entries.Add(entry);
        }
        if (entries.GroupBy(e => (e.Host, e.Port, e.DatabaseName)).Any(g => g.Count() > 1)) throw new InvalidOperationException("Two BU registrations point to the same database.");
        return entries;
    }

    public async Task<DatabaseEndpoint> GetBranchAsync(string hospitalId, CancellationToken ct = default)
    {
        var entries = await ListBranchesAsync(ct);
        var matches = entries.Where(e => string.Equals(e.HospitalId, hospitalId, StringComparison.Ordinal) || string.Equals(e.ConnectionKey, hospitalId, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 ? matches[0] : throw new InvalidOperationException($"No unique active BU database is registered for hospital '{hospitalId}'.");
    }

    private DatabaseEndpoint ReadEndpoint(string connectionKey, string connectionString, string kind)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var endpoint = new DatabaseEndpoint(connectionKey, kind, kind == "bu" ? configuration[$"{connectionKey}_HOSPITAL_ID"] ?? connectionKey : null, builder.Host ?? "", builder.Port, builder.Database ?? "", configuration[$"{connectionKey}_DB_SCHEMA"] ?? (kind == "core" ? "core" : "bu"), builder.Username ?? "", $"{connectionKey}_DB_PASSWORD") { SslMode = builder.SslMode.ToString() };
        Validate(endpoint);
        return endpoint;
    }

    public static void Validate(DatabaseEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Host) || string.IsNullOrWhiteSpace(endpoint.DatabaseName) || string.IsNullOrWhiteSpace(endpoint.Username)) throw new InvalidOperationException($"Host, database and username are required for {endpoint.ConnectionKey}.");
        if (endpoint.Port is < 1 or > 65535) throw new InvalidOperationException($"Invalid port for {endpoint.ConnectionKey}.");
        if (!Regex.IsMatch(endpoint.SchemaName, "^[a-z_][a-z0-9_]{0,62}$")) throw new InvalidOperationException($"Invalid schema name for {endpoint.ConnectionKey}.");
        if (endpoint.SchemaName is "branch" or "registry" or "pg_catalog" or "information_schema" or "public" || endpoint.SchemaName.StartsWith("pg_", StringComparison.Ordinal)) throw new InvalidOperationException($"Reserved schema name for {endpoint.ConnectionKey}.");
        if (endpoint.Kind == "bu" && (string.IsNullOrWhiteSpace(endpoint.HospitalId) || endpoint.HospitalId.Length > 20)) throw new InvalidOperationException($"A hospital ID of at most 20 characters is required for {endpoint.ConnectionKey}.");
        if (!Regex.IsMatch(endpoint.PasswordEnvironment, "^[A-Z][A-Z0-9_]*$")) throw new InvalidOperationException($"Invalid password environment name for {endpoint.ConnectionKey}.");
    }

    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    public void Dispose()
    {
        foreach (var source in sources.Values) source.Dispose();
    }
}
