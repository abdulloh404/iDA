using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.Tax402;

public record TaxAllowanceItemListItem(
    Guid Id,
    Guid TaxAllowanceTypeId,
    string AllowanceName,
    string? Detail,
    decimal Amount,
    int DisplaySeq,
    RecordStatus Status);

public record TaxAllowanceItemDetail(
    Guid Id,
    Guid TaxAllowanceTypeId,
    string AllowanceName,
    string? Detail,
    decimal Amount,
    int DisplaySeq,
    RecordStatus Status,
    string RowVersion);

public record TaxAllowanceItemInput(
    Guid TaxAllowanceTypeId,
    string AllowanceName,
    string? Detail,
    decimal Amount,
    int DisplaySeq,
    RecordStatus Status);

public sealed class TaxAllowanceItemSpec
    : CrudSpec<TaxAllowanceItem, TaxAllowanceItemListItem, TaxAllowanceItemDetail,
        TaxAllowanceItemInput>
{
    public override string Resource => "tax-allowance-items";
    public override string DisplayNameTh => "รายการลดหย่อน";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "displaySeq";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["taxAllowanceTypeId"];

    public override Expression<Func<TaxAllowanceItem, TaxAllowanceItemListItem>> ListProjection =>
        e => new TaxAllowanceItemListItem(e.Id, e.TaxAllowanceTypeId, e.AllowanceName, e.Detail,
            e.Amount, e.DisplaySeq, e.Status);

    public override Expression<Func<TaxAllowanceItem, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(),
            e.AllowanceType == null ? string.Empty : e.AllowanceType.TaxYear.ToString(),
            e.AllowanceName);

    public override Expression<Func<TaxAllowanceItem, TaxAllowanceItemDetail>> DetailProjection =>
        e => new TaxAllowanceItemDetail(e.Id, e.TaxAllowanceTypeId, e.AllowanceName, e.Detail,
            e.Amount, e.DisplaySeq, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<TaxAllowanceItem, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<TaxAllowanceItem, object?>>>
        {
            ["displaySeq"] = e => e.DisplaySeq,
            ["allowanceName"] = e => e.AllowanceName,
            ["amount"] = e => e.Amount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<TaxAllowanceItem> Search(IQueryable<TaxAllowanceItem> query,
        ListRequest r)
    {
        if (r.Filter("taxAllowanceTypeId") is { } typeId && Guid.TryParse(typeId, out var id))
            query = query.Where(e => e.TaxAllowanceTypeId == id);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.AllowanceName.Contains(q) || (e.Detail != null && e.Detail.Contains(q)));
        }

        return query;
    }

    public override void Apply(TaxAllowanceItem e, TaxAllowanceItemInput input, bool isCreate)
    {

        if (isCreate) e.TaxAllowanceTypeId = input.TaxAllowanceTypeId;

        e.AllowanceName = input.AllowanceName.Trim();
        e.Detail = input.Detail?.Trim();
        e.Amount = input.Amount;
        e.DisplaySeq = input.DisplaySeq;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(TaxAllowanceItem e, TaxAllowanceItemInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.TaxAllowanceTypeId, "taxAllowanceTypeId",
            "ปีภาษี");
        MasterFieldRules.Required(errors, input.AllowanceName, "allowanceName", "รายการลดหย่อน");

        if (input.Amount < 0)
            errors.Add("amount", "range", "จำนวนเงินต้องไม่ติดลบ");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<TaxAllowanceItemListItem>> ExportColumns =>
    [
        new("ลำดับ", r => r.DisplaySeq),
        new("รายการลดหย่อน", r => r.AllowanceName),
        new("รายละเอียด", r => r.Detail),
        new("จำนวนเงิน (บาท)", r => r.Amount, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

