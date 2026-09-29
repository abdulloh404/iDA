using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.General;

public record DepartmentListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? CostCenter,
    RecordStatus Status);

public record DepartmentDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? CostCenter,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record DepartmentInput(
    string Code,
    string NameTh,
    string? NameEn,
    string? CostCenter,
    RecordStatus Status,
    string? Remark);

public sealed class DepartmentSpec
    : CrudSpec<MstDepartment, DepartmentListItem, DepartmentDetail, DepartmentInput>
{
    public override string Resource => "departments";
    public override string DisplayNameTh => "แผนก";
    public override string Module => "master-data-general";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "nameEn", "costCenter"];

    public override Expression<Func<MstDepartment, DepartmentListItem>> ListProjection =>
        e => new DepartmentListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.CostCenter, e.Status);

    public override Expression<Func<MstDepartment, DepartmentDetail>> DetailProjection =>
        e => new DepartmentDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.CostCenter,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstDepartment, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstDepartment, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstDepartment, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["costCenter"] = e => e.CostCenter,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstDepartment> Search(IQueryable<MstDepartment> query, ListRequest r)
    {
        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.NameTh.Contains(nameTh));

        if (r.Filter("nameEn") is { } nameEn)
            query = query.Where(e => e.NameEn != null && e.NameEn.Contains(nameEn));

        if (r.Filter("costCenter") is { } costCenter)
            query = query.Where(e => e.CostCenter != null && e.CostCenter.Contains(costCenter));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.NameTh.Contains(q) ||
                (e.NameEn != null && e.NameEn.Contains(q)) ||

                (e.CostCenter != null && e.CostCenter.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstDepartment e, DepartmentInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.CostCenter = input.CostCenter?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstDepartment e, DepartmentInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
            errors.Required("code", "โปรดระบุรหัสแผนก");
        else if (input.Code.Trim().Length > 20)
            errors.Add("code", "max_length", "รหัสแผนกต้องไม่เกิน 20 ตัวอักษร");

        if (string.IsNullOrWhiteSpace(input.NameTh))
            errors.Required("nameTh", "โปรดระบุชื่อแผนก");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<DepartmentListItem>> ExportColumns =>
    [
        new("รหัสแผนก", r => r.Code),
        new("ชื่อแผนก (ไทย)", r => r.NameTh),
        new("ชื่อแผนก (อังกฤษ)", r => r.NameEn),
        new("ศูนย์รายได้/ค่าใช้จ่าย", r => r.CostCenter),
        new("สถานะ", r => r.Status == RecordStatus.Active ? "ใช้งาน" : "ไม่ใช้งาน"),
    ];
}

