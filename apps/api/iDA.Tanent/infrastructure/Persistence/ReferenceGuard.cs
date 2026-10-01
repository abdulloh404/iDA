using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ida.Infrastructure.Persistence;

public class ReferenceGuard(DatabaseContexts contexts) : IReferenceGuard
{
    private static readonly MethodInfo AnyReference =
        typeof(ReferenceGuard).GetMethod(
            nameof(AnyReferenceAsync),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly Dictionary<Type, string> DisplayNames =
        CrudRegistry.Resources
            .GroupBy(resource => resource.EntityType)
            .ToDictionary(group => group.Key, group => group.First().DisplayNameTh);

    private static readonly HashSet<string> GuidReferenceTypes = new(StringComparer.Ordinal)
    {
        "Doctor", "DoctorLicense", "DoctorContact", "DoctorAddress", "DoctorEducation", "DoctorTraining",
        "DoctorWorkHistory", "DoctorAffiliation", "DoctorFamily", "DoctorProfessionalRecord", "DoctorInsurance",
        "DoctorDocument", "DoctorHospitalLink", "MstTitle", "MstSpecialty", "MstSubSpecialty", "MstBank",
        "MstBankBranch", "MstDocumentType", "PitTaxBracket", "TaxAllowanceType", "TaxAllowanceItem",
        "AppUser", "Role", "Permission", "UserHospitalRole", "SysEmailTemplate", "SysPasswordPolicy", "SysTerms",
    };

    public Task<string?> WhyCannotDeleteAsync(object entity, CancellationToken ct) => FindReferenceAsync(contexts.Branch, entity, ct);

    public async Task<CoreReferenceResult> CheckCoreReferenceAsync(CoreReferenceRequest input, CancellationToken ct)
    {
        var keyType = input.EntityType switch
        {
            "Hospital" => typeof(string),
            "AuditLog" or "AuthLog" => typeof(long),
            _ when GuidReferenceTypes.Contains(input.EntityType) => typeof(Guid),
            _ => throw ApiException.BadRequest("invalid_reference", "ไม่รองรับชนิดข้อมูลอ้างอิงนี้"),
        };
        object? value;
        try { value = input.Key.Deserialize(keyType); }
        catch (JsonException) { throw ApiException.BadRequest("invalid_reference", "รหัสข้อมูลอ้างอิงไม่ถูกต้อง"); }
        if (value is null) throw ApiException.BadRequest("invalid_reference", "ต้องระบุรหัสข้อมูลอ้างอิง");
        foreach (var reference in ScalarReferences(input.EntityType).OrderBy(reference => reference.EntityType.Name, StringComparer.Ordinal))
        {
            var property = contexts.Branch.Model.FindEntityType(reference.EntityType)?.FindProperty(reference.PropertyName);
            if (property is null) continue;
            var task = (Task<bool>)AnyReference.MakeGenericMethod(reference.EntityType, property.ClrType)
                .Invoke(null, [contexts.Branch, reference.PropertyName, value, ct])!;
            if (await task) return new CoreReferenceResult(Message(reference.EntityType));
        }
        return new CoreReferenceResult(null);
    }

    private IEnumerable<ScalarReference> ScalarReferences(string entityType) => entityType switch
    {
        "Hospital" => contexts.Branch.Model.GetEntityTypes()
            .Where(entity => DatabaseLayout.IsBuEntity(entity.ClrType) && entity.FindProperty("HospitalId")?.ClrType == typeof(string))
            .Select(entity => new ScalarReference(entity.ClrType, "HospitalId")),
        "Doctor" =>
        [
            new(typeof(DoctorCode), nameof(DoctorCode.DoctorId)),
            new(typeof(DoctorBankAccount), nameof(DoctorBankAccount.DoctorId)),
            new(typeof(DoctorSpecialty), nameof(DoctorSpecialty.DoctorId)),
            new(typeof(DoctorContract), nameof(DoctorContract.DoctorId)),
            new(typeof(BuDoctorDocument), nameof(BuDoctorDocument.DoctorId)),
            new(typeof(DoctorWelfare), nameof(DoctorWelfare.DoctorId)),
            new(typeof(DoctorApprovalRequest), nameof(DoctorApprovalRequest.DoctorId)),
        ],
        "MstBank" => [new(typeof(MstReceiptType), nameof(MstReceiptType.BankId))],
        "MstBankBranch" => [new(typeof(DoctorBankAccount), nameof(DoctorBankAccount.BankBranchId))],
        "MstSpecialty" => [new(typeof(DoctorSpecialty), nameof(DoctorSpecialty.SpecialtyId))],
        "MstSubSpecialty" => [new(typeof(DoctorSpecialty), nameof(DoctorSpecialty.SubSpecialtyId))],
        "MstDocumentType" => [new(typeof(BuDoctorDocument), nameof(BuDoctorDocument.DocTypeId))],
        "TaxAllowanceItem" => [new(typeof(DfTaxDeductionItem), nameof(DfTaxDeductionItem.TaxAllowanceItemId))],
        _ => [],
    };

    private async Task<string?> FindReferenceAsync(IdaDbContext db, object entity, CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(entity.GetType());
        if (entityType is null) return null;

        foreach (var fk in entityType.GetReferencingForeignKeys())
        {
            if (DeclaredCascade(fk) || fk.DeclaringEntityType.IsOwned()) continue;
            if (!DatabaseLayout.IsBuEntity(fk.DeclaringEntityType.ClrType)) continue;
            if (fk.Properties.Count != 1 || fk.PrincipalKey.Properties.Count != 1) continue;
            var keyValue = entity.GetType().GetProperty(fk.PrincipalKey.Properties[0].Name)?.GetValue(entity);
            if (keyValue is null) continue;
            var referencing = fk.DeclaringEntityType.ClrType;
            var task = (Task<bool>)AnyReference.MakeGenericMethod(referencing, fk.Properties[0].ClrType)
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

    private sealed record ScalarReference(Type EntityType, string PropertyName);

    private sealed class Box<T>(T value)
    {
        public readonly T Value = value;
    }
}
