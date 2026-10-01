using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record PaymentTypeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    bool RequireBankAccount,
    RecordStatus Status);

public record PaymentTypeDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    bool RequireBankAccount,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record PaymentTypeInput(
    string Code,
    string NameTh,
    string? NameEn,
    bool RequireBankAccount,
    RecordStatus Status,
    string? Remark);

public sealed class PaymentTypeSpec
    : CrudSpec<MstPaymentType, PaymentTypeListItem, PaymentTypeDetail, PaymentTypeInput>
{
    public override string Resource => "payment-types";
    public override string DisplayNameTh => "ประเภทการจ่ายเงิน";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys => ["code", "nameTh", "requireBankAccount"];

    public override Expression<Func<MstPaymentType, PaymentTypeListItem>> ListProjection =>
        e => new PaymentTypeListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.RequireBankAccount,
            e.Status);

    public override Expression<Func<MstPaymentType, PaymentTypeDetail>> DetailProjection =>
        e => new PaymentTypeDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.RequireBankAccount,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override Expression<Func<MstPaymentType, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstPaymentType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstPaymentType, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["nameEn"] = e => e.NameEn,
            ["requireBankAccount"] = e => e.RequireBankAccount,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstPaymentType> Search(IQueryable<MstPaymentType> query,
        ListRequest r)
    {
        if (r.Filter("requireBankAccount") is { } requires)
            query = query.Where(e => e.RequireBankAccount == (requires == "true"));

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

    public override void Apply(MstPaymentType e, PaymentTypeInput input, bool isCreate)
    {
        if (isCreate) e.Code = input.Code.Trim();

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.RequireBankAccount = input.RequireBankAccount;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstPaymentType e, PaymentTypeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทการจ่ายเงิน", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh",
            "ชื่อประเภทการจ่ายเงิน (ภาษาไทย)");
        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<PaymentTypeListItem>> ExportColumns =>
    [
        new("รหัสประเภทการจ่ายเงิน", r => r.Code),
        new("ชื่อประเภทการจ่ายเงิน (ไทย)", r => r.NameTh),
        new("ชื่อประเภทการจ่ายเงิน (อังกฤษ)", r => r.NameEn),
        new("ต้องมีบัญชีธนาคาร", r => r.RequireBankAccount ? "ใช่" : "ไม่ใช่"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];
}

