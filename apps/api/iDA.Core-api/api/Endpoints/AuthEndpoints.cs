using Ida.Application.Features.Auth;
using Ida.Application.Features.Auth.Commands;
using Ida.Application.Features.Auth.Queries;
using MediatR;

namespace Ida.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("เข้าสู่ระบบ");

        group.MapPost("/login", async (LoginCommand command, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(command, ct)))
            .WithName("auth_login")
            .Produces<SessionDto>()
            .AllowAnonymous();

        group.MapGet("/me", async (ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetMeQuery(), ct)))
            .WithName("auth_me")
            .Produces<MeDto>()
            .RequireAuthorization();

        group.MapPost("/switch-hospital",
                async (SwitchHospitalCommand command, ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("auth_switch_hospital")
            .Produces<SessionDto>()
            .RequireAuthorization();

        group.MapPost("/refresh", async (ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new RefreshSessionCommand(), ct)))
            .WithName("auth_refresh")
            .WithDescription("ออก token ใหม่ของโรงพยาบาลที่เปิดอยู่ พร้อมสิทธิ์ล่าสุดของบทบาท " +
                "— สิทธิ์ที่เพิ่งได้รับจะเห็นโดยไม่ต้องออกจากระบบแล้วเข้าใหม่")
            .Produces<SessionDto>()
            .RequireAuthorization();

    }
}

