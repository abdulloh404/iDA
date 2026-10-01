using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.SystemSettings;

public record HisNotifyEmailListItem(
    Guid Id,
    string Email,
    string? RecipientName,
    RecordStatus Status,
    DateTimeOffset UpdatedAt);

public record HisNotifyEmailDetail(
    Guid Id,
    string Email,
    string? RecipientName,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record HisNotifyEmailInput(
    string? Email,
    string? RecipientName,
    RecordStatus Status,
    string? Remark);

public sealed class HisNotifyEmailSpec
    : CrudSpec<SysHisNotifyEmail, HisNotifyEmailListItem, HisNotifyEmailDetail, HisNotifyEmailInput>
{
    public override string Resource => "his-notify-emails";
    public override string DisplayNameTh => "อีเมล HIS";
    public override string Module => "system-settings";
    public override string DefaultSort => "email";

    public override Expression<Func<SysHisNotifyEmail, HisNotifyEmailListItem>> ListProjection =>
        e => new HisNotifyEmailListItem(e.Id, e.Email, e.RecipientName, e.Status, e.UpdatedAt);

    public override Expression<Func<SysHisNotifyEmail, HisNotifyEmailDetail>> DetailProjection =>
        e => new HisNotifyEmailDetail(e.Id, e.Email, e.RecipientName, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysHisNotifyEmail, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysHisNotifyEmail, object?>>>
        {
            ["email"] = e => e.Email,
            ["recipientName"] = e => e.RecipientName,
            ["status"] = e => e.Status,
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override IQueryable<SysHisNotifyEmail> Search(IQueryable<SysHisNotifyEmail> q,
        ListRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim().ToLowerInvariant();
            q = q.Where(e => e.Email.Contains(text) ||
                (e.RecipientName != null && e.RecipientName.Contains(r.Q.Trim())));
        }

        return q;
    }

    public override void Apply(SysHisNotifyEmail e, HisNotifyEmailInput input, bool isCreate)
    {
        e.Email = input.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        e.RecipientName = string.IsNullOrWhiteSpace(input.RecipientName)
            ? null
            : input.RecipientName.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(SysHisNotifyEmail e, HisNotifyEmailInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Email(errors, input.Email, "email", "อีเมล", required: true);
        MasterFieldRules.MaxLength(errors, input.RecipientName, "recipientName", "ชื่อผู้รับ", 100);
        MasterFieldRules.MaxLength(errors, input.Remark, "remark", "หมายเหตุ", 1000);
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(SysHisNotifyEmail e,
        HisNotifyEmailInput input, bool isCreate, ValidationFailure errors,
        IRepository<SysHisNotifyEmail> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var email = input.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email)) return;
        if (await exec.AnyAsync(repo.Query().Where(o => o.Id != e.Id && o.Email == email), ct))
            errors.Add("email", "duplicate", "อีเมลนี้มีอยู่แล้วในระบบ");
    }

    public override IReadOnlyList<ExcelColumn<HisNotifyEmailListItem>> ExportColumns =>
    [
        new("อีเมล", r => r.Email),
        new("ชื่อผู้รับ", r => r.RecipientName),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

public record HisDoctorCodeMapListItem(
    Guid Id,
    string HisDoctorCode,
    string? DoctorCode,
    string? DoctorName,
    string? DepartmentName,
    RecordStatus Status);

public record HisDoctorCodeMapDetail(
    Guid Id,
    string HisDoctorCode,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record HisDoctorCodeMapInput(
    string? HisDoctorCode,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark);

public sealed class HisDoctorCodeMapSpec
    : CrudSpec<SysHisDoctorCodeMap, HisDoctorCodeMapListItem, HisDoctorCodeMapDetail,
        HisDoctorCodeMapInput>
{
    public override string Resource => "his-doctor-code-maps";
    public override string DisplayNameTh => "รหัสแพทย์ไปเป็นแพทย์กลาง";
    public override string Module => "system-settings";
    public override string DefaultSort => "hisDoctorCode";

    public override IReadOnlyList<string> FilterKeys => ["doctorCodeId", "departmentId"];

    public override Expression<Func<SysHisDoctorCodeMap, HisDoctorCodeMapListItem>> ListProjection =>
        e => new HisDoctorCodeMapListItem(e.Id, e.HisDoctorCode,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Department == null ? null : e.Department.NameTh,
            e.Status);

    public override Expression<Func<SysHisDoctorCodeMap, HisDoctorCodeMapDetail>> DetailProjection =>
        e => new HisDoctorCodeMapDetail(e.Id, e.HisDoctorCode, e.DoctorCodeId, e.DepartmentId,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<SysHisDoctorCodeMap, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<SysHisDoctorCodeMap, object?>>>
        {
            ["hisDoctorCode"] = e => e.HisDoctorCode,
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["departmentName"] = e => e.Department == null ? null : e.Department.NameTh,
            ["status"] = e => e.Status,
        };

    public override IQueryable<SysHisDoctorCodeMap> Search(IQueryable<SysHisDoctorCodeMap> q,
        ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);
        if (r.Filter("departmentId") is { } dep && Guid.TryParse(dep, out var departmentId))
            q = q.Where(e => e.DepartmentId == departmentId);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.HisDoctorCode.Contains(text) ||
                (e.DoctorCode != null &&
                 (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text))));
        }

        return q;
    }

    public override void Apply(SysHisDoctorCodeMap e, HisDoctorCodeMapInput input, bool isCreate)
    {
        e.HisDoctorCode = input.HisDoctorCode?.Trim().ToUpperInvariant() ?? string.Empty;
        e.DoctorCodeId = input.DoctorCodeId;
        e.DepartmentId = input.DepartmentId;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(SysHisDoctorCodeMap e, HisDoctorCodeMapInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.HisDoctorCode, "รหัสแพทย์", maxLength: 20,
            field: "hisDoctorCode");
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "รหัสแพทย์กลาง");
        MasterFieldRules.MaxLength(errors, input.Remark, "remark", "หมายเหตุ", 1000);
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(SysHisDoctorCodeMap e,
        HisDoctorCodeMapInput input, bool isCreate, ValidationFailure errors,
        IRepository<SysHisDoctorCodeMap> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var code = input.HisDoctorCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code)) return;

        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.HisDoctorCode == code && o.DepartmentId == input.DepartmentId), ct))
            errors.Add("hisDoctorCode", "duplicate", input.DepartmentId is null
                ? "รหัสแพทย์นี้ถูกตั้งค่าสำหรับทุกแผนกไว้แล้ว"
                : "รหัสแพทย์นี้ถูกตั้งค่าในแผนกนี้ไว้แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<HisDoctorCodeMapListItem>> ExportColumns =>
    [
        new("รหัสแพทย์ (HIS)", r => r.HisDoctorCode),
        new("รหัสแพทย์กลาง", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("แผนก", r => r.DepartmentName ?? "ทุกแผนก"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

