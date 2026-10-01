using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DoctorApprovalRequest : TenantEntity
{

    public string RequestNo { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string TargetTable { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public Guid? DoctorId { get; set; }

    public string Payload { get; set; } = "{}";
    public string RequestedBy { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }
    public ApprovalStatus CurrentStatus { get; set; } = ApprovalStatus.Pending;
    public DateTimeOffset? ClosedAt { get; set; }

    public ICollection<DoctorApprovalStep> Steps { get; set; } = [];
}

public class DoctorApprovalStep
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public short StepSeq { get; set; }

    public string ApproverRole { get; set; } = string.Empty;
    public string? ApproverUser { get; set; }

    public string? Action { get; set; }
    public DateTimeOffset? ActionAt { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }

    public DoctorApprovalRequest? Request { get; set; }
}

public class DlExportOutbox
{
    public long Id { get; set; }
    public string HospitalId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ExportedAt { get; set; }

    public string ExportStatus { get; set; } = "PENDING";
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

