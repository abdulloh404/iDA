namespace Ida.Application.Features.IngestConfiguration;

public record InterfaceDto(string Code, string Name, string SourceSystem, string? EndpointUrl, string DataCategory);
