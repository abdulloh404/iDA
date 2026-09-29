using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class SysEmailTemplate : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}

public class SysPasswordPolicy : AuditableEntity
{

    public int ResetDays { get; set; } = 90;

    public int WarnDaysBefore { get; set; } = 7;
}

public class SysTerms : AuditableEntity
{

    public string Content { get; set; } = string.Empty;

    public int Version { get; set; } = 1;
}

