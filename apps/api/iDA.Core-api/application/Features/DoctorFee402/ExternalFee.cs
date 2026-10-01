using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

public record ExternalFeeListItem(
    Guid Id,
    ExternalFeeKind Kind,
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    string? CompanyCode,
    string? CompanyName,
    int LineCount,

    decimal TotalAmount,
    ApprovalStatus ApprovalStatus,
    bool CycleClosed,
    RecordStatus Status);

public record ExternalFeeDetail(
    Guid Id,
    ExternalFeeKind Kind,
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    Guid ArCodeId,
    decimal TotalAmount,
    ApprovalStatus ApprovalStatus,
    bool CycleClosed,
    string? DecisionComment,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ExternalFeeInput(
    ExternalFeeKind Kind,
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    Guid ArCodeId,
    RecordStatus Status,
    string? Remark);

public sealed class ExternalFeeSpec
    : CrudSpec<DfExternalFee, ExternalFeeListItem, ExternalFeeDetail, ExternalFeeInput>
{
    public override string Resource => "external-fees";
    public override string DisplayNameTh => "ค่าแพทย์ภายนอก";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "-refDocDate";

    public override IReadOnlyList<string> FilterKeys =>
        ["kind", "year", "month", "arCodeId", "approvalStatus", "cycleClosed", "refDocDate"];

    public override Expression<Func<DfExternalFee, ExternalFeeListItem>> ListProjection =>
        e => new ExternalFeeListItem(e.Id, e.Kind, e.RefDocDate, e.RefDocNo,
            e.PeriodYear, e.PeriodMonth,
            e.ArCode == null ? null : e.ArCode.Code,
            e.ArCode == null ? null : e.ArCode.NameTh,
            e.Lines.Count(l => l.DeletedAt == null),
            e.Lines.Where(l => l.DeletedAt == null).Sum(l => (decimal?)l.Amount) ?? 0m,
            e.ApprovalStatus, e.CycleClosed, e.Status);

    public override Expression<Func<DfExternalFee, ExternalFeeDetail>> DetailProjection =>
        e => new ExternalFeeDetail(e.Id, e.Kind, e.RefDocDate, e.RefDocNo,
            e.PeriodYear, e.PeriodMonth, e.ArCodeId,
            e.Lines.Where(l => l.DeletedAt == null).Sum(l => (decimal?)l.Amount) ?? 0m,
            e.ApprovalStatus, e.CycleClosed, e.DecisionComment, e.DecidedBy, e.DecidedAt,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DfExternalFee, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfExternalFee, object?>>>
        {
            ["refDocDate"] = e => e.RefDocDate,
            ["refDocNo"] = e => e.RefDocNo,
            ["period"] = e => e.PeriodYear * 100 + e.PeriodMonth,
            ["companyName"] = e => e.ArCode == null ? null : e.ArCode.NameTh,
            ["totalAmount"] = e => e.Lines.Where(l => l.DeletedAt == null).Sum(l => l.Amount),
            ["approvalStatus"] = e => e.ApprovalStatus,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfExternalFee> Search(IQueryable<DfExternalFee> q, ListRequest r)
    {
        if (r.Enum<ExternalFeeKind>("kind") is { } kind)
            q = q.Where(e => e.Kind == kind);

        if (r.Filter("year") is { } year && int.TryParse(year, out var y))
            q = q.Where(e => e.PeriodYear == y);

        if (r.Filter("month") is { } month && int.TryParse(month, out var m))
            q = q.Where(e => e.PeriodMonth == m);

        if (r.Filter("arCodeId") is { } aid && Guid.TryParse(aid, out var arCodeId))
            q = q.Where(e => e.ArCodeId == arCodeId);

        if (r.Enum<ApprovalStatus>("approvalStatus") is { } approval)
            q = q.Where(e => e.ApprovalStatus == approval);

        if (r.Filter("cycleClosed") is { } closed && bool.TryParse(closed, out var c))
            q = q.Where(e => e.CycleClosed == c);

        if (r.Filter("refDocDate") is { } date && DateOnly.TryParse(date, out var d))
            q = q.Where(e => e.RefDocDate == d);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.RefDocNo.Contains(text) ||
                (e.ArCode != null && (e.ArCode.Code.Contains(text) || e.ArCode.NameTh.Contains(text))));
        }

        return q;
    }

    public override void Apply(DfExternalFee e, ExternalFeeInput input, bool isCreate)
    {

        if (isCreate) e.Kind = input.Kind;
        else FeeApproval.ResubmitIfReturned(e);

        e.RefDocDate = input.RefDocDate;
        e.RefDocNo = input.RefDocNo?.Trim() ?? string.Empty;
        e.PeriodYear = input.PeriodYear;
        e.PeriodMonth = input.PeriodMonth;
        e.ArCodeId = input.ArCodeId;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfExternalFee e, ExternalFeeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (!isCreate && FeeApproval.WhyLocked(e) is { } locked)
            errors.Add("refDocNo", "locked", locked);

        if (input.RefDocDate == default)
            errors.Required("refDocDate", "โปรดระบุวันที่เอกสารอ้างอิง");
        MasterFieldRules.Required(errors, input.RefDocNo, "refDocNo", "เลขที่เอกสารอ้างอิง");
        MasterFieldRules.RequiredId(errors, input.ArCodeId, "arCodeId", "บริษัท");
        FeeApproval.ValidatePeriod(errors, input.PeriodYear, input.PeriodMonth);

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfExternalFee e, ExternalFeeInput input,
        bool isCreate, ValidationFailure errors, IRepository<DfExternalFee> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var docNo = input.RefDocNo?.Trim() ?? string.Empty;
        if (docNo.Length == 0) return;

        var kind = isCreate ? input.Kind : e.Kind;
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.Kind == kind && o.ArCodeId == input.ArCodeId && o.RefDocNo == docNo);

        if (await exec.AnyAsync(clash, ct))
            errors.Add("refDocNo", "duplicate", "เลขที่เอกสารนี้ของบริษัทนี้ถูกบันทึกไว้แล้ว");
    }

    public override Task<string?> WhyCannotDeleteAsync(DfExternalFee e, CancellationToken ct) =>
        Task.FromResult(FeeApproval.WhyLocked(e));

    public override IReadOnlyList<ExcelColumn<ExternalFeeListItem>> ExportColumns =>
    [
        new("วันที่เอกสาร", r => r.RefDocDate),
        new("เลขที่เอกสาร", r => r.RefDocNo),
        new("รอบปี", r => r.PeriodYear),
        new("รอบเดือน", r => r.PeriodMonth),
        new("รหัสบริษัท", r => r.CompanyCode),
        new("บริษัท", r => r.CompanyName),
        new("จำนวนแพทย์", r => r.LineCount),
        new("รวมยอดจ่าย (บาท)", r => r.TotalAmount),
        new("สถานะอนุมัติ", r => FeeApproval.StatusTh(r.ApprovalStatus)),
        new("สถานะรอบชำระ", r => FeeApproval.CycleTh(r.CycleClosed)),
    ];
}

