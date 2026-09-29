using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

public record HospitalPaidTaxListItem(
    Guid Id,
    string? DoctorCode,
    string? DoctorName,
    string? TaxId,
    RecordStatus Status);

public record HospitalPaidTaxDetail(
    Guid Id,
    Guid DoctorCodeId,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record HospitalPaidTaxInput(Guid DoctorCodeId, RecordStatus Status, string? Remark);

public sealed class HospitalPaidTaxSpec
    : CrudSpec<DfHospitalPaidTax, HospitalPaidTaxListItem, HospitalPaidTaxDetail,
        HospitalPaidTaxInput>
{
    public override string Resource => "hospital-paid-taxes";
    public override string DisplayNameTh => "ภาษีโรงพยาบาลออกให้";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "doctorCode";

    public override IReadOnlyList<string> FilterKeys => ["doctorCodeId"];

    public override Expression<Func<DfHospitalPaidTax, HospitalPaidTaxListItem>> ListProjection =>
        e => new HospitalPaidTaxListItem(e.Id,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.DoctorCode == null || e.DoctorCode.Doctor == null ? null : e.DoctorCode.Doctor.TaxId,
            e.Status);

    public override Expression<Func<DfHospitalPaidTax, HospitalPaidTaxDetail>> DetailProjection =>
        e => new HospitalPaidTaxDetail(e.Id, e.DoctorCodeId, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DfHospitalPaidTax, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfHospitalPaidTax, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfHospitalPaidTax> Search(IQueryable<DfHospitalPaidTax> q,
        ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.DoctorCode != null &&
                (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text)));
        }

        return q;
    }

    public override void Apply(DfHospitalPaidTax e, HospitalPaidTaxInput input, bool isCreate)
    {
        e.DoctorCodeId = input.DoctorCodeId;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfHospitalPaidTax e, HospitalPaidTaxInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfHospitalPaidTax e,
        HospitalPaidTaxInput input, bool isCreate, ValidationFailure errors,
        IRepository<DfHospitalPaidTax> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorCodeId == input.DoctorCodeId), ct))
            errors.Add("doctorCodeId", "duplicate",
                "แพทย์ท่านนี้ถูกตั้งค่าภาษีโรงพยาบาลออกให้ไว้แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<HospitalPaidTaxListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("เลขประจำตัวผู้เสียภาษี", r => r.TaxId),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

public record TaxDeductionListItem(
    Guid Id,
    string? DoctorCode,
    string? DoctorName,
    string? TaxId,
    short TaxYear,
    int ChildCount,
    int ItemCount,
    decimal TotalAmount,
    RecordStatus Status);

public record TaxDeductionDetail(
    Guid Id,
    Guid DoctorCodeId,
    short TaxYear,
    int ChildCount,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TaxDeductionInput(
    Guid DoctorCodeId,
    short TaxYear,
    int ChildCount,
    RecordStatus Status,
    string? Remark);

public sealed class TaxDeductionSpec
    : CrudSpec<DfTaxDeduction, TaxDeductionListItem, TaxDeductionDetail, TaxDeductionInput>
{
    public override string Resource => "tax-deductions";
    public override string DisplayNameTh => "ภาษีลดหย่อน";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "-taxYear";

    public override IReadOnlyList<string> FilterKeys => ["doctorCodeId", "taxYear"];

    public override Expression<Func<DfTaxDeduction, TaxDeductionListItem>> ListProjection =>
        e => new TaxDeductionListItem(e.Id,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.DoctorCode == null || e.DoctorCode.Doctor == null ? null : e.DoctorCode.Doctor.TaxId,
            e.TaxYear, e.ChildCount,
            e.Items.Count(i => i.DeletedAt == null),
            e.Items.Where(i => i.DeletedAt == null).Sum(i => (decimal?)i.Amount) ?? 0m,
            e.Status);

    public override Expression<Func<DfTaxDeduction, TaxDeductionDetail>> DetailProjection =>
        e => new TaxDeductionDetail(e.Id, e.DoctorCodeId, e.TaxYear, e.ChildCount, e.Status,
            e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DfTaxDeduction, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfTaxDeduction, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["taxYear"] = e => e.TaxYear,
            ["childCount"] = e => e.ChildCount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfTaxDeduction> Search(IQueryable<DfTaxDeduction> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        if (r.Filter("taxYear") is { } year && short.TryParse(year, out var y))
            q = q.Where(e => e.TaxYear == y);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {

            var text = r.Q.Trim();
            q = q.Where(e => e.DoctorCode != null &&
                (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text) ||
                 (e.DoctorCode.Doctor != null && e.DoctorCode.Doctor.TaxId != null &&
                  e.DoctorCode.Doctor.TaxId.Contains(text))));
        }

        return q;
    }

    public override void Apply(DfTaxDeduction e, TaxDeductionInput input, bool isCreate)
    {
        e.DoctorCodeId = input.DoctorCodeId;
        e.TaxYear = input.TaxYear;
        e.ChildCount = input.ChildCount;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfTaxDeduction e, TaxDeductionInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        TaxYearRule(errors, input.TaxYear);
        if (input.ChildCount < 0)
            errors.Add("childCount", "min", "จำนวนบุตรต้องไม่ติดลบ");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfTaxDeduction e, TaxDeductionInput input,
        bool isCreate, ValidationFailure errors, IRepository<DfTaxDeduction> repo,
        IQueryExecutor exec, CancellationToken ct)
    {

        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorCodeId == input.DoctorCodeId && o.TaxYear == input.TaxYear), ct))
            errors.Add("taxYear", "duplicate", "แพทย์ท่านนี้มีข้อมูลลดหย่อนของปีภาษีนี้อยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<TaxDeductionListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("เลขประจำตัวผู้เสียภาษี", r => r.TaxId),
        new("ปีภาษี", r => r.TaxYear),
        new("จำนวนบุตร", r => r.ChildCount),
        new("ยอดลดหย่อนรวม (บาท)", r => r.TotalAmount),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    internal static void TaxYearRule(ValidationFailure errors, short taxYear)
    {
        if (taxYear == 0)
            errors.Required("taxYear", "โปรดระบุปีภาษี");
        else if (taxYear is < 2000 or > 2100)
            errors.Add("taxYear", "invalid", "โปรดระบุปีภาษีเป็น ค.ศ.");
    }
}

public record TaxDeductionItemRow(
    Guid Id,
    Guid DeductionId,
    Guid TaxAllowanceItemId,
    string? AllowanceName,

    decimal? AllowanceLimit,
    decimal Amount);

public record TaxDeductionItemDetail(
    Guid Id,
    Guid DeductionId,
    Guid TaxAllowanceItemId,
    string? AllowanceName,
    decimal? AllowanceLimit,
    decimal Amount,
    string RowVersion);

public record TaxDeductionItemInput(Guid DeductionId, Guid TaxAllowanceItemId, decimal Amount);

public sealed class TaxDeductionItemSpec
    : CrudSpec<DfTaxDeductionItem, TaxDeductionItemRow, TaxDeductionItemDetail,
        TaxDeductionItemInput>
{
    public override string Resource => "tax-deduction-items";
    public override string DisplayNameTh => "รายการลดหย่อน";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "allowanceName";

    public override IReadOnlyList<string> FilterKeys => ["deductionId"];

    public override Expression<Func<DfTaxDeductionItem, TaxDeductionItemRow>> ListProjection =>
        e => new TaxDeductionItemRow(e.Id, e.DeductionId, e.TaxAllowanceItemId,
            e.TaxAllowanceItem == null ? null : e.TaxAllowanceItem.AllowanceName,
            e.TaxAllowanceItem == null ? null : e.TaxAllowanceItem.Amount,
            e.Amount);

    public override Expression<Func<DfTaxDeductionItem, TaxDeductionItemDetail>> DetailProjection =>
        e => new TaxDeductionItemDetail(e.Id, e.DeductionId, e.TaxAllowanceItemId,
            e.TaxAllowanceItem == null ? null : e.TaxAllowanceItem.AllowanceName,
            e.TaxAllowanceItem == null ? null : e.TaxAllowanceItem.Amount,
            e.Amount, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DfTaxDeductionItem, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfTaxDeductionItem, object?>>>
        {
            ["allowanceName"] = e => e.TaxAllowanceItem == null ? null : e.TaxAllowanceItem.AllowanceName,
            ["amount"] = e => e.Amount,
        };

    public override IQueryable<DfTaxDeductionItem> Search(IQueryable<DfTaxDeductionItem> q,
        ListRequest r)
    {
        if (r.Filter("deductionId") is { } did && Guid.TryParse(did, out var deductionId))
            q = q.Where(e => e.DeductionId == deductionId);
        return q;
    }

    public override void Apply(DfTaxDeductionItem e, TaxDeductionItemInput input, bool isCreate)
    {
        if (isCreate) e.DeductionId = input.DeductionId;
        e.TaxAllowanceItemId = input.TaxAllowanceItemId;
        e.Amount = input.Amount;
    }

    public override Task ValidateAsync(DfTaxDeductionItem e, TaxDeductionItemInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DeductionId, "deductionId", "ข้อมูลลดหย่อน");
        MasterFieldRules.RequiredId(errors, input.TaxAllowanceItemId, "taxAllowanceItemId",
            "รายการลดหย่อน");
        if (input.Amount < 0)
            errors.Add("amount", "min", "ยอดลดหย่อนต้องไม่ติดลบ");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfTaxDeductionItem e,
        TaxDeductionItemInput input, bool isCreate, ValidationFailure errors,
        IRepository<DfTaxDeductionItem> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var deductionId = isCreate ? input.DeductionId : e.DeductionId;
        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DeductionId == deductionId &&
                o.TaxAllowanceItemId == input.TaxAllowanceItemId), ct))
            errors.Add("taxAllowanceItemId", "duplicate",
                "รายการลดหย่อนนี้ถูกกรอกไว้แล้วในปีภาษีนี้");
    }
}

