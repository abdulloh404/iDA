using System.ComponentModel.DataAnnotations.Schema;
using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class DoctorLicense : AuditableEntity
{
    public Guid DoctorId { get; set; }

    public string LicenseType { get; set; } = "MEDICAL";
    public string LicenseNo { get; set; } = string.Empty;
    public string? IssuedPlace { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsNoExpiry { get; set; }
    public string? Detail { get; set; }
    public DateOnly? ApprovedDate { get; set; }
    public string? DocumentUrl { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorContact : AuditableEntity
{
    public Guid DoctorId { get; set; }

    public string ContactType { get; set; } = "MOBILE";
    public string ContactValue { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorAddress : AuditableEntity
{
    public Guid DoctorId { get; set; }

    public string AddressType { get; set; } = "HOME";
    public string? AddrNo { get; set; }
    public string? Building { get; set; }
    public string? Soi { get; set; }
    public string? Road { get; set; }
    public string? Subdistrict { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? Postcode { get; set; }
    public string? Country { get; set; }
    public bool SameAsHome { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorEducation : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public short? StartYear { get; set; }
    public short? EndYear { get; set; }
    public string DegreeName { get; set; } = string.Empty;
    public string? InstituteName { get; set; }
    public string? Country { get; set; }
    public string? Remark { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorTraining : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public string? InstituteName { get; set; }
    public string? BudgetSource { get; set; }
    public string? BondContractNo { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorWorkHistory : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public short? StartYear { get; set; }
    public short? EndYear { get; set; }
    public string? PositionName { get; set; }
    public string? Workplace { get; set; }
    public string? Remark { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorAffiliation : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public string AffiliationName { get; set; } = string.Empty;
    public string? PositionName { get; set; }
    public bool IsPrimary { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorFamily : AuditableEntity, IHasProtectedSecrets
{
    public Guid DoctorId { get; set; }
    public string FullNameTh { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public byte[]? NationalIdEnc { get; set; }
    public string? NationalIdLast4 { get; set; }
    public RelationGroup RelationGroup { get; set; } = RelationGroup.Family;
    public string? RelationName { get; set; }
    public string? Hn { get; set; }
    public bool IsWelfareEligible { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }

    [NotMapped]
    public IDictionary<string, string?> PendingSecrets { get; } =
        new Dictionary<string, string?>(StringComparer.Ordinal);

    public void ApplySecret(string name, byte[]? cipher, string? hash, string? last4)
    {
        if (name != nameof(NationalIdEnc)) return;
        NationalIdEnc = cipher;
        NationalIdLast4 = last4;
    }
}

public class DoctorProfessionalRecord : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public string? RecordNo { get; set; }
    public DateOnly? RecordDate { get; set; }
    public string? HospitalName { get; set; }
    public string? Subject { get; set; }
    public string? RecordType { get; set; }
    public string? Conclusion { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorInsurance : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public string InsurerName { get; set; } = string.Empty;
    public string? PolicyNo { get; set; }
    public short? PolicyYear { get; set; }
    public decimal? CoverageAmount { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DocumentUrl { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
}

public class DoctorDocument : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public Guid DocTypeId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public int? FileSizeKb { get; set; }
    public string? MimeType { get; set; }
    public bool HasExpiry { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
    public MstDocumentType? DocType { get; set; }
}

public class DoctorHospitalLink : AuditableEntity
{
    public Guid DoctorId { get; set; }
    public string HospitalId { get; set; } = string.Empty;
    public bool IsHome { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public Doctor? Doctor { get; set; }
    public Hospital? Hospital { get; set; }
}

