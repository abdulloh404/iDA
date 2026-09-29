using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.IncomeDocuments;

public record SlipSettingListItem(
    Guid Id,
    string? DoctorCode,
    string? DoctorName,
    string Email,
    string? BackupEmail,
    bool SendPayslip,
    bool SendTaxCertificate406,
    bool SendTaxCertificate50Tawi,
    bool HasPdfPassword,
    RecordStatus Status);

public record SlipSettingDetail(
    Guid Id,
    Guid DoctorCodeId,
    string Email,
    string? BackupEmail,
    bool HasPdfPassword,
    bool SendPayslip,
    bool SendTaxCertificate406,
    bool SendTaxCertificate50Tawi,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record SlipSettingInput(
    Guid DoctorCodeId,
    string? Email,
    string? BackupEmail,
    string? PdfPassword,
    bool SendPayslip,
    bool SendTaxCertificate406,
    bool SendTaxCertificate50Tawi,
    RecordStatus Status,
    string? Remark);

public sealed class SlipSettingSpec
    : CrudSpec<DocSlipSetting, SlipSettingListItem, SlipSettingDetail, SlipSettingInput>
{
    public const int PdfPasswordMin = 4;
    public const int PdfPasswordMax = 20;

    public override string Resource => "slip-settings";
    public override string DisplayNameTh => "ตั้งค่าการออกสลิป";
    public override string Module => "income-documents";
    public override string DefaultSort => "doctorCode";

    public override IReadOnlyList<string> FilterKeys => ["doctorCodeId", "report"];

    public override Expression<Func<DocSlipSetting, SlipSettingListItem>> ListProjection =>
        e => new SlipSettingListItem(e.Id,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            e.Email, e.BackupEmail,
            e.SendPayslip, e.SendTaxCertificate406, e.SendTaxCertificate50Tawi,
            e.PdfPasswordEnc != null, e.Status);

    public override Expression<Func<DocSlipSetting, SlipSettingDetail>> DetailProjection =>
        e => new SlipSettingDetail(e.Id, e.DoctorCodeId, e.Email, e.BackupEmail,
            e.PdfPasswordEnc != null,
            e.SendPayslip, e.SendTaxCertificate406, e.SendTaxCertificate50Tawi,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DocSlipSetting, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DocSlipSetting, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["doctorName"] = e => e.DoctorCode == null ? null : e.DoctorCode.DisplayNameTh,
            ["email"] = e => e.Email,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DocSlipSetting> Search(IQueryable<DocSlipSetting> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        q = r.Filter("report") switch
        {
            "PAYSLIP" => q.Where(e => e.SendPayslip),
            "TAX_CERTIFICATE_406" => q.Where(e => e.SendTaxCertificate406),
            "TAX_CERTIFICATE_50_TAWI" => q.Where(e => e.SendTaxCertificate50Tawi),
            _ => q,
        };

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.Email.Contains(text) ||
                (e.BackupEmail != null && e.BackupEmail.Contains(text)) ||
                (e.DoctorCode != null &&
                 (e.DoctorCode.Code.Contains(text) || e.DoctorCode.DisplayNameTh.Contains(text))));
        }

        return q;
    }

    public override void Apply(DocSlipSetting e, SlipSettingInput input, bool isCreate)
    {
        e.DoctorCodeId = input.DoctorCodeId;
        e.Email = NormaliseEmail(input.Email) ?? string.Empty;
        e.BackupEmail = NormaliseEmail(input.BackupEmail);
        e.SendPayslip = input.SendPayslip;
        e.SendTaxCertificate406 = input.SendTaxCertificate406;
        e.SendTaxCertificate50Tawi = input.SendTaxCertificate50Tawi;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();

        if (!string.IsNullOrWhiteSpace(input.PdfPassword))
            e.PendingSecrets[nameof(DocSlipSetting.PdfPasswordEnc)] = input.PdfPassword.Trim();
    }

    public override Task ValidateAsync(DocSlipSetting e, SlipSettingInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์");
        MasterFieldRules.Email(errors, input.Email, "email", "อีเมล", required: true);
        MasterFieldRules.Email(errors, input.BackupEmail, "backupEmail", "อีเมลสำรอง",
            required: false);

        if (NormaliseEmail(input.BackupEmail) is { } backup && backup == NormaliseEmail(input.Email))
            errors.Add("backupEmail", "same_as_email", "อีเมลสำรองต้องไม่ซ้ำกับอีเมลหลัก");

        if (!string.IsNullOrWhiteSpace(input.PdfPassword))
        {
            var length = input.PdfPassword.Trim().Length;
            if (length is < PdfPasswordMin or > PdfPasswordMax)
                errors.Add("pdfPassword", "length",
                    $"รหัสผ่านสำหรับเปิดไฟล์ PDF ต้องยาว {PdfPasswordMin}–{PdfPasswordMax} ตัวอักษร");
        }

        if (!input.SendPayslip && !input.SendTaxCertificate406 && !input.SendTaxCertificate50Tawi)
            errors.Add("sendPayslip", "required", "โปรดเลือกรายงานที่ส่งอย่างน้อย 1 รายการ");

        MasterFieldRules.MaxLength(errors, input.Remark, "remark", "หมายเหตุ", 1000);
        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DocSlipSetting e, SlipSettingInput input,
        bool isCreate, ValidationFailure errors, IRepository<DocSlipSetting> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        if (await exec.AnyAsync(repo.Query().Where(o =>
                o.Id != e.Id && o.DoctorCodeId == input.DoctorCodeId), ct))
            errors.Add("doctorCodeId", "duplicate", "แพทย์ท่านนี้มีการตั้งค่าการออกสลิปอยู่แล้ว");
    }

    public override IReadOnlyList<ExcelColumn<SlipSettingListItem>> ExportColumns =>
    [
        new("รหัสแพทย์", r => r.DoctorCode),
        new("แพทย์", r => r.DoctorName),
        new("อีเมล", r => r.Email),
        new("อีเมลสำรอง", r => r.BackupEmail),
        new("ส่งสลิปเงินเดือน", r => r.SendPayslip ? "ส่ง" : "ไม่ส่ง"),
        new("ส่งหนังสือรับรอง 40(6)", r => r.SendTaxCertificate406 ? "ส่ง" : "ไม่ส่ง"),
        new("ส่งหนังสือรับรอง 50 ทวิ", r => r.SendTaxCertificate50Tawi ? "ส่ง" : "ไม่ส่ง"),
        new("ตั้งรหัสผ่าน PDF", r => r.HasPdfPassword ? "ตั้งแล้ว" : "ยังไม่ตั้ง"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    internal static string? NormaliseEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}

