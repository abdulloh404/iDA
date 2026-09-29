using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

internal static class TransactionProjection
{
    private const int SchemaVersion = 2;
    private enum FieldType { Text, Date, Money, Number, Boolean }
    private sealed record Field(string Column, string Source, FieldType Type,
        bool Required = false);
    private sealed record Spec(string Table, Field[] CoreFields, Field[] Fields);

    private static readonly IReadOnlyDictionary<string, (string Table, Field[] CoreFields)> CoreSpecs =
        new Dictionary<string, (string, Field[])>(StringComparer.Ordinal)
        {
            ["his_invoice"] = ("trn_his_invoice", [
                new("invoice_no", "INVOICE_NO", FieldType.Text, true),
                new("invoice_date", "INVOICE_DATE", FieldType.Date, true),
                new("request_no", "REQUEST_NO", FieldType.Text),
                new("treatment_code", "TREATMENT_CODE", FieldType.Text, true),
                new("df_doctor_code", "DF_DOCTOR_CODE", FieldType.Text, true),
                new("visit_no", "VISIT_NO", FieldType.Text),
                new("amount_after_discount", "AMOUNT_AFT_DISCOUNT", FieldType.Money, true),
            ]),
            ["his_xray"] = ("trn_his_xray", [
                new("invoice_no", "INVOICE_NO", FieldType.Text),
                new("request_no", "REQUEST_NO", FieldType.Text),
                new("xray_code", "XRAY_CODE", FieldType.Text),
                new("treatment_code", "TREATMENT_CODE", FieldType.Text),
                new("df_doctor_code", "DF_DOCTOR_CODE", FieldType.Text),
                new("visit_no", "VISIT_NO", FieldType.Text),
                new("result_date", "RESULT_DATE", FieldType.Date),
            ]),
            ["his_none_df"] = ("trn_his_none_df", [
                new("invoice_no", "INVOICE_NO", FieldType.Text),
                new("invoice_date", "INVOICE_DATE", FieldType.Date),
                new("admission_code", "ADMISSION_CODE", FieldType.Text),
                new("right_code", "RIGHT_CODE", FieldType.Text),
                new("none_df_amount", "NONEDF_AMOUNT", FieldType.Money),
            ]),
            ["his_accrual_no_invoice"] = ("trn_his_accrual_no_invoice", [
                new("visit_no", "VISIT_NO", FieldType.Text),
                new("accrual_date", "ACCRUAL_DATE", FieldType.Date),
                new("request_no", "REQUEST_NO", FieldType.Text),
                new("treatment_code", "TREATMENT_CODE", FieldType.Text),
                new("doctor_code", "DOCTOR_CODE", FieldType.Text),
                new("amount_after_discount", "AMOUNT_AFT_DISCOUNT", FieldType.Money),
                new("invoice_is_void", "INV_IS_VOID", FieldType.Text),
            ]),
            ["oracle_ar"] = ("trn_oracle_ar", [
                new("cash_receipt_id", "CASH_RECEIPT_ID", FieldType.Text),
                new("receivable_application_id", "RECEIVABLE_APPLICATION_ID", FieldType.Text),
                new("receipt_no", "RECEIPTNO", FieldType.Text),
                new("invoice_no", "INVOICENO", FieldType.Text),
                new("receipt_date", "RECEIPTDATE", FieldType.Date, true),
                new("receipt_amount", "AMOUNTBEFDISCOUNT", FieldType.Money, true),
                new("doc_type", "DOCTYPE", FieldType.Text),
                new("is_void", "ISVOID", FieldType.Text),
            ]),
        };

    private static IReadOnlyDictionary<string, Spec> Specs = new Dictionary<string, Spec>();

