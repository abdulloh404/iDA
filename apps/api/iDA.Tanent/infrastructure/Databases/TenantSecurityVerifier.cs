using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class TenantSecurityVerifier(DatabaseRegistry registry)
{
    public async Task VerifyAsync(CancellationToken ct = default)
    {
        var branch = registry.FixedBranch;
        await using var connection = await registry.OpenAsync(branch, ct);
        await using (var roleCommand = new NpgsqlCommand("SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user", connection))
        {
            if (await roleCommand.ExecuteScalarAsync(ct) is not false)
                throw new InvalidOperationException($"Runtime role '{branch.Username}' must not have SUPERUSER or BYPASSRLS.");
        }
        await using var command = new NpgsqlCommand("""
            SELECT c.relname
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = $1 AND c.relkind IN ('r', 'p')
              AND (c.relrowsecurity OR c.relforcerowsecurity OR EXISTS (
                  SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid AND p.polname = 'p_tenant'))
            ORDER BY 1
            """, connection);
        command.Parameters.AddWithValue(branch.SchemaName);
        var protectedTables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) protectedTables.Add(reader.GetString(0));
        if (protectedTables.Count > 0)
            throw new InvalidOperationException($"Legacy row-level security remains in {branch.ConnectionKey}. Run db:migrate before using the BU database: " + string.Join(", ", protectedTables));
    }
}
