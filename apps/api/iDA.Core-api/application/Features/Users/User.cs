using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Auth;
using Ida.Domain.Common;

namespace Ida.Application.Features.Users;

public static class AuthTypes
{
    public const string Local = "LOCAL";
    public const string ActiveDirectory = "AD";

    public const string LockedPassword = "!";
}

public record UserListItem(
    Guid Id,
    string Username,
    string DisplayName,
    string? EmployeeCode,
    string? Email,
    string? Mobile,
    string AuthType,
    string? Roles,
    DateTimeOffset? LastLoginAt,
    RecordStatus Status);

public record UserDetail(
    Guid Id,
    string Username,
    string DisplayName,
    string? EmployeeCode,
    string? Position,
    string? Email,
    string? Mobile,
    string AuthType,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record UserInput(
    string? DisplayName,
    string? EmployeeCode,
    string? Position,
    string? Email,
    string? Mobile,
    string? AuthType,
    RecordStatus Status,
    string? Remark);

public sealed class UserSpec : CrudSpec<AppUser, UserListItem, UserDetail, UserInput>
{
    public override string Resource => "users";
    public override string DisplayNameTh => "ผู้ใช้งาน";
    public override string Module => "admin";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "displayName";

    public override IReadOnlyList<string> FilterKeys => ["authType", "roleId", "hospitalId"];

    public override Expression<Func<AppUser, UserListItem>> ListProjection =>
        e => new UserListItem(e.Id, e.Username, e.DisplayName, e.EmployeeCode, e.Email, e.Mobile,
            e.AuthType,
            string.Join(", ", e.HospitalRoles
                .Where(h => h.Status == RecordStatus.Active)
                .OrderBy(h => h.HospitalId)
                .Select(h => h.HospitalId + " · " + h.Role!.NameTh)),
            e.LastLoginAt, e.Status);

    public override Expression<Func<AppUser, UserDetail>> DetailProjection =>
        e => new UserDetail(e.Id, e.Username, e.DisplayName, e.EmployeeCode, e.Position, e.Email,
            e.Mobile, e.AuthType, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<AppUser, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<AppUser, object?>>>
        {
            ["displayName"] = e => e.DisplayName,
            ["username"] = e => e.Username,
            ["employeeCode"] = e => e.EmployeeCode,
            ["email"] = e => e.Email,
            ["authType"] = e => e.AuthType,
            ["lastLoginAt"] = e => e.LastLoginAt,
            ["status"] = e => e.Status,
        };

    public override IQueryable<AppUser> Search(IQueryable<AppUser> q, ListRequest r)
    {
        if (r.Filter("authType") is { } authType)
            q = q.Where(e => e.AuthType == authType);
        if (r.Filter("roleId") is { } rid && Guid.TryParse(rid, out var roleId))
            q = q.Where(e => e.HospitalRoles.Any(h => h.RoleId == roleId && h.Status == RecordStatus.Active));
        if (r.Filter("hospitalId") is { } hospitalId)
            q = q.Where(e => e.HospitalRoles.Any(h => h.HospitalId == hospitalId && h.Status == RecordStatus.Active));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            var lower = text.ToLowerInvariant();
            q = q.Where(e => e.DisplayName.Contains(text) || e.Username.Contains(text) ||
                (e.EmployeeCode != null && e.EmployeeCode.Contains(text)) ||
                (e.Email != null && e.Email.Contains(lower)) ||
                (e.Mobile != null && e.Mobile.Contains(text)));
        }

        return q;
    }

    public override void Apply(AppUser e, UserInput input, bool isCreate)
    {
        e.DisplayName = input.DisplayName?.Trim() ?? string.Empty;
        e.EmployeeCode = Blank(input.EmployeeCode);
        e.Position = Blank(input.Position);
        e.Email = Blank(input.Email)?.ToLowerInvariant();
        e.Mobile = Blank(input.Mobile);
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();

        var authType = input.AuthType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (isCreate)
            e.Username = (authType == AuthTypes.ActiveDirectory ? e.Email : e.Mobile) ?? string.Empty;

        if (authType != e.AuthType || isCreate)
        {
            e.AuthType = authType;

            if (authType == AuthTypes.ActiveDirectory)
            {
                e.PasswordHash = null;
                e.PasswordChangedAt = null;
            }
            else if (e.PasswordHash is null)
            {
                e.PasswordHash = AuthTypes.LockedPassword;
            }
        }
    }

    public override Task ValidateAsync(AppUser e, UserInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Required(errors, input.DisplayName, "displayName", "ชื่อ-นามสกุล");
        MasterFieldRules.MaxLength(errors, input.DisplayName, "displayName", "ชื่อ-นามสกุล", 100);
        MasterFieldRules.MaxLength(errors, input.EmployeeCode, "employeeCode", "รหัสพนักงาน", 10);
        MasterFieldRules.MaxLength(errors, input.Position, "position", "ตำแหน่ง", 100);
        MasterFieldRules.Email(errors, input.Email, "email", "อีเมล", required: true);
        MasterFieldRules.MaxLength(errors, input.Remark, "remark", "หมายเหตุ", 1000);

        var mobile = input.Mobile?.Trim();
        if (string.IsNullOrEmpty(mobile))
            errors.Required("mobile", "โปรดระบุเบอร์โทรศัพท์มือถือ");
        else if (mobile.Length != 10 || !mobile.All(char.IsAsciiDigit) || mobile[0] != '0')
            errors.Add("mobile", "invalid", "เบอร์โทรศัพท์มือถือต้องเป็นตัวเลข 10 หลัก ขึ้นต้นด้วย 0");

        var authType = input.AuthType?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(authType))
            errors.Required("authType", "โปรดระบุประเภทผู้ใช้งาน");
        else if (authType is not (AuthTypes.Local or AuthTypes.ActiveDirectory))
            errors.Add("authType", "invalid", "ประเภทผู้ใช้งานต้องเป็น AD หรือ Local");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(AppUser e, UserInput input, bool isCreate,
        ValidationFailure errors, IRepository<AppUser> repo, IQueryExecutor exec,
        CancellationToken ct)
    {
        if (Blank(input.Email)?.ToLowerInvariant() is { } email &&
            await exec.AnyAsync(repo.Query().Where(o => o.Id != e.Id && o.Email == email), ct))
            errors.Add("email", "duplicate", "อีเมลนี้มีอยู่แล้วในระบบ");

        if (Blank(input.Mobile) is { } mobile &&
            await exec.AnyAsync(repo.Query().Where(o => o.Id != e.Id && o.Mobile == mobile), ct))
            errors.Add("mobile", "duplicate", "เบอร์โทรศัพท์นี้มีอยู่แล้วในระบบ");

        if (isCreate && !string.IsNullOrEmpty(e.Username) &&
            await exec.AnyAsync(repo.Query().Where(o => o.Username == e.Username), ct))
            errors.Add(e.AuthType == AuthTypes.ActiveDirectory ? "email" : "mobile", "duplicate",
                $"ชื่อบัญชีผู้ใช้ {e.Username} มีอยู่แล้วในระบบ");
    }

    public override Task ValidateForActorAsync(AppUser e, UserInput input, bool isCreate,
        ValidationFailure errors, ICurrentUser actor, CancellationToken ct)
    {

        if (!isCreate && e.Id == actor.UserId && input.Status != RecordStatus.Active)
            errors.Add("status", "self", "ไม่สามารถปิดการใช้งานบัญชีของตัวเองได้");
        return Task.CompletedTask;
    }

    public override string? WhyActorCannotDelete(AppUser e, ICurrentUser actor) =>
        e.Id == actor.UserId ? "ไม่สามารถลบบัญชีของตัวเองได้" : null;

    public override IReadOnlyList<ExcelColumn<UserListItem>> ExportColumns =>
    [
        new("ชื่อ-นามสกุล", r => r.DisplayName),
        new("รหัสพนักงาน", r => r.EmployeeCode),
        new("อีเมล", r => r.Email),
        new("เบอร์โทรศัพท์มือถือ", r => r.Mobile),
        new("ชื่อบัญชีผู้ใช้", r => r.Username),
        new("สิทธิ์การใช้งาน", r => r.Roles),
        new("ประเภทผู้ใช้งาน", r => r.AuthType == AuthTypes.ActiveDirectory ? "AD" : "Local"),
        new("เข้าใช้งานล่าสุด", r => r.LastLoginAt?.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm")),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public record UserHospitalRoleRow(
    Guid Id,
    Guid UserId,
    string HospitalId,
    string? HospitalName,
    Guid RoleId,
    string? RoleName,
    bool IsDefault,
    RecordStatus Status);

public record UserHospitalRoleDetail(
    Guid Id,
    Guid UserId,
    string HospitalId,
    string? HospitalName,
    Guid RoleId,
    string? RoleName,
    bool IsDefault,
    RecordStatus Status,
    string RowVersion);

public record UserHospitalRoleInput(
    Guid UserId,
    string? HospitalId,
    Guid RoleId,
    bool IsDefault,
    RecordStatus Status);

public sealed class UserHospitalRoleSpec
    : CrudSpec<UserHospitalRole, UserHospitalRoleRow, UserHospitalRoleDetail, UserHospitalRoleInput>
{
    public override string Resource => "user-hospital-roles";
    public override string DisplayNameTh => "สิทธิ์การใช้งานตามโรงพยาบาล";
    public override string Module => "admin";
    public override bool IsGroupLevel => true;
    public override string DefaultSort => "hospitalId";

    public override IReadOnlyList<string> FilterKeys => ["userId"];

    public override Expression<Func<UserHospitalRole, UserHospitalRoleRow>> ListProjection =>
        e => new UserHospitalRoleRow(e.Id, e.UserId, e.HospitalId,
            e.Hospital == null ? null : e.Hospital.HospitalNameTh,
            e.RoleId, e.Role == null ? null : e.Role.NameTh, e.IsDefault, e.Status);

    public override Expression<Func<UserHospitalRole, UserHospitalRoleDetail>> DetailProjection =>
        e => new UserHospitalRoleDetail(e.Id, e.UserId, e.HospitalId,
            e.Hospital == null ? null : e.Hospital.HospitalNameTh,
            e.RoleId, e.Role == null ? null : e.Role.NameTh, e.IsDefault, e.Status,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<UserHospitalRole, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<UserHospitalRole, object?>>>
        {
            ["hospitalId"] = e => e.HospitalId,
            ["roleName"] = e => e.Role == null ? null : e.Role.NameTh,
        };

    public override IQueryable<UserHospitalRole> Search(IQueryable<UserHospitalRole> q,
        ListRequest r)
    {
        if (r.Filter("userId") is { } uid && Guid.TryParse(uid, out var userId))
            q = q.Where(e => e.UserId == userId);
        return q;
    }

    public override void Apply(UserHospitalRole e, UserHospitalRoleInput input, bool isCreate)
    {
        if (isCreate) e.UserId = input.UserId;
        e.HospitalId = input.HospitalId?.Trim() ?? string.Empty;
        e.RoleId = input.RoleId;
        e.IsDefault = input.IsDefault;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(UserHospitalRole e, UserHospitalRoleInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.UserId, "userId", "ผู้ใช้งาน");
        if (string.IsNullOrWhiteSpace(input.HospitalId))
            errors.Required("hospitalId", "โปรดเลือกสังกัดโรงพยาบาล");
        MasterFieldRules.RequiredId(errors, input.RoleId, "roleId", "สิทธิ์การใช้งาน");

        if (input.IsDefault && input.Status != RecordStatus.Active)
            errors.Add("isDefault", "inactive",
                "โรงพยาบาลเริ่มต้นต้องเป็นสิทธิ์ที่ใช้งานอยู่");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(UserHospitalRole e,
        UserHospitalRoleInput input, bool isCreate, ValidationFailure errors,
        IRepository<UserHospitalRole> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var userId = isCreate ? input.UserId : e.UserId;
        var hospitalId = input.HospitalId?.Trim();

        if (!string.IsNullOrEmpty(hospitalId) && await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.UserId == userId && o.HospitalId == hospitalId), ct))
            errors.Add("hospitalId", "duplicate",
                "ผู้ใช้นี้มีสิทธิ์ที่โรงพยาบาลนี้อยู่แล้ว — แก้ไขแถวเดิมแทน");

        if (input.IsDefault)
        {
            var other = await exec.FirstOrDefaultAsync(repo.Query()
                .Where(o => o.Id != e.Id && o.UserId == userId && o.IsDefault)
                .Select(o => o.HospitalId), ct);
            if (other is not null)
                errors.Add("isDefault", "duplicate",
                    $"ผู้ใช้นี้มีโรงพยาบาลเริ่มต้นอยู่แล้ว ({other}) — ยกเลิกของเดิมก่อน");
        }
    }
}

