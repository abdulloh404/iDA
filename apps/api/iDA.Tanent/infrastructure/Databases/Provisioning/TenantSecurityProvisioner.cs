using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class TenantSecurityProvisioner
{
    public static async Task RemoveRowSecurityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT c.relname, EXISTS (
                SELECT 1 FROM pg_policy other
                WHERE other.polrelid = c.oid AND other.polname <> 'p_tenant')
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_policy p ON p.polrelid = c.oid
            WHERE n.nspname = $1 AND p.polname = 'p_tenant'
            ORDER BY c.relname
            """, connection, transaction);
        command.Parameters.AddWithValue(branch.SchemaName);
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var table = reader.GetString(0);
                if (reader.GetBoolean(1)) throw new InvalidOperationException($"Cannot remove BU row security from {branch.SchemaName}.{table} while other policies exist.");
                tables.Add(table);
            }
        }

        foreach (var table in tables)
        {
            var qualified = $"{ProvisioningSql.Identifier(branch.SchemaName)}.{ProvisioningSql.Identifier(table)}";
            var sql = $"""
                ALTER TABLE {qualified} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE {qualified} DISABLE ROW LEVEL SECURITY;
                DROP POLICY p_tenant ON {qualified};
                """;
            await ProvisioningSql.ExecuteAsync(connection, transaction, sql, ct);
        }
    }

    public static Task GrantRuntimeAsync(
        IdaDbContext context,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint endpoint,
        CancellationToken ct)
    {
        var schema = ProvisioningSql.Identifier(endpoint.SchemaName);
        var role = ProvisioningSql.Identifier(endpoint.Username);
        var model = context.GetService<IDesignTimeModel>().Model;
        var tables = model.GetEntityTypes()
            .Where(entity => string.Equals(entity.GetSchema(), endpoint.SchemaName, StringComparison.Ordinal))
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Select(name => $"{schema}.{ProvisioningSql.Identifier(name)}")
            .ToArray();
        var tableGrant = tables.Length == 0
            ? string.Empty
            : $"GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE {string.Join(',', tables)} TO {role};";
        return ProvisioningSql.ExecuteAsync(connection, transaction,
            $"GRANT CONNECT ON DATABASE {ProvisioningSql.Identifier(endpoint.DatabaseName)} TO {role}; GRANT USAGE ON SCHEMA {schema} TO {role}; {tableGrant} GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {schema} TO {role};",
            ct);
    }
}
