using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class AuditLog
{
    public long Id { get; set; }

    public string? HospitalId { get; set; }

    public string TableName { get; set; } = string.Empty;
    public string RecordPk { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? ClientIp { get; set; }
    public string? RequestId { get; set; }
}

