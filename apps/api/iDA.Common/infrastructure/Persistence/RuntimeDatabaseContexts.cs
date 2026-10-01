using Ida.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ida.Infrastructure.Persistence;

public interface IRuntimeDatabaseContexts
{
    DbContext Current { get; }
    IEnumerable<DbContext> Created { get; }
    Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public interface IDatabaseErrorMapper
{
    ApiException ToApiException(string? constraintName);
}

public sealed class DefaultDatabaseErrorMapper : IDatabaseErrorMapper
{
    public ApiException ToApiException(string? constraintName) => ApiException.Conflict("duplicate_value", "ข้อมูลนี้ซ้ำกับรายการที่มีอยู่แล้ว", new { constraint = constraintName });
}
