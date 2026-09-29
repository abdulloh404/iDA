using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Ida.Application.Features.IngestConfiguration;
using Npgsql;
using NpgsqlTypes;

namespace Ida.Infrastructure;

public sealed class IngestConfigurationStore(NpgsqlDataSource source, DatabaseRegistry registry) : IIngestConfigurationStore
{
    private static NpgsqlCommand Cmd(NpgsqlConnection db, NpgsqlTransaction? tx,
        string sql, params (string Name, object? Value)[] values)
    {
        var cmd = new NpgsqlCommand(DatabaseSql.Rewrite(sql, db), db, tx);
        foreach (var (name, value) in values)
        {
            var parameter = cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            if (value is null) parameter.NpgsqlDbType = NpgsqlDbType.Text;
        }
        return cmd;
    }

    private static async Task Tenant(NpgsqlConnection db, NpgsqlTransaction tx,
        string hospital, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hospital)) throw ApiException.Forbidden();
        await using var cmd = Cmd(db, tx, "SELECT set_config('app.hospital_id',@hospital,true)",
            ("hospital", hospital));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<string?> InterfaceSnapshot(NpgsqlConnection db,
        NpgsqlTransaction tx, string hospital, string code, CancellationToken ct)
    {
        await using var cmd = Cmd(db, tx, """
            SELECT to_jsonb(c)::text FROM bu.ingest_interface_config c
            WHERE c.hospital_id=@hospital AND c.dataset_code=@code
            """, ("hospital", hospital), ("code", code));
        return (await cmd.ExecuteScalarAsync(ct))?.ToString();
    }

    private static async Task<string?> ScheduleSnapshot(NpgsqlConnection db,
        NpgsqlTransaction tx, string hospital, Guid id, CancellationToken ct)
    {
        await using var cmd = Cmd(db, tx, """
            SELECT jsonb_build_object('schedule',to_jsonb(s),'datasets',
                coalesce((SELECT jsonb_agg(d.dataset_code ORDER BY d.dataset_code)
                    FROM bu.ingest_schedule_dataset d
                    WHERE d.hospital_id=s.hospital_id AND d.schedule_id=s.id),'[]'::jsonb))::text
            FROM bu.ingest_schedule s WHERE s.hospital_id=@hospital AND s.id=@id
            """, ("hospital", hospital), ("id", id));
        return (await cmd.ExecuteScalarAsync(ct))?.ToString();
    }

    private static async Task Event(NpgsqlConnection db, NpgsqlTransaction tx,
        string hospital, string type, string id, string actor, string? oldValue,
        string newValue, CancellationToken ct)
    {
        await using var cmd = Cmd(db, tx, """
            INSERT INTO bu.ingest_config_event
                (id,hospital_id,entity_type,entity_id,changed_by,old_value,new_value)
            VALUES(@id,@hospital,@type,@entity,@actor,@old::jsonb,@new::jsonb)
            """, ("id", Guid.CreateVersion7()), ("hospital", hospital),
            ("type", type), ("entity", id), ("actor", actor),
            ("old", oldValue), ("new", newValue));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<long> CountDefinitionsAsync(string[] codes, CancellationToken ct)
    {
        await using var core = await registry.CoreSource.OpenConnectionAsync(ct);
        await using var cmd = Cmd(core, null,
            "SELECT count(*) FROM core.ingest_interface_definition WHERE code=ANY(@codes)", ("codes", codes));
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<IngestConfigurationDto> Read(string hospital, CancellationToken ct)
    {
        var definitions = new List<InterfaceDto>();
        await using (var core = await registry.CoreSource.OpenConnectionAsync(ct))
        await using (var cmd = Cmd(core, null, """
            SELECT code,coalesce(display_name,code),source_system
            FROM core.ingest_interface_definition ORDER BY source_system,code
            """))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                definitions.Add(new InterfaceDto(reader.GetString(0), reader.GetString(1), reader.GetString(2), null));

        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        var endpoints = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using (var cmd = Cmd(db, tx, """
            SELECT dataset_code,endpoint_url
            FROM bu.ingest_interface_config WHERE hospital_id=@hospital
            """, ("hospital", hospital)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                endpoints.Add(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
        var interfaces = definitions.Select(definition => definition with { EndpointUrl = endpoints.GetValueOrDefault(definition.Code) }).ToArray();

        var schedules = new List<ScheduleDto>();
        await using (var cmd = Cmd(db, tx, """
            SELECT s.id,s.name,s.interval_value,s.interval_unit,s.next_run_at,
                s.enabled,s.revision,
                coalesce(array_agg(d.dataset_code ORDER BY d.dataset_code)
                    FILTER (WHERE d.dataset_code IS NOT NULL),ARRAY[]::text[]),
                last_run.status,last_run.started_at,s.cancelled_at
            FROM bu.ingest_schedule s
            LEFT JOIN bu.ingest_schedule_dataset d ON d.schedule_id=s.id
                AND d.hospital_id=s.hospital_id
            LEFT JOIN LATERAL (
                SELECT status,started_at FROM bu.ingest_schedule_execution e
                WHERE e.schedule_id=s.id AND e.hospital_id=s.hospital_id
                ORDER BY started_at DESC LIMIT 1
            ) last_run ON true
            WHERE s.hospital_id=@hospital AND s.cancelled_at IS NULL
            GROUP BY s.id,last_run.status,last_run.started_at
            ORDER BY s.created_at DESC,s.id DESC
            """, ("hospital", hospital)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                schedules.Add(new ScheduleDto(reader.GetGuid(0), reader.GetString(1),
                    reader.GetInt32(2), reader.GetString(3), reader.GetDateTime(4),
                    reader.GetBoolean(5), reader.GetInt32(6), reader.GetFieldValue<string[]>(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    reader.IsDBNull(10) ? null : reader.GetDateTime(10)));
        int cancelledScheduleCount;
        await using (var cmd = Cmd(db, tx, """
            SELECT count(*) FROM bu.ingest_schedule
            WHERE hospital_id=@hospital AND cancelled_at IS NOT NULL
            """, ("hospital", hospital)))
            cancelledScheduleCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        DateTime? workerLastSeenAt = null;
        var workerOnline = false;
        await using (var cmd = Cmd(db, tx, """
            SELECT last_seen_at,last_seen_at >= now() - interval '45 seconds'
            FROM bu.ingest_worker_heartbeat WHERE hospital_id=@hospital
            """, ("hospital", hospital)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            if (await reader.ReadAsync(ct))
            {
                workerLastSeenAt = reader.GetDateTime(0);
                workerOnline = reader.GetBoolean(1);
            }
        await tx.CommitAsync(ct);
        return new IngestConfigurationDto(hospital, interfaces, schedules.ToArray(),
            cancelledScheduleCount, workerOnline, workerLastSeenAt);
    }

    public async Task<CancelledSchedulePageDto> ReadCancelledSchedules(string hospital,
        DateTimeOffset? beforeAt, Guid? beforeId, CancellationToken ct)
    {
        if (beforeAt.HasValue != beforeId.HasValue || beforeId == Guid.Empty)
            throw ApiException.BadRequest("invalid_cursor", "ข้อมูลตำแหน่งหน้ารายการไม่ถูกต้อง");
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        const int pageSize = 10;
        var schedules = new List<ScheduleDto>();
        await using (var cmd = Cmd(db, tx, """
            WITH page AS (
                SELECT * FROM bu.ingest_schedule s
                WHERE s.hospital_id=@hospital AND s.cancelled_at IS NOT NULL
                    AND (@before_at::timestamptz IS NULL OR
                        (s.cancelled_at,s.id) < (@before_at::timestamptz,@before_id::uuid))
                ORDER BY s.cancelled_at DESC,s.id DESC
                LIMIT 11
            )
            SELECT s.id,s.name,s.interval_value,s.interval_unit,s.next_run_at,
                s.enabled,s.revision,
                coalesce((SELECT array_agg(d.dataset_code ORDER BY d.dataset_code)
                    FROM bu.ingest_schedule_dataset d
                    WHERE d.hospital_id=s.hospital_id AND d.schedule_id=s.id),
                    ARRAY[]::text[]),
                last_run.status,last_run.started_at,s.cancelled_at
            FROM page s
            LEFT JOIN LATERAL (
                SELECT status,started_at FROM bu.ingest_schedule_execution e
                WHERE e.schedule_id=s.id AND e.hospital_id=s.hospital_id
                ORDER BY started_at DESC LIMIT 1
            ) last_run ON true
            ORDER BY s.cancelled_at DESC,s.id DESC
            """, ("hospital", hospital),
            ("before_at", beforeAt?.UtcDateTime), ("before_id", beforeId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                schedules.Add(new ScheduleDto(reader.GetGuid(0), reader.GetString(1),
                    reader.GetInt32(2), reader.GetString(3), reader.GetDateTime(4),
                    reader.GetBoolean(5), reader.GetInt32(6), reader.GetFieldValue<string[]>(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    reader.GetDateTime(10)));
        await tx.CommitAsync(ct);
        return new CancelledSchedulePageDto(schedules.Take(pageSize).ToArray(),
            schedules.Count > pageSize);
    }

    public async Task SaveInterface(string hospital, string code, InterfaceInput input,
        string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100)
            throw ApiException.BadRequest("invalid_domain", "รหัสโดเมนไม่ถูกต้อง");
        var url = input.EndpointUrl?.Trim();
        if (url?.Length == 0) url = null;
        if (url is not null && (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps || url.Length > 2048
            || !string.IsNullOrEmpty(parsed.UserInfo)))
            throw ApiException.BadRequest("invalid_url", "URL ต้องเป็น HTTPS และไม่มีรหัสผ่านใน URL");
        if (await CountDefinitionsAsync([code], ct) != 1)
            throw ApiException.NotFound("domain_not_found", "ไม่พบโดเมนนี้ในรายการที่รองรับ");
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        var oldValue = await InterfaceSnapshot(db, tx, hospital, code, ct);
        await using var cmd = Cmd(db, tx, """
            INSERT INTO bu.ingest_interface_config
                (hospital_id,dataset_code,endpoint_url,updated_by)
            VALUES(@hospital,@code,@url,@actor)
            ON CONFLICT(hospital_id,dataset_code) DO UPDATE SET
                endpoint_url=EXCLUDED.endpoint_url,
                updated_at=now(),updated_by=EXCLUDED.updated_by
            """, ("hospital", hospital), ("code", code), ("url", url),
            ("actor", actor));
        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            throw ApiException.NotFound("domain_not_found", "ไม่พบโดเมนนี้ในรายการที่รองรับ");
        var newValue = await InterfaceSnapshot(db, tx, hospital, code, ct)
            ?? throw new InvalidOperationException("Saved interface is missing.");
        await Event(db, tx, hospital, "interface", code, actor, oldValue, newValue, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<ScheduleDto> SaveSchedule(string hospital, Guid? id,
        ScheduleInput input, string actor, CancellationToken ct)
    {
        var name = input.Name?.Trim() ?? "";
        var codes = input.DatasetCodes?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (name.Length is < 2 or > 100 || codes.Length is < 1 or > 100 ||
            codes.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100))
            throw ApiException.BadRequest("invalid_schedule", "ระบุชื่อและเลือกโดเมนอย่างน้อยหนึ่งรายการ");
        var duration = input.IntervalUnit switch
        {
            "minute" when input.IntervalValue is >= 1 and <= 525600 =>
                TimeSpan.FromMinutes(input.IntervalValue),
            "hour" when input.IntervalValue is >= 1 and <= 8760 =>
                TimeSpan.FromHours(input.IntervalValue),
            "day" when input.IntervalValue is >= 1 and <= 365 =>
                TimeSpan.FromDays(input.IntervalValue),
            _ => TimeSpan.Zero
        };
        if (duration < TimeSpan.FromMinutes(1) || duration > TimeSpan.FromDays(365))
            throw ApiException.BadRequest("invalid_interval", "รอบเวลาต้องอยู่ระหว่าง 1 นาทีถึง 365 วัน");
        if (id.HasValue && (!input.Revision.HasValue || input.Revision.Value < 1))
            throw ApiException.BadRequest("revision_required", "ต้องส่งรุ่นข้อมูลเดิมเมื่อแก้ไขรอบงาน");
        if (await CountDefinitionsAsync(codes, ct) != codes.Length)
            throw ApiException.BadRequest("unknown_domain", "มีโดเมนที่ระบบยังไม่รองรับ");

        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        var scheduleId = id ?? Guid.CreateVersion7();
        var oldValue = id.HasValue
            ? await ScheduleSnapshot(db, tx, hospital, scheduleId, ct) : null;
        var next = input.FirstRunAt?.UtcDateTime ?? DateTime.UtcNow + duration;
        if (next < DateTime.UtcNow.AddSeconds(-1) || next > DateTime.UtcNow.AddDays(365))
            throw ApiException.BadRequest("invalid_first_run", "เวลาเริ่มรอบแรกต้องเป็นปัจจุบันหรืออนาคตภายใน 365 วัน");
        int revision;
        if (id.HasValue)
        {
            await using var cmd = Cmd(db, tx, """
                UPDATE bu.ingest_schedule SET name=@name,interval_value=@value,
                    interval_unit=@unit,next_run_at=@next,enabled=@enabled,
                    revision=revision+1,updated_at=now(),updated_by=@actor
                WHERE hospital_id=@hospital AND id=@id AND revision=@revision
                    AND cancelled_at IS NULL
                RETURNING revision
                """, ("name", name), ("value", input.IntervalValue),
                ("unit", input.IntervalUnit), ("next", next),
                ("enabled", input.Enabled), ("actor", actor), ("hospital", hospital),
                ("id", scheduleId), ("revision", input.Revision!.Value));
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result is null) throw ApiException.ConcurrencyConflict();
            revision = Convert.ToInt32(result);
            await using var clear = Cmd(db, tx, """
                DELETE FROM bu.ingest_schedule_dataset WHERE hospital_id=@hospital AND schedule_id=@id
                """, ("hospital", hospital), ("id", scheduleId));
            await clear.ExecuteNonQueryAsync(ct);
        }
        else
        {
            await using var cmd = Cmd(db, tx, """
                INSERT INTO bu.ingest_schedule
                    (id,hospital_id,name,interval_value,interval_unit,next_run_at,
                     enabled,created_by,updated_by)
                VALUES(@id,@hospital,@name,@value,@unit,@next,@enabled,@actor,@actor)
                """, ("id", scheduleId), ("hospital", hospital), ("name", name),
                ("value", input.IntervalValue), ("unit", input.IntervalUnit),
                ("next", next), ("enabled", input.Enabled), ("actor", actor));
            await cmd.ExecuteNonQueryAsync(ct);
            revision = 1;
        }
        foreach (var code in codes)
        {
            await using var config = Cmd(db, tx, """
                INSERT INTO bu.ingest_interface_config(hospital_id,dataset_code,updated_by)
                VALUES(@hospital,@code,@actor) ON CONFLICT DO NOTHING
                """, ("hospital", hospital), ("code", code), ("actor", actor));
            await config.ExecuteNonQueryAsync(ct);
            await using var link = Cmd(db, tx, """
                INSERT INTO bu.ingest_schedule_dataset(hospital_id,schedule_id,dataset_code)
                VALUES(@hospital,@id,@code)
                """, ("hospital", hospital), ("id", scheduleId), ("code", code));
            await link.ExecuteNonQueryAsync(ct);
        }
        var newValue = await ScheduleSnapshot(db, tx, hospital, scheduleId, ct)
            ?? throw new InvalidOperationException("Saved schedule is missing.");
        await Event(db, tx, hospital, "schedule", scheduleId.ToString(), actor,
            oldValue, newValue, ct);
        await tx.CommitAsync(ct);
        return new ScheduleDto(scheduleId, name, input.IntervalValue,
            input.IntervalUnit, next, input.Enabled, revision, codes, null, null, null);
    }

    public async Task CancelSchedule(string hospital, Guid id, int revision,
        string actor, CancellationToken ct)
    {
        if (revision < 1)
            throw ApiException.BadRequest("revision_required", "ต้องส่งรุ่นข้อมูลเดิมเมื่อยกเลิกรอบงาน");
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        var oldValue = await ScheduleSnapshot(db, tx, hospital, id, ct)
            ?? throw ApiException.NotFound("schedule_not_found", "ไม่พบรอบดึงข้อมูลนี้");
        await using (var cmd = Cmd(db, tx, """
            UPDATE bu.ingest_schedule SET enabled=false,cancelled_at=now(),
                cancelled_by=@actor,revision=revision+1,updated_at=now(),
                updated_by=@actor
            WHERE hospital_id=@hospital AND id=@id AND revision=@revision
                AND cancelled_at IS NULL
            RETURNING id
            """, ("actor", actor), ("hospital", hospital), ("id", id),
            ("revision", revision)))
            if (await cmd.ExecuteScalarAsync(ct) is null)
                throw ApiException.ConcurrencyConflict();
        var newValue = await ScheduleSnapshot(db, tx, hospital, id, ct)
            ?? throw new InvalidOperationException("Cancelled schedule is missing.");
        await Event(db, tx, hospital, "schedule", id.ToString(), actor,
            oldValue, newValue, ct);
        await tx.CommitAsync(ct);
    }
}
