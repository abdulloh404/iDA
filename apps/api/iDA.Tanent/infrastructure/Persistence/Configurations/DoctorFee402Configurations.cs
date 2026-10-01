using Ida.Domain.Bu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class DfPositionFeeConfiguration : IEntityTypeConfiguration<DfPositionFee>
{
    public void Configure(EntityTypeBuilder<DfPositionFee> b)
    {
        b.Property(e => e.PositionName).HasColumnType("varchar(200)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Clinic).WithMany().HasForeignKey(e => e.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.DoctorCodeId, e.StartDate })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_df_position_fee_doctor");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_df_position_fee_range", "end_date >= start_date");
            t.HasCheckConstraint("ck_df_position_fee_amount", "monthly_amount > 0");
        });
    }
}

public class DfExternalFeeConfiguration : IEntityTypeConfiguration<DfExternalFee>
{
    public void Configure(EntityTypeBuilder<DfExternalFee> b)
    {
        b.Property(e => e.RefDocNo).HasColumnType("varchar(50)");
        b.Property(e => e.DecisionComment).HasColumnType("varchar(500)");
        b.Property(e => e.DecidedBy).HasColumnType("varchar(100)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.ArCode).WithMany().HasForeignKey(e => e.ArCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.Kind, e.ArCodeId, e.RefDocNo })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_df_external_fee_doc");

        b.ToTable(t => t.HasCheckConstraint("ck_df_external_fee_period",
            "period_month BETWEEN 1 AND 12"));
    }
}

public class DfExternalFeeLineConfiguration : IEntityTypeConfiguration<DfExternalFeeLine>
{
    public void Configure(EntityTypeBuilder<DfExternalFeeLine> b)
    {
        b.Property(e => e.Description).HasColumnType("varchar(500)");
        b.Property(e => e.AttachmentUrl).HasColumnType("varchar(500)");

        b.HasOne(e => e.Fee).WithMany(e => e.Lines).HasForeignKey(e => e.FeeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.FeeId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_df_external_fee_line_fee");

        b.ToTable(t => t.HasCheckConstraint("ck_df_external_fee_line_amount", "amount > 0"));
    }
}

public class DfFeeItemConfiguration : IEntityTypeConfiguration<DfFeeItem>
{
    public void Configure(EntityTypeBuilder<DfFeeItem> b)
    {
        b.Property(e => e.RefDocNo).HasColumnType("varchar(50)");
        b.Property(e => e.DecisionComment).HasColumnType("varchar(500)");
        b.Property(e => e.DecidedBy).HasColumnType("varchar(100)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.ArCode).WithMany().HasForeignKey(e => e.ArCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.PeriodYear, e.PeriodMonth })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_df_fee_item_period");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_df_fee_item_period", "period_month BETWEEN 1 AND 12");
            t.HasCheckConstraint("ck_df_fee_item_amount", "amount > 0");
        });
    }
}

public class DfHospitalPaidTaxConfiguration : IEntityTypeConfiguration<DfHospitalPaidTax>
{
    public void Configure(EntityTypeBuilder<DfHospitalPaidTax> b)
    {
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.DoctorCodeId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_df_hospital_paid_tax_doctor");
    }
}

public class DfTaxDeductionConfiguration : IEntityTypeConfiguration<DfTaxDeduction>
{
    public void Configure(EntityTypeBuilder<DfTaxDeduction> b)
    {
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.DoctorCodeId, e.TaxYear })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_df_tax_deduction_year");

        b.ToTable(t => t.HasCheckConstraint("ck_df_tax_deduction_children", "child_count >= 0"));
    }
}

public class DfTaxDeductionItemConfiguration : IEntityTypeConfiguration<DfTaxDeductionItem>
{
    public void Configure(EntityTypeBuilder<DfTaxDeductionItem> b)
    {
        b.HasOne(e => e.Deduction).WithMany(e => e.Items).HasForeignKey(e => e.DeductionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.DeductionId, e.TaxAllowanceItemId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_df_tax_deduction_item");

        b.ToTable(t => t.HasCheckConstraint("ck_df_tax_deduction_item_amount", "amount >= 0"));
    }
}

public class DfTaxExemptionConfiguration : IEntityTypeConfiguration<DfTaxExemption>
{
    public void Configure(EntityTypeBuilder<DfTaxExemption> b)
    {
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.DoctorCodeId, e.TaxYear })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_df_tax_exemption_year");
    }
}
