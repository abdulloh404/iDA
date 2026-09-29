using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record NoWaitPaymentRuleListItem(
    Guid Id,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ActivityCode,
    string? DoctorCode,
    string? TreatmentCode,
    string? TreatmentCategoryCode,
    string? ArCode,
    string? ReceiptTypeCode,
    string? SubInvoice,
    RecordStatus Status);

public record NoWaitPaymentRuleDetail(
    Guid Id,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ActivityCode,
    Guid? DoctorCodeId,
    Guid? TreatmentId,
    Guid? TreatmentCategoryId,
    Guid? ArCodeId,
    Guid? ReceiptTypeId,
    string? SubInvoice,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record NoWaitPaymentRuleInput(
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ActivityCode,
    Guid? DoctorCodeId,
    Guid? TreatmentId,
    Guid? TreatmentCategoryId,
    Guid? ArCodeId,
    Guid? ReceiptTypeId,
    string? SubInvoice,
    RecordStatus Status,
    string? Remark);

public sealed class NoWaitPaymentRuleSpec
    : CrudSpec<NoWaitPaymentRule, NoWaitPaymentRuleListItem, NoWaitPaymentRuleDetail,
        NoWaitPaymentRuleInput>
{
    public override string Resource => "no-wait-payment-rules";
    public override string DisplayNameTh => "รายการไม่รอรับชำระ";
    public override string Module => "master-data-accounting";

    public override string DefaultSort => "-effectiveFrom";

    public override IReadOnlyList<string> FilterKeys =>
        ["activityCode", "doctorCodeId", "treatmentId", "arCodeId", "receiptTypeId"];

    public override Expression<Func<NoWaitPaymentRule, NoWaitPaymentRuleListItem>> ListProjection =>
        e => new NoWaitPaymentRuleListItem(e.Id, e.EffectiveFrom, e.EffectiveTo, e.ActivityCode,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.Treatment == null ? null : e.Treatment.Code,
            e.TreatmentCategory == null ? null : e.TreatmentCategory.Code,
            e.ArCode == null ? null : e.ArCode.Code,
            e.ReceiptType == null ? null : e.ReceiptType.Code,
            e.SubInvoice, e.Status);

    public override Expression<Func<NoWaitPaymentRule, NoWaitPaymentRuleDetail>> DetailProjection =>
        e => new NoWaitPaymentRuleDetail(e.Id, e.EffectiveFrom, e.EffectiveTo, e.ActivityCode,
            e.DoctorCodeId, e.TreatmentId, e.TreatmentCategoryId, e.ArCodeId, e.ReceiptTypeId,
            e.SubInvoice, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<NoWaitPaymentRule, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<NoWaitPaymentRule, object?>>>
        {
            ["effectiveFrom"] = e => e.EffectiveFrom,
            ["effectiveTo"] = e => e.EffectiveTo,
            ["activityCode"] = e => e.ActivityCode,
            ["status"] = e => e.Status,
        };

    public override IQueryable<NoWaitPaymentRule> Search(IQueryable<NoWaitPaymentRule> query,
        ListRequest r)
    {
        if (r.Filter("activityCode") is { } activityCode)
            query = query.Where(e =>
                e.ActivityCode != null && e.ActivityCode.Contains(activityCode));

        if (r.Filter("doctorCodeId") is { } doctorId && Guid.TryParse(doctorId, out var dId))
            query = query.Where(e => e.DoctorCodeId == dId);

        if (r.Filter("treatmentId") is { } treatmentId && Guid.TryParse(treatmentId, out var tId))
            query = query.Where(e => e.TreatmentId == tId);

        if (r.Filter("arCodeId") is { } arCodeId && Guid.TryParse(arCodeId, out var aId))
            query = query.Where(e => e.ArCodeId == aId);

        if (r.Filter("receiptTypeId") is { } receiptTypeId &&
            Guid.TryParse(receiptTypeId, out var rId))
            query = query.Where(e => e.ReceiptTypeId == rId);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                (e.ActivityCode != null && e.ActivityCode.Contains(q)) ||
                (e.SubInvoice != null && e.SubInvoice.Contains(q)) ||
                (e.DoctorCode != null && e.DoctorCode.Code.Contains(q)) ||
                (e.Treatment != null && e.Treatment.Code.Contains(q)) ||
                (e.ArCode != null && e.ArCode.Code.Contains(q)));
        }

        return query;
    }

    public override void Apply(NoWaitPaymentRule e, NoWaitPaymentRuleInput input, bool isCreate)
    {
        e.EffectiveFrom = input.EffectiveFrom;
        e.EffectiveTo = input.EffectiveTo;
        e.ActivityCode = input.ActivityCode?.Trim();
        e.DoctorCodeId = input.DoctorCodeId;
        e.TreatmentId = input.TreatmentId;
        e.TreatmentCategoryId = input.TreatmentCategoryId;
        e.ArCodeId = input.ArCodeId;
        e.ReceiptTypeId = input.ReceiptTypeId;
        e.SubInvoice = input.SubInvoice?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(NoWaitPaymentRule e, NoWaitPaymentRuleInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        if (input.EffectiveFrom == default)
            errors.Required("effectiveFrom", "โปรดระบุวันที่เริ่มใช้");

        if (input.EffectiveTo is { } to && to < input.EffectiveFrom)
            errors.Add("effectiveTo", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(NoWaitPaymentRule e,
        NoWaitPaymentRuleInput input, bool isCreate, ValidationFailure errors,
        IRepository<NoWaitPaymentRule> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (input.EffectiveFrom == default) return;

        var from = input.EffectiveFrom;
        var to = input.EffectiveTo;

        var clash = repo.Query().Where(other =>
            other.Id != e.Id &&
            other.ActivityCode == input.ActivityCode &&
            other.DoctorCodeId == input.DoctorCodeId &&
            other.TreatmentId == input.TreatmentId &&
            other.TreatmentCategoryId == input.TreatmentCategoryId &&
            other.ArCodeId == input.ArCodeId &&
            other.ReceiptTypeId == input.ReceiptTypeId &&
            other.SubInvoice == input.SubInvoice &&
            (to == null || other.EffectiveFrom <= to) &&
            (other.EffectiveTo == null || other.EffectiveTo >= from));

        if (await exec.AnyAsync(clash, ct))
            errors.Add("effectiveFrom", "overlap",
                "มีกฎที่เงื่อนไขเดียวกันและช่วงวันที่ทับกันอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<NoWaitPaymentRuleListItem>> ExportColumns =>
    [
        new("วันที่เริ่มใช้", r => r.EffectiveFrom.ToString("dd/MM/yyyy")),
        new("วันที่สิ้นสุด", r => r.EffectiveTo?.ToString("dd/MM/yyyy")),
        new("Activity Code", r => r.ActivityCode ?? "ทุกรายการ"),
        new("แพทย์", r => r.DoctorCode ?? "ทุกรายการ"),
        new("Treatment", r => r.TreatmentCode ?? "ทุกรายการ"),
        new("Category", r => r.TreatmentCategoryCode ?? "ทุกรายการ"),
        new("AR Code", r => r.ArCode ?? "ทุกรายการ"),
        new("Receipt Type", r => r.ReceiptTypeCode ?? "ทุกรายการ"),
        new("Sub Invoice", r => r.SubInvoice ?? "ทุกรายการ"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

