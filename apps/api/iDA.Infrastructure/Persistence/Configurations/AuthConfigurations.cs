using Ida.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.HasIndex(e => e.Username)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_app_user_username");

        b.Property(e => e.PasswordHash).HasColumnType("varchar(255)");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_app_user_auth_type", "auth_type IN ('LOCAL','AD')");

            t.HasCheckConstraint("ck_app_user_local_password",
                "auth_type <> 'LOCAL' OR password_hash IS NOT NULL");
        });

        b.HasMany(e => e.HospitalRoles).WithOne(e => e.User).HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b) =>
        b.HasMany(e => e.Permissions).WithOne(e => e.Role).HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {

        b.Property(e => e.Code).HasColumnType("varchar(80)");
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {

        b.HasKey(e => new { e.RoleId, e.PermissionId });

        b.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserHospitalRoleConfiguration : IEntityTypeConfiguration<UserHospitalRole>
{
    public void Configure(EntityTypeBuilder<UserHospitalRole> b)
    {
        b.HasIndex(e => new { e.UserId, e.HospitalId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_user_hospital");

        b.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("is_default AND deleted_at IS NULL")
            .HasDatabaseName("uq_user_default_hospital");

        b.HasOne(e => e.Hospital).WithMany().HasForeignKey(e => e.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuthLogConfiguration : IEntityTypeConfiguration<AuthLog>
{
    public void Configure(EntityTypeBuilder<AuthLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).UseIdentityByDefaultColumn();
        b.Property(e => e.ClientIp).HasColumnType("inet").HasConversion(new IpAddressConverter());
        b.Property(e => e.UserAgent).HasColumnType("varchar(500)");

        b.HasOne<Ida.Domain.Core.Hospital>()
            .WithMany()
            .HasForeignKey(e => e.HospitalId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(e => new { e.Username, e.OccurredAt })
            .HasDatabaseName("ix_auth_log_user");
    }
}
