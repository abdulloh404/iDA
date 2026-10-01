using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Domain.Bu;

public interface IDecidableFee
{
    ApprovalStatus ApprovalStatus { get; set; }

    bool CycleClosed { get; set; }
    string? DecisionComment { get; set; }
    string? DecidedBy { get; set; }
    DateTimeOffset? DecidedAt { get; set; }
}

public class DfPositionFee : TenantEntity
{
    public Guid DoctorCodeId { get; set; }
    public Guid? ClinicId { get; set; }

    public string PositionName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal MonthlyAmount { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public MstClinic? Clinic { get; set; }
}

public class DfExternalFee : TenantEntity, IDecidableFee
{
    public ExternalFeeKind Kind { get; set; }

    public DateOnly RefDocDate { get; set; }
    public string RefDocNo { get; set; } = string.Empty;

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public Guid ArCodeId { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public bool CycleClosed { get; set; }
    public string? DecisionComment { get; set; }
    public string? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstArCode? ArCode { get; set; }
    public ICollection<DfExternalFeeLine> Lines { get; set; } = [];
}

public class DfExternalFeeLine : TenantEntity
{
    public Guid FeeId { get; set; }
    public DateOnly IssueDate { get; set; }
    public Guid DoctorCodeId { get; set; }
    public string? Description { get; set; }

    public bool CompareGuarantee { get; set; }

    public decimal Amount { get; set; }

    public string? AttachmentUrl { get; set; }

    public DfExternalFee? Fee { get; set; }
    public DoctorCode? DoctorCode { get; set; }
}

public class DfFeeItem : TenantEntity, IDecidableFee
{
    public DateOnly RefDocDate { get; set; }
    public string RefDocNo { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public FeeItemType ItemType { get; set; }
    public Guid DoctorCodeId { get; set; }
    public Guid? DepartmentId { get; set; }

    public Guid? ArCodeId { get; set; }
    public decimal Amount { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public bool CycleClosed { get; set; }
    public string? DecisionComment { get; set; }
    public string? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public MstDepartment? Department { get; set; }
    public MstArCode? ArCode { get; set; }
}

public class DfHospitalPaidTax : TenantEntity
{
    public Guid DoctorCodeId { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
}

public class DfTaxDeduction : TenantEntity
{
    public Guid DoctorCodeId { get; set; }

    public short TaxYear { get; set; }

    public int ChildCount { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public ICollection<DfTaxDeductionItem> Items { get; set; } = [];
}

public class DfTaxDeductionItem : TenantEntity
{
    public Guid DeductionId { get; set; }
    public Guid TaxAllowanceItemId { get; set; }
    public decimal Amount { get; set; }

    public DfTaxDeduction? Deduction { get; set; }
    public TaxAllowanceItem? TaxAllowanceItem { get; set; }
}

public class DfTaxExemption : TenantEntity
{
    public Guid DoctorCodeId { get; set; }
    public short TaxYear { get; set; }
    public bool IsExempt { get; set; } = true;

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
}

