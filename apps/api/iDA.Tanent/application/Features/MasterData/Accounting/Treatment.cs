using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record TreatmentListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? CategoryCode,
    string? CategoryNameTh,
    decimal? PremiumRate,
    decimal? SocialRate,
    decimal? UnitPrice,
    bool RequiresReading,
    RecordStatus Status);

public record TreatmentDetail(
    Guid Id,
    string Code,
    Guid? TreatmentCategoryId,
    string NameTh,
    string? NameEn,
    decimal? PremiumRate,
    decimal? SocialRate,
    decimal? UnitPrice,
    bool RequiresReading,
    string SourceSystem,
    DateTimeOffset? SyncedAt,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TreatmentInput(
    string Code,
    Guid? TreatmentCategoryId,
    string NameTh,
    string? NameEn,
    decimal? PremiumRate,
    decimal? SocialRate,
    decimal? UnitPrice,
    bool RequiresReading,
    RecordStatus Status,
    string? Remark);

public sealed class TreatmentSpec
    : CrudSpec<MstTreatment, TreatmentListItem, TreatmentDetail, TreatmentInput>
{
    public override string Resource => "treatments";
    public override string DisplayNameTh => "Treatment";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["code", "nameTh", "treatmentCategoryId", "requiresReading"];

    public override Expression<Func<MstTreatment, TreatmentListItem>> ListProjection =>
        e => new TreatmentListItem(e.Id, e.Code, e.NameTh, e.NameEn,
            e.TreatmentCategory == null ? null : e.TreatmentCategory.Code,
            e.TreatmentCategory == null ? null : e.TreatmentCategory.NameTh,
            e.PremiumRate, e.SocialRate, e.UnitPrice, e.RequiresReading, e.Status);

    public override Expression<Func<MstTreatment, TreatmentDetail>> DetailProjection =>
        e => new TreatmentDetail(e.Id, e.Code, e.TreatmentCategoryId, e.NameTh, e.NameEn,
            e.PremiumRate, e.SocialRate, e.UnitPrice, e.RequiresReading, e.SourceSystem,
            e.SyncedAt, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstTreatment, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstTreatment, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstTreatment, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["categoryCode"] = e => e.TreatmentCategory == null ? null : e.TreatmentCategory.Code,
            ["premiumRate"] = e => e.PremiumRate,
            ["socialRate"] = e => e.SocialRate,
            ["unitPrice"] = e => e.UnitPrice,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstTreatment> Search(IQueryable<MstTreatment> query, ListRequest r)
    {
        if (r.Filter("treatmentCategoryId") is { } categoryId &&
            Guid.TryParse(categoryId, out var id))
            query = query.Where(e => e.TreatmentCategoryId == id);

        if (r.Filter("requiresReading") is { } reading)
            query = query.Where(e => e.RequiresReading == (reading == "true"));

        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.NameTh.Contains(q) ||
                (e.NameEn != null && e.NameEn.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstTreatment e, TreatmentInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.Code = input.Code.Trim();
            e.SourceSystem = "MANUAL";
        }

        e.TreatmentCategoryId = input.TreatmentCategoryId;
        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.PremiumRate = input.PremiumRate;
        e.SocialRate = input.SocialRate;
        e.UnitPrice = input.UnitPrice;
        e.RequiresReading = input.RequiresReading;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstTreatment e, TreatmentInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัส Treatment", 40);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อ Treatment (ภาษาไทย)");

        Percent(errors, input.PremiumRate, "premiumRate", "อัตราส่วนแบ่ง Premium");
        Percent(errors, input.SocialRate, "socialRate", "อัตราส่วนแบ่งประกันสังคม");

        if (input.UnitPrice is < 0)
            errors.Add("unitPrice", "range", "ราคา Treatment ต้องไม่ติดลบ");

        return Task.CompletedTask;
    }

    private static void Percent(ValidationFailure errors, decimal? value, string field,
        string labelTh)
    {
        if (value is < 0 or > 100)
            errors.Add(field, "range", $"{labelTh}ต้องอยู่ระหว่าง 0 ถึง 100");
    }

    public override IReadOnlyList<ExcelColumn<TreatmentListItem>> ExportColumns =>
    [
        new("รหัส Treatment", r => r.Code),
        new("ชื่อ Treatment (ไทย)", r => r.NameTh),
        new("ชื่อ Treatment (อังกฤษ)", r => r.NameEn),
        new("รหัส Category", r => r.CategoryCode),
        new("Category", r => r.CategoryNameTh),
        new("อัตรา Premium (%)", r => r.PremiumRate, "#,##0.00"),
        new("อัตราประกันสังคม (%)", r => r.SocialRate, "#,##0.00"),
        new("ราคา", r => r.UnitPrice, "#,##0.00"),
        new("ต้องรออ่านผล", r => r.RequiresReading ? "ใช่" : "ไม่ใช่"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

