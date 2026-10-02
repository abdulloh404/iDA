using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseRegistry : IDisposable
{
    private readonly DatabaseConnectionFactory connections;
    public DatabaseRuntime Runtime => DatabaseRuntime.Tenant;
    public string CoreSchemaName { get; }
    public DatabaseEndpoint FixedBranch { get; }

    public DatabaseRegistry(IConfiguration configuration, DatabaseRuntime runtime = DatabaseRuntime.Tenant)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime), "The Tenant service only supports its configured BU database.");
        connections = new DatabaseConnectionFactory(configuration);
        CoreSchemaName = configuration["CORE_DB_SCHEMA"] ?? "core";
        var key = (configuration["Api:BuId"] ?? configuration["BU_ID"])?.Trim().ToUpperInvariant();
        if (key is null || !Regex.IsMatch(key, "^BU[0-9]+$"))
            throw new InvalidOperationException("Set Api:BuId or BU_ID to the configured BU connection key, for example BU01.");
        if (configuration["BU_ID"] is { } buId && !string.Equals(buId.Trim(), key, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Api:BuId and BU_ID must select the same BU.");
        var connectionString = configuration.GetConnectionString("Tenant") ?? configuration[$"{key}_DB_CONNECTION"] ?? configuration.GetConnectionString(key);
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Set ConnectionStrings:Tenant for the configured BU.");
        FixedBranch = connections.ReadEndpoint(key, connectionString, "bu");
        DatabaseConnectionFactory.Validate(FixedBranch);
        if (!Enum.TryParse<SslMode>(FixedBranch.SslMode, true, out var sslMode) || !Enum.IsDefined(sslMode))
            throw new InvalidOperationException($"Invalid SSL mode for {FixedBranch.ConnectionKey}.");
        if (FixedBranch.SchemaName == CoreSchemaName) throw new InvalidOperationException("The BU schema must differ from the Core schema.");
    }

    public NpgsqlDataSource GetSource(DatabaseEndpoint endpoint)
    {
        RequireFixedBranch(endpoint);
        return connections.GetSource(endpoint);
    }

    public string ConnectionString(DatabaseEndpoint endpoint, bool administrator = false)
    {
        RequireFixedBranch(endpoint);
        return connections.ConnectionString(endpoint, administrator);
    }

    public Task<NpgsqlConnection> OpenAsync(DatabaseEndpoint endpoint, CancellationToken ct = default, bool administrator = false)
    {
        RequireFixedBranch(endpoint);
        return connections.OpenAsync(endpoint, ct, administrator);
    }

    public Task<DatabaseEndpoint> GetBranchAsync(string hospitalId, CancellationToken ct = default)
    {
        if (!string.Equals(hospitalId, FixedBranch.HospitalId, StringComparison.Ordinal) && !string.Equals(hospitalId, FixedBranch.ConnectionKey, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The Tenant runtime can only connect to its configured BU database.");
        return Task.FromResult(FixedBranch);
    }

    private void RequireFixedBranch(DatabaseEndpoint endpoint)
    {
        if (endpoint != FixedBranch) throw new InvalidOperationException("The Tenant runtime can only connect to its configured BU database.");
    }

    public static string Quote(string identifier) => DatabaseConnectionFactory.Quote(identifier);

    public void Dispose() => connections.Dispose();
}
