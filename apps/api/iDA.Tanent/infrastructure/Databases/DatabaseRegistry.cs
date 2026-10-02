using System.Text.Json;
using System.Text.Json.Serialization;
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
        var metadata = configuration["IDA_DATABASE_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            FixedBranch = ReadMetadata(metadata);
            var selectors = new[] { configuration["Api:BuId"], configuration["BU_ID"] }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (selectors.Length == 0 || selectors.Any(value => !string.Equals(value, FixedBranch.ConnectionKey, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("IDA_DATABASE_ENDPOINT connectionKey must match Api:BuId and BU_ID when configured.");
        }
        else
        {
            var key = (configuration["Api:BuId"] ?? configuration["BU_ID"])?.Trim().ToUpperInvariant();
            if (key is null || !Regex.IsMatch(key, "^BU[0-9]+$"))
                throw new InvalidOperationException("Set Api:BuId or BU_ID to the configured BU connection key, for example BU01.");
            var connectionString = configuration.GetConnectionString("Tenant") ?? configuration[$"{key}_DB_CONNECTION"] ?? configuration.GetConnectionString(key);
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Set ConnectionStrings:Tenant for the configured BU.");
            FixedBranch = connections.ReadEndpoint(key, connectionString, "bu");
        }
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

    public Task<IReadOnlyList<DatabaseEndpoint>> InitializeAsync(CancellationToken ct = default) => ListBranchesAsync(ct);

    public Task<IReadOnlyList<DatabaseEndpoint>> ListBranchesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DatabaseEndpoint>>([FixedBranch]);

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

    private static DatabaseEndpoint ReadMetadata(string metadata)
    {
        try
        {
            using var document = JsonDocument.Parse(metadata);
            string[] fields = ["connectionKey", "kind", "hospitalId", "host", "port", "databaseName", "schemaName", "username", "passwordEnvironment", "sslMode"];
            var names = new HashSet<string>(fields, StringComparer.Ordinal);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("IDA_DATABASE_ENDPOINT must contain BU database metadata.");
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!names.Remove(property.Name) || (property.Name == "port"
                    ? property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out _)
                    : property.Value.ValueKind != JsonValueKind.String))
                    throw new InvalidOperationException("IDA_DATABASE_ENDPOINT contains invalid or unsupported metadata fields.");
            }
            if (names.Count != 0) throw new InvalidOperationException("IDA_DATABASE_ENDPOINT must contain all registered BU metadata fields.");
            var endpoint = document.RootElement.Deserialize<DatabaseEndpoint>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = false,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            })!;
            if (string.IsNullOrWhiteSpace(endpoint.ConnectionKey) || endpoint.ConnectionKey.Length > 40 || endpoint.Kind != "bu")
                throw new InvalidOperationException("IDA_DATABASE_ENDPOINT requires a registered BU connectionKey of at most 40 characters and kind 'bu'.");
            return endpoint;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("IDA_DATABASE_ENDPOINT must contain valid BU database metadata JSON.");
        }
    }

    public static string Quote(string identifier) => DatabaseConnectionFactory.Quote(identifier);

    public void Dispose() => connections.Dispose();
}
