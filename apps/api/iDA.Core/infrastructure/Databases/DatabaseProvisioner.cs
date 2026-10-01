using Ida.Infrastructure.Databases.Provisioning;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseProvisioner(DatabaseRegistry registry)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await registry.InitializeAsync(ct);
        await ProvisionAsync(registry.Core, ct);
    }

    private async Task ProvisionAsync(DatabaseEndpoint endpoint, CancellationToken ct)
    {
        await using var connection = await registry.OpenAsync(endpoint, ct, administrator: true);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await EnsureRuntimeRoleAsync(connection, transaction, endpoint, ct);
        await using var context = DatabaseContexts.CreateSchemaContext(
            endpoint,
            registry.ConnectionString(endpoint, administrator: true));

        await EfSchemaProvisioner.ApplyAsync(context, connection, transaction, endpoint, ct);
        var role = ProvisioningSql.Identifier(endpoint.Username);
        await ProvisioningSql.ExecuteAsync(connection, transaction, $"GRANT USAGE ON SCHEMA branch TO {role}; GRANT SELECT ON branch.database_connections TO {role};", ct);
        await CoreRuntimeProvisioner.GrantRuntimeAsync(context, connection, transaction, endpoint, ct);
        await new CoreIngestSchemaProvisioner().ApplyAsync(connection, transaction, endpoint, ct);

        await ProvisioningSql.ExecuteAsync(connection, transaction, "DROP SCHEMA IF EXISTS public RESTRICT;", ct);
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
