using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.Accounting;

public record BankBranchListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? BankCode,
    string? BankNameTh,
    bool IsHeadOffice,
    RecordStatus Status);

public record BankBranchDetail(
    Guid Id,
    string Code,
    Guid BankId,
    string NameTh,
    string? NameEn,
    string? Address,
    string? ContactName,
    string? ContactPhone,
    string? ContactFax,
    string? ContactEmail,
    bool IsHeadOffice,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record BankBranchInput(
    string Code,
    Guid BankId,
    string NameTh,
    string? NameEn,
    string? Address,
    string? ContactName,
    string? ContactPhone,
    string? ContactFax,
    string? ContactEmail,
    bool IsHeadOffice,
    RecordStatus Status,
    string? Remark);

public sealed class BankBranchSpec
    : CrudSpec<MstBankBranch, BankBranchListItem, BankBranchDetail, BankBranchInput>
{
    public override string Resource => "bank-branches";
    public override string DisplayNameTh => "สาขาธนาคาร";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";
    public override bool IsGroupLevel => true;

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "bankId"];

    public override Expression<Func<MstBankBranch, bool>>? UniqueCodeScope(MstBankBranch e)
    {
        var bankId = e.BankId;
        return other => other.BankId == bankId;
    }

    public override string DuplicateCodeMessageTh => "รหัสสาขานี้ถูกใช้ในธนาคารนี้แล้ว";

    public override Expression<Func<MstBankBranch, BankBranchListItem>> ListProjection =>
        e => new BankBranchListItem(e.Id, e.Code, e.BranchNameTh, e.BranchNameEn,
            e.Bank == null ? null : e.Bank.Code,
            e.Bank == null ? null : e.Bank.BankNameTh,
            e.IsHeadOffice, e.Status);

    public override Expression<Func<MstBankBranch, BankBranchDetail>> DetailProjection =>
        e => new BankBranchDetail(e.Id, e.Code, e.BankId, e.BranchNameTh, e.BranchNameEn,
            e.Address, e.ContactName, e.ContactPhone, e.ContactFax, e.ContactEmail,
            e.IsHeadOffice, e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstBankBranch, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.BranchNameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstBankBranch, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstBankBranch, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.BranchNameTh,
            ["nameEn"] = e => e.BranchNameEn,
            ["bankCode"] = e => e.Bank == null ? null : e.Bank.Code,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstBankBranch> Search(IQueryable<MstBankBranch> query, ListRequest r)
    {
        if (r.Filter("bankId") is { } bankId && Guid.TryParse(bankId, out var id))
            query = query.Where(e => e.BankId == id);

        if (r.Filter("code") is { } code)
            query = query.Where(e => e.Code.Contains(code));

        if (r.Filter("nameTh") is { } nameTh)
            query = query.Where(e => e.BranchNameTh.Contains(nameTh));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.Code.Contains(q) ||
                e.BranchNameTh.Contains(q) ||
                (e.BranchNameEn != null && e.BranchNameEn.Contains(q)) ||
                (e.Bank != null && e.Bank.BankNameTh.Contains(q)));
        }

        return query;
    }

    public override void Apply(MstBankBranch e, BankBranchInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.BankId = input.BankId;
        e.BranchNameTh = input.NameTh.Trim();
        e.BranchNameEn = input.NameEn?.Trim();
        e.Address = input.Address?.Trim();
        e.ContactName = input.ContactName?.Trim();
        e.ContactPhone = input.ContactPhone?.Trim();
        e.ContactFax = input.ContactFax?.Trim();
        e.ContactEmail = input.ContactEmail?.Trim();
        e.IsHeadOffice = input.IsHeadOffice;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstBankBranch e, BankBranchInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสสาขาธนาคาร", 20);
        MasterFieldRules.RequiredId(errors, input.BankId, "bankId", "ธนาคาร");
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "ชื่อสาขา (ภาษาไทย)");

        var email = input.ContactEmail?.Trim();
        if (!string.IsNullOrEmpty(email) && !email.Contains('@'))
            errors.Add("contactEmail", "format", "รูปแบบอีเมลไม่ถูกต้อง");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<BankBranchListItem>> ExportColumns =>
    [
        new("รหัสธนาคาร", r => r.BankCode),
        new("ธนาคาร", r => r.BankNameTh),
        new("รหัสสาขา", r => r.Code),
        new("ชื่อสาขา (ไทย)", r => r.NameTh),
        new("ชื่อสาขา (อังกฤษ)", r => r.NameEn),
        new("สำนักงานใหญ่", r => r.IsHeadOffice ? "ใช่" : "ไม่ใช่"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

