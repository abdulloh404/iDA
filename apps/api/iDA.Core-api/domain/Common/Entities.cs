namespace Ida.Domain.Common;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}

public interface IEntity : IEntity<Guid>;

public interface ICodedEntity
{
    string Code { get; set; }
}

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

public interface IConcurrencyAware
{
    uint RowVersion { get; set; }
}

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    string UpdatedBy { get; set; }
}

public abstract class AuditableEntity : IEntity, IAuditable, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public uint RowVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public abstract class TenantEntity : AuditableEntity
{
    public string HospitalId { get; set; } = string.Empty;
}

public abstract class TenantMasterEntity : TenantEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameTh { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public interface IHasProtectedSecrets
{

    IDictionary<string, string?> PendingSecrets { get; }

    void ApplySecret(string name, byte[]? cipher, string? hash, string? last4);
}

