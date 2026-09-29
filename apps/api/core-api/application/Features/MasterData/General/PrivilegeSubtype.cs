using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.General;

public record PrivilegeSubtypeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? PrivilegeTypeCode,
    string? PrivilegeTypeNameTh,
    RecordStatus Status);

public record PrivilegeSubtypeDetail(
    Guid Id,
    string Code,
    Guid PrivilegeTypeId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record PrivilegeSubtypeInput(
    string Code,
    Guid PrivilegeTypeId,
    string NameTh,
    string? NameEn,
    RecordStatus Status,
    string? Remark);

public sealed class PrivilegeSubtypeSpec
    : CrudSpec<MstPrivilegeSubtype, PrivilegeSubtypeListItem, PrivilegeSubtypeDetail,
        PrivilegeSubtypeInput>
{
    public override string Resource => "privilege-subtypes";
    public override string DisplayNameTh => "Privilege SubType";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "privilegeTypeId"];

    public override Expression<Func<MstPrivilegeSubtype, PrivilegeSubtypeListItem>> ListProjection =>
        e => new PrivilegeSubtypeListItem(e.Id, e.Code, e.NameTh, e.NameEn,
            e.PrivilegeType == null ? null : e.PrivilegeType.Code,
            e.PrivilegeType == null ? null : e.PrivilegeType.NameTh,
            e.Status);

    public override Expression<Func<MstPrivilegeSubtype, PrivilegeSubtypeDetail>> DetailProjection =>
        e => new PrivilegeSubtypeDetail(e.Id, e.Code, e.PrivilegeTypeId, e.NameTh, e.NameEn,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstPrivilegeSubtype, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstPrivilegeSubtype, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstPrivilegeSubtype, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["privilegeTypeCode"] = e => e.PrivilegeType == null ? null : e.PrivilegeType.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstPrivilegeSubtype> Search(IQueryable<MstPrivilegeSubtype> query,
        ListRequest r)
    {
        if (r.Filter("privilegeTypeId") is { } typeId && Guid.TryParse(typeId, out var id))
            query = query.Where(e => e.PrivilegeTypeId == id);

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

    public override void Apply(MstPrivilegeSubtype e, PrivilegeSubtypeInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.PrivilegeTypeId = input.PrivilegeTypeId;
        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstPrivilegeSubtype e, PrivilegeSubtypeInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัส Privilege SubType", 20);
        MasterFieldRules.RequiredId(errors, input.PrivilegeTypeId, "privilegeTypeId",
            "Privilege Type");
        MasterFieldRules.Required(errors, input.NameTh, "nameTh",
            "ชื่อ Privilege SubType (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<PrivilegeSubtypeListItem>> ExportColumns =>
    [
        new("รหัส Privilege Type", r => r.PrivilegeTypeCode),
        new("Privilege Type", r => r.PrivilegeTypeNameTh),
        new("รหัส Privilege SubType", r => r.Code),
        new("Privilege SubType (ไทย)", r => r.NameTh),
        new("Privilege SubType (อังกฤษ)", r => r.NameEn),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

