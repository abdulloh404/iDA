using System.Linq.Expressions;
using System.Reflection;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ida.Infrastructure.Persistence;

public class ReferenceGuard(
    DatabaseContexts contexts,
    DatabaseRegistry registry) : IReferenceGuard
{
    private static readonly MethodInfo AnyReference =
        typeof(ReferenceGuard).GetMethod(
            nameof(AnyReferenceAsync),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly Dictionary<Type, string> DisplayNames =
        CrudRegistry.Resources
            .GroupBy(resource => resource.EntityType)
            .ToDictionary(group => group.Key, group => group.First().DisplayNameTh);

    public async Task<string?> WhyCannotDeleteAsync(object entity, CancellationToken ct)
    {
        var entityType = entity.GetType();
        if (DatabaseLayout.IsBuEntity(entityType))
            return await FindReferenceAsync(
                contexts.Branch,
                entity,
                branchReferencesOnly: true,
                ct);

        var coreReference = await FindReferenceAsync(
            contexts.Core,
            entity,
            branchReferencesOnly: false,
            ct);
        if (coreReference is not null) return coreReference;

        var branches = await registry.ListBranchesAsync(ct);
        foreach (var branch in branches)
        {
            var branchReference = await FindReferenceAsync(
                contexts.ForBranch(branch),
                entity,
                branchReferencesOnly: true,
                ct);
            if (branchReference is not null) return branchReference;
        }

        return null;
    }

    private async Task<string?> FindReferenceAsync(
        IdaDbContext db,
        object entity,
        bool branchReferencesOnly,
        CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(entity.GetType());
        if (entityType is null) return null;

        foreach (var fk in entityType.GetReferencingForeignKeys())
        {
            if (DeclaredCascade(fk) || fk.DeclaringEntityType.IsOwned()) continue;
            if (branchReferencesOnly != DatabaseLayout.IsBuEntity(fk.DeclaringEntityType.ClrType))
                continue;
            if (fk.Properties.Count != 1 || fk.PrincipalKey.Properties.Count != 1) continue;

            var keyValue = entity.GetType()
                .GetProperty(fk.PrincipalKey.Properties[0].Name)
                ?.GetValue(entity);
            if (keyValue is null) continue;

            var referencing = fk.DeclaringEntityType.ClrType;
            var task = (Task<bool>)AnyReference
                .MakeGenericMethod(referencing, fk.Properties[0].ClrType)
                .Invoke(null, [db, fk.Properties[0].Name, keyValue, ct])!;

            if (await task) return Message(referencing);
        }

        return null;
    }

    private static bool DeclaredCascade(IForeignKey fk) =>
        fk.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.ClientCascade &&
        (fk as IConventionForeignKey)?.GetDeleteBehaviorConfigurationSource()
            == ConfigurationSource.Explicit;

    private static Task<bool> AnyReferenceAsync<TReferencing, TKey>(
        IdaDbContext db,
        string property,
        TKey key,
        CancellationToken ct)
        where TReferencing : class
    {
        var parameter = Expression.Parameter(typeof(TReferencing), "entity");
        var predicate = Expression.Lambda<Func<TReferencing, bool>>(
            Expression.Equal(
                Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    [typeof(TKey)],
                    parameter,
                    Expression.Constant(property)),
                Expression.Field(
                    Expression.Constant(new Box<TKey>(key)),
                    nameof(Box<TKey>.Value))),
            parameter);

        return db.Set<TReferencing>().AnyAsync(predicate, ct);
    }

    private static string Message(Type referencing) =>
        DisplayNames.TryGetValue(referencing, out var name)
            ? $"ลบไม่ได้ เพราะมีข้อมูล{name}อ้างถึงรายการนี้อยู่ " +
              $"ให้แก้ไขหรือลบข้อมูล{name}ที่เกี่ยวข้องก่อน"
            : "ลบไม่ได้ เพราะยังมีข้อมูลอื่นในระบบอ้างถึงรายการนี้อยู่";

    private sealed class Box<T>(T value)
    {
        public readonly T Value = value;
    }
}