    public static void Configure(IEnumerable<Dataset> datasets)
    {
        var inventory = datasets.ToDictionary(x => x.Code, StringComparer.Ordinal);
        var specs = new Dictionary<string, Spec>(StringComparer.Ordinal);
        foreach (var (code, definition) in CoreSpecs)
        {
            var dataset = inventory[code];
            var coreBySource = definition.CoreFields.ToDictionary(x => x.Source,
                StringComparer.Ordinal);
            var fields = new List<Field>();
            foreach (var source in dataset.Fields)
            {
                if (!Regex.IsMatch(source, "^[A-Z][A-Z0-9_]*$"))
                    throw new InvalidDataException($"Unsafe source field name: {code}.{source}.");
                if (coreBySource.TryGetValue(source, out var core))
                    fields.Add(core);
                else
                {
                    var kind = dataset.PostmanTypes?.GetValueOrDefault(source) switch
                    {
                        "number" => FieldType.Number,
                        "boolean" => FieldType.Boolean,
                        _ => FieldType.Text,
                    };
                    fields.Add(new Field(source.ToLowerInvariant(), source, kind));
                }
            }
            if (fields.Select(x => x.Column).Distinct(StringComparer.Ordinal).Count() != fields.Count)
                throw new InvalidDataException($"Duplicate transaction column mapping: {code}.");
            if (coreBySource.Keys.Except(dataset.Fields, StringComparer.Ordinal).Any())
                throw new InvalidDataException($"Core transaction mapping is absent from inventory: {code}.");
            specs.Add(code, new Spec(definition.Table, definition.CoreFields, fields.ToArray()));
        }
        Specs = specs;
    }

    public static async Task InstallColumns(NpgsqlConnection db, NpgsqlTransaction tx)
    {
        foreach (var spec in Specs.Values)
        {
            var coreNames = spec.CoreFields.Select(x => x.Column).ToHashSet(StringComparer.Ordinal);
            var additions = spec.Fields.Where(x => !coreNames.Contains(x.Column))
                .Select(x => $"ADD COLUMN IF NOT EXISTS \"{x.Column}\" {SqlType(x.Type)}").ToList();
            additions.Add("ADD COLUMN IF NOT EXISTS projection_schema_version integer NOT NULL DEFAULT 1");
            await Db.Exec(db, tx, $"ALTER TABLE bu.{spec.Table} {string.Join(',', additions)}");
        }
    }

    private static string SqlType(FieldType kind) => kind switch
    {
        FieldType.Number => "numeric",
        FieldType.Boolean => "boolean",
        FieldType.Date => "date",
        FieldType.Money => "numeric(15,2)",
        _ => "text",
    };

