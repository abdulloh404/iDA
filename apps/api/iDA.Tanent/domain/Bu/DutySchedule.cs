using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DutySchedule : TenantEntity
{
    public DutyScheduleKind Kind { get; set; } = DutyScheduleKind.Duty;

    public int Year { get; set; }

    public int Month { get; set; }

    public DutyScheduleStatus Status { get; set; } = DutyScheduleStatus.Draft;

    public DateTimeOffset? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }

    public DateTimeOffset? CalculatedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectReason { get; set; }

    public string? Remark { get; set; }

    public ICollection<DutyShift> Shifts { get; set; } = [];
}

public class DutyShift : TenantEntity
{
    public Guid ScheduleId { get; set; }

    public DateOnly ShiftDate { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? ClinicId { get; set; }

    public Guid? DutyRateId { get; set; }
    public Guid? GuaranteeRateId { get; set; }

    public string RoomLabel { get; set; } = string.Empty;

    public DutyPayKind PayKind { get; set; } = DutyPayKind.Normal;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public decimal? HourlyAmount { get; set; }

    public int RequiredDoctors { get; set; } = 1;

    public DutySchedule? Schedule { get; set; }
    public MstDepartment? Department { get; set; }
    public MstClinic? Clinic { get; set; }
    public DutyRate? DutyRate { get; set; }
    public GuaranteeRate? GuaranteeRate { get; set; }
    public ICollection<DutyShiftDoctor> Doctors { get; set; } = [];
}

public class DutyShiftDoctor : TenantEntity
{
    public Guid ShiftId { get; set; }

    public Guid DoctorCodeId { get; set; }

    public bool NoExam { get; set; }

    public DateTimeOffset? WorkStart { get; set; }
    public DateTimeOffset? WorkEnd { get; set; }

    public decimal? WorkHours { get; set; }

    public decimal? DeductAmount { get; set; }
    public string? Remark { get; set; }

    public DutyShift? Shift { get; set; }
    public DoctorCode? DoctorCode { get; set; }
}

