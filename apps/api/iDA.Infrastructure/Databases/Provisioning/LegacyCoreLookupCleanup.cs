using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class LegacyCoreLookupCleanup
{
    private const string ServerName = "ida_core_registry";

    public static async Task ApplyAsync(IModel model, NpgsqlConnection connection, NpgsqlTransaction transaction,
        DatabaseEndpoint branch, DatabaseEndpoint core, CancellationToken ct)
    {
        foreach (var definition in model.GetPostgresEnums())
        {
            var exists = await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
                "SELECT EXISTS(SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname=$1 AND t.typname=$2 AND t.typtype='e')",
                ct, core.SchemaName, definition.Name);
            if (!exists) continue;

            var conflict = await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
                "SELECT EXISTS(SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname=$1 AND t.typname=$2)",
                ct, branch.SchemaName, definition.Name);
            if (conflict)
                throw new InvalidOperationException($"{branch.ConnectionKey}: enum {definition.Name} exists in both {core.SchemaName} and {branch.SchemaName}; resolve the conflict before migrating.");

            await ProvisioningSql.ExecuteAsync(connection, transaction,
                $"ALTER TYPE {ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(definition.Name)} SET SCHEMA {ProvisioningSql.Identifier(branch.SchemaName)};", ct);
        }

        var tables = new List<string>();
        await using (var command = new NpgsqlCommand("""
            SELECT c.relname
            FROM pg_foreign_table f
            JOIN pg_class c ON c.oid=f.ftrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_foreign_server s ON s.oid=f.ftserver
            WHERE n.nspname=$1 AND s.srvname=$2
            """, connection, transaction))
        {
            command.Parameters.AddWithValue(core.SchemaName);
            command.Parameters.AddWithValue(ServerName);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        }
        foreach (var table in tables)
            await ProvisioningSql.ExecuteAsync(connection, transaction,
                $"DROP FOREIGN TABLE {ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(table)} RESTRICT;", ct);

        var serverExists = await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
            "SELECT EXISTS(SELECT 1 FROM pg_foreign_server WHERE srvname=$1)", ct, ServerName);
        if (serverExists)
        {
            var remainingTables = await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
                "SELECT EXISTS(SELECT 1 FROM pg_foreign_table f JOIN pg_foreign_server s ON s.oid=f.ftserver WHERE s.srvname=$1)", ct, ServerName);
            if (remainingTables)
                throw new InvalidOperationException($"{branch.ConnectionKey}: the legacy Core server has foreign tables outside {core.SchemaName}; review them before migrating.");

            var users = new List<string?>();
            await using (var command = new NpgsqlCommand("SELECT CASE WHEN umuser=0 THEN NULL ELSE usename END FROM pg_user_mappings WHERE srvname=$1", connection, transaction))
            {
                command.Parameters.AddWithValue(ServerName);
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct)) users.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            }
            foreach (var user in users)
                await ProvisioningSql.ExecuteAsync(connection, transaction,
                    $"DROP USER MAPPING FOR {(user is null ? "PUBLIC" : ProvisioningSql.Identifier(user))} SERVER {ProvisioningSql.Identifier(ServerName)};", ct);
            await ProvisioningSql.ExecuteAsync(connection, transaction, $"DROP SERVER {ProvisioningSql.Identifier(ServerName)} RESTRICT;", ct);
        }

        await ProvisioningSql.ExecuteAsync(connection, transaction, "DROP EXTENSION IF EXISTS postgres_fdw RESTRICT;", ct);
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"DROP SCHEMA IF EXISTS {ProvisioningSql.Identifier(core.SchemaName)} RESTRICT;", ct);
    }
}