    public static async Task<bool> Sync(NpgsqlConnection db, NpgsqlTransaction tx,
        string hospital, Guid recordId, bool restoreHistorical = false)
    {
        string dataset, sourceKey, payload;
        bool active;
        Guid? sourceRunId;
        await using (var cmd = Db.Command(db, tx, """
            SELECT dataset_code,source_key,payload::text,active,last_run_id
            FROM bu.ingest_record WHERE hospital_id=@h AND id=@id FOR UPDATE
            """, ("h", hospital), ("id", recordId)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                throw new InvalidDataException("Ingest source record not found for transaction projection.");
            dataset = reader.GetString(0);
            sourceKey = reader.GetString(1);
            payload = reader.GetString(2);
            active = reader.GetBoolean(3);
            sourceRunId = reader.IsDBNull(4) ? null : reader.GetGuid(4);
        }
        if (!Specs.TryGetValue(dataset, out var spec)) return false;

        Guid? currentId = null;
        string? currentHash = null;
        var currentSchemaVersion = 0;
        await using (var cmd = Db.Command(db, tx, $"""
            SELECT id,item_hash,projection_schema_version FROM bu.{spec.Table}
            WHERE hospital_id=@h AND source_key=@key AND is_current FOR UPDATE
            """, ("h", hospital), ("key", sourceKey)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                currentId = reader.GetGuid(0);
                currentHash = reader.GetString(1);
                currentSchemaVersion = reader.GetInt32(2);
            }
        }

        if (!active)
        {
            if (currentId is null) return false;
            await SetCurrent(db, tx, spec.Table, hospital, currentId.Value, false);
            return true;
        }

        using var parsed = JsonDocument.Parse(payload);
        var item = Item(parsed.RootElement, dataset);
        if (dataset == "oracle_ar" && item.TryGetProperty("DOCTYPE", out var documentType)
            && documentType.ValueKind == JsonValueKind.String
            && documentType.GetString() == "R" &&
            (!item.TryGetProperty("INVOICENO", out var invoiceNo) ||
                invoiceNo.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(invoiceNo.GetString())))
            throw new InvalidDataException("oracle_ar.INVOICENO: receipt has no invoice to match.");
        var itemHash = Db.Hash(JsonSerializer.Serialize(item));
        if (currentHash == itemHash)
        {
            if (currentSchemaVersion >= SchemaVersion) return false;
            await UpdateFields(db, tx, spec, hospital, currentId!.Value, item, dataset);
            return true;
        }

        Guid? historicalId = null;
        if (restoreHistorical)
        {
            var prior = await Db.Scalar(db, tx, $"""
                SELECT id FROM bu.{spec.Table}
                WHERE hospital_id=@h AND source_key=@key AND item_hash=@hash
                    AND NOT is_current ORDER BY version DESC LIMIT 1 FOR UPDATE
                """, ("h", hospital), ("key", sourceKey), ("hash", itemHash));
            historicalId = prior is Guid id ? id : null;
        }
        if (currentId is not null)
            await SetCurrent(db, tx, spec.Table, hospital, currentId.Value, false);
        if (historicalId is not null)
        {
            await SetCurrent(db, tx, spec.Table, hospital, historicalId.Value, true);
            await UpdateFields(db, tx, spec, hospital, historicalId.Value, item, dataset);
            return true;
        }

        var version = Convert.ToInt32(await Db.Scalar(db, tx, $"""
            SELECT coalesce(max(version),0)+1 FROM bu.{spec.Table}
            WHERE hospital_id=@h AND source_key=@key
            """, ("h", hospital), ("key", sourceKey)));
        var columns = string.Join(',', spec.Fields.Select(field => $"\"{field.Column}\""));
        var values = string.Join(',', spec.Fields.Select((field, index) =>
            $"@field{index}::{SqlType(field.Type)}"));
        var parameters = new List<(string Name, object? Value)>
        {
            ("id", Guid.CreateVersion7()), ("h", hospital), ("record", recordId),
            ("key", sourceKey), ("version", version), ("run", sourceRunId), ("hash", itemHash),
        };
        parameters.AddRange(spec.Fields.Select((field, index) =>
            ($"field{index}", ParseField(item, field, dataset))));
        await Db.Exec(db, tx, $"""
            INSERT INTO bu.{spec.Table}
                (id,hospital_id,source_record_id,source_key,version,source_run_id,item_hash,
                 projection_schema_version,{columns})
            VALUES(@id,@h,@record,@key,@version,@run::uuid,@hash,{SchemaVersion},{values})
            """, parameters.ToArray());
        return true;
    }

    public static async Task<int> Backfill(NpgsqlConnection db, string hospital)
    {
        await using var tx = await db.BeginTransactionAsync();
        var ids = new List<Guid>();
        await using (var cmd = Db.Command(db, tx, """
            SELECT id FROM bu.ingest_record WHERE hospital_id=@h
                AND dataset_code = ANY(@codes) ORDER BY id
            """, ("h", hospital), ("codes", Specs.Keys.ToArray())))
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) ids.Add(reader.GetGuid(0));
        var changed = 0;
        foreach (var id in ids)
            if (await Sync(db, tx, hospital, id)) changed++;
        changed += await BackfillHistoricalRows(db, tx, hospital);
        await tx.CommitAsync();
        return changed;
    }

    private static async Task UpdateFields(NpgsqlConnection db, NpgsqlTransaction tx,
        Spec spec, string hospital, Guid rowId, JsonElement item, string dataset)
    {
        var setters = string.Join(',', spec.Fields.Select((field, index) =>
            $"\"{field.Column}\"=@field{index}::{SqlType(field.Type)}"));
        var parameters = new List<(string Name, object? Value)>
        {
            ("h", hospital), ("id", rowId),
        };
        parameters.AddRange(spec.Fields.Select((field, index) =>
            ($"field{index}", ParseField(item, field, dataset))));
        await Db.Exec(db, tx, $"""
            UPDATE bu.{spec.Table} SET {setters},projection_schema_version={SchemaVersion}
            WHERE hospital_id=@h AND id=@id
            """, parameters.ToArray());
    }

