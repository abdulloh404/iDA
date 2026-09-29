using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Tax402;

public record IncomeType402ListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? DepartmentCode,
    string? AccountNo,
    RecordStatus Status);

public record IncomeType402Detail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    Guid? DepartmentId,
    string? AccountNo,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record IncomeType402Input(
    string Code,
    string NameTh,
    string? NameEn,
    Guid? DepartmentId,
    string? AccountNo,
    RecordStatus Status,
    string? Remark);

public sealed class IncomeType402Spec
    : CrudSpec<MstIncomeType402, IncomeType402ListItem, IncomeType402Detail, IncomeType402Input>
{
    public override string Resource => "income-types-402";
    public override string DisplayNameTh => "ประเภทเงินได้";
    public override string Module => "master-data-tax-402";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "departmentId"];

    public override Expression<Func<MstIncomeType402, IncomeType402ListItem>> ListProjection =>
        e => new IncomeType402ListItem(e.Id, e.Code, e.NameTh, e.NameEn,
            e.Department == null ? null : e.Department.Code,
            e.AccountNo, e.Status);

    public override Expression<Func<MstIncomeType402, IncomeType402Detail>> DetailProjection =>
        e => new IncomeType402Detail(e.Id, e.Code, e.NameTh, e.NameEn, e.DepartmentId,
            e.AccountNo, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstIncomeType402, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string,
        Expression<Func<MstIncomeType402, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstIncomeType402, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["departmentCode"] = e => e.Department == null ? null : e.Department.Code,
            ["accountNo"] = e => e.AccountNo,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstIncomeType402> Search(IQueryable<MstIncomeType402> query,
        ListRequest r)
    {
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

    public override void Apply(MstIncomeType402 e, IncomeType402Input input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.DepartmentId = input.DepartmentId;
        e.AccountNo = input.AccountNo?.Trim();
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstIncomeType402 e, IncomeType402Input input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทเงินได้", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ประเภทเงินได้");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<IncomeType402ListItem>> ExportColumns =>
    [
        new("รหัสประเภทเงินได้", r => r.Code),
        new("ประเภทเงินได้", r => r.NameTh),
        new("รหัสแผนก", r => r.DepartmentCode),
        new("รหัสบัญชี", r => r.AccountNo),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

