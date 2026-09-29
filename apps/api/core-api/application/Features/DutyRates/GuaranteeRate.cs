using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutyRates;

public record GuaranteeRateListItem(
    Guid Id,
    GuaranteeKind Kind,
    string? DoctorCode,
    string? DoctorName,
    string? DepartmentName,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? IncomeAmount,
    decimal? SurplusStartRate,
    GuaranteeCalcMode? CalcMode,
    bool CompareWholeMonth406,
    AdmissionType AdmissionType,
    GuaranteeBasis Basis,
    RecordStatus Status,
    ApprovalStatus ApprovalStatus);

public record GuaranteeRateDetail(
    Guid Id,
    GuaranteeKind Kind,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? IncomeAmount,
    decimal? SurplusStartRate,
    GuaranteeCalcMode? CalcMode,
    bool CompareWholeMonth406,
    DateOnly? CompareInvoiceFrom,
    WorkTimeRule WorkTimeRule,
    int? CheckinToleranceMinutes,
    AdmissionType AdmissionType,
    IncomeBase IncomeBase,
    GuaranteeBasis Basis,
    CreditCardFeeBase CreditCardFeeBase,
    Guid? CompareDoctorCodeId,
    RecordStatus Status,
    ApprovalStatus ApprovalStatus,
    string? Remark,
    string RowVersion);

public record GuaranteeRateInput(
    GuaranteeKind Kind,
    Guid DoctorCodeId,
    Guid? DepartmentId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? IncomeAmount,
    decimal? SurplusStartRate,
    GuaranteeCalcMode? CalcMode,
    bool CompareWholeMonth406,
    DateOnly? CompareInvoiceFrom,
    WorkTimeRule WorkTimeRule,
    int? CheckinToleranceMinutes,
    AdmissionType AdmissionType,
    IncomeBase IncomeBase,
    GuaranteeBasis Basis,
    CreditCardFeeBase CreditCardFeeBase,
    Guid? CompareDoctorCodeId,
    RecordStatus Status,
    ApprovalStatus ApprovalStatus,
    string? Remark);

