using Ida.Application.Common;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Services;

namespace Ida.Api.Endpoints;

public static class InternalEndpoints
{
    public static IEndpointRouteBuilder MapTenantInternalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/references", (CoreReferenceRequest input, ReferenceGuard guard, CancellationToken ct) => guard.CheckCoreReferenceAsync(input, ct))
            .AllowAnonymous().ExcludeFromDescription().AddEndpointFilter<ServiceApiFilter>();
        return app;
    }
}
