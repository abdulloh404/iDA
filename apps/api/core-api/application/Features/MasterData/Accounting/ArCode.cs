using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record ArCodeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? Province,
    string? Phone,
    RecordStatus Status);

public record ArCodeDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? Address1,
    string? Address2,
    string? Address3,
    string? Province,
    string? Postcode,
    string? Phone,
    string? Fax,
    string SourceSystem,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ArCodeInput(
    string Code,
    string NameTh,
    string? NameEn,
    string? Address1,
    string? Address2,
    string? Address3,
    string? Province,
    string? Postcode,
    string? Phone,
    string? Fax,
    RecordStatus Status,
    string? Remark);

public sealed class ArCodeSpec : CrudSpec<MstArCode, ArCodeListItem, ArCodeDetail, ArCodeInput>
{
    public override string Resource => "ar-codes";
    public override string DisplayNameTh => "AR Code";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "province"];

    public override Expression<Func<MstArCode, ArCodeListItem>> ListProjection =>
        e => new ArCodeListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.Province, e.Phone, e.Status);

    public override Expression<Func<MstArCode, ArCodeDetail>> DetailProjection =>
        e => new ArCodeDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Address1, e.Address2,
            e.Address3, e.Province, e.Postcode, e.Phone, e.Fax, e.SourceSystem, e.Status,
            e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstArCode, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstArCode, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstArCode, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["province"] = e => e.Province,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstArCode> Search(IQueryable<MstArCode> query, ListRequest r)
    {
        if (r.Filter("province") is { } province)
            query = query.Where(e => e.Province != null && e.Province.Contains(province));

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

    public override void Apply(MstArCode e, ArCodeInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.Code = input.Code.Trim();
            e.SourceSystem = "MANUAL";
        }

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Address1 = input.Address1?.Trim();
        e.Address2 = input.Address2?.Trim();
        e.Address3 = input.Address3?.Trim();
        e.Province = input.Province?.Trim();
        e.Postcode = input.Postcode?.Trim();
        e.Phone = input.Phone?.Trim();
        e.Fax = input.Fax?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstArCode e, ArCodeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัส AR Code", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อ AR Code (ภาษาไทย)");

        var postcode = input.Postcode?.Trim();
        if (!string.IsNullOrEmpty(postcode) &&
            (postcode.Length != 5 || !postcode.All(char.IsAsciiDigit)))
            errors.Add("postcode", "format", "รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<ArCodeListItem>> ExportColumns =>
    [
        new("รหัส AR Code", r => r.Code),
        new("ชื่อ AR Code (ไทย)", r => r.NameTh),
        new("ชื่อ AR Code (อังกฤษ)", r => r.NameEn),
        new("จังหวัด", r => r.Province),
        new("เบอร์โทรติดต่อ", r => r.Phone),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

