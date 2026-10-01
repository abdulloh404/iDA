using Ida.Domain.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class SysEmailTemplateConfiguration : IEntityTypeConfiguration<SysEmailTemplate>
{
    public void Configure(EntityTypeBuilder<SysEmailTemplate> b)
    {
        b.Property(e => e.Code).HasColumnType("varchar(50)");
        b.Property(e => e.Name).HasColumnType("varchar(200)");
        b.Property(e => e.Subject).HasColumnType("varchar(500)");
        b.Property(e => e.Body).HasColumnType("varchar(4000)");

        b.HasIndex(e => e.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_sys_email_template_code");
    }
}

public class SysPasswordPolicyConfiguration : IEntityTypeConfiguration<SysPasswordPolicy>
{
    public void Configure(EntityTypeBuilder<SysPasswordPolicy> b)
    {
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sys_password_policy_days",
                "reset_days BETWEEN 1 AND 999 AND warn_days_before BETWEEN 1 AND 999");
            t.HasCheckConstraint("ck_sys_password_policy_warn",
                "warn_days_before < reset_days");
        });
    }
}

public class SysTermsConfiguration : IEntityTypeConfiguration<SysTerms>
{
    public void Configure(EntityTypeBuilder<SysTerms> b)
    {
        b.Property(e => e.Content).HasColumnType("text");
    }
}
