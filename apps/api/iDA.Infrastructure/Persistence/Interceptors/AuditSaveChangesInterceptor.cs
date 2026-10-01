using System.Text.Json;
using Ida.Application.Common;
using Ida.Domain.Common;
using Ida.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ida.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor(
    ICurrentUser user,
    ITenantContext tenant,
    IClock clock,
    IRequestContext request)
    : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is { } context) Stamp(context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context) Stamp(context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext context)
    {
        var now = clock.Now;
        var who = user.IsAuthenticated ? user.UserName : "system";
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog or Domain.Auth.AuthLog) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Deleted => "DELETE",
                _ => "UPDATE",
            };

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = who;
                }
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = who;
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable soft)
            {
                entry.State = EntityState.Modified;
                soft.DeletedAt = now;
                soft.DeletedBy = who;
            }

            if (entry.Entity is TenantEntity owned &&
                string.IsNullOrEmpty(owned.HospitalId) && tenant.HasTenant)
                owned.HospitalId = tenant.HospitalId;

            if (Describe(entry, action, who, now) is { } log) logs.Add(log);
        }

        if (logs.Count > 0) context.Set<AuditLog>().AddRange(logs);
    }

    private AuditLog? Describe(EntityEntry entry, string action, string who, DateTimeOffset now)
    {
        var (oldValues, newValues) = Diff(entry, action);

        if (action == "UPDATE" && newValues.Count == 0) return null;

        var key = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        var pk = key is null ? null : entry.Property(key.Name).CurrentValue?.ToString();
        if (pk is null) return null;

        return new AuditLog
        {
            HospitalId = entry.Entity is TenantEntity t && !string.IsNullOrEmpty(t.HospitalId)
                ? t.HospitalId
                : tenant.HasTenant ? tenant.HospitalId : null,
            TableName = entry.Entity.GetType().Name,
            RecordPk = pk,
            Action = action,
            OldValue = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues, Json) : null,
            NewValue = newValues.Count > 0 ? JsonSerializer.Serialize(newValues, Json) : null,
            ChangedBy = who,
            ChangedAt = now,
            ClientIp = request.ClientIp,
            RequestId = request.TraceId,
        };
    }

    private static (Dictionary<string, object?> Old, Dictionary<string, object?> New) Diff(
        EntityEntry entry, string action)
    {
        Dictionary<string, object?> old = [];
        Dictionary<string, object?> current = [];

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (name is nameof(IAuditable.CreatedAt) or nameof(IAuditable.CreatedBy)
                or nameof(IAuditable.UpdatedAt) or nameof(IAuditable.UpdatedBy)) continue;

            if (property.Metadata.ClrType == typeof(byte[])) continue;

            var secret = AuditMasking.IsSecret(name);

            switch (action)
            {
                case "INSERT":
                    if (property.CurrentValue is not null)
                        current[name] = secret ? AuditMasking.Mask : Stringify(property.CurrentValue);
                    break;
                case "DELETE":
                    if (property.OriginalValue is not null)
                        old[name] = secret ? AuditMasking.Mask : Stringify(property.OriginalValue);
                    break;
                default:
                    if (!property.IsModified) break;
                    if (Equals(property.OriginalValue, property.CurrentValue)) break;
                    old[name] = secret ? AuditMasking.Mask : Stringify(property.OriginalValue);
                    current[name] = secret ? AuditMasking.Mask : Stringify(property.CurrentValue);
                    break;
            }
        }

        return (old, current);
    }

    private static object? Stringify(object? value) => value switch
    {
        null => null,
        string or bool or int or long or short or decimal or double => value,
        Enum e => e.ToString(),
        DateTimeOffset d => d.ToString("O"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        TimeOnly t => t.ToString("HH:mm:ss"),
        _ => value.ToString(),
    };
}

public static class AuditMasking
{
    public const string Mask = "<ปกปิด>";

    public static bool IsSecret(string field) =>
        field.EndsWith("Hash", StringComparison.Ordinal);
}
