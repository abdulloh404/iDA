using Ida.Application.Common;
using Ida.Domain.Auth;
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

    public DbSet<SysEmailTemplate> SysEmailTemplates => Set<SysEmailTemplate>();
    public DbSet<SysPasswordPolicy> SysPasswordPolicies => Set<SysPasswordPolicy>();
    public DbSet<SysTerms> SysTerms => Set<SysTerms>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("pgcrypto");

        model.ApplyConfigurationsFromAssembly(typeof(IdaDbContext).Assembly);

        EfModelConventions.Apply(model, CoreSchema, this, nameof(CurrentHospitalId), ApplyHospitalForeignKey);
        ApplyLayout(model);
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

}
