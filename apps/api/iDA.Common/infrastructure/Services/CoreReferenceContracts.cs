using System.Text.Json;

namespace Ida.Infrastructure.Persistence;

public record CoreReferenceRequest(string EntityType, JsonElement Key);
public record CoreReferenceResult(string? Reason);
