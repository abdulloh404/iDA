using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public record SpecialtyListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);

public record SpecialtyDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    int? DisplaySeq,
    RecordStatus Status,
    string? Remark,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy,

    string RowVersion);

public record SpecialtyInput(
    string Code,
    string NameTh,
    string? NameEn,
    int? DisplaySeq,
    RecordStatus Status,
    string? Remark);

public sealed class SpecialtySpec
    : CrudSpec<MstSpecialty, SpecialtyListItem, SpecialtyDetail, SpecialtyInput>
{
    public override string Resource => "specialties";
    public override string DisplayNameTh => "ความเชี่ยวชาญ";
    public override string Module => "master-data-general";

    public override string DefaultSort => "-updatedAt";
    public override string LookupSort => "code";

    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn"];

    public override Expression<Func<MstSpecialty, SpecialtyListItem>> ListProjection =>
        e => new SpecialtyListItem(e.Id, e.Code, e.SpecialtyNameTh, e.SpecialtyNameEn, e.Status,
            e.CreatedAt, e.CreatedBy, e.UpdatedAt, e.UpdatedBy);

    public override Expression<Func<MstSpecialty, SpecialtyDetail>> DetailProjection =>
        e => new SpecialtyDetail(e.Id, e.Code, e.SpecialtyNameTh, e.SpecialtyNameEn,
            e.DisplaySeq, e.Status, e.Remark,
            e.CreatedAt, e.CreatedBy, e.UpdatedAt, e.UpdatedBy, e.RowVersion.ToString());

    public override Expression<Func<MstSpecialty, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.SpecialtyNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstSpecialty, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstSpecialty, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.SpecialtyNameTh,
            ["nameEn"] = e => e.SpecialtyNameEn,
            ["status"] = e => e.Status,
            ["displaySeq"] = e => e.DisplaySeq,
            ["createdAt"] = e => e.CreatedAt,
            ["updatedAt"] = e => e.UpdatedAt,
        };

    public override IQueryable<MstSpecialty> Search(IQueryable<MstSpecialty> query, ListRequest r)
    {

        if (r.Filter("code") is { } code)
        {
            var c = code.ToLower();
            query = query.Where(e => e.Code.ToLower().Contains(c));
        }

        if (r.Filter("nameTh") is { } nameTh)
        {
            var th = nameTh.ToLower();
            query = query.Where(e => e.SpecialtyNameTh.ToLower().Contains(th));
        }

        if (r.Filter("nameEn") is { } nameEn)
        {
            var en = nameEn.ToLower();
            query = query.Where(e =>
                e.SpecialtyNameEn != null && e.SpecialtyNameEn.ToLower().Contains(en));
        }

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim().ToLower();
            query = query.Where(e =>
                e.Code.ToLower().Contains(q) ||
                e.SpecialtyNameTh.ToLower().Contains(q) ||
                (e.SpecialtyNameEn != null && e.SpecialtyNameEn.ToLower().Contains(q)));
        }

        return query;
    }

    public override void Apply(MstSpecialty e, SpecialtyInput input, bool isCreate)
    {

        if (isCreate) e.Code = input.Code.Trim();

        e.SpecialtyNameTh = input.NameTh.Trim();
        e.SpecialtyNameEn = input.NameEn?.Trim();
        e.DisplaySeq = input.DisplaySeq;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstSpecialty e, SpecialtyInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
            errors.Required("code", "โปรดระบุรหัสความเชี่ยวชาญ");
        else if (input.Code.Trim().Length > 20)
            errors.Add("code", "max_length", "รหัสความเชี่ยวชาญต้องไม่เกิน 20 ตัวอักษร");

        if (string.IsNullOrWhiteSpace(input.NameTh))
            errors.Required("nameTh", "โปรดระบุชื่อความเชี่ยวชาญ (ภาษาไทย)");

        return Task.CompletedTask;
    }

    public override async Task<string?> WhyCannotDeleteAsync(MstSpecialty e, CancellationToken ct)
    {
        await Task.CompletedTask;
        return null;
    }

    public override IReadOnlyList<ExcelColumn<SpecialtyListItem>> ExportColumns =>
    [
        new("รหัสความเชี่ยวชาญ", r => r.Code),
        new("ความเชี่ยวชาญ (ไทย)", r => r.NameTh),
        new("ความเชี่ยวชาญ (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => r.Status == RecordStatus.Active ? "ใช้งาน" : "ไม่ใช้งาน"),
    ];
}

