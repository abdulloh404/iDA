using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

internal static class Verify
{
    public static async Task Payloads(NpgsqlConnection db, string hospital, DateOnly day,
        IEnumerable<Fixture> fixtures)
    {
        foreach (var fixture in fixtures)
        {
            var raw = await Db.Scalar(db, null, """
                SELECT payload::text FROM bu.ingest_record
                WHERE hospital_id=@h AND dataset_code=@d AND source_key=@key AND active
                """, ("h", hospital), ("d", fixture.Dataset.Code),
                ("key", fixture.SourceKey)) as string;
            if (raw is null)
                throw new InvalidOperationException($"Missing active payload: {fixture.Dataset.Code}.");
            var payload = JsonNode.Parse(raw)?.AsObject() ??
                throw new InvalidDataException($"Payload is not a JSON object: {fixture.Dataset.Code}.");
            var envelopeKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "status", "results", "pageNumber", "totalPages", "totalCount",
                "hasPreviousPage", "hasNextPage"
            };
            if (!payload.Select(x => x.Key).ToHashSet(StringComparer.Ordinal).SetEquals(envelopeKeys) ||
                payload["status"]?.GetValue<int>() != 200 ||
                payload["pageNumber"]?.GetValue<int>() != 1 ||
                payload["totalPages"]?.GetValue<int>() != 1 ||
                payload["totalCount"]?.GetValue<int>() != 1 ||
                payload["hasPreviousPage"]?.GetValue<bool>() != false ||
                payload["hasNextPage"]?.GetValue<bool>() != false)
                throw new InvalidDataException($"Stored record envelope mismatch: {fixture.Dataset.Code}.");
            var results = payload["results"]?.AsArray() ??
                throw new InvalidDataException($"Missing results array: {fixture.Dataset.Code}.");
            if (results.Count != 1)
                throw new InvalidDataException($"Expected one mock result: {fixture.Dataset.Code}.");
            var record = results[0]?.AsObject() ??
                throw new InvalidDataException($"Result is not a JSON object: {fixture.Dataset.Code}.");
            var expected = fixture.Dataset.Fields.ToHashSet(StringComparer.Ordinal);
            var actual = record.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
            var missing = expected.Except(actual).ToArray();
            var extra = actual.Except(expected).ToArray();
            var typeMismatches = new List<string>();
            foreach (var (field, type) in fixture.Dataset.PostmanTypes ?? [])
            {
                var value = record[field];
                if (value is null) continue;
                var kind = JsonSerializer.SerializeToElement(value).ValueKind;
                if (type switch
                    {
                        "string" => kind != JsonValueKind.String,
                        "number" => kind != JsonValueKind.Number,
                        "boolean" => kind is not (JsonValueKind.True or JsonValueKind.False),
                        _ => true
                    }) typeMismatches.Add(field);
            }
            Console.WriteLine($"{fixture.Dataset.Code,-28} record envelope=200 results=1 " +
                $"fields={actual.Count}/{expected.Count} " +
                $"Postman types={fixture.Dataset.PostmanTypes?.Count ?? 0} " +
                $"missing={missing.Length} extra={extra.Length} wrongType={typeMismatches.Count}");
            if (missing.Length > 0 || extra.Length > 0 || typeMismatches.Count > 0)
                throw new InvalidDataException($"Stored payload differs from field inventory: " +
                    $"{fixture.Dataset.Code}; missing={string.Join(',', missing)}; " +
                    $"extra={string.Join(',', extra)}; wrongType={string.Join(',', typeMismatches)}.");

            string? responseRaw = null, rawBody = null, rawSha256 = null;
            Guid? latestRunId = null, latestBatchId = null;
            await using (var cmd = Db.Command(db, null, """
                SELECT p.payload::text,p.raw_body,p.raw_sha256,p.run_id,r.batch_id
                FROM bu.ingest_response_page p
                JOIN bu.ingest_run r ON r.id=p.run_id AND r.hospital_id=p.hospital_id
                WHERE p.hospital_id=@h AND p.dataset_code=@d AND r.business_date=@day
                    AND r.status='Published' AND p.page_number=1
                ORDER BY r.started_at DESC,r.id DESC LIMIT 1
                """, ("h", hospital), ("d", fixture.Dataset.Code),
                ("day", day)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    responseRaw = reader.IsDBNull(0) ? null : reader.GetString(0);
                    rawBody = reader.IsDBNull(1) ? null : reader.GetString(1);
                    rawSha256 = reader.IsDBNull(2) ? null : reader.GetString(2);
                    latestRunId = reader.GetGuid(3);
                    latestBatchId = reader.IsDBNull(4) ? null : reader.GetGuid(4);
                }
            }
            if (responseRaw is null)
                throw new InvalidOperationException($"Missing source response page: {fixture.Dataset.Code}.");
            if (rawBody is null || rawSha256 != Db.Hash(rawBody))
                throw new InvalidDataException($"Original response text/hash missing or invalid: {fixture.Dataset.Code}.");
            var response = JsonNode.Parse(responseRaw)?.AsObject() ??
                throw new InvalidDataException($"Response is not a JSON object: {fixture.Dataset.Code}.");
            if (!JsonNode.DeepEquals(JsonNode.Parse(rawBody), response))
                throw new InvalidDataException($"Original response differs from parsed copy: {fixture.Dataset.Code}.");
            if (!JsonNode.DeepEquals(response, payload))
                throw new InvalidDataException($"Response page differs from stored record: {fixture.Dataset.Code}.");
            if (latestRunId is null || latestBatchId is null)
                throw new InvalidDataException($"Missing ingest batch: {fixture.Dataset.Code}.");
            var stagedMatches = Convert.ToInt64(await Db.Scalar(db, null, """
                SELECT count(*) FROM bu.ingest_staging_record s
                JOIN bu.ingest_response_page p ON p.hospital_id=s.hospital_id
                    AND p.run_id=s.run_id AND p.page_number=s.page_number
                JOIN bu.ingest_record t ON t.hospital_id=s.hospital_id
                    AND t.id=s.target_record_id
                WHERE s.hospital_id=@h AND s.run_id=@run AND s.dataset_code=@dataset
                    AND s.item_index=0 AND s.validation_status='Valid'
                    AND s.disposition IN ('Insert','Update','Duplicate')
                    AND s.source_key_candidate=@key AND s.payload=p.payload->'results'->0
                    AND t.source_key=@key AND t.active
                """, ("h", hospital), ("run", latestRunId.Value),
                ("dataset", fixture.Dataset.Code), ("key", fixture.SourceKey)));
            if (stagedMatches != 1)
                throw new InvalidDataException($"Staging item is not linked to raw and active record: {fixture.Dataset.Code}.");
            Console.WriteLine($"{fixture.Dataset.Code,-28} raw → staging → active record; batch={latestBatchId} OK");
        }
    }
}

