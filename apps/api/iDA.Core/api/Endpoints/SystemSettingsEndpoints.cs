using Ida.Api.Auth;
using Ida.Application.Features.Users;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SystemSettingsEndpoints
{
    public static void MapCoreSystemSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapUsersEndpoints();
        app.MapUserHospitalRolesEndpoints();

        app.MapGet("/api/master-data/users/{id:guid}/account",
                async (Guid id, ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetUserAccountQuery(id), ct)))
            .WithTags("ผู้ใช้งาน")
            .WithName("users_account")
            .Produces<UserAccountDto>()
            .RequirePermission("users.read");

        app.MapPost("/api/master-data/users/{id:guid}/reset-password",
                async (Guid id, ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new ResetUserPasswordCommand(id), ct)))
            .WithTags("ผู้ใช้งาน")
            .WithName("users_reset_password")
            .WithDescription("ออกรหัสผ่านชั่วคราวให้บัญชี Local — รหัสอยู่ในคำตอบนี้ครั้งเดียว")
            .Produces<ResetUserPasswordResult>()
            .RequirePermission("users.write");

        app.MapRolesEndpoints();

        app.MapGet("/api/master-data/roles/{id:guid}/permissions",
                async (Guid id, ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetRolePermissionsQuery(id), ct)))
            .WithTags("สิทธิ์การใช้งาน")
            .WithName("roles_permissions_get")
            .Produces<RolePermissionsDto>()
            .RequirePermission("roles.read");

        app.MapPut("/api/master-data/roles/{id:guid}/permissions",
                async (Guid id, HttpRequest http, RolePermissionsBody body, ISender mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SetRolePermissionsCommand(id, body.Codes,
                        ListQueryString.RowVersion(http)), ct)))
            .WithTags("สิทธิ์การใช้งาน")
            .WithName("roles_permissions_set")
            .WithDescription("แทนที่สิทธิ์ทั้งชุดของบทบาท — ส่ง If-Match เป็น rowVersion ของบทบาท")
            .Produces<RolePermissionsDto>()
            .RequirePermission("roles.write");

        app.MapEmailTemplatesEndpoints();
        app.MapPasswordPoliciesEndpoints();
        app.MapTermsEndpoints();
    }

    public record RolePermissionsBody(IReadOnlyList<string> Codes);
}