public sealed class GuaranteeRateSpec
    : CrudSpec<GuaranteeRate, GuaranteeRateListItem, GuaranteeRateDetail, GuaranteeRateInput>
{
    public override string Resource => "guarantee-rates";
    public override string DisplayNameTh => "ประกันรายได้";
    public override string Module => "duty-rates";
    public override string DefaultSort => "-startDate";

    public override IReadOnlyList<string> FilterKeys =>
        ["kind", "doctorCodeId", "departmentId", "calcMode", "approvalStatus", "from", "to"];

    public override Expression<Func<GuaranteeRate, GuaranteeRateListItem>> ListProjection =>
        e => new GuaranteeRateListItem(e.Id, e.Kind,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Department == null ? null : e.Department.NameTh,
            e.StartDate, e.EndDate, e.IncomeAmount, e.SurplusStartRate, e.CalcMode,
            e.CompareWholeMonth406, e.AdmissionType, e.Basis, e.Status, e.ApprovalStatus);

    public override Expression<Func<GuaranteeRate, GuaranteeRateDetail>> DetailProjection =>
        e => new GuaranteeRateDetail(e.Id, e.Kind, e.DoctorCodeId, e.DepartmentId, e.StartDate,
            e.EndDate, e.IncomeAmount, e.SurplusStartRate, e.CalcMode, e.CompareWholeMonth406,
            e.CompareInvoiceFrom, e.WorkTimeRule, e.CheckinToleranceMinutes, e.AdmissionType,
            e.IncomeBase, e.Basis, e.CreditCardFeeBase, e.CompareDoctorCodeId, e.Status,
            e.ApprovalStatus, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<GuaranteeRate, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<GuaranteeRate, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["departmentName"] = e => e.Department == null ? null : e.Department.NameTh,
            ["startDate"] = e => e.StartDate,
            ["endDate"] = e => e.EndDate,
            ["incomeAmount"] = e => e.IncomeAmount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<GuaranteeRate> Search(IQueryable<GuaranteeRate> q, ListRequest r)
    {

        if (r.Enum<GuaranteeKind>("kind") is { } kind)
            q = q.Where(e => e.Kind == kind);

        if (r.Filter("doctorCodeId") is { } docId && Guid.TryParse(docId, out var dId))
            q = q.Where(e => e.DoctorCodeId == dId);

        if (r.Filter("departmentId") is { } depId && Guid.TryParse(depId, out var pId))
            q = q.Where(e => e.DepartmentId == pId);

        if (r.Enum<GuaranteeCalcMode>("calcMode") is { } calcMode)
            q = q.Where(e => e.CalcMode == calcMode);

        if (r.Enum<ApprovalStatus>("approvalStatus") is { } approval)
            q = q.Where(e => e.ApprovalStatus == approval);

        if (r.Filter("from") is { } from && DateOnly.TryParse(from, out var fromDate))
            q = q.Where(e => e.EndDate == null || e.EndDate >= fromDate);

        if (r.Filter("to") is { } to && DateOnly.TryParse(to, out var toDate))
            q = q.Where(e => e.StartDate <= toDate);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.DoctorCode != null &&
                (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text)));
        }

        return q;
    }

    public override void Apply(GuaranteeRate e, GuaranteeRateInput input, bool isCreate)
    {

        if (isCreate) e.Kind = input.Kind;

        e.DoctorCodeId = input.DoctorCodeId;
        e.DepartmentId = input.DepartmentId;
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;

        e.IncomeAmount = input.Kind == GuaranteeKind.LumpSum ? null : input.IncomeAmount;
        e.SurplusStartRate = input.Kind == GuaranteeKind.Surplus ? input.SurplusStartRate : null;
        e.CalcMode = input.Kind == GuaranteeKind.Monthly
            ? input.CalcMode ?? GuaranteeCalcMode.Normal
            : null;
        e.CompareWholeMonth406 =
            input.Kind == GuaranteeKind.Hourly && input.CompareWholeMonth406;

        e.CompareInvoiceFrom = input.CompareInvoiceFrom;
        e.WorkTimeRule = input.WorkTimeRule;
        e.CheckinToleranceMinutes = input.CheckinToleranceMinutes;
        e.AdmissionType = input.AdmissionType;
        e.IncomeBase = input.IncomeBase;
        e.Basis = input.Basis;
        e.CreditCardFeeBase = input.CreditCardFeeBase;
        e.CompareDoctorCodeId = input.CompareDoctorCodeId;

        e.Status = input.Status;
        e.ApprovalStatus = input.ApprovalStatus;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(GuaranteeRate e, GuaranteeRateInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");

        if (input.StartDate == default)
            errors.Required("startDate", "โปรดระบุวันที่เริ่มต้น");

        if (input.EndDate is { } end && input.StartDate != default && end < input.StartDate)
            errors.Add("endDate", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มต้น");

        if (input.Kind != GuaranteeKind.LumpSum)
        {
            if (input.IncomeAmount is null)
                errors.Required("incomeAmount", "โปรดระบุรายได้");
            else if (input.IncomeAmount < 0)
                errors.Add("incomeAmount", "range", "รายได้ต้องไม่ติดลบ");
        }

        if (input.Kind == GuaranteeKind.Surplus && input.SurplusStartRate is < 0)
            errors.Add("surplusStartRate", "range", "เรทเริ่มต้นต้องไม่ติดลบ");

        if (input.CheckinToleranceMinutes is { } minutes && Math.Abs(minutes) > 1440)
            errors.Add("checkinToleranceMinutes", "range",
                "ค่าคลาดเคลื่อนต้องไม่เกิน 1440 นาที");

        if (input.CompareDoctorCodeId == input.DoctorCodeId && input.CompareDoctorCodeId != null)
            errors.Add("compareDoctorCodeId", "conflict",
                "รหัสแพทย์เทียบต้องไม่ใช่แพทย์เจ้าของอัตรา");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(GuaranteeRate e,
        GuaranteeRateInput input, bool isCreate, ValidationFailure errors,
        IRepository<GuaranteeRate> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (input.StartDate == default) return;

        var kind = input.Kind;
        var from = input.StartDate;
        var to = input.EndDate;

        var clash = repo.Query().Where(o =>
            o.Id != e.Id &&
            o.Kind == kind &&
            o.DoctorCodeId == input.DoctorCodeId &&
            (to == null || o.StartDate <= to) &&
            (o.EndDate == null || o.EndDate >= from));

        if (await exec.AnyAsync(clash, ct))
            errors.Add("startDate", "overlap",
                "แพทย์ท่านนี้มีอัตราชนิดเดียวกันที่ช่วงวันที่ทับกันอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<GuaranteeRateListItem>> ExportColumns =>
    [
        new("ชนิด", r => KindTh(r.Kind)),
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("แผนก", r => r.DepartmentName),
        new("วันที่เริ่มต้น", r => r.StartDate.ToString("dd/MM/yyyy")),
        new("วันที่สิ้นสุด", r => r.EndDate?.ToString("dd/MM/yyyy") ?? "ไม่มีกำหนด"),
        new("รายได้", r => r.IncomeAmount, "#,##0.00"),
        new("เรทเริ่มต้น Surplus", r => r.SurplusStartRate, "#,##0.00"),
        new("การคำนวณ", r => r.CalcMode is { } m ? CalcModeTh(m) : null),
        new("เทียบฐาน 40(6) ทั้งเดือน", r => r.CompareWholeMonth406 ? "เทียบ" : "ไม่เทียบ"),
        new("Admission Type", r => AdmissionTh(r.AdmissionType)),
        new("รูปแบบการคำนวณ", r => BasisTh(r.Basis)),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string KindTh(GuaranteeKind kind) => kind switch
    {
        GuaranteeKind.LumpSum => "ค่าแพทย์เหมาจ่าย",
        GuaranteeKind.Surplus => "ค่าแพทย์ Surplus",
        GuaranteeKind.Hourly => "ประกันรายได้รายชั่วโมง",
        GuaranteeKind.PerSession => "ประกันรายได้รายคาบ",
        _ => "ประกันรายได้รายเดือน",
    };

    public static string CalcModeTh(GuaranteeCalcMode mode) =>
        mode == GuaranteeCalcMode.Normal ? "แบบปกติ" : "แบบสะสม";

    public static string BasisTh(GuaranteeBasis basis) => basis switch
    {
        GuaranteeBasis.AccrualBasic => "Accrual Basic",
        GuaranteeBasis.AccrualNoWaitPayment =>
            "Accrual Basic ลูกหนี้ไม่รอรับชำระ จ่ายแพทย์ได้เลย",
        GuaranteeBasis.CashBasic => "Cash Basic",
        _ => "Cash Basic ลูกหนี้รับชำระในเดือนทั้งหมดเทียบ",
    };

    public static string AdmissionTh(AdmissionType type) => type switch
    {
        AdmissionType.Ipd => "IPD",
        AdmissionType.Opd => "OPD",
        _ => "ทั้งหมด",
    };
}

