using Ida.Application.Features.Hello.Queries;
using MediatR;

namespace Ida.Api;

public static class HelloEndpoints
{
    public static void MapIdaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/hello", async (string? name, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetHelloQuery(name))))
            .WithName("GetHello");
    }
}

