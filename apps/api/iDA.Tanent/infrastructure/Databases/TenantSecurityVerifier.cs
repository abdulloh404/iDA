using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class TenantSecurityVerifier(DatabaseRegistry registry)
{
    public async Task VerifyAsync(CancellationToken ct = default)
    {
        var branch = registry.FixedBranch;
        await using var connection = await registry.OpenAsync(branch, ct);
        await using var command = new NpgsqlCommand("""
            SELECT c.relname
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = $1 AND c.relkind = 'r'
              AND EXISTS (
                  SELECT 1
                  FROM pg_attribute a
                  WHERE a.attrelid = c.oid
                    AND a.attname = 'hospital_id'
                    AND NOT a.attisdropped)
              AND NOT (
                  c.relrowsecurity
                  AND c.relforcerowsecurity
                  AND EXISTS (
                      SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid))
            ORDER BY 1
            """, connection);
        command.Parameters.AddWithValue(branch.SchemaName);
        var unprotected = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) unprotected.Add(reader.GetString(0));
        if (unprotected.Count > 0)
            throw new InvalidOperationException($"Row-level security is missing in {branch.ConnectionKey}: " + string.Join(", ", unprotected));
    }
}
