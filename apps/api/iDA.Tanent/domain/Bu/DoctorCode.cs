using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DoctorCode : TenantEntity, ICodedEntity
{
    public Guid DoctorId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string? OldDoctorCode { get; set; }

    public bool IsCentralCode { get; set; }

    public string DisplayNameTh { get; set; } = string.Empty;
    public string? DisplayNameEn { get; set; }

    public string? TaxInvoiceNameTh { get; set; }
    public string? TaxInvoiceNameEn { get; set; }

    public string? BoardStatus { get; set; }
    public string? DefaultDfDoctorCode { get; set; }
    public bool IsDutyDoctor { get; set; }

    public string? WhtFormType { get; set; }

    public bool CanCheckinAnywhere { get; set; }

    public bool ArNoWaitPayment { get; set; }

    public bool CalcToHospitalNoPay { get; set; }

    public Guid? DoctorTypeId { get; set; }
    public Guid? DoctorGroupId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid? PrivilegeTypeId { get; set; }
    public Guid? StatusPrivilegeId { get; set; }

    public string? EmployeeCode { get; set; }
    public Guid? PaymentTypeId { get; set; }
    public Guid? BankAccountId { get; set; }

    public DateOnly? StartWorkDate { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Working;
    public DateOnly? ResignDate { get; set; }
    public string? PaymentCondition { get; set; }

    public bool HospitalAbsorbCcFee { get; set; }
    public decimal? CcFeePercent { get; set; }

    public string? TaxId { get; set; }

    public bool UseHomeTaxAddress { get; set; }
    public string? TaxAddrNo { get; set; }
    public string? TaxAddrBuilding { get; set; }
    public string? TaxAddrSoi { get; set; }
    public string? TaxAddrRoad { get; set; }
    public string? TaxAddrSubdistrict { get; set; }
    public string? TaxAddrDistrict { get; set; }
    public string? TaxAddrProvince { get; set; }
    public string? TaxAddrPostcode { get; set; }

    public string[] PublishChannels { get; set; } = [];

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstDoctorType? DoctorType { get; set; }
    public MstDoctorGroup? DoctorGroup { get; set; }
    public MstDepartment? Department { get; set; }
    public MstClinic? Clinic { get; set; }
    public MstPrivilegeType? PrivilegeType { get; set; }
    public MstStatusPrivilege? StatusPrivilege { get; set; }
    public MstPaymentType? PaymentType { get; set; }
    public DoctorBankAccount? BankAccount { get; set; }
}

