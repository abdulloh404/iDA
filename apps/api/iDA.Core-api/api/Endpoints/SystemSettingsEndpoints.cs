using Ida.Api.Auth;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee406;
using Ida.Application.Features.IncomeDocuments;
using Ida.Application.Features.SystemSettings;
using Ida.Application.Features.Users;
using Ida.Domain.Auth;
using Ida.Domain.Bu;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SystemSettingsEndpoints
{
    public static void MapCoreSystemSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<AppUser, UserListItem, UserDetail, UserInput>(Resource("users"));
        app.MapCrud<UserHospitalRole, UserHospitalRoleRow, UserHospitalRoleDetail,
            UserHospitalRoleInput>(Resource("user-hospital-roles"));

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

        app.MapCrud<Role, RoleListItem, RoleDetail, RoleInput>(Resource("roles"));

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

        app.MapCrud<SysEmailTemplate, EmailTemplateListItem, EmailTemplateDetail,
            EmailTemplateInput>(Resource("email-templates"));
        app.MapCrud<SysPasswordPolicy, PasswordPolicyDetail, PasswordPolicyDetail,
            PasswordPolicyInput>(Resource("password-policies"));
        app.MapCrud<SysTerms, TermsDetail, TermsDetail, TermsInput>(Resource("terms"));
    }

    public record RolePermissionsBody(IReadOnlyList<string> Codes);

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException($"No CrudSpec declares the resource '{name}'.");
}

