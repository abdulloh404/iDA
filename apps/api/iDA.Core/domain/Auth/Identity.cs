using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Domain.Auth;

public class AppUser : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? PhotoUrl { get; set; }

    public string AuthType { get; set; } = "LOCAL";
    public string? ExternalId { get; set; }

    public string? UserType { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? PasswordChangedAt { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public ICollection<UserHospitalRole> HospitalRoles { get; set; } = [];
}

public class Role : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameTh { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public bool IsGroupLevel { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public ICollection<RolePermission> Permissions { get; set; } = [];
}

public class Permission : AuditableEntity, ICodedEntity
{

    public string Code { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;
    public string NameTh { get; set; } = string.Empty;

    public bool IsGroupLevel { get; set; }
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}

public class UserHospitalRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public string HospitalId { get; set; } = string.Empty;
    public Guid RoleId { get; set; }

    public bool IsDefault { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public AppUser? User { get; set; }
    public Hospital? Hospital { get; set; }
    public Role? Role { get; set; }
}

public class AuthLog : IAuditExcluded
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;

    public string Event { get; set; } = string.Empty;
    public string? HospitalId { get; set; }
    public bool Succeeded { get; set; }
    public string? Reason { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

