using Ida.Domain.Bu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class DfBadDebtTierConfiguration : IEntityTypeConfiguration<DfBadDebtTier>
{
    public void Configure(EntityTypeBuilder<DfBadDebtTier> b)
    {
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasIndex(e => new { e.HospitalId, e.FromPercent })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_df_bad_debt_tier_from");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_df_bad_debt_tier_range",
                "from_percent >= 0 AND to_percent <= 100 AND from_percent <= to_percent");

            t.HasCheckConstraint("ck_df_bad_debt_tier_pay",
                "(pay_actual AND pay_percent IS NULL) OR " +
                "(NOT pay_actual AND pay_percent IS NOT NULL AND pay_percent BETWEEN 0 AND 100)");
        });
    }
}

public class DocSlipSettingConfiguration : IEntityTypeConfiguration<DocSlipSetting>
{
    public void Configure(EntityTypeBuilder<DocSlipSetting> b)
    {
        b.Property(e => e.Email).HasColumnType("varchar(100)");
        b.Property(e => e.BackupEmail).HasColumnType("varchar(100)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.DoctorCodeId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doc_slip_setting_doctor");
    }
}

public class SysIncomeDocSettingConfiguration : IEntityTypeConfiguration<SysIncomeDocSetting>
{
    public void Configure(EntityTypeBuilder<SysIncomeDocSetting> b)
    {
        b.HasIndex(e => e.HospitalId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_income_doc_setting_hospital");

        b.ToTable(t => t.HasCheckConstraint("ck_sys_income_doc_setting_day",
            "payslip_day BETWEEN 1 AND 31 AND certificate_406_day BETWEEN 1 AND 31 " +
            "AND certificate_50_tawi_day BETWEEN 1 AND 31"));
    }
}

public class SysHisNotifyEmailConfiguration : IEntityTypeConfiguration<SysHisNotifyEmail>
{
    public void Configure(EntityTypeBuilder<SysHisNotifyEmail> b)
    {
        b.Property(e => e.Email).HasColumnType("varchar(100)");
        b.Property(e => e.RecipientName).HasColumnType("varchar(100)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasIndex(e => new { e.HospitalId, e.Email })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_his_notify_email_email");
    }
}

public class SysHisDoctorCodeMapConfiguration : IEntityTypeConfiguration<SysHisDoctorCodeMap>
{
    public void Configure(EntityTypeBuilder<SysHisDoctorCodeMap> b)
    {
        b.Property(e => e.HisDoctorCode).HasColumnType("varchar(20)");
        b.Property(e => e.Remark).HasColumnType("varchar(1000)");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.HisDoctorCode, e.DepartmentId })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_his_doctor_code_map_code");
    }
}

public class SysExpiryAlertSettingConfiguration : IEntityTypeConfiguration<SysExpiryAlertSetting>
{
    public void Configure(EntityTypeBuilder<SysExpiryAlertSetting> b)
    {
        b.HasIndex(e => e.HospitalId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_expiry_alert_setting_hospital");

        b.ToTable(t => t.HasCheckConstraint("ck_sys_expiry_alert_setting_days",
            "alert_days_before BETWEEN 1 AND 999"));
    }
}

public class SysCheckinAreaConfiguration : IEntityTypeConfiguration<SysCheckinArea>
{
    public void Configure(EntityTypeBuilder<SysCheckinArea> b)
    {

        b.Property(e => e.Latitude).HasColumnType("numeric(9,6)");
        b.Property(e => e.Longitude).HasColumnType("numeric(9,6)");

        b.HasIndex(e => e.HospitalId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_checkin_area_hospital");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sys_checkin_area_coords",
                "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
            t.HasCheckConstraint("ck_sys_checkin_area_radius", "radius_meters BETWEEN 1 AND 99999");
        });
    }
}
