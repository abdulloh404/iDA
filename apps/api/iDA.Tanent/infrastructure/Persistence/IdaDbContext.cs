using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

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

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        EfModelConventions.Apply(model, BuSchema, this, nameof(CurrentHospitalId));
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

    private void ApplyLayout(ModelBuilder model)
    {
        foreach (var entity in model.Model.GetEntityTypes())
        {
            var clr = entity.ClrType;
            var table = entity.GetTableName() ?? Naming.ToSnakeCase(clr.Name);
            var builder = model.Entity(clr);

            builder.ToTable(table, Layout.SchemaName);
        }
    }

    private void EnsureWritableEntries()
    {
        if (!Layout.IsBranch) return;

        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            if (!DatabaseLayout.IsBranchTable(entry.Metadata.ClrType))
                throw new InvalidOperationException($"Entity '{entry.Metadata.ClrType.Name}' belongs to the Core database and cannot be saved through a BU context.");

            if (entry.Metadata.FindProperty("HospitalId")?.ClrType != typeof(string)) continue;
            if (!_tenant.HasTenant || string.IsNullOrWhiteSpace(CurrentHospitalId))
                throw new InvalidOperationException("The BU database requires a configured hospital ID before saving data.");

            var hospital = entry.Property("HospitalId");
            if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(hospital.CurrentValue as string))
                hospital.CurrentValue = CurrentHospitalId;

            if (!string.Equals(hospital.CurrentValue as string, CurrentHospitalId, StringComparison.Ordinal)
                || (entry.State != EntityState.Added && !string.Equals(hospital.OriginalValue as string, CurrentHospitalId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Entity '{entry.Metadata.ClrType.Name}' must belong to the configured BU '{CurrentHospitalId}'.");
        }
    }

}
