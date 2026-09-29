using Ida.Infrastructure.Databases.Provisioning;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseProvisioner(DatabaseRegistry registry)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var endpoints = await registry.InitializeAsync(ct);
        await ProvisionAsync(registry.Core, ct);
        foreach (var branch in endpoints.Where(endpoint => endpoint.Kind == "bu"))
            await ProvisionAsync(branch, ct);
    }

    private async Task ProvisionAsync(DatabaseEndpoint endpoint, CancellationToken ct)
    {
        await using var connection = await registry.OpenAsync(endpoint, ct, administrator: true);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await EnsureRuntimeRoleAsync(connection, transaction, endpoint, ct);
        await using var context = DatabaseContexts.CreateSchemaContext(
            endpoint,
            registry.Core,
            registry.ConnectionString(endpoint, administrator: true));

        await EfSchemaProvisioner.ApplyAsync(context, connection, transaction, endpoint, registry.Core, ct);
        var ingest = new IngestSchemaProvisioner();

        if (string.Equals(endpoint.Kind, "core", StringComparison.OrdinalIgnoreCase))
        {
            await TenantSecurityProvisioner.GrantRuntimeAsync(context, connection, transaction, endpoint, ct);
            await ingest.ApplyCoreAsync(connection, transaction, endpoint, ct);
        }
        else
        {
            await TenantSecurityProvisioner.GrantRuntimeAsync(context, connection, transaction, endpoint, ct);
            await ingest.ApplyBranchAsync(connection, transaction, endpoint, ct);
            await TenantSecurityProvisioner.ApplyAsync(context, connection, transaction, endpoint, ct);
        }

        await ProvisioningSql.ExecuteAsync(connection, transaction, "DROP SCHEMA IF EXISTS public RESTRICT;", ct);
        await transaction.CommitAsync(ct);
    }

    private static async Task EnsureRuntimeRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint endpoint,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT rolsuper,rolbypassrls FROM pg_roles WHERE rolname=$1",
            connection,
            transaction);
        command.Parameters.AddWithValue(endpoint.Username);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException($"Runtime role '{endpoint.Username}' does not exist for {endpoint.ConnectionKey}.");
        if (reader.GetBoolean(0) || reader.GetBoolean(1))
            throw new InvalidOperationException($"Runtime role '{endpoint.Username}' must not have SUPERUSER or BYPASSRLS.");
    }
}
