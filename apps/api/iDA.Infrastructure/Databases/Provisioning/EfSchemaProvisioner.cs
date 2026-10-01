using System.Text.RegularExpressions;
using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class EfSchemaProvisioner
{
    public static async Task ApplyAsync(
        IdaDbContext context,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint endpoint,
        string coreSchemaName,
        CancellationToken ct)
    {
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"""
            CREATE SCHEMA IF NOT EXISTS {ProvisioningSql.Identifier(endpoint.SchemaName)};
            CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA {ProvisioningSql.Identifier(endpoint.SchemaName)};
            DO $$
            DECLARE
                extension_name text;
            BEGIN
                FOR extension_name IN
                    SELECT e.extname FROM pg_extension e
                    JOIN pg_namespace n ON n.oid=e.extnamespace
                    WHERE n.nspname='public' AND e.extname='pgcrypto'
                LOOP
                    EXECUTE format('ALTER EXTENSION %I SET SCHEMA %I', extension_name, {ProvisioningSql.Literal(endpoint.SchemaName)});
                END LOOP;
            END $$;
            """,
            ct);

        var model = context.GetService<IDesignTimeModel>().Model;
        if (endpoint.Kind == "bu")
            await LegacyCoreLookupCleanup.ApplyAsync(model, connection, transaction, endpoint, coreSchemaName, ct);
        await EnsureEnumsAsync(model, connection, transaction, endpoint.SchemaName, ct);

        var differ = context.GetService<IMigrationsModelDiffer>();
        var operations = differ.GetDifferences(null, model.GetRelationalModel()).ToList();
        var selected = new List<MigrationOperation>();
        var newTables = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in operations)
        {
            switch (operation)
            {
                case EnsureSchemaOperation:
                case AlterDatabaseOperation:
                    break;
                case CreateTableOperation table when SameSchema(table.Schema, endpoint.SchemaName):
                    RemoveCrossSchemaForeignKeys(table, endpoint.SchemaName);
                    if (await RelationKindAsync(connection, transaction, endpoint.SchemaName, table.Name, ct) is { } kind)
                    {
                        if (kind != "r" && kind != "p")
                            throw new InvalidOperationException($"{endpoint.ConnectionKey}: {endpoint.SchemaName}.{table.Name} exists but is not a table.");
                        await ValidateTableAsync(connection, transaction, endpoint, table, ct);
                    }
                    else
                    {
                        selected.Add(table);
                        newTables.Add(table.Name);
                    }
                    break;
                case CreateIndexOperation index when SameSchema(index.Schema, endpoint.SchemaName):
                    if (newTables.Contains(index.Table) || !await IndexExistsAsync(connection, transaction, endpoint.SchemaName, index.Name, ct))
                        selected.Add(index);
                    break;
                case AddForeignKeyOperation foreignKey when SameSchema(foreignKey.Schema, endpoint.SchemaName) && SameSchema(foreignKey.PrincipalSchema, endpoint.SchemaName):
                    if (!await ConstraintExistsAsync(connection, transaction, endpoint.SchemaName, foreignKey.Table, foreignKey.Name, ct))
                        selected.Add(foreignKey);
                    break;
                case CreateSequenceOperation sequence when SameSchema(sequence.Schema, endpoint.SchemaName):
                    if (!await SequenceExistsAsync(connection, transaction, endpoint.SchemaName, sequence.Name, ct))
                        selected.Add(sequence);
                    break;
                case InsertDataOperation insert when SameSchema(insert.Schema, endpoint.SchemaName) && newTables.Contains(insert.Table):
                    selected.Add(insert);
                    break;
                case CreateTableOperation or CreateIndexOperation or AddForeignKeyOperation or CreateSequenceOperation or InsertDataOperation:
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported non-additive database operation '{operation.GetType().Name}' for {endpoint.ConnectionKey}.");
            }
        }

        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(selected, model, MigrationsSqlGenerationOptions.Default))
        {
            if (command.TransactionSuppressed)
                throw new InvalidOperationException($"Database operation for {endpoint.ConnectionKey} cannot run inside the provisioning transaction.");
            await ProvisioningSql.ExecuteAsync(connection, transaction, command.CommandText, ct);
        }
    }

    private static bool SameSchema(string? value, string expected) =>
        string.Equals(value ?? expected, expected, StringComparison.Ordinal);

    private static void RemoveCrossSchemaForeignKeys(CreateTableOperation table, string schema)
    {
        for (var index = table.ForeignKeys.Count - 1; index >= 0; index--)
        {
            var foreignKey = table.ForeignKeys[index];
            if (!SameSchema(foreignKey.PrincipalSchema, schema)) table.ForeignKeys.RemoveAt(index);
        }
    }

    private static async Task EnsureEnumsAsync(
        IModel model,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string endpointSchema,
        CancellationToken ct)
    {
        foreach (var definition in model.GetPostgresEnums())
        {
            var schema = string.IsNullOrWhiteSpace(definition.Schema) || definition.Schema == IdaDbContext.CoreSchema
                ? endpointSchema
                : definition.Schema;
            var labels = new List<string>();
            await using (var command = new NpgsqlCommand("""
                SELECT e.enumlabel
                FROM pg_type t
                JOIN pg_namespace n ON n.oid=t.typnamespace
                JOIN pg_enum e ON e.enumtypid=t.oid
                WHERE n.nspname=$1 AND t.typname=$2
                ORDER BY e.enumsortorder
                """, connection, transaction))
            {
                command.Parameters.AddWithValue(schema);
                command.Parameters.AddWithValue(definition.Name);
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct)) labels.Add(reader.GetString(0));
            }

            if (labels.Count == 0)
            {
                var values = string.Join(",", definition.Labels.Select(ProvisioningSql.Literal));
                await ProvisioningSql.ExecuteAsync(connection, transaction,
                    $"CREATE TYPE {ProvisioningSql.Identifier(schema)}.{ProvisioningSql.Identifier(definition.Name)} AS ENUM ({values})",
                    ct);
                continue;
            }

            if (!labels.SequenceEqual(definition.Labels, StringComparer.Ordinal))
                throw new InvalidOperationException($"Enum {schema}.{definition.Name} is incompatible with the application model.");
        }
    }

    private static async Task ValidateTableAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint endpoint,
        CreateTableOperation table,
        CancellationToken ct)
    {
        var actual = new Dictionary<string, (string Type, bool Nullable)>(StringComparer.Ordinal);
        await using (var command = new NpgsqlCommand("""
            SELECT a.attname,
                CASE WHEN t.typtype='e' THEN quote_ident(tn.nspname)||'.'||quote_ident(t.typname)
                     ELSE format_type(a.atttypid,a.atttypmod) END,
                NOT a.attnotnull
            FROM pg_attribute a
            JOIN pg_class c ON c.oid=a.attrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_type t ON t.oid=a.atttypid
            JOIN pg_namespace tn ON tn.oid=t.typnamespace
            WHERE n.nspname=$1 AND c.relname=$2 AND a.attnum>0 AND NOT a.attisdropped
            """, connection, transaction))
        {
            command.Parameters.AddWithValue(endpoint.SchemaName);
            command.Parameters.AddWithValue(table.Name);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) actual.Add(reader.GetString(0), (reader.GetString(1), reader.GetBoolean(2)));
        }

        foreach (var expected in table.Columns.Where(column => column.Name != "xmin"))
        {
            if (!actual.TryGetValue(expected.Name, out var found))
                throw new InvalidOperationException($"{endpoint.ConnectionKey}: existing table {endpoint.SchemaName}.{table.Name} is missing column {expected.Name}.");
            if (expected.ColumnType is { Length: > 0 } type && !TypesMatch(type, found.Type))
                throw new InvalidOperationException($"{endpoint.ConnectionKey}: column {endpoint.SchemaName}.{table.Name}.{expected.Name} is {found.Type}, expected {type}.");
            if (found.Nullable != expected.IsNullable)
                throw new InvalidOperationException($"{endpoint.ConnectionKey}: column {endpoint.SchemaName}.{table.Name}.{expected.Name} nullability is incompatible.");
        }
    }

    private static bool TypesMatch(string expected, string actual)
    {
        static string Normalize(string value)
        {
            value = value.Replace("\"", "", StringComparison.Ordinal).ToLowerInvariant();
            value = Regex.Replace(value, "\\s+", " ").Trim();
            return value
                .Replace("character varying", "varchar", StringComparison.Ordinal)
                .Replace("character(", "char(", StringComparison.Ordinal)
                .Replace("timestamp with time zone", "timestamptz", StringComparison.Ordinal)
                .Replace("timestamp without time zone", "timestamp", StringComparison.Ordinal)
                .Replace("boolean", "bool", StringComparison.Ordinal);
        }
        return Normalize(expected) == Normalize(actual);
    }

    private static async Task<string?> RelationKindAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string name,
        CancellationToken ct)
    {
        return await ProvisioningSql.ScalarAsync<string>(connection, transaction,
            "SELECT c.relkind::text FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=$1 AND c.relname=$2",
            ct, schema, name);
    }

    private static async Task<bool> IndexExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema, string name, CancellationToken ct) =>
        await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
            "SELECT EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=$1 AND c.relname=$2 AND c.relkind IN ('i','I'))",
            ct, schema, name);

    private static async Task<bool> ConstraintExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema, string table, string name, CancellationToken ct) =>
        await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
            "SELECT EXISTS(SELECT 1 FROM pg_constraint x JOIN pg_class c ON c.oid=x.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=$1 AND c.relname=$2 AND x.conname=$3)",
            ct, schema, table, name);

    private static async Task<bool> SequenceExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema, string name, CancellationToken ct) =>
        await ProvisioningSql.ScalarAsync<bool>(connection, transaction,
            "SELECT EXISTS(SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=$1 AND c.relname=$2 AND c.relkind='S')",
            ct, schema, name);
}
