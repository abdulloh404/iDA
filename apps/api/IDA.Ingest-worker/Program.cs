using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ida.Infrastructure.Configuration;
using Ida.Infrastructure.Databases;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture =
    System.Globalization.CultureInfo.InvariantCulture;
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddIdaSettings()
    .Build();
var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "preview";
var options = args.Skip(1).Select(x => x.Split('=', 2))
    .Where(x => x.Length == 2).ToDictionary(x => x[0].TrimStart('-'), x => x[1],
        StringComparer.OrdinalIgnoreCase);
string Option(string key, string fallback) => options.GetValueOrDefault(key, fallback);
var date = DateOnly.ParseExact(Option("date", DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")), "yyyy-MM-dd");
var revision = int.Parse(Option("revision", "1"));
var receiptSeq = int.Parse(Option("receipt-seq", "1"));
var source = Option("source", "all").ToLowerInvariant();
var simulateFailureAfterCapture = Option("simulate-failure-after-capture", "false") switch
{
    "true" => true,
    "false" => false,
    _ => throw new ArgumentException("--simulate-failure-after-capture must be true or false.")
};
if (revision is < 1 or > 2) throw new ArgumentException("Demo revision must be 1 or 2.");
if (receiptSeq is < 1 or > 3) throw new ArgumentException("Demo receipt sequence must be 1, 2 or 3.");
if (source is not ("all" or "his" or "oracle"))
    throw new ArgumentException("--source must be all, his or oracle.");

var schemaPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "source-fields.json");
var datasets = JsonSerializer.Deserialize<List<Dataset>>(File.ReadAllText(schemaPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
if (datasets.Count == 0 || datasets.Select(x => x.Code).Distinct().Count() != datasets.Count ||
    datasets.Any(x => x.Fields.Length == 0 ||
    x.Fields.Distinct(StringComparer.Ordinal).Count() != x.Fields.Length ||
    (x.PostmanTypes?.Keys.Any(key => !x.Fields.Contains(key, StringComparer.Ordinal)) ?? false)))
    throw new InvalidDataException("Source inventory needs unique domains and distinct union fields.");
TransactionProjection.Configure(datasets);
Fixture[] Fixtures(string hospital) => datasets.Where(x => source == "all" ||
        (source == "his" ? x.Code.StartsWith("his_", StringComparison.Ordinal) : x.Code == "oracle_ar"))
    .Select(x => MockFixture.Create(x, hospital, date, revision, receiptSeq)).ToArray();

if (command == "preview")
{
    var previewBu = (config["Api:BuId"] ?? config["BU_ID"])?.Trim().ToUpperInvariant();
    var previewHospital = Option("hospital", string.IsNullOrEmpty(previewBu) ? "PT1" :
        config[$"{previewBu}_HOSPITAL_ID"] ?? previewBu);
    var previewFixtures = Fixtures(previewHospital);
    foreach (var item in previewFixtures)
        Console.WriteLine($"{item.Dataset.Code,-28} {item.Payload.Count,2} fields " +
            $"(Postman={item.Dataset.PostmanTypes?.Count ?? 0}) key={item.SourceKey}");
    var payloadCode = Option("payload", "");
    if (payloadCode.Length > 0)
    {
        var selected = previewFixtures.SingleOrDefault(x => x.Dataset.Code == payloadCode) ??
            throw new ArgumentException($"Unknown dataset for --payload: {payloadCode}.");
        Console.WriteLine(JsonSerializer.Serialize(selected.Response(),
            new JsonSerializerOptions { WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
    var receiptAmount = receiptSeq == 1 ? MockFixture.InvoiceAmount(revision) : 200m;
    Console.WriteLine($"MOCK {previewHospital} {date:yyyy-MM-dd} r{revision} ({source}): " +
        $"invoice={MockFixture.InvoiceAmount(revision):N2}, " +
        $"receipt-{receiptSeq}={receiptAmount:N2}, " +
        $"demo fee after receipt-1={MockFixture.InvoiceAmount(revision) * .20m:N2}");
    return;
}

using var registry = new DatabaseRegistry(config, DatabaseRuntime.Tenant);
var endpoint = registry.FixedBranch;
if (!string.Equals(endpoint.Kind, "bu", StringComparison.OrdinalIgnoreCase) ||
    string.IsNullOrWhiteSpace(endpoint.HospitalId))
    throw new InvalidOperationException($"Database endpoint {endpoint.ConnectionKey} is not a BU branch.");
var hospital = endpoint.HospitalId;
var hospitalSelector = Option("hospital", hospital).Trim();
if (hospitalSelector.Equals("all", StringComparison.OrdinalIgnoreCase) ||
    (!string.Equals(hospitalSelector, endpoint.ConnectionKey, StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(hospitalSelector, hospital, StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException($"--hospital must match configured BU {endpoint.ConnectionKey} or hospital {hospital}.");

if (command == "install")
{
    await new DatabaseProvisioner(registry).InitializeTenantAsync(CancellationToken.None);
    Console.WriteLine($"BU database {endpoint.ConnectionKey} initialized from configuration.");
    return;
}

if (command == "serve")
{
    await Scheduler.Run(registry, datasets);
    return;
}

var fixtures = Fixtures(hospital);
await using var db = await registry.OpenAsync(endpoint);

var bypassesRls = await Db.Scalar(db, null, """
    SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user
    """);
if (Convert.ToBoolean(bypassesRls))
    throw new InvalidOperationException("Runtime connection must not have SUPERUSER or BYPASSRLS.");

await Db.Exec(db, null, "SELECT set_config('app.hospital_id', @hospital, false)",
    ("hospital", hospital));
switch (command)
{
    case "ingest":
        var anyChanged = false;
        var batchId = await Batch.Start(db, hospital, date, source);
        Console.WriteLine($"batch={batchId} datasets={fixtures.Length} source={source}");
        try
        {
            foreach (var item in fixtures)
            {
                var run = await Ingest.One(db, item, hospital, date, revision, batchId,
                    simulateFailureAfterCapture);
                anyChanged |= run.Changed > 0;
                Console.WriteLine($"{item.Dataset.Code}: run={run.Id} changed={run.Changed} duplicate={run.Duplicate}");
            }
            await Batch.Publish(db, hospital, batchId);
        }
        catch (Exception ex)
        {
            await Batch.Fail(db, hospital, batchId, ex.Message);
            throw;
        }
        if (anyChanged) await Calculation.Recompute(db, hospital, date);
        else Console.WriteLine("No source changes; latest daily demo result remains unchanged.");
        break;
    case "status":
        await Status.Print(db, hospital, date);
        break;
    case "project":
        var projected = await TransactionProjection.Backfill(db, hospital);
        Console.WriteLine($"Transaction projection {hospital}: {projected} version(s) synchronized.");
        break;
    case "verify":
        await Verify.Payloads(db, hospital, date, fixtures);
        break;
    case "withdraw":
        if (!Guid.TryParse(Option("run", ""), out var runId))
            throw new ArgumentException("withdraw requires --run=<run-guid> from status.");
        var actor = Option("actor", "").Trim();
        var reason = Option("reason", "").Trim();
        if (actor.Length is < 2 or > 100 || reason.Length is < 5 or > 500)
            throw new ArgumentException("withdraw requires --actor=<tester> and --reason=<5-500 characters>.");
        await Ingest.Withdraw(db, hospital, runId, actor, reason);
        break;
    default:
        throw new ArgumentException("Commands: preview, install, ingest, serve, project, status, verify, withdraw.");
}

internal sealed record Dataset(string Code, string Name, string[] Fields,
    Dictionary<string, string>? PostmanTypes = null, string? SourceSystem = null);
internal sealed record Fixture(Dataset Dataset, string SourceKey, JsonObject Payload)
{
    public JsonObject Response() => MockResponse.Wrap(Payload);
}

internal static class MockResponse
{

    public static JsonObject Wrap(JsonObject row) => new()
    {
        ["status"] = JsonValue.Create(200),
        ["results"] = new JsonArray(row.DeepClone()),
        ["pageNumber"] = JsonValue.Create(1),
        ["totalPages"] = JsonValue.Create(1),
        ["totalCount"] = JsonValue.Create(1),
        ["hasPreviousPage"] = JsonValue.Create(false),
        ["hasNextPage"] = JsonValue.Create(false)
    };
}

internal static class MockFixture
{
    public static decimal InvoiceAmount(int revision) => revision == 1 ? 1000m : 1200m;

    public static Fixture Create(Dataset dataset, string hospital, DateOnly date,
        int revision, int receiptSeq)
    {

        var payload = new JsonObject();
        foreach (var field in dataset.Fields)
            payload[field] = MockValue.Create(dataset, field, hospital, date, revision, receiptSeq);
        void Set(string key, JsonNode? value)
        {
            if (payload.ContainsKey(key)) payload[key] = value;
        }
        var day = date.ToString("yyyy-MM-dd");
        var amount = InvoiceAmount(revision);
        Set("HOSPITAL_CODE", JsonValue.Create(hospital));
        Set("HOSPITALCODE", JsonValue.Create(hospital));
        Set("INVOICE_NO", JsonValue.Create($"MOCK-{hospital}-{date:yyyyMMdd}"));
        Set("INVOICENO", JsonValue.Create($"MOCK-{hospital}-{date:yyyyMMdd}"));
        Set("INVOICE_DATE", JsonValue.Create(day));
        Set("INVOICEDATE", JsonValue.Create(day));
        Set("RECEIPTDATE", JsonValue.Create(day));
        Set("VISIT_DATE", JsonValue.Create(day));
        Set("VISITDATE", JsonValue.Create(day));
        Set("DF_DOCTOR_CODE", JsonValue.Create("MOCK-DR-01"));
        Set("DOCTOR_CODE", JsonValue.Create("MOCK-DR-01"));
        Set("TREATMENT_CODE", JsonValue.Create("MOCK-TREAT-01"));
        Set("CLINIC_CODE", JsonValue.Create("MOCK-CLINIC-01"));
        Set("CLINIC_NAME_TH", JsonValue.Create(revision == 1 ? "คลินิกทดสอบ 1" : "คลินิกทดสอบ 2"));
        if (dataset.Code == "his_clinic")
            Set("DESCRIPTION_TH", JsonValue.Create(revision == 1 ? "คลินิกทดสอบ 1" : "คลินิกทดสอบ 2"));
        Set("TREATMENT_CATEGORY_CODE", JsonValue.Create("MOCK-CAT-01"));
        Set("SPECIALTY_CODE", JsonValue.Create("MOCK-SPEC-01"));
        Set("SUB_SPECIALTY_CODE", JsonValue.Create("MOCK-SUB-01"));
        Set("RECEIPTNO", JsonValue.Create($"MOCK-REC-{hospital}-{date:yyyyMMdd}-{receiptSeq}"));
        Set("RECEIVABLE_APPLICATION_ID", JsonValue.Create($"MOCK-APP-{hospital}-{date:yyyyMMdd}-{receiptSeq}"));
        Set("DOCTYPE", JsonValue.Create("R"));
        Set("ISVOID", JsonValue.Create("N"));
        if (dataset.Code == "his_invoice")
        {
            Set("AMOUNT_AFT_DISCOUNT", JsonValue.Create(amount));
            Set("AMOUNT_BEF_DISCOUNT", JsonValue.Create(amount));
            Set("INV_AFT_DISCOUNT", JsonValue.Create(amount));
            Set("INV_BEF_DISCOUNT", JsonValue.Create(amount));
            Set("INV_AMOUNT_AFT_DISCOUNT", JsonValue.Create(amount));
            Set("INV_AMOUNT_BEF_DISCOUNT", JsonValue.Create(amount));
            Set("RECEIPT_AMOUNT1", JsonValue.Create(amount));
        }
        if (dataset.Code == "oracle_ar")
        {
            Set("AMOUNTBEFDISCOUNT", JsonValue.Create(receiptSeq == 1 ? amount : 200m));
            Set("INVAMTBEFDISCOUNT", JsonValue.Create(amount));
        }
        Set("NONEDF_AMOUNT", JsonValue.Create(0m));

        var master = dataset.Code is "his_clinic" or "his_treatment_category" or
            "his_treatment" or "his_specialty" or "his_sub_specialty" or
            "his_examination_schedule" or "his_refrain_schedule" or "his_department";
        var key = master ? dataset.Code + ":MOCK-001" : dataset.Code + ":" + hospital + ":" + day;
        if (dataset.Code == "oracle_ar") key += ":receipt-" + receiptSeq;
        foreach (var (field, expected) in dataset.PostmanTypes ?? [])
        {
            var value = payload[field];
            if (value is null) continue;
            var actual = JsonSerializer.SerializeToElement(value).ValueKind;
            var valid = expected switch
            {
                "string" => actual == JsonValueKind.String,
                "number" => actual == JsonValueKind.Number,
                "boolean" => actual is JsonValueKind.True or JsonValueKind.False,
                _ => false
            };
            if (!valid) throw new InvalidDataException(
                $"Mock type mismatch: {dataset.Code}.{field} expected {expected}, got {actual}.");
        }
        return new Fixture(dataset, key, payload);
    }
}

internal static class Db
{
    public static async Task<int> Exec(NpgsqlConnection db, NpgsqlTransaction? tx, string sql,
        params (string Name, object? Value)[] values)
    {
        await using var cmd = Command(db, tx, sql, values);
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<object?> Scalar(NpgsqlConnection db, NpgsqlTransaction? tx, string sql,
        params (string Name, object? Value)[] values)
    {
        await using var cmd = Command(db, tx, sql, values);
        return await cmd.ExecuteScalarAsync();
    }

    public static NpgsqlCommand Command(NpgsqlConnection db, NpgsqlTransaction? tx, string sql,
        params (string Name, object? Value)[] values)
    {
        var cmd = new NpgsqlCommand(DatabaseSql.Rewrite(sql, db), db, tx);
        foreach (var (name, value) in values)
        {
            var parameter = new NpgsqlParameter(name, value ?? DBNull.Value);

            if (value is null) parameter.NpgsqlDbType = NpgsqlDbType.Text;
            cmd.Parameters.Add(parameter);
        }
        return cmd;
    }

    public static string Hash(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
}
