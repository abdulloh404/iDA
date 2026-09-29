using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using Ida.Domain.Core;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence;

public class IdaDbContext : DbContext
{
    public const string CoreSchema = "core";
    public const string BuSchema = "bu";

    private readonly ITenantContext _tenant;

    public IdaDbContext(
        DbContextOptions<IdaDbContext> options,
        ITenantContext tenant,
        DatabaseLayout layout)
        : base(options)
    {
        _tenant = tenant;
        Layout = layout;
    }

    public DatabaseLayout Layout { get; }

    public string CurrentHospitalId => _tenant.HospitalId;

    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<MstTitle> Titles => Set<MstTitle>();
    public DbSet<MstSpecialty> Specialties => Set<MstSpecialty>();
    public DbSet<MstSubSpecialty> SubSpecialties => Set<MstSubSpecialty>();
    public DbSet<MstBank> Banks => Set<MstBank>();
    public DbSet<MstBankBranch> BankBranches => Set<MstBankBranch>();
    public DbSet<MstDocumentType> DocumentTypes => Set<MstDocumentType>();
    public DbSet<PitTaxBracket> PitTaxBrackets => Set<PitTaxBracket>();
    public DbSet<TaxAllowanceType> TaxAllowanceTypes => Set<TaxAllowanceType>();
    public DbSet<TaxAllowanceItem> TaxAllowanceItems => Set<TaxAllowanceItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorLicense> DoctorLicenses => Set<DoctorLicense>();
    public DbSet<DoctorContact> DoctorContacts => Set<DoctorContact>();
    public DbSet<DoctorAddress> DoctorAddresses => Set<DoctorAddress>();
    public DbSet<DoctorEducation> DoctorEducations => Set<DoctorEducation>();
    public DbSet<DoctorTraining> DoctorTrainings => Set<DoctorTraining>();
    public DbSet<DoctorWorkHistory> DoctorWorkHistories => Set<DoctorWorkHistory>();
    public DbSet<DoctorAffiliation> DoctorAffiliations => Set<DoctorAffiliation>();
    public DbSet<DoctorFamily> DoctorFamilies => Set<DoctorFamily>();
    public DbSet<DoctorProfessionalRecord> DoctorProfessionalRecords => Set<DoctorProfessionalRecord>();
    public DbSet<DoctorInsurance> DoctorInsurances => Set<DoctorInsurance>();
    public DbSet<DoctorDocument> DoctorDocuments => Set<DoctorDocument>();
    public DbSet<DoctorHospitalLink> DoctorHospitalLinks => Set<DoctorHospitalLink>();

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserHospitalRole> UserHospitalRoles => Set<UserHospitalRole>();
    public DbSet<AuthLog> AuthLogs => Set<AuthLog>();

