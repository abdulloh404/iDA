using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Ida.Infrastructure.Databases.Provisioning;

internal sealed class IngestSchemaProvisioner
{
    private static readonly IReadOnlyDictionary<string, ProjectionDefinition> ProjectionTables =
        new Dictionary<string, ProjectionDefinition>(StringComparer.Ordinal)
        {
            ["his_invoice"] = new("trn_his_invoice", ["INVOICE_NO", "INVOICE_DATE", "REQUEST_NO", "TREATMENT_CODE", "DF_DOCTOR_CODE", "VISIT_NO", "AMOUNT_AFT_DISCOUNT"]),
            ["his_xray"] = new("trn_his_xray", ["INVOICE_NO", "REQUEST_NO", "XRAY_CODE", "TREATMENT_CODE", "DF_DOCTOR_CODE", "VISIT_NO", "RESULT_DATE"]),
            ["his_none_df"] = new("trn_his_none_df", ["INVOICE_NO", "INVOICE_DATE", "ADMISSION_CODE", "RIGHT_CODE", "NONEDF_AMOUNT"]),
            ["his_accrual_no_invoice"] = new("trn_his_accrual_no_invoice", ["VISIT_NO", "ACCRUAL_DATE", "REQUEST_NO", "TREATMENT_CODE", "DOCTOR_CODE", "AMOUNT_AFT_DISCOUNT", "INV_IS_VOID"]),
            ["oracle_ar"] = new("trn_oracle_ar", ["CASH_RECEIPT_ID", "RECEIVABLE_APPLICATION_ID", "RECEIPTNO", "INVOICENO", "RECEIPTDATE", "AMOUNTBEFDISCOUNT", "DOCTYPE", "ISVOID"])
        };

