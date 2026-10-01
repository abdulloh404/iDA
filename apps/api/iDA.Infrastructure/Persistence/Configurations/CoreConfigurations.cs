using Ida.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnType("varchar(20)").ValueGeneratedNever();

        b.HasIndex(e => e.DoctorCodePrefix)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND doctor_code_prefix IS NOT NULL")
            .HasDatabaseName("uq_hospital_prefix");
    }
}

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> b)
    {
        b.HasIndex(e => e.DoctorGlobalCode)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_global_code");

        b.HasIndex(e => e.NationalIdHash)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND national_id_hash IS NOT NULL")
            .HasDatabaseName("uq_doctor_national_id_hash");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_doctor_birth_date",
                "birth_date IS NULL OR birth_date < CURRENT_DATE");
            t.HasCheckConstraint("ck_doctor_expiry",
                "id_doc_no_expiry = true OR id_doc_expiry_date IS NOT NULL OR approval_status = 'DRAFT'");
        });

        b.HasOne(e => e.Title).WithMany().HasForeignKey(e => e.TitleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.HomeHospital).WithMany().HasForeignKey(e => e.HomeHospitalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorContactConfiguration : IEntityTypeConfiguration<DoctorContact>
{
    public void Configure(EntityTypeBuilder<DoctorContact> b)
    {

        b.HasIndex(e => new { e.ContactType, e.ContactValue })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_contact_value");

        b.HasOne(e => e.Doctor).WithMany(e => e.Contacts).HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorAddressConfiguration : IEntityTypeConfiguration<DoctorAddress>
{
    public void Configure(EntityTypeBuilder<DoctorAddress> b)
    {
        b.HasIndex(e => new { e.DoctorId, e.AddressType })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_address_type");

        b.ToTable(t => t.HasCheckConstraint("ck_address_type",
            "address_type IN ('HOME','TAX','MAILING','WORK')"));

        b.HasOne(e => e.Doctor).WithMany(e => e.Addresses).HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorLicenseConfiguration : IEntityTypeConfiguration<DoctorLicense>
{
    public void Configure(EntityTypeBuilder<DoctorLicense> b)
    {
        b.HasIndex(e => new { e.DoctorId, e.LicenseType, e.LicenseNo })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_license");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_license_type",
                "license_type IN ('MEDICAL','DENTAL','SPECIALTY_BOARD','OTHER')");
            t.HasCheckConstraint("ck_license_date",
                "expiry_date IS NULL OR issued_date IS NULL OR expiry_date >= issued_date");
        });

        b.HasOne(e => e.Doctor).WithMany(e => e.Licenses).HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorHospitalLinkConfiguration : IEntityTypeConfiguration<DoctorHospitalLink>
{
    public void Configure(EntityTypeBuilder<DoctorHospitalLink> b)
    {
        b.HasIndex(e => new { e.DoctorId, e.HospitalId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_hospital");

        b.ToTable(t => t.HasCheckConstraint("ck_link_date",
            "effective_to IS NULL OR effective_to >= effective_from"));

        b.HasOne(e => e.Doctor).WithMany(e => e.HospitalLinks).HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Hospital).WithMany().HasForeignKey(e => e.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MstBankBranchConfiguration : IEntityTypeConfiguration<MstBankBranch>
{
    public void Configure(EntityTypeBuilder<MstBankBranch> b)
    {

        b.HasIndex(e => new { e.BankId, e.Code })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_bank_branch");

        b.HasOne(e => e.Bank).WithMany(e => e.Branches).HasForeignKey(e => e.BankId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MstDocumentTypeConfiguration : IEntityTypeConfiguration<MstDocumentType>
{
    public void Configure(EntityTypeBuilder<MstDocumentType> b) =>
        b.ToTable(t => t.HasCheckConstraint("ck_document_scope_level",
            "scope_level IN ('CORE','BU')"));
}

public class PitTaxBracketConfiguration : IEntityTypeConfiguration<PitTaxBracket>
{
    public void Configure(EntityTypeBuilder<PitTaxBracket> b)
    {
        b.HasIndex(e => new { e.TaxYear, e.IncomeFrom })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_pit_tax_bracket");

        b.ToTable(t => t.HasCheckConstraint("ck_pit_bracket_range",
            "income_to IS NULL OR income_to > income_from"));
    }
}

public class TaxAllowanceTypeConfiguration : IEntityTypeConfiguration<TaxAllowanceType>
{
    public void Configure(EntityTypeBuilder<TaxAllowanceType> b)
    {

        b.HasIndex(e => e.TaxYear)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_tax_allowance_year");

        b.HasMany(e => e.Items).WithOne(e => e.AllowanceType)
            .HasForeignKey(e => e.TaxAllowanceTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.Property(e => e.ClientIp).HasColumnType("inet").HasConversion(new IpAddressConverter());

        b.HasOne<Hospital>()
            .WithMany()
            .HasForeignKey(e => e.HospitalId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(e => new { e.TableName, e.RecordPk, e.ChangedAt })
            .HasDatabaseName("ix_audit_log_record");

        b.ToTable(t => t.HasCheckConstraint("ck_audit_log_action",
            "action IN ('INSERT','UPDATE','DELETE')"));
    }
}
