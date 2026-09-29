using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public record DocumentTypeListItem(
    Guid Id, string Code, string NameTh, string? NameEn, bool HasExpiry, int? AlertBeforeDays,
    bool IsRequired, string ScopeLevel, RecordStatus Status);

public record DocumentTypeDetail(
    Guid Id, string Code, string NameTh, string? NameEn, bool HasExpiry, int? AlertBeforeDays,
    bool IsRequired, string ScopeLevel, RecordStatus Status, string? Remark, string RowVersion);

public record DocumentTypeInput(
    string Code, string NameTh, string? NameEn, bool HasExpiry, int? AlertBeforeDays,
    bool IsRequired, string ScopeLevel, RecordStatus Status, string? Remark);

public sealed class DocumentTypeSpec
    : CrudSpec<MstDocumentType, DocumentTypeListItem, DocumentTypeDetail, DocumentTypeInput>
{
    public override string Resource => "document-types";
    public override string DisplayNameTh => "ประเภทเอกสาร";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "scopeLevel"];

    public override Expression<Func<MstDocumentType, DocumentTypeListItem>> ListProjection =>
        e => new DocumentTypeListItem(e.Id, e.Code, e.DocTypeNameTh, e.DocTypeNameEn,
            e.HasExpiry, e.AlertBeforeDays, e.IsRequired, e.ScopeLevel, e.Status);

    public override Expression<Func<MstDocumentType, DocumentTypeDetail>> DetailProjection =>
        e => new DocumentTypeDetail(e.Id, e.Code, e.DocTypeNameTh, e.DocTypeNameEn, e.HasExpiry,
            e.AlertBeforeDays, e.IsRequired, e.ScopeLevel, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override Expression<Func<MstDocumentType, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.DocTypeNameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstDocumentType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstDocumentType, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.DocTypeNameTh,
            ["scopeLevel"] = e => e.ScopeLevel,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstDocumentType> Search(
        IQueryable<MstDocumentType> query, ListRequest r)
    {
        if (r.Filter("scopeLevel") is { } scope)
            query = query.Where(e => e.ScopeLevel == scope);

        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e => e.Code.Contains(q) || e.DocTypeNameTh.Contains(q));
        }

        return query;
    }

    public override void Apply(MstDocumentType e, DocumentTypeInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.DocTypeNameTh = input.NameTh.Trim();
        e.DocTypeNameEn = input.NameEn?.Trim();
        e.HasExpiry = input.HasExpiry;

        e.AlertBeforeDays = input.HasExpiry ? input.AlertBeforeDays : null;
        e.IsRequired = input.IsRequired;
        e.ScopeLevel = input.ScopeLevel;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstDocumentType e, DocumentTypeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทเอกสาร", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อประเภทเอกสาร");

        if (input.ScopeLevel is not ("CORE" or "BU"))
            errors.Add("scopeLevel", "invalid", "ระดับการจัดเก็บต้องเป็น CORE หรือ BU");

        if (input.AlertBeforeDays is < 0)
            errors.Add("alertBeforeDays", "range", "จำนวนวันแจ้งเตือนล่วงหน้าต้องไม่ติดลบ");

        return Task.CompletedTask;
    }
}

