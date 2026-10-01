using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class CoreRuntimeProvisioner
{
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
