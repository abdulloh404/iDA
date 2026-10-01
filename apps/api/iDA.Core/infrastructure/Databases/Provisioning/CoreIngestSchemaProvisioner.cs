using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace Ida.Infrastructure.Databases.Provisioning;

internal sealed class CoreIngestSchemaProvisioner
{
    public async Task ApplyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseEndpoint core,
        CancellationToken ct)
    {
        var script = File.ReadAllText(ProvisioningSql.ResolveAsset("Sql", "install.sql"));
        var coreScript = script;
        coreScript = RewriteRole(coreScript, core.Username);
        coreScript = DatabaseSql.Rewrite(coreScript, connection);
        EnsureAdditive(coreScript);
        await ProvisioningSql.ExecuteAsync(connection, transaction, coreScript, ct);
        await SeedDefinitionsAsync(connection, transaction, core, ct);
    }

    private static string RewriteRole(string script, string username)
    {
        script = script.Replace("rolname='ida_app'", $"rolname={ProvisioningSql.Literal(username)}", StringComparison.Ordinal);
        script = Regex.Replace(script, @"\bTO\s+ida_app\b", "TO " + ProvisioningSql.Identifier(username), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        script = Regex.Replace(script, @"\bFROM\s+ida_app\b", "FROM " + ProvisioningSql.Identifier(username), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return script;
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
            var category = item.TryGetProperty("dataCategory", out var dataCategory)
                ? dataCategory.GetString() ?? "Unclassified"
                : "Unclassified";
            var sql = DatabaseSql.Rewrite("""
                INSERT INTO core.ingest_interface_definition(id,code,source_system,legacy_contract_version,fields,display_name,data_category)
                VALUES($1,$2,$3,$4,$5::jsonb,$6,$7)
                ON CONFLICT(code) DO UPDATE SET
                    source_system=EXCLUDED.source_system,
                    legacy_contract_version=EXCLUDED.legacy_contract_version,
                    fields=EXCLUDED.fields,
                    display_name=EXCLUDED.display_name,
                    data_category=EXCLUDED.data_category
                """, connection);
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue(Guid.CreateVersion7());
            command.Parameters.AddWithValue(code);
            command.Parameters.AddWithValue(sourceSystem ?? "HIS");
            command.Parameters.AddWithValue(version);
            command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, item.GetProperty("fields").GetRawText());
            command.Parameters.AddWithValue(name);
            command.Parameters.AddWithValue(category);
            await command.ExecuteNonQueryAsync(ct);
        }
        await ProvisioningSql.ExecuteAsync(connection, transaction,
            $"GRANT SELECT, INSERT, UPDATE ON {ProvisioningSql.Identifier(core.SchemaName)}.ingest_interface_definition TO {ProvisioningSql.Identifier(core.Username)};",
            ct);
    }

}
