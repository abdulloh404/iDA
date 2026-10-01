using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record TreatmentCategoryListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    bool IsPackage,
    string SourceSystem,
    RecordStatus Status);

public record TreatmentCategoryDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    bool IsPackage,
    string SourceSystem,
    DateTimeOffset? SyncedAt,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record TreatmentCategoryInput(
    string Code,
    string NameTh,
    string? NameEn,
    bool IsPackage,
    RecordStatus Status,
    string? Remark);

public sealed class TreatmentCategorySpec
    : CrudSpec<MstTreatmentCategory, TreatmentCategoryListItem, TreatmentCategoryDetail,
        TreatmentCategoryInput>
{
    public override string Resource => "treatment-categories";
    public override string DisplayNameTh => "Treatment Category";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "isPackage"];

    public override Expression<Func<MstTreatmentCategory, TreatmentCategoryListItem>> ListProjection =>
        e => new TreatmentCategoryListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.IsPackage,
            e.SourceSystem, e.Status);

    public override Expression<Func<MstTreatmentCategory, TreatmentCategoryDetail>> DetailProjection =>
        e => new TreatmentCategoryDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.IsPackage,
            e.SourceSystem, e.SyncedAt, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstTreatmentCategory, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstTreatmentCategory, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstTreatmentCategory, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["isPackage"] = e => e.IsPackage,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstTreatmentCategory> Search(IQueryable<MstTreatmentCategory> query,
        ListRequest r)
    {
        if (r.Filter("isPackage") is { } isPackage)
            query = query.Where(e => e.IsPackage == (isPackage == "true"));

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

    public override void Apply(MstTreatmentCategory e, TreatmentCategoryInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.Code = input.Code.Trim();

            e.SourceSystem = "MANUAL";
        }

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.IsPackage = input.IsPackage;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstTreatmentCategory e, TreatmentCategoryInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัส Treatment Category", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "รายละเอียด (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<TreatmentCategoryListItem>> ExportColumns =>
    [
        new("รหัส Treatment Category", r => r.Code),
        new("รายละเอียด (ไทย)", r => r.NameTh),
        new("รายละเอียด (อังกฤษ)", r => r.NameEn),
        new("Package", r => r.IsPackage ? "ใช่" : "ไม่ใช่"),
        new("ที่มาของข้อมูล", r => r.SourceSystem),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