    public DbSet<MstDoctorType> DoctorTypes => Set<MstDoctorType>();
    public DbSet<MstDoctorGroup> DoctorGroups => Set<MstDoctorGroup>();
    public DbSet<MstDepartment> Departments => Set<MstDepartment>();
    public DbSet<MstClinic> Clinics => Set<MstClinic>();
    public DbSet<MstPaymentType> PaymentTypes => Set<MstPaymentType>();
    public DbSet<MstPrivilegeType> PrivilegeTypes => Set<MstPrivilegeType>();
    public DbSet<MstPrivilegeSubtype> PrivilegeSubtypes => Set<MstPrivilegeSubtype>();
    public DbSet<MstStatusPrivilege> StatusPrivileges => Set<MstStatusPrivilege>();
    public DbSet<MstWelfarePlan> WelfarePlans => Set<MstWelfarePlan>();
    public DbSet<MstTaxType> TaxTypes => Set<MstTaxType>();
    public DbSet<MstShareCategory> ShareCategories => Set<MstShareCategory>();
    public DbSet<MstTreatmentCategory> TreatmentCategories => Set<MstTreatmentCategory>();
    public DbSet<MstTreatment> Treatments => Set<MstTreatment>();
    public DbSet<MstReceiptType> ReceiptTypes => Set<MstReceiptType>();
    public DbSet<MstArCode> ArCodes => Set<MstArCode>();
    public DbSet<MstIncomeDeductionItem> IncomeDeductionItems => Set<MstIncomeDeductionItem>();
    public DbSet<GlPostingSetup> GlPostingSetups => Set<GlPostingSetup>();
    public DbSet<NoWaitPaymentRule> NoWaitPaymentRules => Set<NoWaitPaymentRule>();
    public DbSet<MstIncomeType402> IncomeTypes402 => Set<MstIncomeType402>();
    public DbSet<MstAdjustmentType> AdjustmentTypes => Set<MstAdjustmentType>();
    public DbSet<MstExpenseType> ExpenseTypes => Set<MstExpenseType>();
    public DbSet<InvoicePrefixRule> InvoicePrefixRules => Set<InvoicePrefixRule>();
    public DbSet<InvoiceArCashRule> InvoiceArCashRules => Set<InvoiceArCashRule>();
    public DbSet<InvoiceAccrualRule> InvoiceAccrualRules => Set<InvoiceAccrualRule>();
    public DbSet<MstPatientRight> PatientRights => Set<MstPatientRight>();
    public DbSet<ShareRate> ShareRates => Set<ShareRate>();
    public DbSet<ShareRateExclusion> ShareRateExclusions => Set<ShareRateExclusion>();
    public DbSet<DutyHolidayRate> DutyHolidayRates => Set<DutyHolidayRate>();
    public DbSet<DutyHolidayExclusion> DutyHolidayExclusions => Set<DutyHolidayExclusion>();
    public DbSet<DutyRate> DutyRates => Set<DutyRate>();
    public DbSet<DutyRateDay> DutyRateDays => Set<DutyRateDay>();
    public DbSet<GuaranteeRate> GuaranteeRates => Set<GuaranteeRate>();
    public DbSet<GuaranteeRateTreatment> GuaranteeRateTreatments => Set<GuaranteeRateTreatment>();
    public DbSet<GuaranteeRateDay> GuaranteeRateDays => Set<GuaranteeRateDay>();
    public DbSet<DutySchedule> DutySchedules => Set<DutySchedule>();
    public DbSet<DutyShift> DutyShifts => Set<DutyShift>();
    public DbSet<DutyShiftDoctor> DutyShiftDoctors => Set<DutyShiftDoctor>();
    public DbSet<DfPositionFee> DfPositionFees => Set<DfPositionFee>();
    public DbSet<DfExternalFee> DfExternalFees => Set<DfExternalFee>();
    public DbSet<DfExternalFeeLine> DfExternalFeeLines => Set<DfExternalFeeLine>();
    public DbSet<DfFeeItem> DfFeeItems => Set<DfFeeItem>();
    public DbSet<DfHospitalPaidTax> DfHospitalPaidTaxes => Set<DfHospitalPaidTax>();
    public DbSet<DfTaxDeduction> DfTaxDeductions => Set<DfTaxDeduction>();
    public DbSet<DfTaxDeductionItem> DfTaxDeductionItems => Set<DfTaxDeductionItem>();
    public DbSet<DfTaxExemption> DfTaxExemptions => Set<DfTaxExemption>();

    public DbSet<DfBadDebtTier> DfBadDebtTiers => Set<DfBadDebtTier>();
    public DbSet<DocSlipSetting> DocSlipSettings => Set<DocSlipSetting>();
    public DbSet<SysIncomeDocSetting> SysIncomeDocSettings => Set<SysIncomeDocSetting>();
    public DbSet<SysHisNotifyEmail> SysHisNotifyEmails => Set<SysHisNotifyEmail>();
    public DbSet<SysHisDoctorCodeMap> SysHisDoctorCodeMaps => Set<SysHisDoctorCodeMap>();
    public DbSet<SysExpiryAlertSetting> SysExpiryAlertSettings => Set<SysExpiryAlertSetting>();
    public DbSet<SysCheckinArea> SysCheckinAreas => Set<SysCheckinArea>();
    public DbSet<SysEmailTemplate> SysEmailTemplates => Set<SysEmailTemplate>();
    public DbSet<SysPasswordPolicy> SysPasswordPolicies => Set<SysPasswordPolicy>();
    public DbSet<SysTerms> SysTerms => Set<SysTerms>();

