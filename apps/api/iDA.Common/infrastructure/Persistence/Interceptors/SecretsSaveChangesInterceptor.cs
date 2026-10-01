using Ida.Application.Common;
using Ida.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ida.Infrastructure.Persistence.Interceptors;

public class SecretsSaveChangesInterceptor(ISecretProtector protector) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is { } context) Protect(context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context) Protect(context);
        return base.SavingChanges(eventData, result);
    }

    private void Protect(DbContext context)
    {
        var entries = context.ChangeTracker.Entries<IHasProtectedSecrets>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            if (entity.PendingSecrets.Count == 0) continue;

            foreach (var (column, plain) in entity.PendingSecrets)
            {

                if (plain is null) continue;

                if (plain.Length == 0)
                {
                    entity.ApplySecret(column, null, null, null);
                    continue;
                }

                var trimmed = plain.Trim();
                entity.ApplySecret(column,
                    protector.Encrypt(trimmed),
                    protector.Hash(trimmed),
                    protector.Last4(trimmed));
            }

            entity.PendingSecrets.Clear();
        }
    }
}
