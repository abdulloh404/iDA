using Ida.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.Property(e => e.ClientIp).HasColumnType("inet").HasConversion(new IpAddressConverter());
        b.HasIndex(e => new { e.TableName, e.RecordPk, e.ChangedAt }).HasDatabaseName("ix_audit_log_record");
        b.ToTable(t => t.HasCheckConstraint("ck_audit_log_action", "action IN ('INSERT','UPDATE','DELETE')"));
    }
}
