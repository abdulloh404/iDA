using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

public record FeeItemListItem(
    Guid Id,
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    FeeItemType ItemType,
    string? DoctorCode,
    string? DoctorName,
    string? DepartmentName,
    decimal Amount,
    ApprovalStatus ApprovalStatus,
    bool CycleClosed,
    RecordStatus Status);

public record FeeItemDetail(
    Guid Id,
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    FeeItemType ItemType,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    Guid? ArCodeId,
    decimal Amount,
    ApprovalStatus ApprovalStatus,
    bool CycleClosed,
    string? DecisionComment,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record FeeItemInput(
    DateOnly RefDocDate,
    string RefDocNo,
    int PeriodYear,
    int PeriodMonth,
    FeeItemType ItemType,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    Guid? ArCodeId,
    decimal Amount,
    RecordStatus Status,
    string? Remark);

public sealed class FeeItemSpec
    : CrudSpec<DfFeeItem, FeeItemListItem, FeeItemDetail, FeeItemInput>
{
    public override string Resource => "fee-items";
    public override string DisplayNameTh => "รายการค่าแพทย์";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "-refDocDate";

    public override IReadOnlyList<string> FilterKeys =>
        ["year", "month", "doctorCodeId", "departmentId", "itemType", "approvalStatus",
         "cycleClosed", "refDocDate"];

    public override Expression<Func<DfFeeItem, FeeItemListItem>> ListProjection =>
        e => new FeeItemListItem(e.Id, e.RefDocDate, e.RefDocNo, e.PeriodYear, e.PeriodMonth,
            e.ItemType,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Department == null ? null : e.Department.NameTh,
            e.Amount, e.ApprovalStatus, e.CycleClosed, e.Status);

    public override Expression<Func<DfFeeItem, FeeItemDetail>> DetailProjection =>
        e => new FeeItemDetail(e.Id, e.RefDocDate, e.RefDocNo, e.PeriodYear, e.PeriodMonth,
            e.ItemType, e.DoctorCodeId, e.DepartmentId, e.ArCodeId, e.Amount,
            e.ApprovalStatus, e.CycleClosed, e.DecisionComment, e.DecidedBy, e.DecidedAt,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DfFeeItem, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfFeeItem, object?>>>
        {
            ["refDocDate"] = e => e.RefDocDate,
            ["refDocNo"] = e => e.RefDocNo,
            ["period"] = e => e.PeriodYear * 100 + e.PeriodMonth,
            ["itemType"] = e => e.ItemType,
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["amount"] = e => e.Amount,
            ["approvalStatus"] = e => e.ApprovalStatus,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfFeeItem> Search(IQueryable<DfFeeItem> q, ListRequest r)
    {
        if (r.Filter("year") is { } year && int.TryParse(year, out var y))
            q = q.Where(e => e.PeriodYear == y);

        if (r.Filter("month") is { } month && int.TryParse(month, out var m))
            q = q.Where(e => e.PeriodMonth == m);

        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        if (r.Filter("departmentId") is { } dep && Guid.TryParse(dep, out var departmentId))
            q = q.Where(e => e.DepartmentId == departmentId);

        if (r.Enum<FeeItemType>("itemType") is { } type)
            q = q.Where(e => e.ItemType == type);

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
                (e.DoctorCode != null && (e.DoctorCode.Code.Contains(text) ||
                    e.DoctorCode.DisplayNameTh.Contains(text))));
        }

        return q;
    }

    public override void Apply(DfFeeItem e, FeeItemInput input, bool isCreate)
    {
        if (!isCreate) FeeApproval.ResubmitIfReturned(e);

        e.RefDocDate = input.RefDocDate;
        e.RefDocNo = input.RefDocNo?.Trim() ?? string.Empty;
        e.PeriodYear = input.PeriodYear;
        e.PeriodMonth = input.PeriodMonth;
        e.ItemType = input.ItemType;
        e.DoctorCodeId = input.DoctorCodeId;
        e.DepartmentId = input.DepartmentId;
        e.ArCodeId = input.ArCodeId;
        e.Amount = input.Amount;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfFeeItem e, FeeItemInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (!isCreate && FeeApproval.WhyLocked(e) is { } locked)
            errors.Add("amount", "locked", locked);

        if (input.RefDocDate == default)
            errors.Required("refDocDate", "โปรดระบุวันที่เอกสารอ้างอิง");
        MasterFieldRules.Required(errors, input.RefDocNo, "refDocNo", "เลขที่เอกสารอ้างอิง");
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        FeeApproval.ValidatePeriod(errors, input.PeriodYear, input.PeriodMonth);

        if (input.Amount <= 0)
            errors.Required("amount", "โปรดระบุรายได้ (บาท)");

        return Task.CompletedTask;
    }

    public override Task<string?> WhyCannotDeleteAsync(DfFeeItem e, CancellationToken ct) =>
        Task.FromResult(FeeApproval.WhyLocked(e));

    public override IReadOnlyList<ExcelColumn<FeeItemListItem>> ExportColumns =>
    [
        new("วันที่เอกสาร", r => r.RefDocDate),
        new("เลขที่เอกสาร", r => r.RefDocNo),
        new("รอบปี", r => r.PeriodYear),
        new("รอบเดือน", r => r.PeriodMonth),
        new("ประเภทรายการ", r => ItemTypeTh(r.ItemType)),
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("แผนก", r => r.DepartmentName),
        new("จำนวนเงิน (บาท)", r => r.Amount),
        new("สถานะอนุมัติ", r => FeeApproval.StatusTh(r.ApprovalStatus)),
        new("สถานะรอบชำระ", r => FeeApproval.CycleTh(r.CycleClosed)),
    ];

    public static string ItemTypeTh(FeeItemType type) => type switch
    {
        FeeItemType.MeetingAllowance => "ค่าเบี้ยประชุม",
        FeeItemType.SpeakerPromotion => "ค่าแพทย์วิทยากร-Promotion",
        FeeItemType.SpeakerPromotionEvent => "ค่าแพทย์วิทยากร-Promotion Event",
        FeeItemType.SpeakerTraining => "ค่าแพทย์วิทยากร-Training",
        FeeItemType.AnnualCompensation => "ค่าตอบแทนรายปี",
        FeeItemType.PerCaseCompensation => "ค่าตอบแทนพิเศษตามจำนวนราย",
        FeeItemType.HealthCheckConsultant => "ค่าแพทย์ที่ปรึกษาผลการตรวจสุขภาพ",
        FeeItemType.TravelCompensation => "ค่าตอบแทนค่าเดินแพทย์",
        _ => type.ToString(),
    };
}

