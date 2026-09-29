using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class ShareRate : TenantEntity
{
    public ShareRateLevel Level { get; set; }

    public SocialKind? SocialKind { get; set; }

    public string? PrivateCaseCode { get; set; }

    public string? PackageCode { get; set; }

    public string? Location { get; set; }

    public string? ActivityCode { get; set; }

    public string? Detail { get; set; }

    public Guid? ArCodeId { get; set; }
    public Guid? PatientRightId { get; set; }
    public Guid? DoctorCodeId { get; set; }
    public Guid? TreatmentId { get; set; }
    public Guid? TreatmentCategoryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public ShareTaxKind TaxKind { get; set; } = ShareTaxKind.Tax406;
    public ShareTaxBase TaxBase { get; set; } = ShareTaxBase.BeforeShare;
    public AdmissionType AdmissionType { get; set; } = AdmissionType.All;

    public ShareMode ShareMode { get; set; } = ShareMode.Percent;
    public decimal? SharePercent { get; set; }

    public decimal? FixPriceFrom { get; set; }
    public decimal? FixPriceTo { get; set; }

    public decimal? DoctorPayAmount { get; set; }

    public bool ExcludeXrayEkg { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool NoExpiry { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstArCode? ArCode { get; set; }
    public MstPatientRight? PatientRight { get; set; }
    public DoctorCode? DoctorCode { get; set; }
    public MstTreatment? Treatment { get; set; }
    public MstTreatmentCategory? TreatmentCategory { get; set; }
    public MstDepartment? Department { get; set; }

    public ICollection<ShareRateExclusion> Exclusions { get; set; } = [];

    public static ShareRateScheme SchemeOf(ShareRateLevel level) =>
        level >= ShareRateLevel.SocialArCode
            ? ShareRateScheme.SocialSecurity
            : ShareRateScheme.Premium;
}

public class ShareRateExclusion : TenantEntity
{
    public Guid RateId { get; set; }
    public Guid? DoctorCodeId { get; set; }
    public Guid? DoctorGroupId { get; set; }

    public ShareRate? Rate { get; set; }
    public DoctorCode? DoctorCode { get; set; }
    public MstDoctorGroup? DoctorGroup { get; set; }
}

public class MstPatientRight : TenantMasterEntity;

