using Ida.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ida.Infrastructure.Persistence;

public static class EfModelConventions
{
    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>
        UtcConverter = new(v => v.ToUniversalTime(), v => v);

    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, DateTimeOffset?>
        UtcNullableConverter = new(v => v.HasValue ? v.Value.ToUniversalTime() : v, v => v);

    public static void Apply(ModelBuilder model, string defaultSchema, DbContext context, string tenantProperty, Action<EntityTypeBuilder, IMutableEntityType>? configureRelations = null)
    {
        foreach (var entity in model.Model.GetEntityTypes())
        {
            var clr = entity.ClrType;

            var explicitlyNamed = ((IConventionEntityType)entity)
                .GetTableNameConfigurationSource() == ConfigurationSource.Explicit;

            if (!explicitlyNamed)
            {
                entity.SetTableName(Naming.ToSnakeCase(clr.Name));
                entity.SetSchema(defaultSchema);
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
            configureRelations?.Invoke(builder, entity);
            ApplyRowFilter(builder, clr, context, tenantProperty);
            ApplyIndexes(builder, entity, clr, table);
        }
    }

    private static void ApplyClientGeneratedKey(IMutableEntityType entity)
    {
        var key = entity.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1) return;

        var property = key.Properties[0];
        if (property.ClrType == typeof(Guid)) property.ValueGenerated = ValueGenerated.Never;
    }

    private static void ApplyRowFilter(EntityTypeBuilder builder, Type clr, DbContext context, string tenantProperty)
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
                System.Linq.Expressions.Expression.Constant(context),
                tenantProperty);
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
