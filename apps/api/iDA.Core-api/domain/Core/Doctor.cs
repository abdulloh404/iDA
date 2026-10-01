using System.ComponentModel.DataAnnotations.Schema;
using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class Doctor : AuditableEntity, IHasProtectedSecrets
{

    public string DoctorGlobalCode { get; set; } = string.Empty;

    public string? EpmsPersonId { get; set; }

    public Guid? TitleId { get; set; }
    public string FirstNameTh { get; set; } = string.Empty;
    public string LastNameTh { get; set; } = string.Empty;
    public string? FirstNameEn { get; set; }
    public string? LastNameEn { get; set; }

    public GenderType Gender { get; set; } = GenderType.U;
    public DateOnly? BirthDate { get; set; }
    public string? Nationality { get; set; }

    public IdDocType IdDocType { get; set; } = IdDocType.NationalId;
    public byte[]? NationalIdEnc { get; set; }
    public string? NationalIdLast4 { get; set; }

    public string? NationalIdHash { get; set; }
    public byte[]? PassportNoEnc { get; set; }
    public DateOnly? IdDocExpiryDate { get; set; }
    public bool IdDocNoExpiry { get; set; }

    public string? TaxId { get; set; }
    public TaxEntityType TaxEntityType { get; set; } = TaxEntityType.Individual;

    public bool IsCentralDoctor { get; set; }
    public string? HomeHospitalId { get; set; }

    public string? PhotoUrl { get; set; }
    public string? SpokenLanguages { get; set; }
    public bool TreatForeignPatient { get; set; }
    public DateOnly? FirstJoinDate { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public string SourceSystem { get; set; } = "MANUAL";
    public DateTimeOffset? SyncedAt { get; set; }

    public MstTitle? Title { get; set; }
    public Hospital? HomeHospital { get; set; }

    public ICollection<DoctorLicense> Licenses { get; set; } = [];
    public ICollection<DoctorContact> Contacts { get; set; } = [];
    public ICollection<DoctorAddress> Addresses { get; set; } = [];
    public ICollection<DoctorEducation> Educations { get; set; } = [];
    public ICollection<DoctorTraining> Trainings { get; set; } = [];
    public ICollection<DoctorWorkHistory> WorkHistories { get; set; } = [];
    public ICollection<DoctorAffiliation> Affiliations { get; set; } = [];
    public ICollection<DoctorFamily> Families { get; set; } = [];
    public ICollection<DoctorProfessionalRecord> ProfessionalRecords { get; set; } = [];
    public ICollection<DoctorInsurance> Insurances { get; set; } = [];
    public ICollection<DoctorDocument> Documents { get; set; } = [];
    public ICollection<DoctorHospitalLink> HospitalLinks { get; set; } = [];

    [NotMapped]
    public IDictionary<string, string?> PendingSecrets { get; } =
        new Dictionary<string, string?>(StringComparer.Ordinal);

    public void ApplySecret(string name, byte[]? cipher, string? hash, string? last4)
    {
        switch (name)
        {
            case nameof(NationalIdEnc):
                NationalIdEnc = cipher;
                NationalIdHash = hash;
                NationalIdLast4 = last4;
                break;
            case nameof(PassportNoEnc):

                PassportNoEnc = cipher;
                break;
        }
    }
}

