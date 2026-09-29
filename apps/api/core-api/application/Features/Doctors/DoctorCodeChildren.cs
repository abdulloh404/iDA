using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.Doctors;

public record DoctorBankAccountRow(
    Guid Id, Guid DoctorId, string? BankName, string? BranchName, string AccountNoLast4,
    string AccountName, string AccountType, string? PaymentTypeName, DateOnly EffectiveFrom,
    bool IsActive, ApprovalStatus ApprovalStatus, RecordStatus Status);

public record DoctorBankAccountDetail(
    Guid Id, Guid DoctorId, Guid BankBranchId, string AccountNoLast4, string AccountName,
    string AccountType, Guid PaymentTypeId, string? PayeeName, string? PfemVendorCode,
    string? BookBankDocUrl, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive,
    ApprovalStatus ApprovalStatus, RecordStatus Status, string? Remark, string RowVersion);

public record DoctorBankAccountInput(
    Guid DoctorId, Guid BankBranchId, string? AccountNo, string AccountName, string AccountType,
    Guid PaymentTypeId, string? PayeeName, string? PfemVendorCode, string? BookBankDocUrl,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, RecordStatus Status,
    string? Remark);

public sealed class DoctorBankAccountSpec
    : CrudSpec<DoctorBankAccount, DoctorBankAccountRow, DoctorBankAccountDetail,
        DoctorBankAccountInput>
{
    public override string Resource => "doctor-bank-accounts";
    public override string DisplayNameTh => "บัญชีธนาคารของแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "-effectiveFrom";

    public override IReadOnlyList<string> FilterKeys => ["doctorId", "isActive"];

    public override Expression<Func<DoctorBankAccount, DoctorBankAccountRow>> ListProjection =>
        e => new DoctorBankAccountRow(e.Id, e.DoctorId,
            e.BankBranch == null || e.BankBranch.Bank == null ? null : e.BankBranch.Bank.BankNameTh,
            e.BankBranch == null ? null : e.BankBranch.BranchNameTh,
            e.AccountNoLast4, e.AccountName, e.AccountType,
            e.PaymentType == null ? null : e.PaymentType.NameTh,
            e.EffectiveFrom, e.IsActive, e.ApprovalStatus, e.Status);

    public override Expression<Func<DoctorBankAccount, DoctorBankAccountDetail>> DetailProjection =>
        e => new DoctorBankAccountDetail(e.Id, e.DoctorId, e.BankBranchId, e.AccountNoLast4,
            e.AccountName, e.AccountType, e.PaymentTypeId, e.PayeeName, e.PfemVendorCode,
            e.BookBankDocUrl, e.EffectiveFrom, e.EffectiveTo, e.IsActive, e.ApprovalStatus,
            e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorBankAccount, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorBankAccount, object?>>>
        {
            ["effectiveFrom"] = e => e.EffectiveFrom,
            ["accountName"] = e => e.AccountName,
            ["isActive"] = e => e.IsActive,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorBankAccount> Search(
        IQueryable<DoctorBankAccount> q, ListRequest r)
    {
        if (r.Filter("doctorId") is { } id && Guid.TryParse(id, out var doctorId))
            q = q.Where(e => e.DoctorId == doctorId);

        if (r.Filter("isActive") is { } active)
            q = q.Where(e => e.IsActive == (active == "true"));

        return q;
    }

    public override void Apply(DoctorBankAccount e, DoctorBankAccountInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.BankBranchId = input.BankBranchId;
        e.AccountName = input.AccountName.Trim();
        e.AccountType = input.AccountType;
        e.PaymentTypeId = input.PaymentTypeId;
        e.PayeeName = input.PayeeName?.Trim();
        e.PfemVendorCode = input.PfemVendorCode?.Trim();
        e.BookBankDocUrl = input.BookBankDocUrl?.Trim();
        e.EffectiveFrom = input.EffectiveFrom;
        e.EffectiveTo = input.EffectiveTo;
        e.IsActive = input.IsActive;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();

        e.PendingSecrets[nameof(DoctorBankAccount.AccountNoEnc)] = input.AccountNo;
    }

    public override Task ValidateAsync(DoctorBankAccount e, DoctorBankAccountInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.RequiredId(errors, input.BankBranchId, "bankBranchId", "สาขาธนาคาร");
        MasterFieldRules.RequiredId(errors, input.PaymentTypeId, "paymentTypeId",
            "ประเภทการจ่ายเงิน");
        MasterFieldRules.Required(errors, input.AccountName, "accountName", "ชื่อบัญชี");

        if (isCreate && string.IsNullOrWhiteSpace(input.AccountNo))
            errors.Required("accountNo", "โปรดระบุเลขที่บัญชีธนาคาร");

        var accountNo = input.AccountNo?.Trim();
        if (!string.IsNullOrEmpty(accountNo) && !accountNo.All(char.IsAsciiDigit))
            errors.Add("accountNo", "format", "เลขที่บัญชีต้องเป็นตัวเลขเท่านั้น");

        if (input.EffectiveTo is { } to && to < input.EffectiveFrom)
            errors.Add("effectiveTo", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorBankAccount e,
        DoctorBankAccountInput input, bool isCreate, ValidationFailure errors,
        IRepository<DoctorBankAccount> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (!input.IsActive) return;

        var doctorId = input.DoctorId;
        var clash = repo.Query().Where(o => o.Id != e.Id && o.DoctorId == doctorId && o.IsActive);

        if (await exec.AnyAsync(clash, ct))
            errors.Add("isActive", "duplicate",
                "แพทย์รายนี้มีบัญชีธนาคารที่ใช้งานอยู่แล้ว — ปิดบัญชีเดิมก่อนเปิดบัญชีใหม่");
    }
}

public record DoctorSpecialtyRow(
    Guid Id, Guid DoctorId, Guid? DoctorCodeId, string? SpecialtyName, string? SubSpecialtyName,
    bool IsPrimary, string? OtherSpecialty, string? BoardCertNo, RecordStatus Status);

public record DoctorSpecialtyDetail(
    Guid Id, Guid DoctorId, Guid? DoctorCodeId, Guid SpecialtyId, Guid? SubSpecialtyId,
    bool IsPrimary, string? OtherSpecialty, string? BoardCertNo, DateOnly? BoardCertDate,
    short? DisplaySeq, string[] PublishChannels, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    RecordStatus Status, string? Remark, string RowVersion);

public record DoctorSpecialtyInput(
    Guid DoctorId, Guid? DoctorCodeId, Guid SpecialtyId, Guid? SubSpecialtyId, bool IsPrimary,
    string? OtherSpecialty, string? BoardCertNo, DateOnly? BoardCertDate, short? DisplaySeq,
    string[] PublishChannels, DateOnly EffectiveFrom, DateOnly? EffectiveTo, RecordStatus Status,
    string? Remark);

public sealed class DoctorSpecialtySpec
    : CrudSpec<DoctorSpecialty, DoctorSpecialtyRow, DoctorSpecialtyDetail, DoctorSpecialtyInput>
{
    public override string Resource => "doctor-specialties";
    public override string DisplayNameTh => "ความเชี่ยวชาญของแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "displaySeq";

    public override IReadOnlyList<string> FilterKeys => ["doctorId", "doctorCodeId"];

    public override Expression<Func<DoctorSpecialty, DoctorSpecialtyRow>> ListProjection =>
        e => new DoctorSpecialtyRow(e.Id, e.DoctorId, e.DoctorCodeId,
            e.Specialty == null ? null : e.Specialty.SpecialtyNameTh,
            e.SubSpecialty == null ? null : e.SubSpecialty.SubSpecialtyNameTh,
            e.IsPrimary, e.OtherSpecialty, e.BoardCertNo, e.Status);

    public override Expression<Func<DoctorSpecialty, DoctorSpecialtyDetail>> DetailProjection =>
        e => new DoctorSpecialtyDetail(e.Id, e.DoctorId, e.DoctorCodeId, e.SpecialtyId,
            e.SubSpecialtyId, e.IsPrimary, e.OtherSpecialty, e.BoardCertNo, e.BoardCertDate,
            e.DisplaySeq, e.PublishChannels, e.EffectiveFrom, e.EffectiveTo, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorSpecialty, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorSpecialty, object?>>>
        {
            ["displaySeq"] = e => e.DisplaySeq,
            ["isPrimary"] = e => e.IsPrimary,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorSpecialty> Search(IQueryable<DoctorSpecialty> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } codeId && Guid.TryParse(codeId, out var dcId))
            q = q.Where(e => e.DoctorCodeId == dcId);

        if (r.Filter("doctorId") is { } id && Guid.TryParse(id, out var doctorId))
            q = q.Where(e => e.DoctorId == doctorId);

        return q;
    }

    public override void Apply(DoctorSpecialty e, DoctorSpecialtyInput input, bool isCreate)
    {
        if (isCreate) e.DoctorId = input.DoctorId;

        e.DoctorCodeId = input.DoctorCodeId;
        e.SpecialtyId = input.SpecialtyId;
        e.SubSpecialtyId = input.SubSpecialtyId;
        e.IsPrimary = input.IsPrimary;
        e.OtherSpecialty = input.OtherSpecialty?.Trim();
        e.BoardCertNo = input.BoardCertNo?.Trim();
        e.BoardCertDate = input.BoardCertDate;
        e.DisplaySeq = input.DisplaySeq;
        e.PublishChannels = input.PublishChannels;
        e.EffectiveFrom = input.EffectiveFrom;
        e.EffectiveTo = input.EffectiveTo;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DoctorSpecialty e, DoctorSpecialtyInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.RequiredId(errors, input.SpecialtyId, "specialtyId", "ความเชี่ยวชาญ");

        if (input.EffectiveTo is { } to && to < input.EffectiveFrom)
            errors.Add("effectiveTo", "range", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorSpecialty e,
        DoctorSpecialtyInput input, bool isCreate, ValidationFailure errors,
        IRepository<DoctorSpecialty> repo, IQueryExecutor exec, CancellationToken ct)
    {
        if (!input.IsPrimary) return;

        var doctorId = input.DoctorId;
        var clash = repo.Query().Where(o => o.Id != e.Id && o.DoctorId == doctorId && o.IsPrimary);

        if (await exec.AnyAsync(clash, ct))
            errors.Add("isPrimary", "duplicate", "แพทย์รายนี้มีความเชี่ยวชาญหลักอยู่แล้ว");
    }
}

public record DoctorContractRow(
    Guid Id, Guid DoctorId, Guid? DoctorCodeId, string ContractNo, string ContractType,
    string? ContractName, DateOnly StartDate, DateOnly? EndDate, decimal? GuaranteeAmount,
    string ContractStatus, RecordStatus Status);

public record DoctorContractDetail(
    Guid Id, Guid DoctorId, Guid? DoctorCodeId, string ContractNo, string ContractType,
    string? ContractName, DateOnly StartDate, DateOnly? EndDate, bool AutoRenew, int? NoticeDays,
    decimal? GuaranteeAmount, string? DocumentUrl, DateOnly? SignedDate, string ContractStatus,
    RecordStatus Status, string? Remark, string RowVersion);

public record DoctorContractInput(
    Guid DoctorId, Guid? DoctorCodeId, string ContractNo, string ContractType,
    string? ContractName, DateOnly StartDate, DateOnly? EndDate, bool AutoRenew, int? NoticeDays,
    decimal? GuaranteeAmount, string? DocumentUrl, DateOnly? SignedDate, string ContractStatus,
    RecordStatus Status, string? Remark);

public sealed class DoctorContractSpec
    : CrudSpec<DoctorContract, DoctorContractRow, DoctorContractDetail, DoctorContractInput>
{
    public override string Resource => "doctor-contracts";
    public override string DisplayNameTh => "สัญญาแพทย์";
    public override string Module => "doctors";
    public override string DefaultSort => "-startDate";

    public override IReadOnlyList<string> FilterKeys =>
        ["doctorId", "doctorCodeId", "contractType", "contractStatus"];

    public override Expression<Func<DoctorContract, DoctorContractRow>> ListProjection =>
        e => new DoctorContractRow(e.Id, e.DoctorId, e.DoctorCodeId, e.ContractNo, e.ContractType,
            e.ContractName, e.StartDate, e.EndDate, e.GuaranteeAmount, e.ContractStatus, e.Status);

    public override Expression<Func<DoctorContract, DoctorContractDetail>> DetailProjection =>
        e => new DoctorContractDetail(e.Id, e.DoctorId, e.DoctorCodeId, e.ContractNo,
            e.ContractType, e.ContractName, e.StartDate, e.EndDate, e.AutoRenew, e.NoticeDays,
            e.GuaranteeAmount, e.DocumentUrl, e.SignedDate, e.ContractStatus, e.Status, e.Remark,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DoctorContract, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DoctorContract, object?>>>
        {
            ["startDate"] = e => e.StartDate,
            ["contractNo"] = e => e.ContractNo,
            ["contractType"] = e => e.ContractType,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DoctorContract> Search(IQueryable<DoctorContract> q, ListRequest r)
    {
        if (r.Filter("doctorCodeId") is { } codeId && Guid.TryParse(codeId, out var dcId))
            q = q.Where(e => e.DoctorCodeId == dcId);

        if (r.Filter("doctorId") is { } id && Guid.TryParse(id, out var doctorId))
            q = q.Where(e => e.DoctorId == doctorId);

        if (r.Filter("contractType") is { } type)
            q = q.Where(e => e.ContractType == type);

        if (r.Filter("contractStatus") is { } status)
            q = q.Where(e => e.ContractStatus == status);

        return q;
    }

    public override void Apply(DoctorContract e, DoctorContractInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.ContractNo = input.ContractNo.Trim();
        }

        e.DoctorCodeId = input.DoctorCodeId;
        e.ContractType = input.ContractType;
        e.ContractName = input.ContractName?.Trim();
        e.StartDate = input.StartDate;
        e.EndDate = input.EndDate;
        e.AutoRenew = input.AutoRenew;
        e.NoticeDays = input.NoticeDays;
        e.GuaranteeAmount = input.GuaranteeAmount;
        e.DocumentUrl = input.DocumentUrl?.Trim();
        e.SignedDate = input.SignedDate;
        e.ContractStatus = input.ContractStatus;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DoctorContract e, DoctorContractInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.Required(errors, input.ContractNo, "contractNo", "เลขที่สัญญา");

        if (input.StartDate == default)
            errors.Required("startDate", "โปรดระบุวันที่เริ่มสัญญา");

        if (input.EndDate is { } end && end < input.StartDate)
            errors.Add("endDate", "range", "วันที่สิ้นสุดสัญญาต้องไม่ก่อนวันที่เริ่ม");

        if (input.GuaranteeAmount is < 0)
            errors.Add("guaranteeAmount", "range", "วงเงินประกันรายได้ต้องไม่ติดลบ");

        if (input.NoticeDays is < 0)
            errors.Add("noticeDays", "range", "จำนวนวันบอกกล่าวล่วงหน้าต้องไม่ติดลบ");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DoctorContract e,
        DoctorContractInput input, bool isCreate, ValidationFailure errors,
        IRepository<DoctorContract> repo, IQueryExecutor exec, CancellationToken ct)
    {
        var doctorId = input.DoctorId;
        var type = input.ContractType;
        var from = input.StartDate;
        var to = input.EndDate;

        var clash = repo.Query().Where(o =>
            o.Id != e.Id &&
            o.DoctorId == doctorId &&
            o.ContractType == type &&
            (to == null || o.StartDate <= to) &&
            (o.EndDate == null || o.EndDate >= from));

        if (await exec.AnyAsync(clash, ct))
            errors.Add("startDate", "overlap",
                "มีสัญญาชนิดเดียวกันของแพทย์รายนี้ที่ช่วงวันที่ทับกันอยู่แล้ว");
    }
}

public record BuDoctorDocumentRow(
    Guid Id, Guid DoctorId, string? DocTypeName, string DocumentName, bool HasExpiry,
    DateOnly? ExpiryDate, string? RefTable, string? UploadedBy, DateTimeOffset UploadedAt,
    RecordStatus Status);

public record BuDoctorDocumentDetail(
    Guid Id, Guid DoctorId, Guid DocTypeId, string DocumentName, string FileUrl, string? MimeType,
    bool HasExpiry, DateOnly? ExpiryDate, string? RefTable, Guid? RefId, string? UploadedBy,
    DateTimeOffset UploadedAt, RecordStatus Status, string RowVersion);

public record BuDoctorDocumentInput(
    Guid DoctorId, Guid DocTypeId, string DocumentName, string FileUrl, bool HasExpiry,
    DateOnly? ExpiryDate, string? RefTable, Guid? RefId, RecordStatus Status);

public sealed class BuDoctorDocumentSpec
    : CrudSpec<BuDoctorDocument, BuDoctorDocumentRow, BuDoctorDocumentDetail,
        BuDoctorDocumentInput>
{
    public override string Resource => "bu-doctor-documents";
    public override string DisplayNameTh => "เอกสารแพทย์ระดับโรงพยาบาล";
    public override string Module => "doctors";
    public override string DefaultSort => "-uploadedAt";

    public override IReadOnlyList<string> FilterKeys => ["doctorId", "refTable"];

    public override Expression<Func<BuDoctorDocument, BuDoctorDocumentRow>> ListProjection =>
        e => new BuDoctorDocumentRow(e.Id, e.DoctorId,
            e.DocType == null ? null : e.DocType.DocTypeNameTh,
            e.DocumentName, e.HasExpiry, e.ExpiryDate, e.RefTable, e.UploadedBy, e.UploadedAt,
            e.Status);

    public override Expression<Func<BuDoctorDocument, BuDoctorDocumentDetail>> DetailProjection =>
        e => new BuDoctorDocumentDetail(e.Id, e.DoctorId, e.DocTypeId, e.DocumentName, e.FileUrl,
            e.MimeType, e.HasExpiry, e.ExpiryDate, e.RefTable, e.RefId, e.UploadedBy,
            e.UploadedAt, e.Status, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<BuDoctorDocument, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<BuDoctorDocument, object?>>>
        {
            ["uploadedAt"] = e => e.UploadedAt,
            ["documentName"] = e => e.DocumentName,
            ["expiryDate"] = e => e.ExpiryDate,
            ["status"] = e => e.Status,
        };

    public override IQueryable<BuDoctorDocument> Search(
        IQueryable<BuDoctorDocument> q, ListRequest r)
    {
        if (r.Filter("doctorId") is { } id && Guid.TryParse(id, out var doctorId))
            q = q.Where(e => e.DoctorId == doctorId);

        if (r.Filter("refTable") is { } refTable)
            q = q.Where(e => e.RefTable == refTable);

        return q;
    }

    public override void Apply(BuDoctorDocument e, BuDoctorDocumentInput input, bool isCreate)
    {
        if (isCreate)
        {
            e.DoctorId = input.DoctorId;
            e.UploadedAt = DateTimeOffset.UtcNow;
        }

        e.DocTypeId = input.DocTypeId;
        e.DocumentName = input.DocumentName.Trim();
        e.FileUrl = input.FileUrl.Trim();
        e.HasExpiry = input.HasExpiry;
        e.ExpiryDate = input.HasExpiry ? input.ExpiryDate : null;
        e.RefTable = input.RefTable?.Trim();
        e.RefId = input.RefId;
        e.Status = input.Status;
    }

    public override Task ValidateAsync(BuDoctorDocument e, BuDoctorDocumentInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DoctorId, "doctorId", "แพทย์");
        MasterFieldRules.RequiredId(errors, input.DocTypeId, "docTypeId", "ประเภทเอกสาร");
        MasterFieldRules.Required(errors, input.DocumentName, "documentName", "ชื่อเอกสาร");
        MasterFieldRules.Required(errors, input.FileUrl, "fileUrl", "ไฟล์เอกสาร");

        if (input.HasExpiry && input.ExpiryDate is null)
            errors.Required("expiryDate", "เอกสารที่มีวันหมดอายุต้องระบุวันที่หมดอายุ");

        return Task.CompletedTask;
    }
}

