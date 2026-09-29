using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record GlPostingSetupListItem(
    Guid Id,
    string? ShareCategoryCode,
    string? ShareCategoryNameTh,
    string DebitAccountNo,
    string? DebitDepartment,
    string CreditAccountNo,
    string? CreditDepartment,
    string? DoctorCode,
    GlPostingDateRule PostingDateRule,
    RecordStatus Status);

public record GlPostingSetupDetail(
    Guid Id,
    Guid ShareCategoryId,
    string DebitAccountNo,
    string? DebitDepartment,
    string CreditAccountNo,
    string? CreditDepartment,
    Guid? DoctorCodeId,
    GlPostingDateRule PostingDateRule,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record GlPostingSetupInput(
    Guid ShareCategoryId,
    string DebitAccountNo,
    string? DebitDepartment,
    string CreditAccountNo,
    string? CreditDepartment,
    Guid? DoctorCodeId,
    GlPostingDateRule PostingDateRule,
    RecordStatus Status,
    string? Remark);

public sealed class GlPostingSetupSpec
    : CrudSpec<GlPostingSetup, GlPostingSetupListItem, GlPostingSetupDetail, GlPostingSetupInput>
{
    public override string Resource => "gl-posting-setups";
    public override string DisplayNameTh => "ตั้งค่าบันทึกบัญชี";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "shareCategoryCode";

    public override IReadOnlyList<string> FilterKeys =>
        ["shareCategoryId", "postingDateRule", "accountNo"];

    public override Expression<Func<GlPostingSetup, GlPostingSetupListItem>> ListProjection =>
        e => new GlPostingSetupListItem(e.Id,
            e.ShareCategory == null ? null : e.ShareCategory.Code,
            e.ShareCategory == null ? null : e.ShareCategory.NameTh,
            e.DebitAccountNo, e.DebitDepartment, e.CreditAccountNo, e.CreditDepartment,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.PostingDateRule, e.Status);

    public override Expression<Func<GlPostingSetup, GlPostingSetupDetail>> DetailProjection =>
        e => new GlPostingSetupDetail(e.Id, e.ShareCategoryId, e.DebitAccountNo,
            e.DebitDepartment, e.CreditAccountNo, e.CreditDepartment, e.DoctorCodeId,
            e.PostingDateRule, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<GlPostingSetup, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<GlPostingSetup, object?>>>
        {
            ["shareCategoryCode"] = e => e.ShareCategory == null ? null : e.ShareCategory.Code,
            ["debitAccountNo"] = e => e.DebitAccountNo,
            ["creditAccountNo"] = e => e.CreditAccountNo,
            ["postingDateRule"] = e => e.PostingDateRule,
            ["status"] = e => e.Status,
        };

    public override IQueryable<GlPostingSetup> Search(IQueryable<GlPostingSetup> query,
        ListRequest r)
    {
        if (r.Filter("shareCategoryId") is { } categoryId && Guid.TryParse(categoryId, out var id))
            query = query.Where(e => e.ShareCategoryId == id);

        if (r.Enum<GlPostingDateRule>("postingDateRule") is { } parsed)
            query = query.Where(e => e.PostingDateRule == parsed);

        if (r.Filter("accountNo") is { } accountNo)
            query = query.Where(e =>
                e.DebitAccountNo.Contains(accountNo) || e.CreditAccountNo.Contains(accountNo));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.DebitAccountNo.Contains(q) ||
                e.CreditAccountNo.Contains(q) ||
                (e.ShareCategory != null && e.ShareCategory.NameTh.Contains(q)) ||
                (e.DoctorCode != null && e.DoctorCode.Code.Contains(q)));
        }

        return query;
    }

    public override void Apply(GlPostingSetup e, GlPostingSetupInput input, bool isCreate)
    {
        e.ShareCategoryId = input.ShareCategoryId;
        e.DebitAccountNo = input.DebitAccountNo.Trim();
        e.DebitDepartment = input.DebitDepartment?.Trim();
        e.CreditAccountNo = input.CreditAccountNo.Trim();
        e.CreditDepartment = input.CreditDepartment?.Trim();
        e.DoctorCodeId = input.DoctorCodeId;
        e.PostingDateRule = input.PostingDateRule;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(GlPostingSetup e, GlPostingSetupInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.ShareCategoryId, "shareCategoryId",
            "ประเภทส่วนแบ่ง");
        MasterFieldRules.Required(errors, input.DebitAccountNo, "debitAccountNo",
            "รหัสบัญชี (Debit)");
        MasterFieldRules.Required(errors, input.CreditAccountNo, "creditAccountNo",
            "รหัสบัญชี (Credit)");

        if (!string.IsNullOrWhiteSpace(input.DebitAccountNo) &&
            input.DebitAccountNo.Trim() == input.CreditAccountNo?.Trim() &&
            input.DebitDepartment?.Trim() == input.CreditDepartment?.Trim())
            errors.Add("creditAccountNo", "same_account",
                "รหัสบัญชี Debit และ Credit ต้องไม่ใช่บัญชีและแผนกเดียวกัน");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(GlPostingSetup e,
        GlPostingSetupInput input, bool isCreate, ValidationFailure errors,
        IRepository<GlPostingSetup> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (input.ShareCategoryId == Guid.Empty) return;

        var clash = repo.Query().Where(other =>
            other.Id != e.Id &&
            other.ShareCategoryId == input.ShareCategoryId &&
            other.DoctorCodeId == input.DoctorCodeId);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("shareCategoryId",
                input.DoctorCodeId is null
                    ? "ประเภทส่วนแบ่งนี้ตั้งค่าบันทึกบัญชีไว้แล้ว"
                    : "ประเภทส่วนแบ่งนี้ตั้งค่าไว้สำหรับแพทย์รายนี้แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<GlPostingSetupListItem>> ExportColumns =>
    [
        new("รหัสประเภทส่วนแบ่ง", r => r.ShareCategoryCode),
        new("ประเภทส่วนแบ่ง", r => r.ShareCategoryNameTh),
        new("รหัสบัญชี (Debit)", r => r.DebitAccountNo),
        new("แผนก (Debit)", r => r.DebitDepartment),
        new("รหัสบัญชี (Credit)", r => r.CreditAccountNo),
        new("แผนก (Credit)", r => r.CreditDepartment),
        new("ใช้เฉพาะแพทย์", r => r.DoctorCode),
        new("วันที่บันทึกบัญชี", r => PostingDateRuleTh(r.PostingDateRule)),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string PostingDateRuleTh(GlPostingDateRule rule) => rule switch
    {
        GlPostingDateRule.BatchDate => "วันที่ Batch Date",
        GlPostingDateRule.MonthEnd => "วันสิ้นสุดเดือน",
        GlPostingDateRule.PaymentDate => "วันที่จ่ายเงิน",
        _ => rule.ToString(),
    };
}

