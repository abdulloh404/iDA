using System.Text.RegularExpressions;
using Ida.Domain.Common;
using Ida.Domain.Core;
using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal sealed class CoreLookupProvisioner(IConfiguration configuration, DatabaseRegistry registry)
{
    private const string BranchSchema = "branch";
    private const string ServerName = "ida_core_registry";

    public async Task CreateViewsAsync(
        IdaDbContext context,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        var credentials = await EnsureLookupRoleAsync(connection, transaction, core, ct);
        var required = RequiredLookupTables(core);
        var model = context.GetService<IDesignTimeModel>().Model;
        var concurrencyTables = model.GetEntityTypes()
            .Where(entity => typeof(IConcurrencyAware).IsAssignableFrom(entity.ClrType))
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null && required.Contains(name))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        var tables = await ReadTablesAsync(connection, transaction, core.SchemaName, required, ct);
        var missing = required.Except(tables.Select(table => table.Name), StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Core lookup tables are missing: {string.Join(", ", missing)}.");

        var role = ProvisioningSql.Identifier(credentials.Username);
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA {ProvisioningSql.Identifier(core.SchemaName)} FROM {role}; REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA {ProvisioningSql.Identifier(BranchSchema)} FROM {role}; REVOKE ALL PRIVILEGES ON SCHEMA {ProvisioningSql.Identifier(core.SchemaName)}, {ProvisioningSql.Identifier(BranchSchema)} FROM {role}; GRANT CONNECT ON DATABASE {ProvisioningSql.Identifier(core.DatabaseName)} TO {role}; GRANT USAGE ON SCHEMA {ProvisioningSql.Identifier(core.SchemaName)}, {ProvisioningSql.Identifier(BranchSchema)} TO {role};",
            ct);

        foreach (var table in tables)
        {
            var view = "lookup_" + table.Name;
            var safeColumns = table.Columns.Where(column => !IsSensitive(column.Name)).ToArray();
            if (safeColumns.Length == 0)
                throw new InvalidOperationException($"Core lookup table {table.Name} has no safe columns.");
            var projection = string.Join(",", table.Columns.Select(column => IsSensitive(column.Name)
                ? $"NULL::{column.Type} AS {ProvisioningSql.Identifier(column.Name)}"
                : ProvisioningSql.Identifier(column.Name)));
            if (concurrencyTables.Contains(table.Name))
                projection += $",{ProvisioningSql.Identifier(table.Name)}.xmin AS row_version";
            var viewName = $"{ProvisioningSql.Identifier(BranchSchema)}.{ProvisioningSql.Identifier(view)}";
            var tableName = $"{ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(table.Name)}";
            var columnGrant = string.Join(",", safeColumns.Select(column => ProvisioningSql.Identifier(column.Name)));
            var sql = $"CREATE OR REPLACE VIEW {viewName} AS SELECT {projection} FROM {tableName}; GRANT SELECT ({columnGrant}) ON TABLE {tableName} TO {role}; GRANT SELECT ON {viewName} TO {role};";
            await ProvisioningSql.ExecuteAsync(connection, transaction, sql, ct);
        }
    }

    public async Task CreateForeignTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        var credentials = ReadLookupCredentials();
        var required = RequiredLookupTables(core);
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"CREATE EXTENSION IF NOT EXISTS postgres_fdw WITH SCHEMA {ProvisioningSql.Identifier(branch.SchemaName)}; CREATE SCHEMA IF NOT EXISTS {ProvisioningSql.Identifier(core.SchemaName)};",
            ct);
        await ConfigureServerAsync(connection, transaction, core, ct);
        await ConfigureUserMappingAsync(connection, transaction, branch, credentials, ct);

        await using var coreConnection = await registry.OpenAsync(core, ct, administrator: true);
        var views = await ReadViewsAsync(coreConnection, BranchSchema, required, ct);
        var missing = required.Except(views.Select(view => view.Name["lookup_".Length..]), StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Core lookup views are missing: {string.Join(", ", missing)}.");

        var created = new List<string>();
        foreach (var view in views)
        {
            var localName = view.Name["lookup_".Length..];
            var relationKind = await RelationKindAsync(connection, transaction, core.SchemaName, localName, ct);
            if (relationKind is not null && relationKind != "f")
                throw new InvalidOperationException($"{branch.Code}: {core.SchemaName}.{localName} must be a foreign table, but an incompatible relation already exists.");
            if (relationKind is null)
            {
                var columns = string.Join(",", view.Columns.Select(column =>
                    $"{ProvisioningSql.Identifier(column.Name)} {column.Type}"));
                var sql = $"CREATE FOREIGN TABLE {ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(localName)} ({columns}) SERVER {ProvisioningSql.Identifier(ServerName)} OPTIONS (schema_name {ProvisioningSql.Literal(BranchSchema)}, table_name {ProvisioningSql.Literal(view.Name)}, updatable 'false');";
                await ProvisioningSql.ExecuteAsync(connection, transaction, sql, ct);
            }
            else
            {
                await UpdateForeignTableAsync(connection, transaction, branch, core.SchemaName, localName, view.Columns, ct);
                await ProvisioningSql.ExecuteAsync(connection, transaction,
                    $"ALTER FOREIGN TABLE {ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(localName)} OPTIONS (SET schema_name {ProvisioningSql.Literal(BranchSchema)});",
                    ct);
            }
            created.Add($"{ProvisioningSql.Identifier(core.SchemaName)}.{ProvisioningSql.Identifier(localName)}");
        }

        var role = ProvisioningSql.Identifier(branch.Username);
        var grants = created.Count == 0
            ? string.Empty
            : $"GRANT SELECT ON TABLE {string.Join(',', created)} TO {role};";
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA {ProvisioningSql.Identifier(core.SchemaName)} FROM {role}; GRANT USAGE ON SCHEMA {ProvisioningSql.Identifier(core.SchemaName)} TO {role}; {grants}",
            ct);
    }

    private async Task<LookupCredentials> EnsureLookupRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        var credentials = ReadLookupCredentials();
        var admin = new NpgsqlConnectionStringBuilder(connection.ConnectionString).Username;
        if (new[] { core.Username, admin, "postgres" }.Any(value => string.Equals(value, credentials.Username, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("CORE_DB_LOOKUP_USERNAME must identify a dedicated lookup role.");

        bool? privileged = null;
        await using (var command = new NpgsqlCommand("""
            SELECT rolsuper OR rolbypassrls OR rolcreatedb OR rolcreaterole OR rolreplication OR
                EXISTS(SELECT 1 FROM pg_auth_members m WHERE m.member=r.oid)
            FROM pg_roles r WHERE rolname=$1
            """, connection, transaction))
        {
            command.Parameters.AddWithValue(credentials.Username);
            var result = await command.ExecuteScalarAsync(ct);
            if (result is not null) privileged = Convert.ToBoolean(result, System.Globalization.CultureInfo.InvariantCulture);
        }
        if (privileged == true)
            throw new InvalidOperationException($"Existing role '{credentials.Username}' is privileged or belongs to another role and cannot be used for Core lookups.");

        var role = ProvisioningSql.Identifier(credentials.Username);
        var password = ProvisioningSql.Literal(credentials.Password);
        var sql = privileged is null
            ? $"CREATE ROLE {role} WITH LOGIN PASSWORD {password} NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;"
            : $"ALTER ROLE {role} WITH LOGIN PASSWORD {password} NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;";
        await ProvisioningSql.ExecuteAsync(connection, transaction, sql, ct);
        return credentials;
    }

    private LookupCredentials ReadLookupCredentials()
    {
        var username = configuration["CORE_DB_LOOKUP_USERNAME"] ?? "core_lookup";
        var password = configuration["CORE_DB_LOOKUP_PASSWORD"];
        if (!Regex.IsMatch(username, "^[a-z_][a-z0-9_]{0,62}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("CORE_DB_LOOKUP_USERNAME must be a lowercase PostgreSQL identifier.");
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Set CORE_DB_LOOKUP_PASSWORD before provisioning databases.");
        return new LookupCredentials(username, password);
    }

    private async Task ConfigureServerAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        string[]? existing = null;
        await using (var command = new NpgsqlCommand(
            "SELECT srvoptions FROM pg_foreign_server WHERE srvname=$1",
            connection,
            transaction))
        {
            command.Parameters.AddWithValue(ServerName);
            var result = await command.ExecuteScalarAsync(ct);
            if (result is string[] options) existing = options;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = core.Host,
            ["port"] = core.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["dbname"] = core.DatabaseName,
            ["sslmode"] = core.SslMode.ToLowerInvariant()
        };
        if (existing is null)
        {
            var options = string.Join(",", values.Select(pair => $"{pair.Key} {ProvisioningSql.Literal(pair.Value)}"));
            await ProvisioningSql.ExecuteAsync(connection, transaction,
                $"CREATE SERVER {ProvisioningSql.Identifier(ServerName)} FOREIGN DATA WRAPPER postgres_fdw OPTIONS ({options});",
                ct);
            return;
        }

        var keys = existing.Select(option => option.Split('=', 2)[0]).ToHashSet(StringComparer.Ordinal);
        var changes = string.Join(",", values.Select(pair =>
            $"{(keys.Contains(pair.Key) ? "SET" : "ADD")} {pair.Key} {ProvisioningSql.Literal(pair.Value)}"));
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"ALTER SERVER {ProvisioningSql.Identifier(ServerName)} OPTIONS ({changes});",
            ct);
    }

    private static async Task ConfigureUserMappingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        LookupCredentials credentials,
        CancellationToken ct)
    {
        string[]? existing = null;
        await using (var command = new NpgsqlCommand("""
            SELECT umoptions FROM pg_user_mappings
            WHERE srvname=$1 AND usename=$2
            """, connection, transaction))
        {
            command.Parameters.AddWithValue(ServerName);
            command.Parameters.AddWithValue(branch.Username);
            var result = await command.ExecuteScalarAsync(ct);
            if (result is string[] options) existing = options;
        }
        var server = ProvisioningSql.Identifier(ServerName);
        var branchRole = ProvisioningSql.Identifier(branch.Username);
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["user"] = credentials.Username,
            ["password"] = credentials.Password
        };
        if (existing is null)
        {
            var options = string.Join(",", values.Select(pair => $"{pair.Key} {ProvisioningSql.Literal(pair.Value)}"));
            await ProvisioningSql.ExecuteAsync(connection, transaction,
                $"CREATE USER MAPPING FOR {branchRole} SERVER {server} OPTIONS ({options});",
                ct);
        }
        else
        {
            var keys = existing.Select(option => option.Split('=', 2)[0]).ToHashSet(StringComparer.Ordinal);
            var changes = string.Join(",", values.Select(pair =>
                $"{(keys.Contains(pair.Key) ? "SET" : "ADD")} {pair.Key} {ProvisioningSql.Literal(pair.Value)}"));
            await ProvisioningSql.ExecuteAsync(connection, transaction,
                $"ALTER USER MAPPING FOR {branchRole} SERVER {server} OPTIONS ({changes});",
                ct);
        }
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"GRANT USAGE ON FOREIGN SERVER {server} TO {branchRole};",
            ct);
    }

    private HashSet<string> RequiredLookupTables(DatabaseEndpoint core)
    {
        var modelEndpoint = new DatabaseEndpoint(
            "LOOKUP_MODEL",
            "lookup_model",
            "LOOKUP_MODEL",
            core.Host,
            core.Port,
            core.DatabaseName,
            "bu",
            core.Username,
            core.PasswordEnvironment);
        using var context = DatabaseContexts.CreateSchemaContext(
            modelEndpoint,
            core,
            registry.ConnectionString(core, administrator: true));
        var model = context.GetService<IDesignTimeModel>().Model;
        var queue = new Queue<IReadOnlyEntityType>(model.GetEntityTypes()
            .Where(entity => DatabaseLayout.IsBranchTable(entity.ClrType))
            .SelectMany(entity => entity.GetForeignKeys())
            .Select(foreignKey => foreignKey.PrincipalEntityType)
            .Where(IsAllowedCoreLookup));
        var entities = new HashSet<IReadOnlyEntityType>();
        while (queue.TryDequeue(out var entity))
        {
            if (!entities.Add(entity)) continue;
            foreach (var principal in entity.GetForeignKeys().Select(foreignKey => foreignKey.PrincipalEntityType).Where(IsAllowedCoreLookup))
                queue.Enqueue(principal);
        }
        var required = entities
            .Select(entity => entity.GetViewName() ?? entity.GetTableName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        required.Add("ingest_interface_definition");
        required.Remove("audit_log");
        return required;
    }

    private static bool IsAllowedCoreLookup(IReadOnlyEntityType entity) =>
        !DatabaseLayout.IsBranchTable(entity.ClrType) &&
        !string.Equals(entity.ClrType.Namespace, "Ida.Domain.Auth", StringComparison.Ordinal) &&
        entity.ClrType != typeof(AuditLog);

    private static bool IsSensitive(string column) =>
        column.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        column.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        column.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        column.EndsWith("_hash", StringComparison.OrdinalIgnoreCase) ||
        column.EndsWith("_enc", StringComparison.OrdinalIgnoreCase) ||
        column is "tenant_db_name" or "tenant_db_host";

    private static async Task<List<TableDefinition>> ReadTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        IReadOnlySet<string> required,
        CancellationToken ct)
    {
        var tables = new Dictionary<string, List<ColumnDefinition>>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand("""
            SELECT c.relname,a.attname,format_type(a.atttypid,a.atttypmod)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_attribute a ON a.attrelid=c.oid
            WHERE n.nspname=$1 AND c.relkind IN ('r','p') AND a.attnum>0 AND NOT a.attisdropped
            ORDER BY c.relname,a.attnum
            """, connection, transaction);
        command.Parameters.AddWithValue(schema);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var table = reader.GetString(0);
            if (!required.Contains(table)) continue;
            if (!tables.TryGetValue(table, out var columns)) tables.Add(table, columns = []);
            columns.Add(new ColumnDefinition(reader.GetString(1), reader.GetString(2)));
        }
        return tables.Select(pair => new TableDefinition(pair.Key, pair.Value)).ToList();
    }

    private static async Task<List<TableDefinition>> ReadViewsAsync(
        NpgsqlConnection connection,
        string schema,
        IReadOnlySet<string> required,
        CancellationToken ct)
    {
        var views = new Dictionary<string, List<ColumnDefinition>>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand("""
            SELECT c.relname,a.attname,format_type(a.atttypid,a.atttypmod)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_attribute a ON a.attrelid=c.oid
            WHERE n.nspname=$1 AND c.relkind='v' AND c.relname LIKE 'lookup\_%' ESCAPE '\' AND a.attnum>0 AND NOT a.attisdropped
            ORDER BY c.relname,a.attnum
            """, connection);
        command.Parameters.AddWithValue(schema);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var view = reader.GetString(0);
            if (!view.StartsWith("lookup_", StringComparison.Ordinal) || !required.Contains(view["lookup_".Length..])) continue;
            if (!views.TryGetValue(view, out var columns)) views.Add(view, columns = []);
            columns.Add(new ColumnDefinition(reader.GetString(1), reader.GetString(2)));
        }
        return views.Select(pair => new TableDefinition(pair.Key, pair.Value)).ToList();
    }

    private static async Task<string?> RelationKindAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema, string name, CancellationToken ct) =>
        await ProvisioningSql.ScalarAsync<string>(connection, transaction,
            "SELECT c.relkind::text FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname=$1 AND c.relname=$2",
            ct, schema, name);

    private static async Task UpdateForeignTableAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        string schema,
        string table,
        IReadOnlyList<ColumnDefinition> expected,
        CancellationToken ct)
    {
        var actual = new Dictionary<string, string>(StringComparer.Ordinal);
        await using (var command = new NpgsqlCommand("""
            SELECT a.attname,format_type(a.atttypid,a.atttypmod)
            FROM pg_attribute a
            JOIN pg_class c ON c.oid=a.attrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=$1 AND c.relname=$2 AND a.attnum>0 AND NOT a.attisdropped
            """, connection, transaction))
        {
            command.Parameters.AddWithValue(schema);
            command.Parameters.AddWithValue(table);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) actual.Add(reader.GetString(0), reader.GetString(1));
        }

        foreach (var column in expected)
        {
            if (!actual.TryGetValue(column.Name, out var type))
            {
                await ProvisioningSql.ExecuteAsync(connection, transaction,
                    $"ALTER FOREIGN TABLE {ProvisioningSql.Identifier(schema)}.{ProvisioningSql.Identifier(table)} ADD COLUMN {ProvisioningSql.Identifier(column.Name)} {column.Type};",
                    ct);
                continue;
            }
            if (!string.Equals(type.Replace("\"", "", StringComparison.Ordinal), column.Type.Replace("\"", "", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{branch.Code}: foreign table {schema}.{table} is incompatible at column {column.Name}.");
        }
    }

    private sealed record LookupCredentials(string Username, string Password);
    private sealed record ColumnDefinition(string Name, string Type);
    private sealed record TableDefinition(string Name, IReadOnlyList<ColumnDefinition> Columns);
}
