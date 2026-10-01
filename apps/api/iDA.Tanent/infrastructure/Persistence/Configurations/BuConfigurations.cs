using Ida.Domain.Bu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class BuDoctorDocumentConfiguration : IEntityTypeConfiguration<BuDoctorDocument>
{
    public void Configure(EntityTypeBuilder<BuDoctorDocument> b)
    {

        b.ToTable("doctor_document", IdaDbContext.BuSchema);

    }
}

public class DoctorCodeConfiguration : IEntityTypeConfiguration<DoctorCode>
{
    public void Configure(EntityTypeBuilder<DoctorCode> b)
    {
        b.ToTable(t => t.HasCheckConstraint("ck_doctor_code_wht_form",
            "wht_form_type IS NULL OR wht_form_type IN ('PND3','PND53')"));


        b.HasOne(e => e.DoctorType).WithMany().HasForeignKey(e => e.DoctorTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.DoctorGroup).WithMany().HasForeignKey(e => e.DoctorGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Clinic).WithMany().HasForeignKey(e => e.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.PrivilegeType).WithMany().HasForeignKey(e => e.PrivilegeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.StatusPrivilege).WithMany().HasForeignKey(e => e.StatusPrivilegeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.PaymentType).WithMany().HasForeignKey(e => e.PaymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.BankAccount).WithMany().HasForeignKey(e => e.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorBankAccountConfiguration : IEntityTypeConfiguration<DoctorBankAccount>
{
    public void Configure(EntityTypeBuilder<DoctorBankAccount> b)
    {

        b.HasIndex(e => new { e.HospitalId, e.DoctorId })
            .IsUnique()
            .HasFilter("is_active AND deleted_at IS NULL")
            .HasDatabaseName("uq_one_active_bank_per_doctor");

        b.HasIndex(e => e.AccountNoHash)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_bank_account_no");

        b.HasOne(e => e.PaymentType).WithMany().HasForeignKey(e => e.PaymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorBankAccountHistoryConfiguration
    : IEntityTypeConfiguration<DoctorBankAccountHistory>
{
    public void Configure(EntityTypeBuilder<DoctorBankAccountHistory> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.HasIndex(e => new { e.BankAccountId, e.ChangedAt })
            .HasDatabaseName("ix_bank_account_history");

        b.HasOne(e => e.BankAccount).WithMany().HasForeignKey(e => e.BankAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> b)
    {
        b.HasIndex(e => new { e.HospitalId, e.DoctorId, e.SpecialtyId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_specialty");

        b.HasIndex(e => new { e.HospitalId, e.DoctorId })
            .IsUnique()
            .HasFilter("is_primary AND deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_primary_specialty");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class DoctorContractConfiguration : IEntityTypeConfiguration<DoctorContract>
{
    public void Configure(EntityTypeBuilder<DoctorContract> b)
    {
        b.HasIndex(e => new { e.HospitalId, e.ContractNo })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_contract_no");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_contract_type",
                "contract_type IN ('PRACTICE_SPACE','EMPLOYMENT','GUARANTEE_INCOME','SERVICE','OTHER')");
            t.HasCheckConstraint("ck_contract_date",
                "end_date IS NULL OR end_date >= start_date");
        });

    }
}

public class DoctorWelfareConfiguration : IEntityTypeConfiguration<DoctorWelfare>
{
    public void Configure(EntityTypeBuilder<DoctorWelfare> b)
    {
        b.HasIndex(e => new { e.HospitalId, e.DoctorId, e.WelfareYear })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_doctor_welfare_year");

        b.Property(e => e.RemainingAmount)
            .HasComputedColumnSql("annual_limit - used_amount", stored: true);

        b.HasOne(e => e.WelfarePlan).WithMany().HasForeignKey(e => e.WelfarePlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorWelfareUsageConfiguration : IEntityTypeConfiguration<DoctorWelfareUsage>
{
    public void Configure(EntityTypeBuilder<DoctorWelfareUsage> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.HasOne(e => e.Welfare).WithMany(e => e.Usages).HasForeignKey(e => e.WelfareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.HasIndex(e => new { e.HospitalId, e.ScheduleDate })
            .HasDatabaseName("ix_doctor_schedule_date");

        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Clinic).WithMany().HasForeignKey(e => e.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorScheduleOffConfiguration : IEntityTypeConfiguration<DoctorScheduleOff>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleOff> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorApprovalRequestConfiguration : IEntityTypeConfiguration<DoctorApprovalRequest>
{
    public void Configure(EntityTypeBuilder<DoctorApprovalRequest> b)
    {
        b.HasIndex(e => new { e.HospitalId, e.RequestNo })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_approval_request_no");

        b.ToTable(t => t.HasCheckConstraint("ck_approval_request_type",
            "request_type IN ('DOCTOR_PROFILE','DOCTOR_CODE','BANK_ACCOUNT','SPECIALTY','CONTRACT','WELFARE')"));

        b.HasMany(e => e.Steps).WithOne(e => e.Request).HasForeignKey(e => e.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DoctorApprovalStepConfiguration : IEntityTypeConfiguration<DoctorApprovalStep>
{
    public void Configure(EntityTypeBuilder<DoctorApprovalStep> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.HasIndex(e => new { e.RequestId, e.StepSeq })
            .IsUnique()
            .HasDatabaseName("uq_approval_step_seq");
    }
}

public class DlExportOutboxConfiguration : IEntityTypeConfiguration<DlExportOutbox>
{
    public void Configure(EntityTypeBuilder<DlExportOutbox> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();

        b.HasIndex(e => new { e.ExportStatus, e.OccurredAt })
            .HasFilter("export_status <> 'SENT'")
            .HasDatabaseName("ix_outbox_pending");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outbox_operation", "operation IN ('INSERT','UPDATE','DELETE')");
            t.HasCheckConstraint("ck_outbox_status", "export_status IN ('PENDING','SENT','FAILED')");
        });
    }
}

public class NoWaitPaymentRuleConfiguration : IEntityTypeConfiguration<NoWaitPaymentRule>
{
    public void Configure(EntityTypeBuilder<NoWaitPaymentRule> b) =>
        b.ToTable(t => t.HasCheckConstraint("ck_no_wait_rule_date",
            "effective_to IS NULL OR effective_to >= effective_from"));
}

public class InvoiceAccrualRuleConfiguration : IEntityTypeConfiguration<InvoiceAccrualRule>
{
    public void Configure(EntityTypeBuilder<InvoiceAccrualRule> b) =>
        b.ToTable(t => t.HasCheckConstraint("ck_accrual_rule_date",
            "effective_to IS NULL OR effective_to >= effective_from"));
}

public class DutyHolidayRateConfiguration : IEntityTypeConfiguration<DutyHolidayRate>
{
    public void Configure(EntityTypeBuilder<DutyHolidayRate> b)
    {
        b.Property(e => e.HolidayName).HasColumnType("varchar(200)");

        b.HasIndex(e => new { e.HospitalId, e.StartDate })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_duty_holiday_rate_tenant");

        b.ToTable(t => t.HasCheckConstraint("ck_duty_holiday_date",
            "end_date >= start_date"));
    }
}

public class DutyRateConfiguration : IEntityTypeConfiguration<DutyRate>
{
    public void Configure(EntityTypeBuilder<DutyRate> b)
    {
        b.Property(e => e.RoomOther).HasColumnType("varchar(100)");

        b.HasIndex(e => new { e.HospitalId, e.DepartmentId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_duty_rate_tenant");
    }
}

public class DutyRateDayConfiguration : IEntityTypeConfiguration<DutyRateDay>
{
    public void Configure(EntityTypeBuilder<DutyRateDay> b)
    {

        b.HasIndex(e => new { e.DutyRateId, e.DayOfWeek })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_duty_rate_day");

        b.ToTable(t => t.HasCheckConstraint("ck_duty_rate_day_of_week",
            "day_of_week BETWEEN 0 AND 6"));
    }
}

public class GuaranteeRateConfiguration : IEntityTypeConfiguration<GuaranteeRate>
{
    public void Configure(EntityTypeBuilder<GuaranteeRate> b)
    {

        b.HasOne(e => e.DoctorCode).WithMany()
            .HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.CompareDoctorCode).WithMany()
            .HasForeignKey(e => e.CompareDoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.HospitalId, e.Kind, e.DoctorCodeId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_guarantee_rate_tenant");

        b.ToTable(t => t.HasCheckConstraint("ck_guarantee_rate_date",
            "end_date IS NULL OR end_date >= start_date"));
    }
}

public class GuaranteeRateTreatmentConfiguration
    : IEntityTypeConfiguration<GuaranteeRateTreatment>
{
    public void Configure(EntityTypeBuilder<GuaranteeRateTreatment> b) =>

        b.HasIndex(e => new { e.GuaranteeRateId, e.TreatmentId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_guarantee_rate_treatment");
}

public class GuaranteeRateDayConfiguration : IEntityTypeConfiguration<GuaranteeRateDay>
{
    public void Configure(EntityTypeBuilder<GuaranteeRateDay> b)
    {
        b.HasIndex(e => new { e.GuaranteeRateId, e.DayOfWeek })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_guarantee_rate_day");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_guarantee_day_of_week", "day_of_week BETWEEN 0 AND 6");
            t.HasCheckConstraint("ck_guarantee_day_time",
                "start_time IS NULL OR end_time IS NULL OR end_time > start_time");
        });
    }
}

public class DutyScheduleConfiguration : IEntityTypeConfiguration<DutySchedule>
{
    public void Configure(EntityTypeBuilder<DutySchedule> b)
    {

        b.HasIndex(e => new { e.HospitalId, e.Kind, e.Year, e.Month })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_duty_schedule_month");

        b.Property(e => e.SubmittedBy).HasColumnType("varchar(100)");
        b.Property(e => e.RejectedBy).HasColumnType("varchar(100)");
        b.Property(e => e.RejectReason).HasColumnType("varchar(500)");

        b.ToTable(t => t.HasCheckConstraint("ck_duty_schedule_month",
            "month BETWEEN 1 AND 12"));
    }
}

public class DutyShiftConfiguration : IEntityTypeConfiguration<DutyShift>
{
    public void Configure(EntityTypeBuilder<DutyShift> b)
    {
        b.Property(e => e.RoomLabel).HasColumnType("varchar(100)");

        b.HasOne(e => e.Schedule).WithMany(e => e.Shifts)
            .HasForeignKey(e => e.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Clinic).WithMany().HasForeignKey(e => e.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.DutyRate).WithMany().HasForeignKey(e => e.DutyRateId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.GuaranteeRate).WithMany().HasForeignKey(e => e.GuaranteeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.ScheduleId, e.ShiftDate })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_duty_shift_date");

        b.ToTable(t => t.HasCheckConstraint("ck_duty_shift_time", "end_time <> start_time"));
    }
}

public class DutyShiftDoctorConfiguration : IEntityTypeConfiguration<DutyShiftDoctor>
{
    public void Configure(EntityTypeBuilder<DutyShiftDoctor> b)
    {
        b.Property(e => e.Remark).HasColumnType("varchar(250)");

        b.HasOne(e => e.Shift).WithMany(e => e.Doctors)
            .HasForeignKey(e => e.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.DoctorCode).WithMany().HasForeignKey(e => e.DoctorCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => new { e.ShiftId, e.DoctorCodeId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_duty_shift_doctor");

        b.ToTable(t => t.HasCheckConstraint("ck_duty_shift_doctor_time",
            "work_start IS NULL OR work_end IS NULL OR work_end > work_start"));
    }
}