    public async Task ApplyCoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        var script = File.ReadAllText(ProvisioningSql.ResolveAsset("Sql", "install.sql"));
        var marker = "CREATE TABLE IF NOT EXISTS bu.ingest_interface_config";
        var position = script.IndexOf(marker, StringComparison.Ordinal);
        if (position < 0) throw new InvalidDataException("The ingest schema is missing its BU boundary.");
        var coreScript = script[..position];
        coreScript = RewriteRole(coreScript, core.Username);
        coreScript = DatabaseSql.Rewrite(coreScript, connection);
        EnsureAdditive(coreScript);
        await ProvisioningSql.ExecuteAsync(connection, transaction, coreScript, ct);
        await SeedDefinitionsAsync(connection, transaction, core, ct);
    }

    public async Task ApplyBranchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint branch,
        CancellationToken ct)
    {
        var script = File.ReadAllText(ProvisioningSql.ResolveAsset("Sql", "install.sql"));
        var marker = "CREATE TABLE IF NOT EXISTS bu.ingest_interface_config";
        var position = script.IndexOf(marker, StringComparison.Ordinal);
        if (position < 0) throw new InvalidDataException("The ingest schema is missing its BU boundary.");
        var branchScript = SanitizeBranchScript(script[position..]);
        branchScript = RemoveCrossDatabaseReferences(branchScript);
        branchScript = RewriteRole(branchScript, branch.Username);
        branchScript = RewriteSchemas(branchScript, connection, branch.SchemaName);
        EnsureAdditive(branchScript);
        await ProvisioningSql.ExecuteAsync(connection, transaction, branchScript, ct);

        var cancelledConstraint = DatabaseSql.Rewrite("""
            DO $constraint$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint x
                    JOIN pg_class c ON c.oid=x.conrelid
                    JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='bu' AND c.relname='ingest_schedule'
                        AND x.conname='ingest_schedule_cancelled_disabled_check'
                ) THEN
                    ALTER TABLE bu.ingest_schedule
                        ADD CONSTRAINT ingest_schedule_cancelled_disabled_check
                        CHECK (cancelled_at IS NULL OR NOT enabled);
                END IF;
            END
            $constraint$;
            """, connection).Replace("n.nspname='bu'", $"n.nspname={ProvisioningSql.Literal(branch.SchemaName)}", StringComparison.Ordinal);
        await ProvisioningSql.ExecuteAsync(connection, transaction, cancelledConstraint, ct);

        var jobs = File.ReadAllText(ProvisioningSql.ResolveAsset("Sql", "jobs.sql"));
        jobs = RemoveCrossDatabaseReferences(jobs);
        jobs = RewriteSchemas(jobs, connection, branch.SchemaName);
        EnsureAdditive(jobs);
        await ProvisioningSql.ExecuteAsync(connection, transaction, jobs, ct);
        await InstallProjectionColumnsAsync(connection, transaction, ct);
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"GRANT SELECT, INSERT, UPDATE ON {ProvisioningSql.Identifier(branch.SchemaName)}.ingest_job TO {ProvisioningSql.Identifier(branch.Username)};",
            ct);
    }

    private static string SanitizeBranchScript(string script)
    {
        script = Regex.Replace(script, @"GRANT USAGE ON SCHEMA core, bu TO ida_app;\s*", string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script, @"GRANT SELECT, INSERT, UPDATE ON core\.ingest_interface_definition TO ida_app;\s*", string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"DO \$interface_cleanup\$[\s\S]*?\$interface_cleanup\$;",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"ALTER TABLE bu\.ingest_schedule DROP CONSTRAINT IF EXISTS ingest_schedule_cancelled_disabled_check;\s*ALTER TABLE bu\.ingest_schedule ADD CONSTRAINT ingest_schedule_cancelled_disabled_check\s*CHECK \(cancelled_at IS NULL OR NOT enabled\);",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"ALTER TABLE bu\.ingest_schedule DROP CONSTRAINT IF EXISTS ingest_schedule_interval_unit_check;\s*ALTER TABLE bu\.ingest_schedule ADD CONSTRAINT ingest_schedule_interval_unit_check\s*CHECK \(interval_unit IN \('minute','hour','day'\)\);",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"ALTER TABLE bu\.ingest_batch DROP CONSTRAINT IF EXISTS ingest_batch_source_filter_check;\s*ALTER TABLE bu\.ingest_batch ADD CONSTRAINT ingest_batch_source_filter_check\s*CHECK \(source_filter IN \('all','his','oracle','custom'\)\);",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"ALTER TABLE bu\.ingest_response_page ALTER COLUMN payload DROP NOT NULL;\s*",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"ALTER TABLE bu\.ingest_response_page DROP CONSTRAINT IF EXISTS ingest_response_page_payload_check;\s*",
            string.Empty, RegexOptions.CultureInvariant);
        script = Regex.Replace(script,
            @"DROP TRIGGER IF EXISTS trg_ingest_response_page_append_only ON bu\.ingest_response_page;\s*",
            string.Empty, RegexOptions.CultureInvariant);
        return script.Replace("CREATE TRIGGER trg_ingest_response_page_append_only", "CREATE OR REPLACE TRIGGER trg_ingest_response_page_append_only", StringComparison.Ordinal);
    }

    private static string RemoveCrossDatabaseReferences(string script) =>
        Regex.Replace(script,
            @"\s+REFERENCES\s+core\.[a-z_][a-z0-9_]*\s*\([^)]*\)",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string RewriteRole(string script, string username)
    {
        script = script.Replace("rolname='ida_app'", $"rolname={ProvisioningSql.Literal(username)}", StringComparison.Ordinal);
        script = Regex.Replace(script, @"\bTO\s+ida_app\b", "TO " + ProvisioningSql.Identifier(username), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        script = Regex.Replace(script, @"\bFROM\s+ida_app\b", "FROM " + ProvisioningSql.Identifier(username), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return script;
    }

    private static string RewriteSchemas(string script, NpgsqlConnection connection, string branchSchema)
    {
        script = DatabaseSql.Rewrite(script, connection);
        return script.Replace("schemaname='bu'", $"schemaname={ProvisioningSql.Literal(branchSchema)}", StringComparison.Ordinal);
    }

    private static void EnsureAdditive(string script)
    {
        if (Regex.IsMatch(script, @"\b(DROP|TRUNCATE)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            throw new InvalidDataException("Database provisioning SQL contains a destructive operation.");
    }

    private async Task SeedDefinitionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProvisioningSql.ResolveAsset("Fixtures", "source-fields.json")));
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var code = item.GetProperty("code").GetString() ?? throw new InvalidDataException("An ingest definition has no code.");
            var name = item.GetProperty("name").GetString() ?? code;
            var sourceSystem = item.TryGetProperty("sourceSystem", out var source)
                ? source.GetString()
                : code == "oracle_ar" ? "Oracle AR" : "HIS";
            var version = code == "oracle_ar" ? "G5-v1.0-MOCK" : "CustomerPostmanExample-2026-09-24+G5-v1.4-MOCK";
            var sql = DatabaseSql.Rewrite("""
                INSERT INTO core.ingest_interface_definition(id,code,source_system,legacy_contract_version,fields,display_name)
                VALUES($1,$2,$3,$4,$5::jsonb,$6)
                ON CONFLICT(code) DO UPDATE SET
                    source_system=EXCLUDED.source_system,
                    legacy_contract_version=EXCLUDED.legacy_contract_version,
                    fields=EXCLUDED.fields,
                    display_name=EXCLUDED.display_name
                """, connection);
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue(Guid.CreateVersion7());
            command.Parameters.AddWithValue(code);
            command.Parameters.AddWithValue(sourceSystem ?? "HIS");
            command.Parameters.AddWithValue(version);
            command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, item.GetProperty("fields").GetRawText());
            command.Parameters.AddWithValue(name);
            await command.ExecuteNonQueryAsync(ct);
        }
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"GRANT SELECT, INSERT, UPDATE ON {ProvisioningSql.Identifier(core.SchemaName)}.ingest_interface_definition TO {ProvisioningSql.Identifier(core.Username)};",
            ct);
    }

    private static async Task InstallProjectionColumnsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProvisioningSql.ResolveAsset("Fixtures", "source-fields.json")));
        foreach (var dataset in document.RootElement.EnumerateArray())
        {
            var code = dataset.GetProperty("code").GetString();
            if (code is null || !ProjectionTables.TryGetValue(code, out var projection)) continue;
            var types = dataset.TryGetProperty("postmanTypes", out var configuredTypes)
                ? configuredTypes
                : default;
            var additions = new List<string>();
            foreach (var fieldElement in dataset.GetProperty("fields").EnumerateArray())
            {
                var field = fieldElement.GetString() ?? throw new InvalidDataException($"{code} contains an empty source field.");
                if (!Regex.IsMatch(field, "^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant))
                    throw new InvalidDataException($"Unsafe source field name: {code}.{field}.");
                if (projection.CoreSources.Contains(field)) continue;
                var kind = types.ValueKind == JsonValueKind.Object && types.TryGetProperty(field, out var type)
                    ? type.GetString()
                    : null;
                var sqlType = kind switch
                {
                    "number" => "numeric",
                    "boolean" => "boolean",
                    _ => "text"
                };
                additions.Add($"ADD COLUMN IF NOT EXISTS {ProvisioningSql.Identifier(field.ToLowerInvariant())} {sqlType}");
            }
            additions.Add("ADD COLUMN IF NOT EXISTS projection_schema_version integer NOT NULL DEFAULT 1");
            var sql = DatabaseSql.Rewrite($"ALTER TABLE bu.{ProvisioningSql.Identifier(projection.Table)} {string.Join(',', additions.Distinct(StringComparer.Ordinal))}", connection);
            await ProvisioningSql.ExecuteAsync(connection, transaction, sql, ct);
        }
    }

    private sealed record ProjectionDefinition(string Table, string[] CoreSources);
}
