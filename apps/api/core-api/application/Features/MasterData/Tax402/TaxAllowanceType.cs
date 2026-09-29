using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.Tax402;

public record TaxAllowanceTypeListItem(
    Guid Id,
    short TaxYear,
    int ItemCount,
    decimal TotalAmount,
    RecordStatus Status);

public record TaxAllowanceTypeDetail(
    Guid Id,
    short TaxYear,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TaxAllowanceTypeInput(
    short TaxYear,
    RecordStatus Status,
    string? Remark);

public sealed class TaxAllowanceTypeSpec
    : CrudSpec<TaxAllowanceType, TaxAllowanceTypeListItem, TaxAllowanceTypeDetail,
        TaxAllowanceTypeInput>
{
    public override string Resource => "tax-allowance-types";
    public override string DisplayNameTh => "ประเภทลดหย่อน";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "-taxYear";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["taxYear"];

    public override Expression<Func<TaxAllowanceType, TaxAllowanceTypeListItem>> ListProjection =>
        e => new TaxAllowanceTypeListItem(e.Id, e.TaxYear,
            e.Items.Count,
            e.Items.Sum(i => (decimal?)i.Amount) ?? 0m,
            e.Status);

    public override Expression<Func<TaxAllowanceType, TaxAllowanceTypeDetail>> DetailProjection =>
        e => new TaxAllowanceTypeDetail(e.Id, e.TaxYear, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<TaxAllowanceType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<TaxAllowanceType, object?>>>
        {
            ["taxYear"] = e => e.TaxYear,
            ["itemCount"] = e => e.Items.Count,
            ["status"] = e => e.Status,
        };

    public override IQueryable<TaxAllowanceType> Search(IQueryable<TaxAllowanceType> query,
        ListRequest r)
    {
        if (r.Filter("taxYear") is { } year && short.TryParse(year, out var taxYear))
            query = query.Where(e => e.TaxYear == taxYear);

        if (!string.IsNullOrWhiteSpace(r.Q) && short.TryParse(r.Q.Trim(), out var queryYear))
            query = query.Where(e => e.TaxYear == queryYear);

        return query;
    }

    public override void Apply(TaxAllowanceType e, TaxAllowanceTypeInput input, bool isCreate)
    {
        e.TaxYear = input.TaxYear;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(TaxAllowanceType e, TaxAllowanceTypeInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        if (input.TaxYear is < 2000 or > 2100)
            errors.Add("taxYear", "range", "ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(TaxAllowanceType e,
        TaxAllowanceTypeInput input, bool isCreate, ValidationFailure errors,
        IRepository<TaxAllowanceType> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var clash = repo.Query().Where(other =>
            other.Id != e.Id && other.TaxYear == input.TaxYear);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("taxYear", $"ปีภาษี {input.TaxYear} มีชุดลดหย่อนอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<TaxAllowanceTypeListItem>> ExportColumns =>
    [
        new("ปีภาษี (ค.ศ.)", r => (int)r.TaxYear),
        new("จำนวนรายการลดหย่อน", r => r.ItemCount),
        new("รวมจำนวนเงิน (บาท)", r => r.TotalAmount, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

