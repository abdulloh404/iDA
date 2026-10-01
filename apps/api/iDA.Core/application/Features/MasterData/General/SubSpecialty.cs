using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public record SubSpecialtyListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? SpecialtyCode,
    string? SpecialtyNameTh,
    RecordStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);

public record SubSpecialtyDetail(
    Guid Id,
    string Code,
    Guid SpecialtyId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy,
    string RowVersion);

public record SubSpecialtyInput(
    string Code,
    Guid SpecialtyId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark);

public sealed class SubSpecialtySpec
    : CrudSpec<MstSubSpecialty, SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput>
{
    public override string Resource => "sub-specialties";
    public override string DisplayNameTh => "ความเชี่ยวชาญเฉพาะทาง";
    public override string Module => "master-data-general";

    public override string DefaultSort => "-updatedAt";
    public override string LookupSort => "code";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "specialtyId"];

    public override Expression<Func<MstSubSpecialty, SubSpecialtyListItem>> ListProjection =>
        e => new SubSpecialtyListItem(e.Id, e.Code, e.SubSpecialtyNameTh, e.SubSpecialtyNameEn,
            e.Specialty == null ? null : e.Specialty.Code,
            e.Specialty == null ? null : e.Specialty.SpecialtyNameTh,
            e.Status,
            e.CreatedAt, e.CreatedBy, e.UpdatedAt, e.UpdatedBy);

    public override Expression<Func<MstSubSpecialty, SubSpecialtyDetail>> DetailProjection =>
        e => new SubSpecialtyDetail(e.Id, e.Code, e.SpecialtyId, e.SubSpecialtyNameTh,
            e.SubSpecialtyNameEn, e.Status, e.Remark,
            e.CreatedAt, e.CreatedBy, e.UpdatedAt, e.UpdatedBy, e.RowVersion.ToString());

    public override Expression<Func<MstSubSpecialty, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.SubSpecialtyNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstSubSpecialty, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstSubSpecialty, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.SubSpecialtyNameTh,
            ["nameEn"] = e => e.SubSpecialtyNameEn,
            ["specialtyCode"] = e => e.Specialty == null ? null : e.Specialty.Code,
            ["status"] = e => e.Status,
            ["createdAt"] = e => e.CreatedAt,
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override IQueryable<MstSubSpecialty> Search(IQueryable<MstSubSpecialty> query,
        ListRequest r)
    {

        if (r.Filter("specialtyId") is { } specialtyId && Guid.TryParse(specialtyId, out var id))
            query = query.Where(e => e.SpecialtyId == id);

        if (r.Filter("code") is { } code)
        {
            var c = code.ToLower();
            query = query.Where(e => e.Code.ToLower().Contains(c));
        }

        if (r.Filter("nameTh") is { } nameTh)
        {
            var th = nameTh.ToLower();
            query = query.Where(e => e.SubSpecialtyNameTh.ToLower().Contains(th));
        }

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim().ToLower();
            query = query.Where(e =>
                e.Code.ToLower().Contains(q) ||
                e.SubSpecialtyNameTh.ToLower().Contains(q) ||
                (e.Specialty != null && (
                    e.Specialty.Code.ToLower().Contains(q) ||
                    e.Specialty.SpecialtyNameTh.ToLower().Contains(q))));
        }

        return query;
    }

    public override void Apply(MstSubSpecialty e, SubSpecialtyInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.SpecialtyId = input.SpecialtyId;
        e.SubSpecialtyNameTh = input.NameTh.Trim();
        e.SubSpecialtyNameEn = input.NameEn?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstSubSpecialty e, SubSpecialtyInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสความเชี่ยวชาญเฉพาะทาง", 20);
        MasterFieldRules.RequiredId(errors, input.SpecialtyId, "specialtyId", "ความเชี่ยวชาญ");
        MasterFieldRules.Required(errors, input.NameTh, "nameTh",
            "ความเชี่ยวชาญเฉพาะทาง (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<SubSpecialtyListItem>> ExportColumns =>
    [
        new("รหัสความเชี่ยวชาญ", r => r.SpecialtyCode),
        new("ความเชี่ยวชาญ", r => r.SpecialtyNameTh),
        new("รหัสความเชี่ยวชาญเฉพาะทาง", r => r.Code),
        new("ความเชี่ยวชาญเฉพาะทาง (ไทย)", r => r.NameTh),
        new("ความเชี่ยวชาญเฉพาะทาง (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

