using System.Text.Json;
using Ida.Application.Common;
using Ida.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Ida.Infrastructure.Persistence.Interceptors;
using Npgsql;

namespace Ida.Infrastructure.Persistence;

public class Repository<TEntity>(IRuntimeDatabaseContexts contexts) : IRepository<TEntity>
    where TEntity : class
{
    private DbContext Db => contexts.Current;

    public IQueryable<TEntity> Query() => Db.Set<TEntity>().AsNoTracking();

    public IQueryable<TEntity> Track() => Db.Set<TEntity>();

    public void Add(TEntity entity) => Db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity) => Db.Set<TEntity>().Remove(entity);

    public void SetConcurrencyToken(TEntity entity, string rowVersion)
    {
        if (!uint.TryParse(rowVersion, out var xmin))
            throw ApiException.BadRequest("invalid_row_version",
                "ค่าเวอร์ชันของข้อมูลไม่ถูกต้อง กรุณาโหลดข้อมูลใหม่");

        Db.Entry(entity).Property(nameof(IConcurrencyAware.RowVersion)).OriginalValue = xmin;
    }

    public string GetConcurrencyToken(TEntity entity) =>
        Db.Entry(entity).Property<uint>(nameof(IConcurrencyAware.RowVersion)).CurrentValue.ToString();
}

public class EfQueryExecutor : IQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.ToListAsync(ct);

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.CountAsync(ct);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.FirstOrDefaultAsync(ct);

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.AnyAsync(ct);
}

public class UnitOfWork(IRuntimeDatabaseContexts contexts, IDatabaseErrorMapper errors) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        foreach (var db in contexts.Created.Where(context => context.ChangeTracker.HasChanges()))
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {

                throw ApiException.ConcurrencyConflict();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException { SqlState: "23505" } pg)
            {

                throw errors.ToApiException(pg.ConstraintName);
            }
        }
    }
}

public class AuditTrail(IRuntimeDatabaseContexts contexts) : IAuditTrail
{
    public async Task<IReadOnlyList<AuditEntryDto>> ForAsync(string tableName, string recordPk,
        CancellationToken ct)
    {
        var db = contexts.Current;
        var rows = await db.Set<AuditLog>()
            .AsNoTracking()
            .Where(e => e.TableName == tableName && e.RecordPk == recordPk)
            .OrderByDescending(e => e.ChangedAt)
            .ThenByDescending(e => e.Id)
            .Take(200)
            .ToListAsync(ct);

        return [.. rows.Select(Convert).Where(e => e.Action != "UPDATE" || e.Changes.Count > 0)];
    }

    private static AuditEntryDto Convert(AuditLog log)
    {
        var before = Parse(log.OldValue);
        var after = Parse(log.NewValue);

        var changes = before.Keys.Union(after.Keys)
            .Where(f => !Technical.Contains(f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => new AuditFieldChange(
                f,
                AuditMasking.IsSecret(f) ? Mask(before.GetValueOrDefault(f)) : before.GetValueOrDefault(f),
                AuditMasking.IsSecret(f) ? Mask(after.GetValueOrDefault(f)) : after.GetValueOrDefault(f)))
            .ToList();

        return new AuditEntryDto(log.Id, log.Action, log.ChangedBy, log.ChangedAt, changes);
    }

    private static readonly HashSet<string> Technical = new(StringComparer.Ordinal)
    {
        "Id", "RowVersion", "HospitalId", "LastLoginAt",
    };

    private static string? Mask(string? value) => value is null ? null : AuditMasking.Mask;

    private static Dictionary<string, string?> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.EnumerateObject().ToDictionary(
                p => p.Name,
                p => p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ToString());
        }
        catch (JsonException)
        {

            return [];
        }
    }
}
