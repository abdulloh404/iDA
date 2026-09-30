using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class TenantSecurityProvisioner
{
    public static async Task ApplyAsync(
        IdaDbContext context,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        CancellationToken ct)
    {
        var model = context.GetService<IDesignTimeModel>().Model;
        var entities = model.GetEntityTypes()
            .Where(entity => DatabaseLayout.IsBranchTable(entity.ClrType))
            .Where(entity => string.Equals(entity.GetSchema(), branch.SchemaName, StringComparison.Ordinal))
            .Where(entity => entity.GetTableName() is not null)
            .ToList();

        foreach (var entity in entities.GroupBy(entity => entity.GetTableName(), StringComparer.Ordinal).Select(group => group.First()))
        {
            var table = entity.GetTableName()!;
            var predicate = BuildPredicate(entity, ProvisioningSql.Identifier(table), branch.SchemaName, new HashSet<IReadOnlyEntityType>(), 0);
            if (predicate is null) continue;
            var qualified = $"{ProvisioningSql.Identifier(branch.SchemaName)}.{ProvisioningSql.Identifier(table)}";
            var sql = $"""
                ALTER TABLE {qualified} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {qualified} FORCE ROW LEVEL SECURITY;
                DO $policy$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_policies
                        WHERE schemaname={ProvisioningSql.Literal(branch.SchemaName)}
                            AND tablename={ProvisioningSql.Literal(table)}
                            AND policyname='p_tenant'
                    ) THEN
                        CREATE POLICY p_tenant ON {qualified}
                            USING ({predicate})
                            WITH CHECK ({predicate});
                    END IF;
                END
                $policy$;
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

    private static string? BuildPredicate(
        IReadOnlyEntityType entity,
        string alias,
        string schema,
        HashSet<IReadOnlyEntityType> visited,
        int depth)
    {
        if (!visited.Add(entity)) return null;
        var table = entity.GetTableName();
        if (table is null) return null;
        var store = StoreObjectIdentifier.Table(table, schema);
        var hospital = entity.FindProperty("HospitalId");
        if (hospital is not null)
        {
            var column = hospital.GetColumnName(store);
            if (column is not null)
                return $"{alias}.{ProvisioningSql.Identifier(column)} = current_setting('app.hospital_id', true)";
        }

        foreach (var foreignKey in entity.GetForeignKeys())
        {
            var principal = foreignKey.PrincipalEntityType;
            if (!DatabaseLayout.IsBranchTable(principal.ClrType) || !string.Equals(principal.GetSchema(), schema, StringComparison.Ordinal))
                continue;
            var principalTable = principal.GetTableName();
            if (principalTable is null) continue;
            var nextAlias = "parent" + depth.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var parentPredicate = BuildPredicate(principal, nextAlias, schema, new HashSet<IReadOnlyEntityType>(visited), depth + 1);
            if (parentPredicate is null) continue;
            var principalStore = StoreObjectIdentifier.Table(principalTable, schema);
            var joins = foreignKey.Properties.Zip(foreignKey.PrincipalKey.Properties, (dependent, parent) =>
            {
                var dependentColumn = dependent.GetColumnName(store)
                    ?? throw new InvalidOperationException($"No column mapping for {entity.ClrType.Name}.{dependent.Name}.");
                var parentColumn = parent.GetColumnName(principalStore)
                    ?? throw new InvalidOperationException($"No column mapping for {principal.ClrType.Name}.{parent.Name}.");
                return $"{nextAlias}.{ProvisioningSql.Identifier(parentColumn)} = {alias}.{ProvisioningSql.Identifier(dependentColumn)}";
            });
            return $"EXISTS (SELECT 1 FROM {ProvisioningSql.Identifier(schema)}.{ProvisioningSql.Identifier(principalTable)} AS {nextAlias} WHERE {string.Join(" AND ", joins)} AND {parentPredicate})";
        }
        return null;
    }
}
