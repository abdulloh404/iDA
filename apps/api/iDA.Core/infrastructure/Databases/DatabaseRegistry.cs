using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseRegistry : IDisposable
{
    private readonly DatabaseConnectionFactory connections;
    private readonly DatabaseEndpoint core;
    public DatabaseRuntime Runtime { get; }
    public DatabaseEndpoint Core => core;
    public NpgsqlDataSource CoreSource => GetSource(Core);

    public DatabaseRegistry(IConfiguration configuration, DatabaseRuntime runtime = DatabaseRuntime.Management)
    {
        connections = new DatabaseConnectionFactory(configuration);
        Runtime = runtime;
        var value = configuration["CORE_DB_CONNECTION"] ?? configuration.GetConnectionString("Core") ?? configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Set ConnectionStrings:Core before starting iDA.");
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

    private DatabaseEndpoint ReadEndpoint(string connectionKey, string connectionString, string kind) =>
        connections.ReadEndpoint(connectionKey, connectionString, kind);

    public static string Quote(string identifier) => DatabaseConnectionFactory.Quote(identifier);

    public void Dispose() => connections.Dispose();
}
