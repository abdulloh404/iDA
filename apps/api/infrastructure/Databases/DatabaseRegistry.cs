using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Ida.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed record DatabaseEndpoint(string Code, string Kind, string? HospitalId, string Host, int Port, string DatabaseName, string SchemaName, string Username, string PasswordEnvironment)
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
        var configured = configuration[$"{endpoint.Code}_DB_CONNECTION"];
        var original = string.IsNullOrWhiteSpace(configured) ? null : new NpgsqlConnectionStringBuilder(configured);
        var password = configuration[endpoint.PasswordEnvironment] ?? original?.Password;
        var user = endpoint.Username;
        if (administrator)
        {
            var admin = configuration[$"{endpoint.Code}_DB_ADMIN_CONNECTION"];
            if (!string.IsNullOrWhiteSpace(admin))
            {
                var explicitAdmin = new NpgsqlConnectionStringBuilder(admin);
                if (explicitAdmin.Host != endpoint.Host || explicitAdmin.Port != endpoint.Port || explicitAdmin.Database != endpoint.DatabaseName)
                    throw new InvalidOperationException($"{endpoint.Code}_DB_ADMIN_CONNECTION must identify the same database as the registered endpoint.");
                explicitAdmin.SearchPath = SearchPath(endpoint);
                return explicitAdmin.ConnectionString;
            }
            user = configuration[$"{endpoint.Code}_DB_ADMIN_USER"] ?? "postgres";
            password = configuration[$"{endpoint.Code}_DB_ADMIN_PASSWORD"];
        }
        if (string.IsNullOrEmpty(password)) throw new InvalidOperationException($"Set {(administrator ? endpoint.Code + "_DB_ADMIN_PASSWORD" : endpoint.PasswordEnvironment)}.");
        if (!Enum.TryParse<Npgsql.SslMode>(endpoint.SslMode, true, out var sslMode)) throw new InvalidOperationException($"Invalid SSL mode for {endpoint.Code}.");
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

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var endpoints = new List<DatabaseEndpoint> { Core };
        foreach (var pair in configuration.AsEnumerable().Where(p => Regex.IsMatch(p.Key, "^BU[0-9]+_DB_CONNECTION$", RegexOptions.IgnoreCase)).OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(pair.Value)) continue;
            var code = pair.Key[..^"_DB_CONNECTION".Length].ToUpperInvariant();
            var endpoint = ReadEndpoint(code, pair.Value, "bu");
            if (endpoint.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{code} must use a schema name distinct from the Core lookup schema.");
            if (endpoints.Any(other => other.Host == endpoint.Host && other.Port == endpoint.Port && other.DatabaseName == endpoint.DatabaseName))
                throw new InvalidOperationException($"{code} must have a separate database from Core and other BUs.");
            if (endpoints.Any(other => other.HospitalId == endpoint.HospitalId)) throw new InvalidOperationException($"Duplicate hospital ID for {code}.");
            endpoints.Add(endpoint);
        }
        await using var connection = await OpenAsync(Core, ct, administrator: true);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = new NpgsqlCommand("""
            CREATE SCHEMA IF NOT EXISTS registry;
            CREATE TABLE IF NOT EXISTS registry.database_connections (
                code varchar(40) PRIMARY KEY,
                kind varchar(4) NOT NULL CHECK (kind IN ('core','bu')),
                hospital_id varchar(20) UNIQUE,
                host text NOT NULL,
                port integer NOT NULL CHECK (port BETWEEN 1 AND 65535),
                database_name text NOT NULL,
                schema_name varchar(63) NOT NULL,
                username text NOT NULL,
                password_environment text NOT NULL,
                ssl_mode text NOT NULL DEFAULT 'Prefer',
                enabled boolean NOT NULL DEFAULT true,
                updated_at timestamptz NOT NULL DEFAULT now(),
                CHECK ((kind='core' AND hospital_id IS NULL) OR (kind='bu' AND hospital_id IS NOT NULL))
            );
            CREATE UNIQUE INDEX IF NOT EXISTS one_core_database ON registry.database_connections(kind) WHERE kind='core';
            """, connection, transaction)) await command.ExecuteNonQueryAsync(ct);
        foreach (var endpoint in endpoints)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO registry.database_connections (code,kind,hospital_id,host,port,database_name,schema_name,username,password_environment,ssl_mode)
                VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
                ON CONFLICT (code) DO NOTHING
                """, connection, transaction);
            command.Parameters.AddWithValue(endpoint.Code);
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
        await using (var command = new NpgsqlCommand($"GRANT USAGE ON SCHEMA registry TO {Quote(Core.Username)}; GRANT SELECT ON registry.database_connections TO {Quote(Core.Username)};", connection, transaction)) await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<DatabaseEndpoint>> ListBranchesAsync(CancellationToken ct = default)
    {
        await using var connection = await CoreSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT code,kind,hospital_id,host,port,database_name,schema_name,username,password_environment,ssl_mode FROM registry.database_connections WHERE enabled AND kind='bu' ORDER BY code", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var entries = new List<DatabaseEndpoint>();
        while (await reader.ReadAsync(ct))
        {
            var entry = new DatabaseEndpoint(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)) { SslMode = reader.GetString(9) };
            Validate(entry);
            if (entry.SchemaName == Core.SchemaName) throw new InvalidOperationException($"{entry.Code} conflicts with the Core lookup schema.");
            if (entry.Host == Core.Host && entry.Port == Core.Port && entry.DatabaseName == Core.DatabaseName) throw new InvalidOperationException($"{entry.Code} points to the Core database.");
            entries.Add(entry);
        }
        if (entries.GroupBy(e => (e.Host, e.Port, e.DatabaseName)).Any(g => g.Count() > 1)) throw new InvalidOperationException("Two BU registrations point to the same database.");
        return entries;
    }

    public async Task<DatabaseEndpoint> GetBranchAsync(string hospitalId, CancellationToken ct = default)
    {
        var entries = await ListBranchesAsync(ct);
        var matches = entries.Where(e => string.Equals(e.HospitalId, hospitalId, StringComparison.Ordinal) || string.Equals(e.Code, hospitalId, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 ? matches[0] : throw new InvalidOperationException($"No unique enabled BU database is registered for hospital '{hospitalId}'.");
    }

    private DatabaseEndpoint ReadEndpoint(string code, string connectionString, string kind)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var endpoint = new DatabaseEndpoint(code, kind, kind == "bu" ? configuration[$"{code}_HOSPITAL_ID"] ?? code : null, builder.Host ?? "", builder.Port, builder.Database ?? "", configuration[$"{code}_DB_SCHEMA"] ?? (kind == "core" ? "core" : "bu"), builder.Username ?? "", $"{code}_DB_PASSWORD") { SslMode = builder.SslMode.ToString() };
        Validate(endpoint);
        return endpoint;
    }

    public static void Validate(DatabaseEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Host) || string.IsNullOrWhiteSpace(endpoint.DatabaseName) || string.IsNullOrWhiteSpace(endpoint.Username)) throw new InvalidOperationException($"Host, database and username are required for {endpoint.Code}.");
        if (endpoint.Port is < 1 or > 65535) throw new InvalidOperationException($"Invalid port for {endpoint.Code}.");
        if (!Regex.IsMatch(endpoint.SchemaName, "^[a-z_][a-z0-9_]{0,62}$")) throw new InvalidOperationException($"Invalid schema name for {endpoint.Code}.");
        if (endpoint.SchemaName is "registry" or "pg_catalog" or "information_schema" or "public" || endpoint.SchemaName.StartsWith("pg_", StringComparison.Ordinal)) throw new InvalidOperationException($"Reserved schema name for {endpoint.Code}.");
        if (endpoint.Kind == "bu" && (string.IsNullOrWhiteSpace(endpoint.HospitalId) || endpoint.HospitalId.Length > 20)) throw new InvalidOperationException($"A hospital ID of at most 20 characters is required for {endpoint.Code}.");
        if (!Regex.IsMatch(endpoint.PasswordEnvironment, "^[A-Z][A-Z0-9_]*$")) throw new InvalidOperationException($"Invalid password environment name for {endpoint.Code}.");
    }

    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    public void Dispose()
    {
        foreach (var source in sources.Values) source.Dispose();
    }
}
