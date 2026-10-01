using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.Tax402;

public record PitTaxBracketListItem(
    Guid Id,
    short TaxYear,
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Percent,
    decimal BaseAmount,
    RecordStatus Status);

public record PitTaxBracketDetail(
    Guid Id,
    short TaxYear,
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Percent,
    decimal BaseAmount,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record PitTaxBracketInput(
    short TaxYear,
    decimal IncomeFrom,
    decimal? IncomeTo,
    decimal Percent,
    decimal BaseAmount,
    RecordStatus Status,
    string? Remark);

public sealed class PitTaxBracketSpec
    : CrudSpec<PitTaxBracket, PitTaxBracketListItem, PitTaxBracketDetail, PitTaxBracketInput>
{
    public override string Resource => "pit-tax-brackets";
    public override string DisplayNameTh => "เงื่อนไขภาษีเงินได้บุคคลธรรมดา";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "incomeFrom";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["taxYear"];

    public override Expression<Func<PitTaxBracket, PitTaxBracketListItem>> ListProjection =>
        e => new PitTaxBracketListItem(e.Id, e.TaxYear, e.IncomeFrom, e.IncomeTo, e.Percent,
            e.BaseAmount, e.Status);

    public override Expression<Func<PitTaxBracket, PitTaxBracketDetail>> DetailProjection =>
        e => new PitTaxBracketDetail(e.Id, e.TaxYear, e.IncomeFrom, e.IncomeTo, e.Percent,
            e.BaseAmount, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<PitTaxBracket, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<PitTaxBracket, object?>>>
        {
            ["taxYear"] = e => e.TaxYear,
            ["incomeFrom"] = e => e.IncomeFrom,
            ["incomeTo"] = e => e.IncomeTo,
            ["percent"] = e => e.Percent,
            ["baseAmount"] = e => e.BaseAmount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<PitTaxBracket> Search(IQueryable<PitTaxBracket> query, ListRequest r)
    {
        if (r.Filter("taxYear") is { } year && short.TryParse(year, out var taxYear))
            query = query.Where(e => e.TaxYear == taxYear);

        if (!string.IsNullOrWhiteSpace(r.Q) && short.TryParse(r.Q.Trim(), out var queryYear))
            query = query.Where(e => e.TaxYear == queryYear);

        return query;
    }

    public override void Apply(PitTaxBracket e, PitTaxBracketInput input, bool isCreate)
    {
        e.TaxYear = input.TaxYear;
        e.IncomeFrom = input.IncomeFrom;
        e.IncomeTo = input.IncomeTo;
        e.Percent = input.Percent;
        e.BaseAmount = input.BaseAmount;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(PitTaxBracket e, PitTaxBracketInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {

        if (input.TaxYear is < 2000 or > 2100)
            errors.Add("taxYear", "range", "ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100");

        if (input.IncomeFrom < 0)
            errors.Add("incomeFrom", "range", "เงินได้ตั้งแต่ต้องไม่ติดลบ");

        if (input.IncomeTo is { } to && to <= input.IncomeFrom)
            errors.Add("incomeTo", "range", "เงินได้ถึงต้องมากกว่าเงินได้ตั้งแต่");

        if (input.Percent is < 0 or > 100)
            errors.Add("percent", "range", "เปอร์เซ็นต์ที่ใช้คำนวณต้องอยู่ระหว่าง 0 ถึง 100");

        if (input.BaseAmount < 0)
            errors.Add("baseAmount", "range", "ฐานที่ใช้คำนวณต้องไม่ติดลบ");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(PitTaxBracket e,
        PitTaxBracketInput input, bool isCreate, ValidationFailure errors,
        IRepository<PitTaxBracket> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var from = input.IncomeFrom;
        var to = input.IncomeTo;

        var clash = repo.Query().Where(other =>
            other.Id != e.Id &&
            other.TaxYear == input.TaxYear &&
            (to == null || other.IncomeFrom < to) &&
            (other.IncomeTo == null || other.IncomeTo > from));

        if (await exec.AnyAsync(clash, ct))
            errors.Add("incomeFrom", "overlap",
                $"ช่วงเงินได้นี้ทับกับขั้นภาษีอื่นของปี {input.TaxYear} อยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<PitTaxBracketListItem>> ExportColumns =>
    [
        new("ปีภาษี (ค.ศ.)", r => (int)r.TaxYear),
        new("เงินได้ตั้งแต่", r => r.IncomeFrom, "#,##0.00"),
        new("ถึง", r => r.IncomeTo, "#,##0.00"),
        new("เปอร์เซ็นต์ที่ใช้คำนวณ", r => r.Percent, "#,##0.00"),
        new("ฐานที่ใช้คำนวณ", r => r.BaseAmount, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

