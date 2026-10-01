using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Tax406;

public record InvoicePrefixRuleListItem(
    Guid Id,
    string InvoicePrefix,
    string? PaymentLocation,
    InvoiceCalcMode CalcMode,
    RecordStatus Status);

public record InvoicePrefixRuleDetail(
    Guid Id,
    string InvoicePrefix,
    string? PaymentLocation,
    InvoiceCalcMode CalcMode,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record InvoicePrefixRuleInput(
    string InvoicePrefix,
    string? PaymentLocation,
    InvoiceCalcMode CalcMode,
    RecordStatus Status,
    string? Remark);

public sealed class InvoicePrefixRuleSpec
    : CrudSpec<InvoicePrefixRule, InvoicePrefixRuleListItem, InvoicePrefixRuleDetail,
        InvoicePrefixRuleInput>
{
    public override string Resource => "invoice-prefix-rules";
    public override string DisplayNameTh => "Import Invoice";
    public override string Module => "master-data-tax-406";
    public override string DefaultSort => "invoicePrefix";

    public override IReadOnlyList<string> FilterKeys => ["invoicePrefix", "calcMode"];

    public override Expression<Func<InvoicePrefixRule, InvoicePrefixRuleListItem>> ListProjection =>
        e => new InvoicePrefixRuleListItem(e.Id, e.InvoicePrefix, e.PaymentLocation, e.CalcMode,
            e.Status);

    public override Expression<Func<InvoicePrefixRule, InvoicePrefixRuleDetail>> DetailProjection =>
        e => new InvoicePrefixRuleDetail(e.Id, e.InvoicePrefix, e.PaymentLocation, e.CalcMode,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<InvoicePrefixRule, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<InvoicePrefixRule, object?>>>
        {
            ["invoicePrefix"] = e => e.InvoicePrefix,
            ["paymentLocation"] = e => e.PaymentLocation,
            ["calcMode"] = e => e.CalcMode,
            ["status"] = e => e.Status,
        };

    public override IQueryable<InvoicePrefixRule> Search(IQueryable<InvoicePrefixRule> query,
        ListRequest r)
    {
        if (r.Enum<InvoiceCalcMode>("calcMode") is { } parsed)
            query = query.Where(e => e.CalcMode == parsed);

        if (r.Filter("invoicePrefix") is { } prefix)
            query = query.Where(e => e.InvoicePrefix.Contains(prefix));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.InvoicePrefix.Contains(q) ||
                (e.PaymentLocation != null && e.PaymentLocation.Contains(q)));
        }

        return query;
    }

    public override void Apply(InvoicePrefixRule e, InvoicePrefixRuleInput input, bool isCreate)
    {

        e.InvoicePrefix = input.InvoicePrefix.Trim().ToUpperInvariant();
        e.PaymentLocation = input.PaymentLocation?.Trim();
        e.CalcMode = input.CalcMode;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(InvoicePrefixRule e, InvoicePrefixRuleInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Required(errors, input.InvoicePrefix, "invoicePrefix",
            "รหัสขึ้นต้นของ Invoice");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(InvoicePrefixRule e,
        InvoicePrefixRuleInput input, bool isCreate, ValidationFailure errors,
        IRepository<InvoicePrefixRule> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var prefix = input.InvoicePrefix.Trim().ToUpperInvariant();
        if (prefix.Length == 0) return;

        var location = input.PaymentLocation?.Trim();
        var clash = repo.Query().Where(other =>
            other.Id != e.Id &&
            other.InvoicePrefix == prefix &&
            other.PaymentLocation == location);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("invoicePrefix",
                location is null
                    ? "รหัสขึ้นต้นนี้ตั้งค่าไว้สำหรับทุก Location แล้ว"
                    : $"รหัสขึ้นต้นนี้ตั้งค่าไว้สำหรับ Location {location} แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<InvoicePrefixRuleListItem>> ExportColumns =>
    [
        new("Invoice ขึ้นต้นด้วย", r => r.InvoicePrefix),
        new("Location จ่ายเงิน", r => r.PaymentLocation ?? "ทุก Location"),
        new("รูปแบบการคำนวณ", r => CalcModeTh(r.CalcMode)),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string CalcModeTh(InvoiceCalcMode mode) => mode switch
    {
        InvoiceCalcMode.NormalShare => "คำนวณส่งแบ่งปกติ",
        InvoiceCalcMode.ToHospital => "คำนวณเข้าโรงพยาบาล",
        _ => mode.ToString(),
    };
}

