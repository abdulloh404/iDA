using Ida.Infrastructure.Databases.Provisioning;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseProvisioner(DatabaseRegistry registry)
{
    public Task InitializeAsync(CancellationToken ct = default) => InitializeTenantAsync(ct);

    public Task InitializeTenantAsync(CancellationToken ct = default) => ProvisionAsync(registry.FixedBranch, ct);

    private async Task ProvisionAsync(DatabaseEndpoint endpoint, CancellationToken ct)
    {
        await using var connection = await registry.OpenAsync(endpoint, ct, administrator: true);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await EnsureRuntimeRoleAsync(connection, transaction, endpoint, ct);
        await TenantSecurityProvisioner.RemoveRowSecurityAsync(connection, transaction, endpoint, ct);
        await using var context = DatabaseContexts.CreateSchemaContext(
            endpoint,
            registry.CoreSchemaName,
            registry.ConnectionString(endpoint, administrator: true));

        await EfSchemaProvisioner.ApplyAsync(context, connection, transaction, endpoint, ct);
        var ingest = new IngestSchemaProvisioner();
        await TenantSecurityProvisioner.GrantRuntimeAsync(context, connection, transaction, endpoint, ct);
        await ingest.ApplyBranchAsync(connection, transaction, endpoint, ct);

        await transaction.CommitAsync(ct);
    }

    private async Task EnsureRuntimeRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint endpoint,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT rolsuper,rolbypassrls,rolcanlogin FROM pg_roles WHERE rolname=$1",
            connection,
            transaction);
        command.Parameters.AddWithValue(endpoint.Username);
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                if (reader.GetBoolean(0) || reader.GetBoolean(1))
                    throw new InvalidOperationException($"Runtime role '{endpoint.Username}' must not have SUPERUSER or BYPASSRLS.");
                if (!reader.GetBoolean(2))
                    throw new InvalidOperationException($"Runtime role '{endpoint.Username}' must have LOGIN for {endpoint.ConnectionKey}.");
                return;
            }
        }

        var password = new NpgsqlConnectionStringBuilder(registry.ConnectionString(endpoint)).Password!;
        var role = ProvisioningSql.Identifier(endpoint.Username);
        var passwordLiteral = "E" + ProvisioningSql.Literal(password.Replace("\\", "\\\\"));
        try
        {
            await ProvisioningSql.ExecuteAsync(connection, transaction, $"CREATE ROLE {role} WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD {passwordLiteral};", ct);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Could not create runtime role '{endpoint.Username}' for {endpoint.ConnectionKey} (SQLSTATE {ex.SqlState}).");
        }
    }
}
