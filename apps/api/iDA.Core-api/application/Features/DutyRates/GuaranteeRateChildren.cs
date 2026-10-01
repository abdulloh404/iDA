using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutyRates;

public record GuaranteeTreatmentRow(
    Guid Id, Guid GuaranteeRateId, Guid TreatmentId, string? TreatmentCode,
    string? TreatmentName, TreatmentScopeKind Scope);

public record GuaranteeTreatmentDetail(
    Guid Id, Guid GuaranteeRateId, Guid TreatmentId, TreatmentScopeKind Scope,
    string RowVersion);

public record GuaranteeTreatmentInput(
    Guid GuaranteeRateId, Guid TreatmentId, TreatmentScopeKind Scope);

public sealed class GuaranteeRateTreatmentSpec
    : CrudSpec<GuaranteeRateTreatment, GuaranteeTreatmentRow, GuaranteeTreatmentDetail,
        GuaranteeTreatmentInput>
{
    public override string Resource => "guarantee-rate-treatments";
    public override string DisplayNameTh => "Treatment ที่เทียบประกันรายได้";
    public override string Module => "duty-rates";
    public override string DefaultSort => "treatmentCode";

    public override IReadOnlyList<string> FilterKeys => ["guaranteeRateId", "scope"];

    public override Expression<Func<GuaranteeRateTreatment, GuaranteeTreatmentRow>>
        ListProjection =>
        e => new GuaranteeTreatmentRow(e.Id, e.GuaranteeRateId, e.TreatmentId,
            e.Treatment == null ? null : e.Treatment.Code,
            e.Treatment == null ? null : e.Treatment.NameTh,
            e.Scope);

    public override Expression<Func<GuaranteeRateTreatment, GuaranteeTreatmentDetail>>
        DetailProjection =>
        e => new GuaranteeTreatmentDetail(e.Id, e.GuaranteeRateId, e.TreatmentId, e.Scope,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<GuaranteeRateTreatment, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<GuaranteeRateTreatment, object?>>>
        {
            ["treatmentCode"] = e => e.Treatment == null ? null : e.Treatment.Code,
            ["scope"] = e => e.Scope,
        };

    public override IQueryable<GuaranteeRateTreatment> Search(
        IQueryable<GuaranteeRateTreatment> q, ListRequest r)
    {
        if (r.Filter("guaranteeRateId") is { } id && Guid.TryParse(id, out var rateId))
            q = q.Where(e => e.GuaranteeRateId == rateId);

        if (r.Enum<TreatmentScopeKind>("scope") is { } scope)
            q = q.Where(e => e.Scope == scope);

        return q;
    }

    public override void Apply(GuaranteeRateTreatment e, GuaranteeTreatmentInput input,
        bool isCreate)
    {
        if (isCreate) e.GuaranteeRateId = input.GuaranteeRateId;
        e.TreatmentId = input.TreatmentId;
        e.Scope = input.Scope;
    }

    public override Task ValidateAsync(GuaranteeRateTreatment e, GuaranteeTreatmentInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.GuaranteeRateId, "guaranteeRateId",
            "อัตราประกันรายได้");
        MasterFieldRules.RequiredId(errors, input.TreatmentId, "treatmentId", "Treatment");
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(GuaranteeRateTreatment e,
        GuaranteeTreatmentInput input, bool isCreate, ValidationFailure errors,
        IRepository<GuaranteeRateTreatment> repo, IQueryExecutor exec, CancellationToken ct)
    {

        var existing = await exec.FirstOrDefaultAsync(
            repo.Query()
                .Where(o => o.Id != e.Id &&
                            o.GuaranteeRateId == input.GuaranteeRateId &&
                            o.TreatmentId == input.TreatmentId)
                .Select(o => new { o.Scope }),
            ct);

        if (existing is null) return;

        errors.Duplicate("treatmentId", existing.Scope == input.Scope
            ? "Treatment นี้อยู่ในรายการแล้ว"
            : existing.Scope == TreatmentScopeKind.Include
                ? "Treatment นี้อยู่ในรายการที่ใช้เทียบอยู่แล้ว"
                : "Treatment นี้อยู่ในรายการที่ยกเว้นอยู่แล้ว");
    }

    public static string ScopeTh(TreatmentScopeKind scope) =>
        scope == TreatmentScopeKind.Include ? "ใช้เทียบ" : "ยกเว้น";
}

public record GuaranteeDayRow(
    Guid Id, Guid GuaranteeRateId, short DayOfWeek, string DayNameTh,
    decimal? IncomeAmount, TimeOnly? StartTime, TimeOnly? EndTime, bool IsExcluded);

public record GuaranteeDayDetail(
    Guid Id, Guid GuaranteeRateId, short DayOfWeek, decimal? IncomeAmount,
    TimeOnly? StartTime, TimeOnly? EndTime, bool IsExcluded, string RowVersion);

public record GuaranteeDayInput(
    Guid GuaranteeRateId, short DayOfWeek, decimal? IncomeAmount,
    TimeOnly? StartTime, TimeOnly? EndTime, bool IsExcluded);

public sealed class GuaranteeRateDaySpec
    : CrudSpec<GuaranteeRateDay, GuaranteeDayRow, GuaranteeDayDetail, GuaranteeDayInput>
{
    public override string Resource => "guarantee-rate-days";
    public override string DisplayNameTh => "เงื่อนไขรายวันของประกันรายได้";
    public override string Module => "duty-rates";
    public override string DefaultSort => "dayOfWeek";

    public override IReadOnlyList<string> FilterKeys => ["guaranteeRateId"];

    public override Expression<Func<GuaranteeRateDay, GuaranteeDayRow>> ListProjection =>
        e => new GuaranteeDayRow(e.Id, e.GuaranteeRateId, e.DayOfWeek,
            DutyRateDaySpec.DayNames[e.DayOfWeek],
            e.IncomeAmount, e.StartTime, e.EndTime, e.IsExcluded);

    public override Expression<Func<GuaranteeRateDay, GuaranteeDayDetail>> DetailProjection =>
        e => new GuaranteeDayDetail(e.Id, e.GuaranteeRateId, e.DayOfWeek, e.IncomeAmount,
            e.StartTime, e.EndTime, e.IsExcluded, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<GuaranteeRateDay, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<GuaranteeRateDay, object?>>>
        {
            ["dayOfWeek"] = e => e.DayOfWeek,
        };

    public override IQueryable<GuaranteeRateDay> Search(
        IQueryable<GuaranteeRateDay> q, ListRequest r) =>
        r.Filter("guaranteeRateId") is { } id && Guid.TryParse(id, out var rateId)
            ? q.Where(e => e.GuaranteeRateId == rateId)
            : q;

    public override void Apply(GuaranteeRateDay e, GuaranteeDayInput input, bool isCreate)
    {
        if (isCreate) e.GuaranteeRateId = input.GuaranteeRateId;
        e.DayOfWeek = input.DayOfWeek;
        e.IncomeAmount = input.IncomeAmount;
        e.StartTime = input.StartTime;
        e.EndTime = input.EndTime;
        e.IsExcluded = input.IsExcluded;
    }

    public override Task ValidateAsync(GuaranteeRateDay e, GuaranteeDayInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.GuaranteeRateId, "guaranteeRateId",
            "อัตราประกันรายได้");

        if (input.DayOfWeek is < 0 or > 6)
            errors.Add("dayOfWeek", "range", "โปรดเลือกวันในสัปดาห์");

        if (input.IncomeAmount is < 0)
            errors.Add("incomeAmount", "range", "รายได้ต้องไม่ติดลบ");

        if (input.StartTime is null != (input.EndTime is null))
            errors.Add("endTime", "required", "โปรดระบุทั้งเวลาเริ่มต้นและเวลาสิ้นสุด");
        else if (input.StartTime is { } from && input.EndTime is { } to && to <= from)
            errors.Add("endTime", "range", "เวลาสิ้นสุดต้องหลังเวลาเริ่มต้น");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(GuaranteeRateDay e,
        GuaranteeDayInput input, bool isCreate, ValidationFailure errors,
        IRepository<GuaranteeRateDay> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.GuaranteeRateId == input.GuaranteeRateId &&
            o.DayOfWeek == input.DayOfWeek);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("dayOfWeek", "วันนี้มีเงื่อนไขอยู่แล้วในชุดนี้");
    }
}

