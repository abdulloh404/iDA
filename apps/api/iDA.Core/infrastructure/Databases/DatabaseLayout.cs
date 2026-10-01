namespace Ida.Infrastructure.Databases;

public sealed record DatabaseLayout(string SchemaName)
{
    public string CacheKey => SchemaName;

    public static DatabaseLayout Core(string schemaName) => new(schemaName);
}
