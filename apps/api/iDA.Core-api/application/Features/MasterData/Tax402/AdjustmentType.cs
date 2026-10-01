using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Tax402;

public record AdjustmentTypeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    string? DepartmentCode,
    RecordStatus Status);

public record AdjustmentTypeDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record AdjustmentTypeInput(
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark);

public sealed class AdjustmentTypeSpec
    : CrudSpec<MstAdjustmentType, AdjustmentTypeListItem, AdjustmentTypeDetail,
        AdjustmentTypeInput>
{
    public override string Resource => "adjustment-types";
    public override string DisplayNameTh => "ประเภทรายการปรับปรุง";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["code", "nameTh", "direction", "departmentId"];

    public override Expression<Func<MstAdjustmentType, AdjustmentTypeListItem>> ListProjection =>
        e => new AdjustmentTypeListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.Direction,
            e.Department == null ? null : e.Department.Code,
            e.Status);

    public override Expression<Func<MstAdjustmentType, AdjustmentTypeDetail>> DetailProjection =>
        e => new AdjustmentTypeDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Direction,
            e.DepartmentId, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstAdjustmentType, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstAdjustmentType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstAdjustmentType, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["direction"] = e => e.Direction,
            ["departmentCode"] = e => e.Department == null ? null : e.Department.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstAdjustmentType> Search(IQueryable<MstAdjustmentType> query,
        ListRequest r)
    {
        if (r.Enum<ItemDirection>("direction") is { } parsed)
            query = query.Where(e => e.Direction == parsed);

        if (r.Filter("departmentId") is { } departmentId &&
            Guid.TryParse(departmentId, out var id))
            query = query.Where(e => e.DepartmentId == id);

        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e => e.Code.Contains(q) || e.NameTh.Contains(q));
        }

        return query;
    }

    public override void Apply(MstAdjustmentType e, AdjustmentTypeInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Direction = input.Direction;
        e.DepartmentId = input.DepartmentId;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstAdjustmentType e, AdjustmentTypeInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทรายการปรับปรุง", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ประเภทรายการปรับปรุง");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<AdjustmentTypeListItem>> ExportColumns =>
    [
        new("รหัสประเภท", r => r.Code),
        new("ประเภทรายการปรับปรุง", r => r.NameTh),
        new("ประเภทรายการ", r => r.Direction == ItemDirection.Add ? "เพิ่ม" : "หัก"),
        new("รหัสแผนก", r => r.DepartmentCode),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