public record ExternalFeeLineRow(
    Guid Id,
    Guid FeeId,
    DateOnly IssueDate,
    Guid DoctorCodeId,
    string? DoctorCode,
    string? DoctorName,
    string? Description,
    bool CompareGuarantee,
    decimal Amount,
    string? AttachmentUrl);

public record ExternalFeeLineDetail(
    Guid Id,
    Guid FeeId,
    DateOnly IssueDate,
    Guid DoctorCodeId,
    string? DoctorCode,
    string? DoctorName,
    string? Description,
    bool CompareGuarantee,
    decimal Amount,
    string? AttachmentUrl,
    string RowVersion);

public record ExternalFeeLineInput(
    Guid FeeId,
    DateOnly IssueDate,
    Guid DoctorCodeId,
    string? Description,
    bool CompareGuarantee,
    decimal Amount,
    string? AttachmentUrl);

public sealed class ExternalFeeLineSpec
    : CrudSpec<DfExternalFeeLine, ExternalFeeLineRow, ExternalFeeLineDetail, ExternalFeeLineInput>
{
    public override string Resource => "external-fee-lines";
    public override string DisplayNameTh => "รายการค่าแพทย์ภายนอก";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "issueDate";

    public override IReadOnlyList<string> FilterKeys => ["feeId", "doctorCodeId"];

    public override Expression<Func<DfExternalFeeLine, ExternalFeeLineRow>> ListProjection =>
        e => new ExternalFeeLineRow(e.Id, e.FeeId, e.IssueDate, e.DoctorCodeId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Description, e.CompareGuarantee, e.Amount, e.AttachmentUrl);

    public override Expression<Func<DfExternalFeeLine, ExternalFeeLineDetail>> DetailProjection =>
        e => new ExternalFeeLineDetail(e.Id, e.FeeId, e.IssueDate, e.DoctorCodeId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Description, e.CompareGuarantee, e.Amount, e.AttachmentUrl,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DfExternalFeeLine, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfExternalFeeLine, object?>>>
        {
            ["issueDate"] = e => e.IssueDate,
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["amount"] = e => e.Amount,
        };

    public override IQueryable<DfExternalFeeLine> Search(IQueryable<DfExternalFeeLine> q,
        ListRequest r)
    {
        if (r.Filter("feeId") is { } fid && Guid.TryParse(fid, out var feeId))
            q = q.Where(e => e.FeeId == feeId);

        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        return q;
    }

    public override void Apply(DfExternalFeeLine e, ExternalFeeLineInput input, bool isCreate)
    {
        if (isCreate) e.FeeId = input.FeeId;
        e.IssueDate = input.IssueDate;
        e.DoctorCodeId = input.DoctorCodeId;
        e.Description = input.Description?.Trim();
        e.CompareGuarantee = input.CompareGuarantee;
        e.Amount = input.Amount;
        e.AttachmentUrl = input.AttachmentUrl?.Trim();
    }

    public override Task ValidateAsync(DfExternalFeeLine e, ExternalFeeLineInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.FeeId, "feeId", "เอกสาร");
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        if (input.IssueDate == default)
            errors.Required("issueDate", "โปรดระบุวันที่ออกเอกสาร");
        if (input.Amount <= 0)
            errors.Required("amount", "โปรดระบุรายได้ต่อครั้ง (บาท)");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfExternalFeeLine e,
        ExternalFeeLineInput input, bool isCreate, ValidationFailure errors,
        IRepository<DfExternalFeeLine> repo, IQueryExecutor exec, CancellationToken ct)
    {

        var feeId = isCreate ? input.FeeId : e.FeeId;
        var parent = await exec.FirstOrDefaultAsync(
            repo.Query().Where(l => l.FeeId == feeId && l.Fee != null)
                .Select(l => new { l.Fee!.ApprovalStatus, l.Fee.CycleClosed }), ct);

        if (parent is not null &&
            (parent.CycleClosed ||
             parent.ApprovalStatus is ApprovalStatus.Approved or ApprovalStatus.Rejected
                 or ApprovalStatus.Cancelled))
            errors.Add("amount", "locked",
                "เอกสารนี้อนุมัติหรือปิดรอบแล้ว แก้ไขรายการแพทย์ไม่ได้");
    }

    public override async Task<string?> WhyCannotDeleteAgainstDataAsync(DfExternalFeeLine e,
        IRepository<DfExternalFeeLine> repo, IQueryExecutor exec, CancellationToken ct)
    {

        var parent = await exec.FirstOrDefaultAsync(
            repo.Query().Where(l => l.Id == e.Id && l.Fee != null)
                .Select(l => new { l.Fee!.ApprovalStatus, l.Fee.CycleClosed }), ct);

        return parent is not null &&
               (parent.CycleClosed ||
                parent.ApprovalStatus is ApprovalStatus.Approved or ApprovalStatus.Rejected
                    or ApprovalStatus.Cancelled)
            ? "เอกสารนี้อนุมัติหรือปิดรอบแล้ว ลบรายการแพทย์ไม่ได้"
            : null;
    }
}

