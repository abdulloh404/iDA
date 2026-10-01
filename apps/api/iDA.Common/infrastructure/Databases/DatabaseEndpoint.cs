namespace Ida.Infrastructure.Databases;

public sealed record DatabaseEndpoint(string ConnectionKey, string Kind, string? HospitalId, string Host, int Port, string DatabaseName, string SchemaName, string Username, string PasswordEnvironment)
{
    public string SslMode { get; init; } = "Prefer";
}

