namespace Ida.Application.Common;

public interface IGreetingProvider
{
    string Greet(string name);
}

public interface IRepository<TEntity> where TEntity : class
{

    IQueryable<TEntity> Query();

    IQueryable<TEntity> Track();

    void Add(TEntity entity);

    void SetConcurrencyToken(TEntity entity, string rowVersion);

    string GetConcurrencyToken(TEntity entity);

    void Remove(TEntity entity);
}

public interface IQueryExecutor
{
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct);
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct);
    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct);

}

public interface ICrudRelatedData
{
    ICoreDirectory Core { get; }

    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IUserActivity
{
    Task MarkLoginAsync(Guid userId, DateTimeOffset at, CancellationToken ct);
}

public interface IReferenceGuard
{

    Task<string?> WhyCannotDeleteAsync(object entity, CancellationToken ct);
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    string UserName { get; }
    string DisplayName { get; }
    IReadOnlyCollection<string> Permissions { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool Can(string permission);
    bool IsInRole(string role);
}

public interface ITenantContext
{

    string HospitalId { get; }
    bool HasTenant { get; }
    IReadOnlyCollection<string> AllowedHospitals { get; }
}

public record AuditEntryDto(
    long Id,
    string Action,
    string ChangedBy,
    DateTimeOffset ChangedAt,
    IReadOnlyList<AuditFieldChange> Changes);

public record AuditFieldChange(string Field, string? OldValue, string? NewValue);

public interface IAuditTrail
{
    Task<IReadOnlyList<AuditEntryDto>> ForAsync(string tableName, string recordPk,
        CancellationToken ct);
}

public record ExcelColumn<T>(string Header, Func<T, object?> Value, string? Format = null);

public interface IExcelWriter
{
    byte[] Write<T>(IEnumerable<T> rows, IReadOnlyList<ExcelColumn<T>> columns, string sheetName);
}

public interface ISecretProtector
{
    byte[] Encrypt(string plaintext);
    string Decrypt(byte[] ciphertext);
    string Hash(string plaintext);
    string Last4(string plaintext);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public record AuthTokenResult(string Token, DateTimeOffset ExpiresAt);

public record TokenSubject(
    Guid UserId,
    string Username,
    string DisplayName,
    string HospitalId,
    IReadOnlyCollection<string> Hospitals,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public interface ITokenService
{
    AuthTokenResult Issue(TokenSubject subject);
}

public interface IClock
{
    DateTimeOffset Now { get; }
}

public interface IRequestContext
{
    string? ClientIp { get; }
    string? UserAgent { get; }
    string? TraceId { get; }
}
