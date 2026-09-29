using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DutyHolidayRate : TenantEntity
{
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal? SpecialHours { get; set; }

    public HolidayPayMode PayMode { get; set; } = HolidayPayMode.Multiplier;

    public decimal? PayMultiplier { get; set; }

    public decimal? BoardHourlyAmount { get; set; }
    public decimal? NonBoardHourlyAmount { get; set; }

    public Guid? ClinicId { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstClinic? Clinic { get; set; }
    public ICollection<DutyHolidayExclusion> Exclusions { get; set; } = [];
}

public class DutyHolidayExclusion : TenantEntity
{
    public Guid HolidayRateId { get; set; }
    public Guid DoctorCodeId { get; set; }

    public DutyHolidayRate? HolidayRate { get; set; }
    public DoctorCode? DoctorCode { get; set; }
}

public class DutyRate : TenantEntity
{
    public Guid DepartmentId { get; set; }
    public DutyRoom Room { get; set; } = DutyRoom.Room1;

    public string? RoomOther { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public DutyPayKind PayKind { get; set; } = DutyPayKind.Normal;

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstDepartment? Department { get; set; }
    public ICollection<DutyRateDay> Days { get; set; } = [];
}

public class DutyRateDay : TenantEntity
{
    public Guid DutyRateId { get; set; }

    public short DayOfWeek { get; set; }
    public decimal? NonBoardHourlyAmount { get; set; }
    public decimal? BoardHourlyAmount { get; set; }

    public DutyRate? DutyRate { get; set; }
}

public class GuaranteeRate : TenantEntity
{
    public GuaranteeKind Kind { get; set; }
    public Guid DoctorCodeId { get; set; }

    public Guid? DepartmentId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public decimal? IncomeAmount { get; set; }

    public decimal? SurplusStartRate { get; set; }

    public GuaranteeCalcMode? CalcMode { get; set; }

    public bool CompareWholeMonth406 { get; set; }

    public DateOnly? CompareInvoiceFrom { get; set; }
    public WorkTimeRule WorkTimeRule { get; set; } = WorkTimeRule.ByWorkTime;

    public int? CheckinToleranceMinutes { get; set; }
    public AdmissionType AdmissionType { get; set; } = AdmissionType.All;
    public IncomeBase IncomeBase { get; set; } = IncomeBase.BeforeShare;
    public GuaranteeBasis Basis { get; set; } = GuaranteeBasis.AccrualBasic;
    public CreditCardFeeBase CreditCardFeeBase { get; set; } = CreditCardFeeBase.BeforeFee;

    public Guid? CompareDoctorCodeId { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public DoctorCode? CompareDoctorCode { get; set; }
    public MstDepartment? Department { get; set; }
    public ICollection<GuaranteeRateTreatment> Treatments { get; set; } = [];
    public ICollection<GuaranteeRateDay> Days { get; set; } = [];
}

public class GuaranteeRateTreatment : TenantEntity
{
    public Guid GuaranteeRateId { get; set; }
    public Guid TreatmentId { get; set; }
    public TreatmentScopeKind Scope { get; set; } = TreatmentScopeKind.Include;

    public GuaranteeRate? GuaranteeRate { get; set; }
    public MstTreatment? Treatment { get; set; }
}

public class GuaranteeRateDay : TenantEntity
{
    public Guid GuaranteeRateId { get; set; }

    public short DayOfWeek { get; set; }
    public decimal? IncomeAmount { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public bool IsExcluded { get; set; }

    public GuaranteeRate? GuaranteeRate { get; set; }
}

