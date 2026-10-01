using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.General;

public record ClinicListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? Location,
    string? Phone,
    RecordStatus Status);

public record ClinicDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? Location,
    string? Phone,
    string? Fax,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ClinicInput(
    string Code,
    string NameTh,
    string? NameEn,
    string? Location,
    string? Phone,
    string? Fax,
    RecordStatus Status,
    string? Remark);

public sealed class ClinicSpec : CrudSpec<MstClinic, ClinicListItem, ClinicDetail, ClinicInput>
{
    public override string Resource => "clinics";
    public override string DisplayNameTh => "คลินิก";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn", "location"];

    public override Expression<Func<MstClinic, ClinicListItem>> ListProjection =>
        e => new ClinicListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.Location, e.Phone, e.Status);

    public override Expression<Func<MstClinic, ClinicDetail>> DetailProjection =>
        e => new ClinicDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Location, e.Phone, e.Fax,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstClinic, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstClinic, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstClinic, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["location"] = e => e.Location,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstClinic> Search(IQueryable<MstClinic> query, ListRequest r)
    {
        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (r.Filter("nameEn") is { } nameEn)
            query = query.Where(e => e.NameEn != null && e.NameEn.Contains(nameEn));

        if (r.Filter("location") is { } location)
            query = query.Where(e => e.Location != null && e.Location.Contains(location));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.NameTh.Contains(q) ||
                (e.NameEn != null && e.NameEn.Contains(q)) ||
                (e.Location != null && e.Location.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstClinic e, ClinicInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Location = input.Location?.Trim();
        e.Phone = input.Phone?.Trim();
        e.Fax = input.Fax?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstClinic e, ClinicInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสคลินิก", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อคลินิก (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<ClinicListItem>> ExportColumns =>
    [
        new("รหัสคลินิก", r => r.Code),
        new("ชื่อคลินิก (ไทย)", r => r.NameTh),
        new("ชื่อคลินิก (อังกฤษ)", r => r.NameEn),
        new("Location", r => r.Location),
        new("เบอร์โทร", r => r.Phone),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