public record TaxExemptionListItem(
    Guid Id,
    string? DoctorCode,
    string? DoctorName,
    string? TaxId,
    short TaxYear,
    bool IsExempt,
    RecordStatus Status);

public record TaxExemptionDetail(
    Guid Id,
    Guid DoctorCodeId,
    short TaxYear,
    bool IsExempt,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TaxExemptionInput(
    Guid DoctorCodeId,
    short TaxYear,
    bool IsExempt,
    RecordStatus Status,
    string? Remark);

public sealed class TaxExemptionSpec
    : CrudSpec<DfTaxExemption, TaxExemptionListItem, TaxExemptionDetail, TaxExemptionInput>
{
    public override string Resource => "tax-exemptions";
    public override string DisplayNameTh => "ข้อมูลยกเว้นภาษี";
    public override string Module => "doctor-fee-402";
    public override string DefaultSort => "-taxYear";

    public override IReadOnlyList<string> FilterKeys => ["doctorCodeId", "taxYear", "isExempt"];

    public override Expression<Func<DfTaxExemption, TaxExemptionListItem>> ListProjection =>
        e => new TaxExemptionListItem(e.Id,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.DoctorCode == null || e.DoctorCode.Doctor == null ? null : e.DoctorCode.Doctor.TaxId,
            e.TaxYear, e.IsExempt, e.Status);

    public override Expression<Func<DfTaxExemption, TaxExemptionDetail>> DetailProjection =>
        e => new TaxExemptionDetail(e.Id, e.DoctorCodeId, e.TaxYear, e.IsExempt, e.Status,
            e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DfTaxExemption, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfTaxExemption, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["taxYear"] = e => e.TaxYear,
            ["isExempt"] = e => e.IsExempt,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfTaxExemption> Search(IQueryable<DfTaxExemption> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        if (r.Filter("taxYear") is { } year && short.TryParse(year, out var y))
            q = q.Where(e => e.TaxYear == y);

        if (r.Filter("isExempt") is { } exempt && bool.TryParse(exempt, out var x))
            q = q.Where(e => e.IsExempt == x);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.DoctorCode != null &&
                (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text) ||
                 (e.DoctorCode.Doctor != null && e.DoctorCode.Doctor.TaxId != null &&
                  e.DoctorCode.Doctor.TaxId.Contains(text))));
        }

        return q;
    }

    public override void Apply(DfTaxExemption e, TaxExemptionInput input, bool isCreate)
    {
        e.DoctorCodeId = input.DoctorCodeId;
        e.TaxYear = input.TaxYear;
        e.IsExempt = input.IsExempt;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfTaxExemption e, TaxExemptionInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        TaxDeductionSpec.TaxYearRule(errors, input.TaxYear);
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfTaxExemption e, TaxExemptionInput input,
        bool isCreate, ValidationFailure errors, IRepository<DfTaxExemption> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorCodeId == input.DoctorCodeId && o.TaxYear == input.TaxYear), ct))
            errors.Add("taxYear", "duplicate", "แพทย์ท่านนี้มีข้อมูลยกเว้นภาษีของปีภาษีนี้อยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<TaxExemptionListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("เลขประจำตัวผู้เสียภาษี", r => r.TaxId),
        new("ปีภาษี", r => r.TaxYear),
        new("ยกเว้นภาษี", r => r.IsExempt ? "ยกเว้น" : "ไม่ยกเว้น"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