    private static async Task<int> BackfillHistoricalRows(NpgsqlConnection db,
        NpgsqlTransaction tx, string hospital)
    {
        var changed = 0;
        var unresolved = 0;
        foreach (var (dataset, spec) in Specs)
        {
            var rows = new List<(Guid Id, Guid SourceRecordId, Guid? SourceRunId, string Hash)>();
            await using (var cmd = Db.Command(db, tx, $"""
                SELECT id,source_record_id,source_run_id,item_hash FROM bu.{spec.Table}
                WHERE hospital_id=@h AND NOT is_current
                    AND projection_schema_version < {SchemaVersion} ORDER BY source_key,version
                FOR UPDATE
                """, ("h", hospital)))
            await using (var reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    rows.Add((reader.GetGuid(0), reader.GetGuid(1),
                        reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.GetString(3)));

            foreach (var row in rows)
            {
                var candidates = new List<string>();
                if (row.SourceRunId is Guid runId)
                {
                    var change = await Db.Scalar(db, tx, """
                        SELECT after_payload::text FROM bu.ingest_change
                        WHERE hospital_id=@h AND record_id=@record AND run_id=@run
                            AND action IN ('Insert','Update')
                        ORDER BY occurred_at DESC LIMIT 1
                        """, ("h", hospital), ("record", row.SourceRecordId),
                        ("run", runId));
                    if (change is string changeJson) candidates.Add(changeJson);
                    var page = await Db.Scalar(db, tx, """
                        SELECT payload::text FROM bu.ingest_response_page
                        WHERE hospital_id=@h AND run_id=@run ORDER BY page_number LIMIT 1
                        """, ("h", hospital), ("run", runId));
                    if (page is string pageJson) candidates.Add(pageJson);
                }
                var current = await Db.Scalar(db, tx, """
                    SELECT payload::text FROM bu.ingest_record
                    WHERE hospital_id=@h AND id=@record
                    """, ("h", hospital), ("record", row.SourceRecordId));
                if (current is string currentJson) candidates.Add(currentJson);

                var found = false;
                foreach (var candidate in candidates)
                {
                    using var parsed = JsonDocument.Parse(candidate);
                    var item = Item(parsed.RootElement, dataset);
                    if (Db.Hash(JsonSerializer.Serialize(item)) != row.Hash) continue;
                    await UpdateFields(db, tx, spec, hospital, row.Id, item, dataset);
                    changed++;
                    found = true;
                    break;
                }
                if (!found) unresolved++;
            }
        }
        if (unresolved > 0)
            Console.WriteLine($"Historical transaction versions without matching saved payload: {unresolved}.");
        return changed;
    }

    private static Task SetCurrent(NpgsqlConnection db, NpgsqlTransaction tx,
        string table, string hospital, Guid id, bool current) => Db.Exec(db, tx, $"""
            UPDATE bu.{table} SET is_current=@current WHERE hospital_id=@h AND id=@id
            """, ("current", current), ("h", hospital), ("id", id));

    private static JsonElement Item(JsonElement payload, string dataset)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{dataset}: source payload is not an object.");
        if (!payload.TryGetProperty("results", out var results)) return payload;
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != 1 ||
            results[0].ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{dataset}: transaction projection needs exactly one published item.");
        return results[0];
    }

    private static object? ParseField(JsonElement item, Field field, string dataset)
    {
        if (!item.TryGetProperty(field.Source, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return Missing(field, dataset);
        var raw = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        if (string.IsNullOrWhiteSpace(raw)) return Missing(field, dataset);
        if (field.Type == FieldType.Text) return raw;
        if ((field.Type is FieldType.Money or FieldType.Number) && decimal.TryParse(raw,
            NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return amount;
        if (field.Type == FieldType.Boolean && bool.TryParse(raw, out var boolean)) return boolean;
        if (field.Type == FieldType.Date)
        {
            var datePart = raw.Length >= 10 && raw[4] == '-' && raw[7] == '-'
                ? raw[..10] : raw;
            if (DateOnly.TryParseExact(datePart, ["yyyy-MM-dd", "yyyyMMdd"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        }
        throw new InvalidDataException($"{dataset}.{field.Source}: invalid {field.Type} value.");
    }

    private static object? Missing(Field field, string dataset)
    {
        if (field.Required)
            throw new InvalidDataException($"{dataset}.{field.Source}: required transaction field is missing.");
        return null;
    }
}

