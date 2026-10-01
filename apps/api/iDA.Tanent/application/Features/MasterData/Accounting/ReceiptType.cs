using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.MasterData.Accounting;

public record ReceiptTypeListItem(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    ReceiptPaymentForm PaymentForm,
    string? BankCode,
    bool IsCharged,
    decimal? VatPercent,
    RecordStatus Status);

public record ReceiptTypeDetail(
    Guid Id,
    string Code,
    string NameTh,
    string? NameEn,
    Guid? BankId,
    ReceiptPaymentForm PaymentForm,
    bool IsCharged,
    decimal? VatPercent,
    string SourceSystem,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record ReceiptTypeInput(
    string Code,
    string NameTh,
    string? NameEn,
    Guid? BankId,
    ReceiptPaymentForm PaymentForm,
    bool IsCharged,
    decimal? VatPercent,
    RecordStatus Status,
    string? Remark);

public sealed class ReceiptTypeSpec
    : CrudSpec<MstReceiptType, ReceiptTypeListItem, ReceiptTypeDetail, ReceiptTypeInput>
{
    public override string Resource => "receipt-types";
    public override string DisplayNameTh => "ประเภทการรับเงิน";
    public override string Module => "master-data-accounting";
    public override string DefaultSort => "code";

    public override IReadOnlyList<string> FilterKeys =>
        ["code", "nameTh", "paymentForm", "isCharged"];

    public override Expression<Func<MstReceiptType, ReceiptTypeListItem>> ListProjection =>
        e => new ReceiptTypeListItem(e.Id, e.Code, e.NameTh, e.NameEn, e.PaymentForm,
            null,
            e.IsCharged, e.VatPercent, e.Status);

    public override async Task<IReadOnlyList<ReceiptTypeListItem>> EnrichListAsync(
        IReadOnlyList<ReceiptTypeListItem> items, ICrudRelatedData related, CancellationToken ct)
    {
        var ids = items.Select(e => e.Id).ToArray();
        var links = await related.ToListAsync(related.Query<MstReceiptType>()
            .Where(e => ids.Contains(e.Id)).Select(e => new { e.Id, e.BankId }), ct);
        var bankIds = links.Where(e => e.BankId != null).Select(e => e.BankId!.Value).Distinct().ToArray();
        var banks = await related.Core.BanksAsync(bankIds, ct);
        var codes = banks.ToDictionary(e => e.Id, e => e.Code);
        var linkById = links.ToDictionary(e => e.Id, e => e.BankId);
        return items.Select(e => linkById.TryGetValue(e.Id, out var bankId) && bankId is { } id
            ? e with { BankCode = codes.GetValueOrDefault(id) }
            : e).ToList();
    }

    public override Expression<Func<MstReceiptType, ReceiptTypeDetail>> DetailProjection =>
        e => new ReceiptTypeDetail(e.Id, e.Code, e.NameTh, e.NameEn, e.BankId, e.PaymentForm,
            e.IsCharged, e.VatPercent, e.SourceSystem, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override Expression<Func<MstReceiptType, LookupItem>>? LookupProjection =>
        e => new LookupItem(e.Id.ToString(), e.Code, e.NameTh);

    public override IReadOnlyDictionary<string, Expression<Func<MstReceiptType, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<MstReceiptType, object?>>>
        {
            ["code"] = e => e.Code,
            ["nameTh"] = e => e.NameTh,
            ["paymentForm"] = e => e.PaymentForm,
            ["isCharged"] = e => e.IsCharged,
            ["vatPercent"] = e => e.VatPercent,
            ["status"] = e => e.Status,
        };

    public override IQueryable<MstReceiptType> Search(IQueryable<MstReceiptType> query,
        ListRequest r)
    {
        if (r.Enum<ReceiptPaymentForm>("paymentForm") is { } parsed)
            query = query.Where(e => e.PaymentForm == parsed);

        if (r.Filter("isCharged") is { } charged)
            query = query.Where(e => e.IsCharged == (charged == "true"));

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

    public override void Apply(MstReceiptType e, ReceiptTypeInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.Code = input.Code.Trim();
            e.SourceSystem = "MANUAL";
        }

        e.NameTh = input.NameTh.Trim();
        e.NameEn = input.NameEn?.Trim();
        e.BankId = input.BankId;
        e.PaymentForm = input.PaymentForm;
        e.IsCharged = input.IsCharged;
        e.VatPercent = input.VatPercent;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(MstReceiptType e, ReceiptTypeInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.Code(errors, input.Code, "รหัสประเภทการรับเงิน", 20);
        MasterFieldRules.Required(errors, input.NameTh, "nameTh", "รายละเอียด (ภาษาไทย)");

        if (input.VatPercent is < 0 or > 100)
            errors.Add("vatPercent", "range", "ภาษีมูลค่าเพิ่มต้องอยู่ระหว่าง 0 ถึง 100");

        if (input.PaymentForm is ReceiptPaymentForm.CreditCard or ReceiptPaymentForm.Cheque &&
            input.BankId is null)
            errors.Required("bankId", "รูปแบบการชำระเงินนี้ต้องระบุธนาคาร");

        return Task.CompletedTask;
    }

    public override IReadOnlyList<ExcelColumn<ReceiptTypeListItem>> ExportColumns =>
    [
        new("รหัสประเภทการรับเงิน", r => r.Code),
        new("รายละเอียด (ไทย)", r => r.NameTh),
        new("รายละเอียด (อังกฤษ)", r => r.NameEn),
        new("รูปแบบการชำระเงิน", r => PaymentFormTh(r.PaymentForm)),
        new("รหัสธนาคาร", r => r.BankCode),
        new("สถานะประเภทการรับเงิน", r => r.IsCharged ? "ชาร์จ" : "ไม่ชาร์จ"),
        new("ภาษีมูลค่าเพิ่ม (%)", r => r.VatPercent, "#,##0.00"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string PaymentFormTh(ReceiptPaymentForm form) => form switch
    {
        ReceiptPaymentForm.Ar => "AR (ตั้งหนี้)",
        ReceiptPaymentForm.Cash => "CASH (เงินสด)",
        ReceiptPaymentForm.Cheque => "CQ (เช็ค)",
        ReceiptPaymentForm.CreditCard => "CREDIT CARD",
        ReceiptPaymentForm.Dp => "DP (เงินมัดจำ)",
        ReceiptPaymentForm.Dpc => "DPC (ตัดเงินมัดจำ)",
        ReceiptPaymentForm.Invoice => "INVOICE",
        _ => form.ToString(),
    };
}
