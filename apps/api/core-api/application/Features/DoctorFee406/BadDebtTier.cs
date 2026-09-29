using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee406;

public record BadDebtTierListItem(
    Guid Id,
    decimal FromPercent,
    decimal ToPercent,
    bool PayActual,
    decimal? PayPercent,
    RecordStatus Status);

public record BadDebtTierDetail(
    Guid Id,
    decimal FromPercent,
    decimal ToPercent,
    bool PayActual,
    decimal? PayPercent,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record BadDebtTierInput(
    decimal? FromPercent,
    decimal? ToPercent,
    bool PayActual,
    decimal? PayPercent,
    RecordStatus Status,
    string? Remark);

public sealed class BadDebtTierSpec
    : CrudSpec<DfBadDebtTier, BadDebtTierListItem, BadDebtTierDetail, BadDebtTierInput>
{
    public override string Resource => "bad-debt-tiers";
    public override string DisplayNameTh => "ขั้นบันไดหนี้สูญ";
    public override string Module => "doctor-fee-406";
    public override string DefaultSort => "fromPercent";

    public override IReadOnlyList<string> FilterKeys => ["payActual"];

    public override Expression<Func<DfBadDebtTier, BadDebtTierListItem>> ListProjection =>
        e => new BadDebtTierListItem(e.Id, e.FromPercent, e.ToPercent, e.PayActual,
            e.PayPercent, e.Status);

    public override Expression<Func<DfBadDebtTier, BadDebtTierDetail>> DetailProjection =>
        e => new BadDebtTierDetail(e.Id, e.FromPercent, e.ToPercent, e.PayActual, e.PayPercent,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DfBadDebtTier, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DfBadDebtTier, object?>>>
        {
            ["fromPercent"] = e => e.FromPercent,
            ["toPercent"] = e => e.ToPercent,
            ["payPercent"] = e => e.PayPercent,
            ["payActual"] = e => e.PayActual,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DfBadDebtTier> Search(IQueryable<DfBadDebtTier> q, ListRequest r)
    {
        if (r.Filter("payActual") is { } pa && bool.TryParse(pa, out var payActual))
            q = q.Where(e => e.PayActual == payActual);

        if (!string.IsNullOrWhiteSpace(r.Q) &&
            decimal.TryParse(r.Q.Trim().TrimEnd('%'), out var percent))
            q = q.Where(e => e.FromPercent <= percent && e.ToPercent >= percent);

        return q;
    }

    public override void Apply(DfBadDebtTier e, BadDebtTierInput input, bool isCreate)
    {
        e.FromPercent = input.FromPercent ?? 0;
        e.ToPercent = input.ToPercent ?? 0;
        e.PayActual = input.PayActual;

        e.PayPercent = input.PayActual ? null : input.PayPercent;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DfBadDebtTier e, BadDebtTierInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        PercentRule(errors, input.FromPercent, "fromPercent", "เปอร์เซ็นต์ชำระเริ่มต้น (%)");
        PercentRule(errors, input.ToPercent, "toPercent", "ถึง (%)");

        if (input.FromPercent is { } from && input.ToPercent is { } to && from > to)
            errors.Add("toPercent", "range",
                "ถึง (%) ต้องมากกว่าหรือเท่ากับเปอร์เซ็นต์ชำระเริ่มต้น (%)");

        if (!input.PayActual)
            PercentRule(errors, input.PayPercent, "payPercent", "เปอร์เซ็นต์จ่ายแพทย์");

        MasterFieldRules.MaxLength(errors, input.Remark, "remark", "หมายเหตุ", 1000);
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DfBadDebtTier e, BadDebtTierInput input,
        bool isCreate, ValidationFailure errors, IRepository<DfBadDebtTier> repo,
        IQueryExecutor exec, CancellationToken ct)
    {

        if (input.Status != RecordStatus.Active ||
            input.FromPercent is not { } from || input.ToPercent is not { } to)
            return;

        var clash = await exec.FirstOrDefaultAsync(repo.Query()
            .Where(o => o.Id != e.Id && o.Status == RecordStatus.Active &&
                        o.FromPercent <= to && o.ToPercent >= from)
            .Select(o => new { o.FromPercent, o.ToPercent }), ct);

        if (clash is not null)
            errors.Add("fromPercent", "overlap",
                $"ช่วงเปอร์เซ็นต์นี้ทับกับขั้น {clash.FromPercent:0.##}–{clash.ToPercent:0.##}% " +
                "ที่ใช้งานอยู่");
    }

    public override IReadOnlyList<ExcelColumn<BadDebtTierListItem>> ExportColumns =>
    [
        new("เปอร์เซ็นต์ชำระเริ่มต้น (%)", r => r.FromPercent),
        new("เปอร์เซ็นต์รับชำระถึง (%)", r => r.ToPercent),
        new("เปอร์เซ็นต์จ่ายแพทย์ (%)", r => r.PayPercent),
        new("จ่ายแพทย์ตาม % ที่รับชำระจริง", r => r.PayActual ? "ใช่" : "ไม่ใช่"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    private static void PercentRule(ValidationFailure errors, decimal? value, string field,
        string labelTh)
    {
        if (value is null)
            errors.Required(field, $"โปรดระบุ{labelTh}");
        else if (value is < 0 or > 100)
            errors.Add(field, "range", $"{labelTh}ต้องอยู่ระหว่าง 0 ถึง 100");
        else if (decimal.Round(value.Value, 2) != value.Value)
            errors.Add(field, "precision", $"{labelTh}มีทศนิยมได้ไม่เกิน 2 ตำแหน่ง");
    }
}

