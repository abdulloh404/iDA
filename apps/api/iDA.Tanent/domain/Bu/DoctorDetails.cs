using System.ComponentModel.DataAnnotations.Schema;
using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DoctorBankAccount : TenantEntity, IHasProtectedSecrets
{
    public Guid DoctorId { get; set; }
    public Guid BankBranchId { get; set; }

    public byte[] AccountNoEnc { get; set; } = [];
    public string AccountNoLast4 { get; set; } = string.Empty;

    public string AccountNoHash { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "SAVING";

    public Guid PaymentTypeId { get; set; }

    public string? PayeeName { get; set; }
    public string? PfemVendorCode { get; set; }
    public string? BookBankDocUrl { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstPaymentType? PaymentType { get; set; }

    [NotMapped]
    public IDictionary<string, string?> PendingSecrets { get; } =
        new Dictionary<string, string?>(StringComparer.Ordinal);

    public void ApplySecret(string name, byte[]? cipher, string? hash, string? last4)
    {
        if (name != nameof(AccountNoEnc)) return;
        AccountNoEnc = cipher ?? [];
        AccountNoHash = hash ?? string.Empty;
        AccountNoLast4 = last4 ?? string.Empty;
    }
}

public class DoctorBankAccountHistory
{
    public long Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid DoctorId { get; set; }

    public string ChangeType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? ChangeReason { get; set; }

    public DoctorBankAccount? BankAccount { get; set; }
}

public class DoctorSpecialty : TenantEntity
{
    public Guid DoctorId { get; set; }
    public Guid? DoctorCodeId { get; set; }
    public Guid SpecialtyId { get; set; }
    public Guid? SubSpecialtyId { get; set; }

    public bool IsPrimary { get; set; }
    public string? OtherSpecialty { get; set; }
    public string? BoardCertNo { get; set; }
    public DateOnly? BoardCertDate { get; set; }
    public short? DisplaySeq { get; set; }
    public string[] PublishChannels { get; set; } = [];
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
}

public class DoctorContract : TenantEntity
{
    public Guid DoctorId { get; set; }
    public Guid? DoctorCodeId { get; set; }
    public string ContractNo { get; set; } = string.Empty;

    public string ContractType { get; set; } = "PRACTICE_SPACE";
    public string? ContractName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool AutoRenew { get; set; }
    public int? NoticeDays { get; set; }
    public decimal? GuaranteeAmount { get; set; }
    public string? DocumentUrl { get; set; }
    public DateOnly? SignedDate { get; set; }

    public string ContractStatus { get; set; } = "ACTIVE";
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
}

public class BuDoctorDocument : TenantEntity
{
    public Guid DoctorId { get; set; }
    public Guid DocTypeId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public bool HasExpiry { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public string? RefTable { get; set; }
    public Guid? RefId { get; set; }
    public string? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

}

public class DoctorWelfare : TenantEntity
{
    public Guid DoctorId { get; set; }
    public Guid WelfarePlanId { get; set; }
    public short WelfareYear { get; set; }
    public decimal AnnualLimit { get; set; }
    public decimal UsedAmount { get; set; }

    public decimal? RemainingAmount { get; set; }
    public string? DocumentUrl { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstWelfarePlan? WelfarePlan { get; set; }
    public ICollection<DoctorWelfareUsage> Usages { get; set; } = [];
}

public class DoctorWelfareUsage
{
    public long Id { get; set; }
    public Guid WelfareId { get; set; }
    public string PatientHn { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? RelationName { get; set; }
    public DateOnly VisitDate { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal UsedAmount { get; set; }
    public string? SourceSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DoctorWelfare? Welfare { get; set; }
}

public class DoctorSchedule
{
    public long Id { get; set; }
    public string HospitalId { get; set; } = string.Empty;
    public Guid DoctorCodeId { get; set; }
    public Guid? ClinicId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? RoomNo { get; set; }
    public string ScheduleType { get; set; } = "NORMAL";
    public string SourceSystem { get; set; } = "SSB";
    public string? SourceRef { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DoctorCode? DoctorCode { get; set; }
    public MstClinic? Clinic { get; set; }
}

public class DoctorScheduleOff
{
    public long Id { get; set; }
    public string HospitalId { get; set; } = string.Empty;
    public Guid DoctorCodeId { get; set; }
    public DateOnly OffDateFrom { get; set; }
    public DateOnly OffDateTo { get; set; }
    public string? Reason { get; set; }
    public string SourceSystem { get; set; } = "SSB";
    public DateTimeOffset SyncedAt { get; set; }

    public DoctorCode? DoctorCode { get; set; }
}

