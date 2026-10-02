using Ida.Application.Common;
using Ida.Application.Features.IngestMonitoring;
using Ida.Infrastructure.Databases;
using Npgsql;
using NpgsqlTypes;

namespace Ida.Infrastructure;

public sealed class MockIngestRunner(DatabaseRegistry registry, ICoreDirectory core) : IMockIngestRunner
{
    public async Task<TriggerMockIngestResult> Trigger(string hospital, string actor,
        TriggerMockIngestInput input, CancellationToken ct)
    {
        var endpoint = await registry.GetBranchAsync(hospital, ct);
        if (string.IsNullOrWhiteSpace(endpoint.HospitalId)) throw ApiException.Forbidden();
        hospital = endpoint.HospitalId;
        await using var db = await registry.OpenAsync(endpoint, ct);
        await CheckRuntimeRole(db, ct);
        var codes = input.DatasetCodes ?? [];
        if (input.Source == "custom")
        {
            var definitions = await core.IngestDefinitionsAsync(ct);
            var knownCount = definitions.Select(definition => definition.Code).Distinct(StringComparer.Ordinal).LongCount(code => codes.Contains(code, StringComparer.Ordinal));
            if (knownCount != codes.Length)
                throw ApiException.BadRequest("unknown_dataset_code", "พบโดเมนที่ไม่มีในทะเบียน mock");
        }
        await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            await using var insert = Command(db, tx, """
                INSERT INTO bu.ingest_manual_request(hospital_id,idempotency_key,requested_by,
                    business_date,source_filter,dataset_codes,status)
                VALUES(@hospital,@key,@actor,@day,@source,@codes,'Running')
                ON CONFLICT DO NOTHING
                """, ("hospital", hospital), ("key", input.IdempotencyKey), ("actor", actor),
                ("day", input.BusinessDate), ("source", input.Source), ("codes", codes));
            if (await insert.ExecuteNonQueryAsync(ct) == 1)
            {
                var batchId = Guid.CreateVersion7();
                await Exec(db, tx, """
                    INSERT INTO bu.ingest_batch(id,hospital_id,business_date,source_filter,status,
                        trigger_kind,triggered_by)
                    VALUES(@id,@hospital,@day,@source,'Running','Manual',@actor)
                    """, ct, ("id", batchId), ("hospital", hospital),
                    ("day", input.BusinessDate), ("source", input.Source), ("actor", actor));
                await Exec(db, tx, """
                    INSERT INTO bu.ingest_job(id,hospital_id,batch_id,idempotency_key,business_date,
                        source_filter,dataset_codes,simulate_failure_after_capture,status)
                    VALUES(@id,@hospital,@batch,@key,@day,@source,@codes,@simulate,'Pending')
                    """, ct, ("id", Guid.CreateVersion7()), ("hospital", hospital),
                    ("batch", batchId), ("key", input.IdempotencyKey),
                    ("day", input.BusinessDate), ("source", input.Source), ("codes", codes),
                    ("simulate", input.SimulateFailureAfterCapture));
                await Exec(db, tx, """
                    UPDATE bu.ingest_manual_request SET batch_id=@batch
                    WHERE hospital_id=@hospital AND idempotency_key=@key
                    """, ct, ("batch", batchId), ("hospital", hospital),
                    ("key", input.IdempotencyKey));
                await tx.CommitAsync(ct);
                return new TriggerMockIngestResult(batchId, "Running", input.BusinessDate,
                    input.Source, false, null);
            }

            await using var existing = Command(db, tx, """
                SELECT batch_id,status,business_date,source_filter,error_message,dataset_codes
                FROM bu.ingest_manual_request
                WHERE hospital_id=@hospital AND idempotency_key=@key
                FOR UPDATE
                """, ("hospital", hospital), ("key", input.IdempotencyKey));
            await using var reader = await existing.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                throw ApiException.Conflict("idempotency_race",
                    "คำสั่งนำเข้าซ้ำกำลังถูกบันทึก โปรดลองใหม่");
            if (reader.IsDBNull(0))
                throw ApiException.Conflict("request_running",
                    "คำสั่งนำเข้าด้วย idempotency key นี้กำลังทำงานอยู่");
            if (DateOnly.FromDateTime(reader.GetDateTime(2)) != input.BusinessDate ||
                reader.GetString(3) != input.Source || !reader.GetFieldValue<string[]>(5).SequenceEqual(codes))
                throw ApiException.Conflict("idempotency_mismatch", "idempotency key นี้ใช้กับคำสั่งนำเข้าอื่นแล้ว");
            var result = new TriggerMockIngestResult(reader.GetGuid(0), reader.GetString(1),
                DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetString(3), true,
                reader.IsDBNull(4) ? null : reader.GetString(4));
            await reader.DisposeAsync();
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task CheckRuntimeRole(NpgsqlConnection db, CancellationToken ct)
    {
        var bypassesRls = await Scalar(db, null, """
            SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user
            """, ct);
        if (Convert.ToBoolean(bypassesRls))
            throw new InvalidOperationException(
                "Runtime connection must not have SUPERUSER or BYPASSRLS.");
    }

    private static async Task<int> Exec(NpgsqlConnection db, NpgsqlTransaction? tx, string sql,
        CancellationToken ct, params (string Name, object? Value)[] values)
    {
        await using var cmd = Command(db, tx, sql, values);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<object?> Scalar(NpgsqlConnection db, NpgsqlTransaction? tx,
        string sql, CancellationToken ct, params (string Name, object? Value)[] values)
    {
        await using var cmd = Command(db, tx, sql, values);
        return await cmd.ExecuteScalarAsync(ct);
    }

    private static NpgsqlCommand Command(NpgsqlConnection db, NpgsqlTransaction? tx, string sql,
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
}
