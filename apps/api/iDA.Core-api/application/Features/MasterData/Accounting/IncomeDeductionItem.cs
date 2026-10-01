using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record IncomeDeductionItemListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? TaxTypeNameTh,
    ItemDirection Direction,
    string? DepartmentCode,
    string? AccountNoOpd,
    string? AccountNoIpd,
    RecordStatus Status);

public record IncomeDeductionItemDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    Guid? TaxTypeId,
    ItemDirection Direction,
    Guid? DepartmentId,
    string? AccountNoOpd,
    string? AccountNoIpd,
    Guid? ExpenseTypeId,
    string? JvType,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record IncomeDeductionItemInput(
    string Code,
    string NameTh,
    string? NameEn,
    Guid? TaxTypeId,
    ItemDirection Direction,
    Guid? DepartmentId,
    string? AccountNoOpd,
    string? AccountNoIpd,
    Guid? ExpenseTypeId,
    string? JvType,
    RecordStatus Status,
    string? Remark);

public sealed class IncomeDeductionItemSpec
    : CrudSpec<MstIncomeDeductionItem, IncomeDeductionItemListItem, IncomeDeductionItemDetail,
        IncomeDeductionItemInput>
{
    public override string Resource => "income-deduction-items";
    public override string DisplayNameTh => "รายได้และรายการหัก";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["code", "nameTh", "direction", "taxTypeId", "departmentId"];

    public override Expression<Func<MstIncomeDeductionItem, IncomeDeductionItemListItem>>
        ListProjection =>
        e => new IncomeDeductionItemListItem(e.Id, e.Code, e.NameTh,
            e.TaxType == null ? null : e.TaxType.NameTh,
            e.Direction,
            e.Department == null ? null : e.Department.Code,
            e.AccountNoOpd, e.AccountNoIpd, e.Status);

    public override Expression<Func<MstIncomeDeductionItem, IncomeDeductionItemDetail>>
        DetailProjection =>
        e => new IncomeDeductionItemDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.TaxTypeId,
            e.Direction, e.DepartmentId, e.AccountNoOpd, e.AccountNoIpd, e.ExpenseTypeId,
            e.JvType, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstIncomeDeductionItem, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstIncomeDeductionItem, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstIncomeDeductionItem, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["direction"] = e => e.Direction,
            ["departmentCode"] = e => e.Department == null ? null : e.Department.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstIncomeDeductionItem> Search(
        IQueryable<MstIncomeDeductionItem> query, ListRequest r)
    {
        if (r.Enum<ItemDirection>("direction") is { } parsed)
            query = query.Where(e => e.Direction == parsed);

        if (r.Filter("taxTypeId") is { } taxTypeId && Guid.TryParse(taxTypeId, out var taxId))
            query = query.Where(e => e.TaxTypeId == taxId);

        if (r.Filter("departmentId") is { } departmentId &&
            Guid.TryParse(departmentId, out var deptId))
            query = query.Where(e => e.DepartmentId == deptId);

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
                (e.AccountNoOpd != null && e.AccountNoOpd.Contains(q)) ||
                (e.AccountNoIpd != null && e.AccountNoIpd.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstIncomeDeductionItem e, IncomeDeductionItemInput input,
        bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.TaxTypeId = input.TaxTypeId;
        e.Direction = input.Direction;
        e.DepartmentId = input.DepartmentId;
        e.AccountNoOpd = input.AccountNoOpd?.Trim();
        e.AccountNoIpd = input.AccountNoIpd?.Trim();
        e.ExpenseTypeId = input.ExpenseTypeId;
        e.JvType = input.JvType?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstIncomeDeductionItem e, IncomeDeductionItemInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสรายการ", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อรายการ");

        if (string.IsNullOrWhiteSpace(input.AccountNoOpd) &&
            string.IsNullOrWhiteSpace(input.AccountNoIpd))
            errors.Required("accountNoOpd",
                "ต้องระบุรหัสบัญชีอย่างน้อยหนึ่งฝั่ง (ผู้ป่วยนอกหรือผู้ป่วยใน)");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<IncomeDeductionItemListItem>> ExportColumns =>
    [
        new("รหัสรายการ", r => r.Code),
        new("ชื่อรายการ", r => r.NameTh),
        new("ประเภทภาษี", r => r.TaxTypeNameTh),
        new("ประเภทรายการ", r => r.Direction == ItemDirection.Add ? "เพิ่ม" : "หัก"),
        new("ลงบัญชี Department", r => r.DepartmentCode),
        new("ACCOUNT_NO_OPD", r => r.AccountNoOpd),
        new("ACCOUNT_NO_IPD", r => r.AccountNoIpd),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

