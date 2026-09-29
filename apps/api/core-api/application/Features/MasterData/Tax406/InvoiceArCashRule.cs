using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Tax406;

public record InvoiceArCashRuleListItem(
    Guid Id,
    string InvoicePrefix,
    bool IsAr,
    RecordStatus Status);

public record InvoiceArCashRuleDetail(
    Guid Id,
    string InvoicePrefix,
    bool IsAr,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record InvoiceArCashRuleInput(
    string InvoicePrefix,
    bool IsAr,
    RecordStatus Status,
    string? Remark);

public sealed class InvoiceArCashRuleSpec
    : CrudSpec<InvoiceArCashRule, InvoiceArCashRuleListItem, InvoiceArCashRuleDetail,
        InvoiceArCashRuleInput>
{
    public override string Resource => "invoice-ar-cash-rules";
    public override string DisplayNameTh => "Invoice AR/Cash";
    public override string Module => "master-data-tax-406";
    public override string DefaultSort => "invoicePrefix";

    public override IReadOnlyList<string> FilterKeys => ["invoicePrefix", "isAr"];

    public override Expression<Func<InvoiceArCashRule, InvoiceArCashRuleListItem>> ListProjection =>
        e => new InvoiceArCashRuleListItem(e.Id, e.InvoicePrefix, e.IsAr, e.Status);

    public override Expression<Func<InvoiceArCashRule, InvoiceArCashRuleDetail>> DetailProjection =>
        e => new InvoiceArCashRuleDetail(e.Id, e.InvoicePrefix, e.IsAr, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<InvoiceArCashRule, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<InvoiceArCashRule, object?>>>
        {
            ["invoicePrefix"] = e => e.InvoicePrefix,
            ["isAr"] = e => e.IsAr,
            ["status"] = e => e.Status,
        };

    public override IQueryable<InvoiceArCashRule> Search(IQueryable<InvoiceArCashRule> query,
        ListRequest r)
    {
        if (r.Filter("isAr") is { } isAr)
            query = query.Where(e => e.IsAr == (isAr == "true"));

        if (r.Filter("invoicePrefix") is { } prefix)
            query = query.Where(e => e.InvoicePrefix.Contains(prefix));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e => e.InvoicePrefix.Contains(q));
        }

        return query;
    }

    public override void Apply(InvoiceArCashRule e, InvoiceArCashRuleInput input, bool isCreate)
    {
        e.InvoicePrefix = input.InvoicePrefix.Trim().ToUpperInvariant();
        e.IsAr = input.IsAr;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(InvoiceArCashRule e, InvoiceArCashRuleInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Required(errors, input.InvoicePrefix, "invoicePrefix",
            "รหัสขึ้นต้นของ Invoice");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(InvoiceArCashRule e,
        InvoiceArCashRuleInput input, bool isCreate, ValidationFailure errors,
        IRepository<InvoiceArCashRule> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var prefix = input.InvoicePrefix.Trim().ToUpperInvariant();
        if (prefix.Length == 0) return;

        var clash = repo.Query().Where(other =>
            other.Id != e.Id && other.InvoicePrefix == prefix);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("invoicePrefix", "รหัสขึ้นต้นนี้ตั้งค่าไว้แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<InvoiceArCashRuleListItem>> ExportColumns =>
    [
        new("Invoice ขึ้นต้นด้วย", r => r.InvoicePrefix),
        new("AR/Cash", r => r.IsAr ? "AR (ตั้งหนี้)" : "Cash (เงินสด)"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