    public DbSet<DoctorCode> DoctorCodes => Set<DoctorCode>();
    public DbSet<DoctorBankAccount> DoctorBankAccounts => Set<DoctorBankAccount>();
    public DbSet<DoctorBankAccountHistory> DoctorBankAccountHistories => Set<DoctorBankAccountHistory>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<DoctorContract> DoctorContracts => Set<DoctorContract>();
    public DbSet<BuDoctorDocument> BuDoctorDocuments => Set<BuDoctorDocument>();
    public DbSet<DoctorWelfare> DoctorWelfares => Set<DoctorWelfare>();
    public DbSet<DoctorWelfareUsage> DoctorWelfareUsages => Set<DoctorWelfareUsage>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorScheduleOff> DoctorScheduleOffs => Set<DoctorScheduleOff>();
    public DbSet<DoctorApprovalRequest> DoctorApprovalRequests => Set<DoctorApprovalRequest>();
    public DbSet<DoctorApprovalStep> DoctorApprovalSteps => Set<DoctorApprovalStep>();
    public DbSet<DlExportOutbox> DlExportOutbox => Set<DlExportOutbox>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("pgcrypto");

        model.ApplyConfigurationsFromAssembly(typeof(IdaDbContext).Assembly);

        if (!Layout.IsReferenceModel)
        {
            var excludedTypes = model.Model.GetEntityTypes()
                .Where(entity => Layout.IsBranch
                    ? !DatabaseLayout.IsBranchTable(entity.ClrType)
                    : DatabaseLayout.IsBuEntity(entity.ClrType))
                .Select(entity => entity.ClrType)
                .Distinct()
                .ToList();
            foreach (var type in excludedTypes) model.Ignore(type);
        }

