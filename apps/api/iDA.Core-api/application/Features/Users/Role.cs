using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Auth;
using Ida.Domain.Common;

namespace Ida.Application.Features.Users;

public static class SystemRoleCodes
{

    public const string GroupAdmin = "GROUP_ADMIN";
}

public record RoleListItem(
    Guid Id,
    string Code,
    string NameTh,
    bool IsGroupLevel,
    bool IsSystem,
    int PermissionCount,
    RecordStatus Status,
    DateTimeOffset UpdatedAt);

public record RoleDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? Description,
    bool IsGroupLevel,
    bool IsSystem,
    RecordStatus Status,
    string RowVersion);

public record RoleInput(
    string? Code,
    string? NameTh,
    string? NameEn,
    string? Description,
    bool IsGroupLevel,
    RecordStatus Status);

public sealed class RoleSpec : CrudSpec<Role, RoleListItem, RoleDetail, RoleInput>
{
    public override string Resource => "roles";
    public override string DisplayNameTh => "สิทธิ์การใช้งาน";
    public override string Module => "admin";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "nameTh";

    public override Expression<Func<Role, RoleListItem>> ListProjection =>
        e => new RoleListItem(e.Id, e.Code, e.NameTh, e.IsGroupLevel, e.IsSystem,
            e.Permissions.Count(p => p.Permission != null), e.Status, e.UpdatedAt);

    public override Expression<Func<Role, RoleDetail>> DetailProjection =>
        e => new RoleDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Description, e.IsGroupLevel,
            e.IsSystem, e.Status, e.RowVersion.ToString());

    public override Expression<Func<Role, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<Role, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<Role, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["status"] = e => e.Status,
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override IQueryable<Role> Search(IQueryable<Role> q, ListRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.NameTh.Contains(text) || e.Code.Contains(text.ToUpper()));
        }

        return q;
    }

    public override void Apply(Role e, RoleInput input, bool isCreate)
    {

        if (isCreate)
        {
            e.Code = input.Code?.Trim().ToUpperInvariant() ?? string.Empty;

            e.IsGroupLevel = input.IsGroupLevel;
        }

        e.NameTh = input.NameTh?.Trim() ?? string.Empty;
        e.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
        e.Description = input.Description?.Trim();
        e.Status = input.Status;
    }

    public override Task ValidateAsync(Role e, RoleInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (isCreate)
        {
            MasterFieldRules.Code(errors, input.Code, "รหัสสิทธิ์การใช้งาน");
            if (!string.IsNullOrWhiteSpace(input.Code) &&
                !input.Code.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                errors.Add("code", "invalid",
                    "รหัสสิทธิ์การใช้งานใช้ได้เฉพาะตัวอักษรอังกฤษ ตัวเลข และ _");
        }

        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "สิทธิ์การใช้งาน");
        MasterFieldRules.MaxLength(errors, input.NameTh, "nameTh", "สิทธิ์การใช้งาน", 100);
        MasterFieldRules.MaxLength(errors, input.NameEn, "nameEn", "ชื่อภาษาอังกฤษ", 100);
        MasterFieldRules.MaxLength(errors, input.Description, "description", "หมายเหตุ", 1000);

        if (!isCreate && e.IsSystem && input.Status != RecordStatus.Active)
            errors.Add("status", "system", "สิทธิ์การใช้งานของระบบปิดการใช้งานไม่ได้");
        return Task.CompletedTask;
    }

    public override Task<string?> WhyCannotDeleteAsync(Role e, CancellationToken ct) =>
        Task.FromResult(e.IsSystem ? "สิทธิ์การใช้งานของระบบลบไม่ได้" : null);

    public override IReadOnlyList<ExcelColumn<RoleListItem>> ExportColumns =>
    [
        new("รหัส", r => r.Code),
        new("สิทธิ์การใช้งาน", r => r.NameTh),
        new("ระดับ", r => r.IsGroupLevel ? "ระดับเครือ" : "ระดับโรงพยาบาล"),
        new("จำนวนสิทธิ์", r => r.PermissionCount),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
        new("วันที่แก้ไขล่าสุด", r => r.UpdatedAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm")),
    ];
}

