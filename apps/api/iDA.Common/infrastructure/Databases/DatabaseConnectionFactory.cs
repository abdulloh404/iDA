using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Ida.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseConnectionFactory(IConfiguration configuration) : IDisposable
{
    private readonly ConcurrentDictionary<string, NpgsqlDataSource> sources = new(StringComparer.Ordinal);

    public NpgsqlDataSource GetSource(DatabaseEndpoint endpoint)
    {
        var connectionString = ConnectionString(endpoint);
        return sources.GetOrAdd(connectionString, value => new NpgsqlDataSourceBuilder(value).MapIdaEnums(endpoint.SchemaName).Build());
    }

    public string ConnectionString(DatabaseEndpoint endpoint, bool administrator = false)
    {
        Validate(endpoint);
        var connectionName = endpoint.ConnectionKey + (administrator ? "Migration" : "");
        var tenantConnectionName = administrator ? "TenantMigration" : "Tenant";
        if (endpoint.Kind == "bu" && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(tenantConnectionName))) connectionName = tenantConnectionName;
        var explicitValue = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(explicitValue) && endpoint.Kind == "core")
            explicitValue = configuration.GetConnectionString(administrator ? "PostgresMigration" : "Postgres");
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            var explicitConnection = new NpgsqlConnectionStringBuilder(explicitValue);
            if (explicitConnection.Host != endpoint.Host || explicitConnection.Port != endpoint.Port || explicitConnection.Database != endpoint.DatabaseName || (!administrator && explicitConnection.Username != endpoint.Username))
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

    private static string SearchPath(DatabaseEndpoint endpoint) => endpoint.SchemaName;

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

    public DatabaseEndpoint ReadEndpoint(string connectionKey, string connectionString, string kind)
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

    public static string Quote(string identifier) => DatabaseSql.Quote(identifier);

    public void Dispose()
    {
        foreach (var source in sources.Values) source.Dispose();
    }
}
