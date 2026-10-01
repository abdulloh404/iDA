using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseRegistry : IDisposable
{
    private readonly IConfiguration configuration;
    private readonly DatabaseConnectionFactory connections;
    private readonly DatabaseEndpoint core;
    public DatabaseRuntime Runtime { get; }
    public DatabaseEndpoint Core => core;
    public NpgsqlDataSource CoreSource => GetSource(Core);

    public DatabaseRegistry(IConfiguration configuration, DatabaseRuntime runtime = DatabaseRuntime.Management)
    {
        this.configuration = configuration;
        connections = new DatabaseConnectionFactory(configuration);
        Runtime = runtime;
        var value = configuration["CORE_DB_CONNECTION"] ?? configuration.GetConnectionString("Core") ?? configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Set CORE_DB_CONNECTION and CORE_DB_PASSWORD before starting iDA.");
        core = ReadEndpoint("CORE", value, "core");
    }

    public NpgsqlDataSource GetSource(DatabaseEndpoint endpoint)
    {
        EnsureCoreEndpoint(endpoint);
        return connections.GetSource(endpoint);
    }

    public string ConnectionString(DatabaseEndpoint endpoint, bool administrator = false)
    {
        EnsureCoreEndpoint(endpoint);
        return connections.ConnectionString(endpoint, administrator);
    }

    public Task<NpgsqlConnection> OpenAsync(DatabaseEndpoint endpoint, CancellationToken ct = default, bool administrator = false)
    {
        EnsureCoreEndpoint(endpoint);
        return connections.OpenAsync(endpoint, ct, administrator);
    }

    private void EnsureCoreEndpoint(DatabaseEndpoint endpoint)
    {
        if (endpoint != Core)
            throw new InvalidOperationException("The Core service can only connect to its configured Core database. Call the Tenant API for BU data.");
    }

    public async Task<IReadOnlyList<DatabaseEndpoint>> InitializeAsync(CancellationToken ct = default)
    {
        if (Runtime != DatabaseRuntime.Management) throw new InvalidOperationException("Database registry migration requires Management mode.");
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
            if (endpoint.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{connectionKey} must use a BU schema name distinct from the Core schema.");
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
        await transaction.CommitAsync(ct);
        return endpoints;
    }

    public async Task<IReadOnlyList<DatabaseEndpoint>> ListBranchesAsync(CancellationToken ct = default, bool administrator = false)
    {
        await using var connection = await OpenAsync(Core, ct, administrator);
        await using var command = new NpgsqlCommand("SELECT connection_key,kind,hospital_id,host,port,database_name,schema_name,username,password_environment,ssl_mode FROM branch.database_connections WHERE is_active AND kind='bu' ORDER BY connection_key", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var entries = new List<DatabaseEndpoint>();
        while (await reader.ReadAsync(ct))
        {
            var entry = new DatabaseEndpoint(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)) { SslMode = reader.GetString(9) };
            Validate(entry);
            if (entry.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{entry.ConnectionKey} must use a BU schema name distinct from the Core schema.");
            if (entry.Host == Core.Host && entry.Port == Core.Port && entry.DatabaseName == Core.DatabaseName) throw new InvalidOperationException($"{entry.ConnectionKey} points to the Core database.");
            entries.Add(entry);
        }
        if (entries.GroupBy(e => (e.Host, e.Port, e.DatabaseName)).Any(g => g.Count() > 1)) throw new InvalidOperationException("Two BU registrations point to the same database.");
        return entries;
    }

    private DatabaseEndpoint ReadEndpoint(string connectionKey, string connectionString, string kind) =>
        connections.ReadEndpoint(connectionKey, connectionString, kind);

    public static void Validate(DatabaseEndpoint endpoint) => DatabaseConnectionFactory.Validate(endpoint);

    public static string Quote(string identifier) => DatabaseConnectionFactory.Quote(identifier);

    public void Dispose() => connections.Dispose();
}