        ApplyConventions(model);
        ApplyLayout(model);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureWritableEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken ct = default)
    {
        EnsureWritableEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>
        UtcConverter = new(v => v.ToUniversalTime(), v => v);

    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, DateTimeOffset?>
        UtcNullableConverter = new(v => v.HasValue ? v.Value.ToUniversalTime() : v, v => v);

    private void ApplyConventions(ModelBuilder model)
    {
        foreach (var entity in model.Model.GetEntityTypes())
        {
            var clr = entity.ClrType;

            var explicitlyNamed = ((IConventionEntityType)entity)
                .GetTableNameConfigurationSource() == ConfigurationSource.Explicit;

            if (!explicitlyNamed)
            {
                entity.SetTableName(Naming.ToSnakeCase(clr.Name));
                entity.SetSchema(clr.Namespace == "Ida.Domain.Bu" ? BuSchema : CoreSchema);
            }

            var table = entity.GetTableName()!;

            foreach (var property in entity.GetProperties())
                property.SetColumnName(Naming.ToSnakeCase(property.Name));

            foreach (var key in entity.GetKeys())
                key.SetName(key.IsPrimaryKey()
                    ? $"{table}_pkey"
                    : $"uq_{table}_{string.Join('_', key.Properties.Select(p => p.GetColumnName()))}");

            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(
                    $"fk_{table}_{string.Join('_', fk.Properties.Select(p => p.GetColumnName()))}");

            foreach (var property in entity.GetProperties()
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                         .Where(p => ((IConventionProperty)p).GetColumnTypeConfigurationSource()
                                     != ConfigurationSource.Explicit))
                property.SetColumnType("numeric(15,2)");

            foreach (var property in entity.GetProperties()
                         .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
                property.SetValueConverter(property.ClrType == typeof(DateTimeOffset) ? UtcConverter : UtcNullableConverter);

            foreach (var property in entity.GetProperties()
                         .Where(p => p.Name.EndsWith("Percent", StringComparison.Ordinal)))
                property.SetColumnType("numeric(5,2)");

            foreach (var property in entity.GetProperties()
                         .Where(p => p.GetColumnType() is null)
                         .Where(p => p.ClrType == typeof(string) || p.ClrType == typeof(string[])))
                property.SetColumnType(ColumnTypes.For(property.Name));

            var builder = model.Entity(clr);

            if (typeof(IConcurrencyAware).IsAssignableFrom(clr))
                builder.Property(nameof(IConcurrencyAware.RowVersion))
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

            ApplyClientGeneratedKey(entity);
            if (!Layout.IsBranch) ApplyHospitalForeignKey(builder, entity);
            ApplyRowFilter(builder, clr);
            ApplyIndexes(builder, entity, clr, table);
        }
    }

    private void ApplyLayout(ModelBuilder model)
    {
        foreach (var entity in model.Model.GetEntityTypes())
        {
            var clr = entity.ClrType;
            var table = entity.GetTableName() ?? Naming.ToSnakeCase(clr.Name);
            var builder = model.Entity(clr);

            builder.ToTable(table, Layout.IsReferenceModel && DatabaseLayout.IsBranchTable(clr) ? BuSchema : Layout.SchemaName);
        }
    }

    private void EnsureWritableEntries()
    {
        if (!Layout.IsBranch) return;

        var invalid = ChangeTracker.Entries()
            .FirstOrDefault(entry =>
                (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
                !DatabaseLayout.IsBranchTable(entry.Metadata.ClrType));

        if (invalid is not null)
            throw new InvalidOperationException(
                $"Entity '{invalid.Metadata.ClrType.Name}' belongs to the Core database and cannot be saved through a BU context.");
    }

    private static void ApplyClientGeneratedKey(IMutableEntityType entity)
    {
        var key = entity.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1) return;

        var property = key.Properties[0];
        if (property.ClrType == typeof(Guid)) property.ValueGenerated = ValueGenerated.Never;
    }

    private static void ApplyHospitalForeignKey(EntityTypeBuilder builder, IMutableEntityType entity)
    {
        if (entity.ClrType == typeof(Hospital)) return;

        var property = entity.FindProperty("HospitalId");
        if (property is null || property.ClrType != typeof(string)) return;

        if (entity.GetForeignKeys().Any(fk => fk.Properties.Contains(property))) return;

        builder
            .HasOne(typeof(Hospital))
            .WithMany()
            .HasForeignKey("HospitalId")
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ApplyRowFilter(EntityTypeBuilder builder, Type clr)
    {
        var isTenant = typeof(TenantEntity).IsAssignableFrom(clr);
        var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clr);
        if (!isTenant && !isSoftDeletable) return;

        var parameter = System.Linq.Expressions.Expression.Parameter(clr, "e");
        System.Linq.Expressions.Expression? body = null;

        if (isSoftDeletable)
            body = System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt)),
                System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?)));

        if (isTenant)
        {
            var current = System.Linq.Expressions.Expression.Property(
                System.Linq.Expressions.Expression.Constant(this),
                nameof(CurrentHospitalId));
            var sameTenant = System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Property(parameter, nameof(TenantEntity.HospitalId)),
                current);
            body = body is null ? sameTenant
                : System.Linq.Expressions.Expression.AndAlso(body, sameTenant);
        }

        builder.HasQueryFilter(System.Linq.Expressions.Expression.Lambda(body!, parameter));
    }

    private static void ApplyIndexes(EntityTypeBuilder builder, IMutableEntityType entity, Type clr,
        string table)
    {
        if (!typeof(ICodedEntity).IsAssignableFrom(clr)) return;

        if (entity.GetIndexes().Any(i => i.IsUnique &&
                i.Properties.Any(p => p.Name == nameof(ICodedEntity.Code)))) return;

        var isTenant = typeof(TenantEntity).IsAssignableFrom(clr);
        string[] columns = isTenant
            ? [nameof(TenantEntity.HospitalId), nameof(ICodedEntity.Code)]
            : [nameof(ICodedEntity.Code)];

        builder.HasIndex(columns)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName($"uq_{table}_code");
    }
}
