using Ida.Domain.Common;

namespace Ida.Infrastructure.Databases;

public sealed record DatabaseLayout(
    string Kind,
    string SchemaName,
    string CoreSchemaName,
    string? HospitalId)
{
    public bool IsBranch => string.Equals(Kind, "bu", StringComparison.OrdinalIgnoreCase);
    public string CacheKey =>
        $"{Kind}:{SchemaName}:{CoreSchemaName}:{HospitalId ?? string.Empty}";

    public static DatabaseLayout For(DatabaseEndpoint endpoint, string coreSchemaName) =>
        new(endpoint.Kind, endpoint.SchemaName, coreSchemaName, endpoint.HospitalId);

    public static bool IsBuEntity(Type type) =>
        string.Equals(type.Namespace, "Ida.Domain.Bu", StringComparison.Ordinal);

    public static bool IsBranchTable(Type type) =>
        IsBuEntity(type) || type == typeof(AuditLog);
}
