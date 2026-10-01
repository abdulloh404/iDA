using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Tax402;

public record ExpenseTypeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    string? AccountNo,
    string? DepartmentCode,
    RecordStatus Status);

public record ExpenseTypeDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    string? AccountNo,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ExpenseTypeInput(
    string Code,
    string NameTh,
    string? NameEn,
    ItemDirection Direction,
    string? AccountNo,
    Guid? DepartmentId,
    RecordStatus Status,
    string? Remark);

public sealed class ExpenseTypeSpec
    : CrudSpec<MstExpenseType, ExpenseTypeListItem, ExpenseTypeDetail, ExpenseTypeInput>
{
    public override string Resource => "expense-types";
    public override string DisplayNameTh => "ประเภทรายการค่าใช้จ่าย";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["code", "nameTh", "direction", "departmentId"];

    public override Expression<Func<MstExpenseType, ExpenseTypeListItem>> ListProjection =>
        e => new ExpenseTypeListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.Direction, e.AccountNo,
            e.Department == null ? null : e.Department.Code,
            e.Status);

    public override Expression<Func<MstExpenseType, ExpenseTypeDetail>> DetailProjection =>
        e => new ExpenseTypeDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.Direction, e.AccountNo,
            e.DepartmentId, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstExpenseType, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstExpenseType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstExpenseType, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["direction"] = e => e.Direction,
            ["accountNo"] = e => e.AccountNo,
            ["departmentCode"] = e => e.Department == null ? null : e.Department.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstExpenseType> Search(IQueryable<MstExpenseType> query,
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
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.NameTh.Contains(q) ||
                (e.AccountNo != null && e.AccountNo.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstExpenseType e, ExpenseTypeInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.Direction = input.Direction;
        e.AccountNo = input.AccountNo?.Trim();
        e.DepartmentId = input.DepartmentId;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstExpenseType e, ExpenseTypeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทรายการค่าใช้จ่าย", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ประเภทรายการค่าใช้จ่าย");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<ExpenseTypeListItem>> ExportColumns =>
    [
        new("รหัสประเภท", r => r.Code),
        new("ประเภทรายการค่าใช้จ่าย", r => r.NameTh),
        new("ประเภทรายการ", r => r.Direction == ItemDirection.Add ? "เพิ่ม" : "หัก"),
        new("รหัสบัญชี", r => r.AccountNo),
        new("รหัสแผนก", r => r.DepartmentCode),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

