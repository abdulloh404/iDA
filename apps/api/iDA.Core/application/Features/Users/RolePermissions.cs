using System.Text.Json;
using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.Users;

public record RolePermissionItem(
    string Code,
    string Resource,
    string Action,
    string Module,
    string NameTh,
    bool IsGroupLevel,
    bool Granted,

    bool Assignable);

public record RolePermissionsDto(
    Guid RoleId,
    string RoleCode,
    bool IsGroupLevel,

    string? LockedReason,
    IReadOnlyList<RolePermissionItem> Permissions);

public record GetRolePermissionsQuery(Guid RoleId) : IQuery<RolePermissionsDto>;

public class GetRolePermissionsHandler(
    IRepository<Role> roles,
    IRepository<Permission> permissions,
    IRepository<RolePermission> grants,
    IQueryExecutor exec)
    : IQueryHandler<GetRolePermissionsQuery, RolePermissionsDto>
{
    public async Task<RolePermissionsDto> Handle(GetRolePermissionsQuery query, CancellationToken ct)
    {
        var role = await RolePermissionRules.LoadRoleAsync(roles, exec, query.RoleId, ct);

        var granted = (await exec.ToListAsync(grants.Query()
            .Where(g => g.RoleId == role.Id)
            .Select(g => g.PermissionId), ct)).ToHashSet();

        var all = await exec.ToListAsync(permissions.Query()
            .OrderBy(p => p.Module).ThenBy(p => p.Resource).ThenBy(p => p.Action), ct);

        return new RolePermissionsDto(role.Id, role.Code, role.IsGroupLevel,
            RolePermissionRules.WhyLocked(role),
            all.Select(p => new RolePermissionItem(p.Code, p.Resource, p.Action, p.Module,
                    p.NameTh, p.IsGroupLevel, granted.Contains(p.Id),
                    RolePermissionRules.IsAssignable(role, p)))
                .ToList());
    }
}

public record SetRolePermissionsCommand(Guid RoleId, IReadOnlyList<string> Codes, string? RowVersion)
    : ICommand<RolePermissionsDto>;

public class SetRolePermissionsHandler(
    IRepository<Role> roles,
    IRepository<Permission> permissions,
    IRepository<RolePermission> grants,
    IRepository<AuditLog> auditLogs,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser current,
    IRequestContext request,
    IClock clock,
    MediatR.ISender sender)
    : ICommandHandler<SetRolePermissionsCommand, RolePermissionsDto>
{
    public async Task<RolePermissionsDto> Handle(SetRolePermissionsCommand command,
        CancellationToken ct)
    {
        var role = await exec.FirstOrDefaultAsync(roles.Track().Where(r => r.Id == command.RoleId), ct)
            ?? throw ApiException.NotFound("roles_not_found", "ไม่พบข้อมูลสิทธิ์การใช้งานที่ระบุ");

        if (RolePermissionRules.WhyLocked(role) is { } locked)
            throw ApiException.BadRequest("role_locked", locked);

        if (!string.IsNullOrEmpty(command.RowVersion))
            roles.SetConcurrencyToken(role, command.RowVersion);

        var wanted = (command.Codes ?? []).Select(c => c.Trim()).Where(c => c.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var catalogue = await exec.ToListAsync(permissions.Query()
            .Where(p => wanted.Contains(p.Code)), ct);

        var unknown = wanted.Except(catalogue.Select(p => p.Code)).ToList();
        if (unknown.Count > 0)
            throw ApiException.BadRequest("unknown_permission",
                $"ไม่พบสิทธิ์ {string.Join(", ", unknown.Take(5))}");

        var refused = catalogue.Where(p => !RolePermissionRules.IsAssignable(role, p)).ToList();
        if (refused.Count > 0)
            throw ApiException.BadRequest("permission_not_assignable",
                "สิทธิ์แก้ไขข้อมูลระดับเครือให้ได้เฉพาะสิทธิ์การใช้งานระดับเครือ: " +
                string.Join(", ", refused.Take(5).Select(p => p.NameTh)));

        var existing = await exec.ToListAsync(grants.Track()
            .Where(g => g.RoleId == role.Id)
            .Select(g => g), ct);
        var existingIds = existing.Select(g => g.PermissionId).ToHashSet();
        var wantedIds = catalogue.Select(p => p.Id).ToHashSet();

        var removed = existing.Where(g => !wantedIds.Contains(g.PermissionId)).ToList();
        var added = catalogue.Where(p => !existingIds.Contains(p.Id)).ToList();

        if (removed.Count > 0 || added.Count > 0)
        {
            foreach (var g in removed) grants.Remove(g);
            foreach (var p in added) grants.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });

            var removedIds = removed.Select(g => g.PermissionId).ToList();
            var removedCodes = await exec.ToListAsync(permissions.Query()
                .Where(p => removedIds.Contains(p.Id))
                .Select(p => p.Code), ct);
            auditLogs.Add(new AuditLog
            {

                TableName = nameof(Role),
                RecordPk = role.Id.ToString(),
                Action = "UPDATE",
                NewValue = JsonSerializer.Serialize(new Dictionary<string, string?>
                {
                    ["permissions_added"] = added.Count == 0 ? null
                        : string.Join(", ", added.Select(p => p.Code).Order()),
                    ["permissions_removed"] = removedCodes.Count == 0 ? null
                        : string.Join(", ", removedCodes.Order()),
                }),
                ChangedBy = current.UserName,
                ChangedAt = clock.Now,
                ClientIp = request.ClientIp,
                RequestId = request.TraceId,
            });

            role.UpdatedAt = clock.Now;
            await uow.SaveChangesAsync(ct);
        }

        return await sender.Send(new GetRolePermissionsQuery(role.Id), ct);
    }
}

internal static class RolePermissionRules
{
    public static async Task<Role> LoadRoleAsync(IRepository<Role> roles, IQueryExecutor exec,
        Guid id, CancellationToken ct) =>
        await exec.FirstOrDefaultAsync(roles.Query().Where(r => r.Id == id), ct)
        ?? throw ApiException.NotFound("roles_not_found", "ไม่พบข้อมูลสิทธิ์การใช้งานที่ระบุ");

    public static string? WhyLocked(Role role) =>
        role.Code == SystemRoleCodes.GroupAdmin
            ? "ผู้ดูแลระบบส่วนกลางมีทุกสิทธิ์เสมอ ตารางสิทธิ์ของบทบาทนี้แก้ไม่ได้"
            : null;

    public static bool IsAssignable(Role role, Permission permission) =>
        role.IsGroupLevel || !permission.IsGroupLevel ||
        permission.Action is "read" or "export";
}

