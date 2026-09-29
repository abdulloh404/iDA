using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

internal sealed record RunOutcome(Guid Id, int Changed, int Duplicate);

internal static class Ingest
{
    public static async Task<RunOutcome> One(NpgsqlConnection db, Fixture fixture,
        string hospital, DateOnly day, int revision, Guid batchId,
        bool simulateFailureAfterCapture = false)
    {
        var dataset = fixture.Dataset;
        var runId = Guid.CreateVersion7();
        var fixtureVersion = dataset.Code == "oracle_ar"
            ? $"mock-g5-record-envelope-r{revision}" :
                $"mock-postman-record-envelope-r{revision}";
        var response = fixture.Response();
        var responseJson = JsonSerializer.Serialize(response);
        var stagedRows = response["results"]!.AsArray()
            .Select((item, index) => (Id: Guid.CreateVersion7(), Index: index,
                Json: JsonSerializer.Serialize(item))).ToArray();
        var responsePageId = Guid.CreateVersion7();

        var json = responseJson;
        var hash = Db.Hash(json);
        var amountField = dataset.Code switch
        {
            "his_invoice" => "AMOUNT_AFT_DISCOUNT",
            "oracle_ar" => "AMOUNTBEFDISCOUNT",
            _ => null
        };
        var amount = amountField is null ? 0m : fixture.Payload[amountField]!.GetValue<decimal>();

        await using (var capture = await db.BeginTransactionAsync())
        {
            await Db.Exec(db, capture, """
                INSERT INTO bu.ingest_run(id,hospital_id,batch_id,dataset_code,fixture_version,
                    business_date,status,received_count,staged_count,pending_count)
                VALUES(@id,@h,@batch,@d,@v,@day,'Running',@count,@count,@count)
                """, ("id", runId), ("h", hospital), ("d", dataset.Code),
                ("v", fixtureVersion), ("day", day), ("batch", batchId),
                ("count", stagedRows.Length));
            await Db.Exec(db, capture, """
                INSERT INTO bu.ingest_run_event(id,hospital_id,run_id,status)
                VALUES(@id,@h,@run,'Running')
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId));
            await Db.Exec(db, capture, """
                INSERT INTO bu.ingest_response_page(id,hospital_id,run_id,dataset_code,
                    page_number,raw_body,raw_sha256,payload)
                VALUES(@id,@h,@run,@dataset,1,@raw,@hash,@payload::jsonb)
                """, ("id", responsePageId), ("h", hospital), ("run", runId),
                ("dataset", dataset.Code), ("raw", responseJson),
                ("hash", Db.Hash(responseJson)), ("payload", responseJson));
            foreach (var row in stagedRows)
                await Db.Exec(db, capture, """
                    INSERT INTO bu.ingest_staging_record(id,hospital_id,run_id,dataset_code,
                        page_number,item_index,source_key_candidate,payload,payload_hash,
                        validation_status,disposition)
                    VALUES(@id,@h,@run,@dataset,1,@index,@key,@item::jsonb,@hash,'Pending','Pending')
                    """, ("id", row.Id), ("h", hospital), ("run", runId),
                    ("dataset", dataset.Code), ("index", row.Index),
                    ("key", stagedRows.Length == 1 ? fixture.SourceKey : null),
                    ("item", row.Json), ("hash", Db.Hash(row.Json)));
            await capture.CommitAsync();
        }

        await using var tx = await db.BeginTransactionAsync();
        try
        {
            if (simulateFailureAfterCapture)
                throw new InvalidOperationException("Simulated mock processing failure after raw capture.");
            if (stagedRows.Length != 1)
                throw new InvalidDataException(
                    "Mock publisher handles one result only; all received items remain staged for review.");
            ValidateMockItem(fixture);

            Guid recordId;
            string? beforeJson = null, beforeHash = null;
            var beforeRevision = 0;
            var existingActive = false;
            await using (var cmd = Db.Command(db, tx, """
                SELECT id,payload::text,payload_hash,revision,active
                FROM bu.ingest_record
                WHERE hospital_id=@h AND dataset_code=@d AND source_key=@key FOR UPDATE
                """, ("h", hospital), ("d", dataset.Code), ("key", fixture.SourceKey)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    recordId = reader.GetGuid(0);
                    beforeJson = reader.GetString(1);
                    beforeHash = reader.GetString(2);
                    beforeRevision = reader.GetInt32(3);
                    existingActive = reader.GetBoolean(4);
                }
                else recordId = Guid.CreateVersion7();
            }

            var duplicate = existingActive && beforeHash == hash;
            if (!duplicate)
            {
                if (beforeRevision == 0)
                    await Db.Exec(db, tx, """
                        INSERT INTO bu.ingest_record(id,hospital_id,dataset_code,source_key,payload,
                            payload_hash,revision,last_run_id)
                        VALUES(@id,@h,@d,@key,@json::jsonb,@hash,1,@run)
                        """, ("id", recordId), ("h", hospital), ("d", dataset.Code),
                        ("key", fixture.SourceKey), ("json", json), ("hash", hash), ("run", runId));
                else
                    await Db.Exec(db, tx, """
                        UPDATE bu.ingest_record SET payload=@json::jsonb,payload_hash=@hash,
                            revision=revision+1,active=true,last_run_id=@run,updated_at=now()
                        WHERE id=@id AND hospital_id=@h
                        """, ("json", json), ("hash", hash), ("run", runId),
                        ("id", recordId), ("h", hospital));

                await Db.Exec(db, tx, """
                    INSERT INTO bu.ingest_change(id,hospital_id,run_id,record_id,action,
                        before_payload,after_payload,before_hash,after_hash,before_revision,after_revision)
                    VALUES(@id,@h,@run,@record,@action,@before::jsonb,@after::jsonb,
                        @beforeHash,@afterHash,@beforeRev,@afterRev)
                    """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
                    ("record", recordId), ("action", beforeRevision == 0 ? "Insert" : "Update"),
                    ("before", beforeJson), ("after", json), ("beforeHash", beforeHash),
                    ("afterHash", hash), ("beforeRev", beforeRevision),
                    ("afterRev", beforeRevision + 1));
            }

            var loadedAmount = amountField is null ? 0m : Convert.ToDecimal(await Db.Scalar(db, tx, """
                SELECT (payload->'results'->0->>@field)::numeric FROM bu.ingest_record
                WHERE id=@id AND hospital_id=@h AND active
                """, ("field", amountField), ("id", recordId), ("h", hospital)));
            if (loadedAmount != amount)
                throw new InvalidOperationException($"Reconciliation mismatch for {dataset.Code}: source={amount}, loaded={loadedAmount}.");

            await Db.Exec(db, tx, """
                UPDATE bu.ingest_staging_record
                SET validation_status='Valid',disposition=@action,
                    target_record_id=@record,processed_at=now()
                WHERE id=@id AND hospital_id=@h AND run_id=@run
                """, ("action", duplicate ? "Duplicate" : beforeRevision == 0 ? "Insert" : "Update"),
                ("record", recordId), ("id", stagedRows[0].Id), ("h", hospital), ("run", runId));

            await TransactionProjection.Sync(db, tx, hospital, recordId);

            await Db.Exec(db, tx, """
                UPDATE bu.ingest_run SET status='Published',finished_at=now(),received_count=1,
                    changed_count=@changed,duplicate_count=@duplicate,pending_count=0,
                    source_total=@amount,
                    loaded_total=@loaded WHERE id=@id AND hospital_id=@h
                """, ("changed", duplicate ? 0 : 1), ("duplicate", duplicate ? 1 : 0),
                ("amount", amount), ("loaded", loadedAmount), ("id", runId), ("h", hospital));
            await Db.Exec(db, tx, """
                INSERT INTO bu.ingest_run_event(id,hospital_id,run_id,status)
                VALUES(@id,@h,@run,'Published')
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId));
            await Db.Exec(db, tx, """
                INSERT INTO bu.ingest_reconciliation(id,hospital_id,run_id,received_count,
                    changed_count,duplicate_count,source_total,loaded_total,difference,status)
                VALUES(@id,@h,@run,1,@changed,@duplicate,@amount,@loaded,0,'Matched')
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
                ("changed", duplicate ? 0 : 1), ("duplicate", duplicate ? 1 : 0),
                ("amount", amount), ("loaded", loadedAmount));
            await tx.CommitAsync();
            return new RunOutcome(runId, duplicate ? 0 : 1, duplicate ? 1 : 0);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            await using var failure = await db.BeginTransactionAsync();
            await Db.Exec(db, failure, """
                UPDATE bu.ingest_run SET status='Failed',finished_at=now(),error_message=@error,
                    pending_count=@pending,rejected_count=@rejected
                WHERE id=@id AND hospital_id=@h
                """, ("id", runId), ("h", hospital),
                ("error", ex.Message[..Math.Min(ex.Message.Length, 500)]),
                ("pending", ex is InvalidDataException ? 0 : stagedRows.Length),
                ("rejected", ex is InvalidDataException ? stagedRows.Length : 0));
            if (ex is InvalidDataException)
                await Db.Exec(db, failure, """
                    UPDATE bu.ingest_staging_record
                    SET validation_status='Invalid',disposition='Rejected',processed_at=now()
                    WHERE hospital_id=@h AND run_id=@run
                    """, ("h", hospital), ("run", runId));
            await Db.Exec(db, failure, """
                INSERT INTO bu.ingest_issue(id,hospital_id,run_id,staging_id,
                    issue_kind,issue_code,message)
                VALUES(@id,@h,@run,@staging::uuid,@kind,@code,@message)
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
                ("staging", stagedRows.Length == 1 ? stagedRows[0].Id : (Guid?)null),
                ("kind", ex is InvalidDataException ? "Data" : "System"),
                ("code", ex is InvalidDataException ? "MOCK_VALIDATION" : "MOCK_PROCESSING"),
                ("message", ex.Message[..Math.Min(ex.Message.Length, 500)]));
            await Db.Exec(db, failure, """
                INSERT INTO bu.ingest_run_event(id,hospital_id,run_id,status,detail)
                VALUES(@id,@h,@run,'Failed',@detail)
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
                ("detail", ex.Message));
            await failure.CommitAsync();
            throw;
        }
    }

    private static void ValidateMockItem(Fixture fixture)
    {
        var expected = fixture.Dataset.Fields.ToHashSet(StringComparer.Ordinal);
        var actual = fixture.Payload.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(expected))
            throw new InvalidDataException($"Mock field inventory mismatch: {fixture.Dataset.Code}.");
        foreach (var (field, type) in fixture.Dataset.PostmanTypes ?? [])
        {
            var value = fixture.Payload[field];
            if (value is null) continue;
            var kind = JsonSerializer.SerializeToElement(value).ValueKind;
            if (type switch
                {
                    "string" => kind != JsonValueKind.String,
                    "number" => kind != JsonValueKind.Number,
                    "boolean" => kind is not (JsonValueKind.True or JsonValueKind.False),
                    _ => true
                })
                throw new InvalidDataException($"Mock field type mismatch: {fixture.Dataset.Code}.{field}.");
        }
    }

    public static async Task Withdraw(NpgsqlConnection db, string hospital, Guid runId,
        string actor, string reason)
    {
        await using var tx = await db.BeginTransactionAsync();
        DateOnly day;
        await using (var cmd = Db.Command(db, tx, """
            SELECT business_date FROM bu.ingest_run
            WHERE id=@id AND hospital_id=@h AND status='Published' FOR UPDATE
            """, ("id", runId), ("h", hospital)))
        {
            var result = await cmd.ExecuteScalarAsync();
            day = result switch
            {
                DateOnly value => value,
                DateTime value => DateOnly.FromDateTime(value),
                _ => throw new InvalidOperationException(
                    "Only a published run in this hospital can be withdrawn.")
            };
        }

        var changes = new List<(Guid RecordId, string? BeforeJson, string? BeforeHash, int AfterRevision)>();
        await using (var cmd = Db.Command(db, tx, """
            SELECT record_id,before_payload::text,before_hash,after_revision
            FROM bu.ingest_change WHERE hospital_id=@h AND run_id=@run
                AND action IN ('Insert','Update') ORDER BY occurred_at DESC
            """, ("h", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                changes.Add((reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt32(3)));
        }
        foreach (var change in changes)
        {
            string? currentJson = null, currentHash = null;
            await using (var cmd = Db.Command(db, tx, """
                SELECT payload::text,payload_hash FROM bu.ingest_record
                WHERE id=@id AND hospital_id=@h AND last_run_id=@run
                    AND revision=@rev AND active FOR UPDATE
                """, ("id", change.RecordId), ("h", hospital), ("run", runId),
                ("rev", change.AfterRevision)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    currentJson = reader.GetString(0);
                    currentHash = reader.GetString(1);
                }
            }
            if (currentJson is null)
                throw new InvalidOperationException("A later run changed a record; withdraw it first.");
            var restoreJson = change.BeforeJson;
            var restoreHash = change.BeforeHash;
            if (restoreJson is not null)
            {
                var prior = JsonNode.Parse(restoreJson)?.AsObject() ??
                    throw new InvalidDataException("Prior payload is not a JSON object.");

                if (prior["results"] is not JsonArray)
                {
                    restoreJson = JsonSerializer.Serialize(MockResponse.Wrap(prior));
                    restoreHash = Db.Hash(restoreJson);
                }
            }
            await Db.Exec(db, tx, """
                UPDATE bu.ingest_record SET payload=COALESCE(@before::jsonb,payload),
                    payload_hash=COALESCE(@hash,payload_hash),active=@active,
                    revision=revision+1,last_run_id=NULL,updated_at=now()
                WHERE id=@id AND hospital_id=@h
                """, ("before", restoreJson), ("hash", restoreHash),
                ("active", change.BeforeJson is not null), ("id", change.RecordId), ("h", hospital));
            await Db.Exec(db, tx, """
                INSERT INTO bu.ingest_change(id,hospital_id,run_id,record_id,action,
                    before_payload,after_payload,before_hash,after_hash,before_revision,after_revision)
                SELECT @id,@h,@run,id,'Withdraw',@current::jsonb,
                    @before::jsonb,@currentHash,@hash,@old,@new
                FROM bu.ingest_record WHERE id=@record AND hospital_id=@h
                """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
                ("record", change.RecordId), ("before", restoreJson),
                ("hash", restoreHash), ("current", currentJson),
                ("currentHash", currentHash), ("old", change.AfterRevision),
                ("new", change.AfterRevision + 1));
            await TransactionProjection.Sync(db, tx, hospital, change.RecordId,
                restoreHistorical: true);
        }
        await Db.Exec(db, tx, """
            UPDATE bu.ingest_run SET status='Withdrawn',finished_at=now()
            WHERE id=@id AND hospital_id=@h
            """, ("id", runId), ("h", hospital));
        await Db.Exec(db, tx, """
            INSERT INTO bu.ingest_run_event(id,hospital_id,run_id,status,detail)
            VALUES(@id,@h,@run,'Withdrawn',@detail)
            """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
            ("detail", reason));
        await Db.Exec(db, tx, """
            INSERT INTO bu.ingest_control_action(id,hospital_id,run_id,action,actor,reason)
            VALUES(@id,@h,@run,'Withdraw',@actor,@reason)
            """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", runId),
            ("actor", actor), ("reason", reason));
        await Calculation.Recompute(db, hospital, day, tx);
        await tx.CommitAsync();
        Console.WriteLine($"Withdrawn {runId}; {changes.Count} record(s) restored; daily demo result recomputed.");
    }
}

