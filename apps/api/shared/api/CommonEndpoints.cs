namespace Ida.Api;

public static class CommonEndpoints
{
    public static void MapCommonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIdaEndpoints();
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy", checkedAt = DateTimeOffset.UtcNow })).AllowAnonymous();
    }
}
