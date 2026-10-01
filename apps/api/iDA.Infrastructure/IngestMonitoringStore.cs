using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Ida.Application.Features.IngestMonitoring;
using Npgsql;
using NpgsqlTypes;

namespace Ida.Infrastructure;

public sealed class IngestMonitoringStore(NpgsqlDataSource source, DatabaseRegistry registry) : IIngestMonitoringStore
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

    public async Task<IngestBatchListDto> ListBatches(string hospital, ListRequest request,
        CancellationToken ct)
    {
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        var filter = BatchFilter(request);

        var total = Convert.ToInt32(await Scalar(db, tx,
            $"SELECT count(*) FROM bu.ingest_batch b WHERE {filter.Where}",
            filter.Values, ct));

        var summary = await ReadSummary(db, tx, filter, ct);
        var legacy = await ReadLegacy(db, tx, request, ct);
        var rows = new List<IngestBatchListItem>();
        var orderBy = BatchOrderBy(request.Sort);
        await using (var cmd = Command(db, tx, $"""
            SELECT b.id,b.business_date,b.source_mode,b.source_filter,b.status,
                b.started_at,b.finished_at,b.trigger_kind,b.triggered_by,
                count(r.id)::int AS dataset_runs,
                count(r.id) FILTER (WHERE r.status='Failed')::int AS failed_runs,
                count(r.id) FILTER (WHERE r.status='Withdrawn')::int AS withdrawn_runs,
                coalesce(sum(r.received_count),0)::int AS received,
                coalesce(sum(r.staged_count),0)::int AS staged,
                coalesce(sum(r.changed_count),0)::int AS changed,
                coalesce(sum(r.duplicate_count),0)::int AS duplicate,
                coalesce(sum(r.pending_count),0)::int AS pending,
                coalesce(sum(r.rejected_count),0)::int AS rejected,
                coalesce(raw.raw_pages,0)::int AS raw_pages,
                coalesce(raw.raw_bodies,0)::int AS raw_bodies,
                b.error_message
            FROM bu.ingest_batch b
            LEFT JOIN bu.ingest_run r ON r.batch_id=b.id AND r.hospital_id=b.hospital_id
            LEFT JOIN LATERAL (
                SELECT count(*) AS raw_pages,count(p.raw_body) AS raw_bodies
                FROM bu.ingest_run rr
                JOIN bu.ingest_response_page p ON p.hospital_id=rr.hospital_id
                    AND p.run_id=rr.id
                WHERE rr.hospital_id=b.hospital_id AND rr.batch_id=b.id
            ) raw ON true
            WHERE {filter.Where}
            GROUP BY b.id,raw.raw_pages,raw.raw_bodies
            ORDER BY {orderBy}
            LIMIT @limit OFFSET @offset
            """, filter.Values.Concat([
                ("limit", request.PageSize),
                ("offset", request.Skip)
            ]).ToArray()))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) rows.Add(ReadBatch(reader));

        await tx.CommitAsync(ct);
        return new IngestBatchListDto(
            new PagedResult<IngestBatchListItem>(rows, request.Page, request.PageSize, total),
            summary, legacy);
    }

    public async Task<IngestBatchDetail> GetBatch(string hospital, Guid batchId,
        CancellationToken ct)
    {
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);

        IngestBatchListItem? batch = null;
        await using (var cmd = Cmd(db, tx, """
            SELECT b.id,b.business_date,b.source_mode,b.source_filter,b.status,
                b.started_at,b.finished_at,b.trigger_kind,b.triggered_by,
                count(r.id)::int AS dataset_runs,
                count(r.id) FILTER (WHERE r.status='Failed')::int AS failed_runs,
                count(r.id) FILTER (WHERE r.status='Withdrawn')::int AS withdrawn_runs,
                coalesce(sum(r.received_count),0)::int AS received,
                coalesce(sum(r.staged_count),0)::int AS staged,
                coalesce(sum(r.changed_count),0)::int AS changed,
                coalesce(sum(r.duplicate_count),0)::int AS duplicate,
                coalesce(sum(r.pending_count),0)::int AS pending,
                coalesce(sum(r.rejected_count),0)::int AS rejected,
                coalesce(raw.raw_pages,0)::int AS raw_pages,
                coalesce(raw.raw_bodies,0)::int AS raw_bodies,
                b.error_message
            FROM bu.ingest_batch b
            LEFT JOIN bu.ingest_run r ON r.batch_id=b.id AND r.hospital_id=b.hospital_id
            LEFT JOIN LATERAL (
                SELECT count(*) AS raw_pages,count(p.raw_body) AS raw_bodies
                FROM bu.ingest_run rr
                JOIN bu.ingest_response_page p ON p.hospital_id=rr.hospital_id
                    AND p.run_id=rr.id
                WHERE rr.hospital_id=b.hospital_id AND rr.batch_id=b.id
            ) raw ON true
            WHERE b.hospital_id=@hospital AND b.id=@id
            GROUP BY b.id,raw.raw_pages,raw.raw_bodies
            """, ("hospital", hospital), ("id", batchId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            if (await reader.ReadAsync(ct)) batch = ReadBatch(reader);
        if (batch is null)
            throw ApiException.NotFound("batch_not_found", "ไม่พบ batch การนำเข้านี้");

        var runs = await ReadRuns(db, tx, "r.batch_id=@id", [("id", batchId)], ct);
        await tx.CommitAsync(ct);
        return new IngestBatchDetail(batch, runs);
    }

    public async Task<IngestRunDetail> GetRun(string hospital, Guid runId, CancellationToken ct)
    {
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);

        var runs = await ReadRuns(db, tx, "r.id=@id", [("id", runId)], ct);
        var run = runs.SingleOrDefault()
            ?? throw ApiException.NotFound("run_not_found", "ไม่พบรอบนำเข้านี้");

        var stagingCount = Convert.ToInt32(await Scalar(db, tx, """
            SELECT count(*)
            FROM bu.ingest_staging_record
            WHERE hospital_id=@hospital AND run_id=@run
            """, [("hospital", hospital), ("run", runId)], ct));

        var staging = new List<IngestStagingItem>();
        await using (var cmd = Cmd(db, tx, """
            SELECT id,page_number,item_index,dataset_code,source_key_candidate,
                validation_status,disposition,target_record_id,received_at,processed_at,
                payload::text
            FROM bu.ingest_staging_record
            WHERE hospital_id=@hospital AND run_id=@run
            ORDER BY page_number,item_index LIMIT 200
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                staging.Add(new IngestStagingItem(reader.GetGuid(0), reader.GetInt32(1),
                    reader.GetInt32(2), reader.GetString(3), Str(reader, 4), Str(reader, 5),
                    Str(reader, 6), GuidOrNull(reader, 7), reader.GetDateTime(8),
                    DateOrNull(reader, 9), Str(reader, 10)));

        var issues = new List<IngestIssueItem>();
        await using (var cmd = Cmd(db, tx, """
            SELECT id,staging_id,issue_kind,issue_code,field_name,message,occurred_at
            FROM bu.ingest_issue
            WHERE hospital_id=@hospital AND run_id=@run
            ORDER BY occurred_at,id
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                issues.Add(new IngestIssueItem(reader.GetGuid(0), GuidOrNull(reader, 1),
                    reader.GetString(2), reader.GetString(3), Str(reader, 4),
                    reader.GetString(5), reader.GetDateTime(6)));

        var events = new List<IngestRunEventItem>();
        await using (var cmd = Cmd(db, tx, """
            SELECT id,status,occurred_at,detail
            FROM bu.ingest_run_event
            WHERE hospital_id=@hospital AND run_id=@run
            ORDER BY occurred_at,id
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                events.Add(new IngestRunEventItem(reader.GetGuid(0), reader.GetString(1),
                    reader.GetDateTime(2), Str(reader, 3)));

        var rawPages = new List<IngestRawPageItem>();
        await using (var cmd = Cmd(db, tx, """
            SELECT page_number,received_at,raw_sha256,raw_body IS NOT NULL,payload IS NOT NULL
            FROM bu.ingest_response_page
            WHERE hospital_id=@hospital AND run_id=@run
            ORDER BY page_number
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                rawPages.Add(new IngestRawPageItem(reader.GetInt32(0), reader.GetDateTime(1),
                    Str(reader, 2), reader.GetBoolean(3), reader.GetBoolean(4)));

        var actions = new List<IngestControlActionItem>();
        await using (var cmd = Cmd(db, tx, """
            SELECT id,action,actor,reason,acted_at
            FROM bu.ingest_control_action
            WHERE hospital_id=@hospital AND run_id=@run
            ORDER BY acted_at,id
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                actions.Add(new IngestControlActionItem(reader.GetGuid(0), reader.GetString(1),
                    reader.GetString(2), reader.GetString(3), reader.GetDateTime(4)));

        IngestReconciliationDto? reconciliation = null;
        await using (var cmd = Cmd(db, tx, """
            SELECT received_count,changed_count,duplicate_count,source_total,loaded_total,
                difference,status
            FROM bu.ingest_reconciliation
            WHERE hospital_id=@hospital AND run_id=@run
            """, ("hospital", hospital), ("run", runId)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            if (await reader.ReadAsync(ct))
                reconciliation = new IngestReconciliationDto(reader.GetInt32(0),
                    reader.GetInt32(1), reader.GetInt32(2), reader.GetDecimal(3),
                    reader.GetDecimal(4), reader.GetDecimal(5), reader.GetString(6));

        await tx.CommitAsync(ct);
        return new IngestRunDetail(run, staging, stagingCount, issues, events, rawPages, actions,
            reconciliation);
    }

    public async Task<IngestRawPageDetail> GetRawPage(string hospital, Guid runId,
        int pageNumber, CancellationToken ct)
    {
        await using var db = await source.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Tenant(db, tx, hospital, ct);
        await using var cmd = Cmd(db, tx, """
            SELECT run_id,dataset_code,page_number,received_at,raw_sha256,raw_body,payload::text
            FROM bu.ingest_response_page
            WHERE hospital_id=@hospital AND run_id=@run AND page_number=@page
            """, ("hospital", hospital), ("run", runId), ("page", pageNumber));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw ApiException.NotFound("raw_page_not_found", "ไม่พบ raw page ของรอบนี้");
        var detail = new IngestRawPageDetail(reader.GetGuid(0), reader.GetString(1),
            reader.GetInt32(2), reader.GetDateTime(3), Str(reader, 4), Str(reader, 5),
            Str(reader, 6));
        await tx.CommitAsync(ct);
        return detail;
    }

    private static async Task<IngestBatchSummary> ReadSummary(NpgsqlConnection db,
        NpgsqlTransaction tx, SqlFilter filter, CancellationToken ct)
    {
        await using var cmd = Command(db, tx, $"""
            SELECT count(DISTINCT b.id)::int,
                count(r.id)::int,
                count(r.id) FILTER (WHERE r.status='Published')::int,
                count(r.id) FILTER (WHERE r.status='Failed')::int,
                count(r.id) FILTER (WHERE r.status='Withdrawn')::int,
                coalesce(sum(r.received_count),0)::int,
                coalesce(sum(r.changed_count),0)::int,
                coalesce(sum(r.duplicate_count),0)::int,
                coalesce(sum(r.pending_count),0)::int,
                coalesce(sum(r.rejected_count),0)::int
            FROM bu.ingest_batch b
            LEFT JOIN bu.ingest_run r ON r.batch_id=b.id AND r.hospital_id=b.hospital_id
            WHERE {filter.Where}
            """, filter.Values);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new IngestBatchSummary(reader.GetInt32(0), reader.GetInt32(1),
            reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5),
            reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9));
    }

    private static async Task<IngestLegacySummary> ReadLegacy(NpgsqlConnection db,
        NpgsqlTransaction tx, ListRequest request, CancellationToken ct)
    {
        var filter = RunFilter(request, "r", legacyOnly: true);
        await using var cmd = Command(db, tx, $"""
            SELECT count(r.id)::int,count(p.id)::int,min(r.started_at),max(r.started_at)
            FROM bu.ingest_run r
            LEFT JOIN bu.ingest_response_page p ON p.hospital_id=r.hospital_id
                AND p.run_id=r.id
            WHERE {filter.Where}
            """, filter.Values);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new IngestLegacySummary(reader.GetInt32(0), reader.GetInt32(1),
            DateOrNull(reader, 2), DateOrNull(reader, 3));
    }

    private async Task<IReadOnlyList<IngestRunListItem>> ReadRuns(NpgsqlConnection db,
        NpgsqlTransaction tx, string extraWhere, (string Name, object? Value)[] extraValues,
        CancellationToken ct)
    {
        var rows = new List<IngestRunListItem>();
        await using (var cmd = Cmd(db, tx, $"""
            SELECT r.id,r.batch_id,r.dataset_code,NULL::text,r.fixture_version,
                r.business_date,r.source_mode,r.status,r.started_at,r.finished_at,
                r.received_count,r.staged_count,r.changed_count,r.duplicate_count,
                r.pending_count,r.rejected_count,r.source_total,r.loaded_total,
                coalesce(raw.raw_pages,0)::int,coalesce(raw.raw_bodies,0)::int,
                coalesce(issue.issue_count,0)::int,x.status,x.difference,r.error_message
            FROM bu.ingest_run r
            LEFT JOIN LATERAL (
                SELECT count(*) AS raw_pages,count(p.raw_body) AS raw_bodies
                FROM bu.ingest_response_page p
                WHERE p.hospital_id=r.hospital_id AND p.run_id=r.id
            ) raw ON true
            LEFT JOIN LATERAL (
                SELECT count(*) AS issue_count FROM bu.ingest_issue i
                WHERE i.hospital_id=r.hospital_id AND i.run_id=r.id
            ) issue ON true
            LEFT JOIN bu.ingest_reconciliation x ON x.hospital_id=r.hospital_id
                AND x.run_id=r.id
            WHERE r.hospital_id=current_setting('app.hospital_id', true) AND {extraWhere}
            ORDER BY r.started_at DESC,r.id DESC
            """, extraValues))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) rows.Add(ReadRun(reader));
        if (rows.Count == 0) return rows;

        var codes = rows.Select(row => row.DatasetCode).Distinct(StringComparer.Ordinal).ToArray();
        var names = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using var core = await registry.CoreSource.OpenConnectionAsync(ct);
        await using (var cmd = Cmd(core, null,
            "SELECT code,display_name FROM core.ingest_interface_definition WHERE code=ANY(@codes)", ("codes", codes)))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0), Str(reader, 1));
        return rows.Select(row => row with { DatasetName = names.GetValueOrDefault(row.DatasetCode) }).ToArray();
    }

    private static SqlFilter BatchFilter(ListRequest request)
    {
        var where = new List<string> { "b.hospital_id=current_setting('app.hospital_id', true)" };
        var values = new List<(string Name, object? Value)>();
        AddDateFilter(request.Filter("calledFrom"), "calledFrom",
            "(b.started_at AT TIME ZONE 'Asia/Bangkok')::date >= @calledFrom", where, values);
        AddDateFilter(request.Filter("calledTo"), "calledTo",
            "(b.started_at AT TIME ZONE 'Asia/Bangkok')::date <= @calledTo", where, values);
        AddDateFilter(request.Filter("businessFrom"), "businessFrom",
            "b.business_date >= @businessFrom", where, values);
        AddDateFilter(request.Filter("businessTo"), "businessTo",
            "b.business_date <= @businessTo", where, values);
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            where.Add("""
                (b.id::text ILIKE @q OR b.source_filter ILIKE @q OR b.status ILIKE @q
                    OR coalesce(b.triggered_by,'') ILIKE @q
                    OR coalesce(b.error_message,'') ILIKE @q)
                """);
            values.Add(("q", $"%{request.Q.Trim()}%"));
        }
        if (request.Filter("source") is { } source && source is not "all")
        {
            where.Add("b.source_filter=@source");
            values.Add(("source", source));
        }
        if (request.Filter("status") is { } status && status is not "all")
        {
            where.Add("b.status=@status");
            values.Add(("status", status));
        }
        return new SqlFilter(string.Join(" AND ", where), values.ToArray());
    }

    private static SqlFilter RunFilter(ListRequest request, string alias, bool legacyOnly)
    {
        var where = new List<string> { $"{alias}.hospital_id=current_setting('app.hospital_id', true)" };
        if (legacyOnly) where.Add($"{alias}.batch_id IS NULL");
        var values = new List<(string Name, object? Value)>();
        AddDateFilter(request.Filter("calledFrom"), "calledFrom",
            $"({alias}.started_at AT TIME ZONE 'Asia/Bangkok')::date >= @calledFrom", where, values);
        AddDateFilter(request.Filter("calledTo"), "calledTo",
            $"({alias}.started_at AT TIME ZONE 'Asia/Bangkok')::date <= @calledTo", where, values);
        AddDateFilter(request.Filter("businessFrom"), "businessFrom",
            $"{alias}.business_date >= @businessFrom", where, values);
        AddDateFilter(request.Filter("businessTo"), "businessTo",
            $"{alias}.business_date <= @businessTo", where, values);
        if (request.Filter("source") is { } source && source is not "all")
            where.Add(source == "his"
                ? $"{alias}.dataset_code LIKE 'his\\_%' ESCAPE '\\'"
                : $"{alias}.dataset_code='oracle_ar'");
        if (request.Filter("status") is { } status && status is not "all")
        {
            where.Add($"{alias}.status=@status");
            values.Add(("status", status));
        }
        return new SqlFilter(string.Join(" AND ", where), values.ToArray());
    }

    private static void AddDateFilter(string? raw, string name, string condition,
        List<string> where, List<(string Name, object? Value)> values)
    {
        if (raw is null) return;
        if (!DateOnly.TryParse(raw, out var date)) return;
        where.Add(condition);
        values.Add((name, date));
    }

    private static string BatchOrderBy(string? sort)
    {
        var (key, desc) = ParseSort(sort, "-startedAt");
        var column = key switch
        {
            "businessDate" => "b.business_date",
            "sourceFilter" => "b.source_filter",
            "status" => "b.status",
            "startedAt" => "b.started_at",
            "finishedAt" => "b.finished_at",
            _ => "b.started_at"
        };
        return $"{column} {(desc ? "DESC" : "ASC")}, b.id DESC";
    }

    private static (string Key, bool Desc) ParseSort(string? raw, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
        return value.StartsWith('-') ? (value[1..], true) : (value, false);
    }

    private static NpgsqlCommand Command(NpgsqlConnection db, NpgsqlTransaction tx,
        string sql, IReadOnlyList<(string Name, object? Value)> values)
    {
        var cmd = new NpgsqlCommand(DatabaseSql.Rewrite(sql, db), db, tx);
        foreach (var (name, value) in values)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    private static async Task<object?> Scalar(NpgsqlConnection db, NpgsqlTransaction tx,
        string sql, IReadOnlyList<(string Name, object? Value)> values, CancellationToken ct)
    {
        await using var cmd = Command(db, tx, sql, values);
        return await cmd.ExecuteScalarAsync(ct);
    }

    private static IngestBatchListItem ReadBatch(NpgsqlDataReader r) =>
        new(r.GetGuid(0), DateOnly.FromDateTime(r.GetDateTime(1)), r.GetString(2),
            r.GetString(3), r.GetString(4), r.GetDateTime(5), DateOrNull(r, 6),
            Str(r, 7), Str(r, 8), r.GetInt32(9), r.GetInt32(10), r.GetInt32(11),
            r.GetInt32(12), r.GetInt32(13), r.GetInt32(14), r.GetInt32(15),
            r.GetInt32(16), r.GetInt32(17), r.GetInt32(18), r.GetInt32(19),
            Str(r, 20));

    private static IngestRunListItem ReadRun(NpgsqlDataReader r) =>
        new(r.GetGuid(0), GuidOrNull(r, 1), r.GetString(2), Str(r, 3), r.GetString(4),
            DateOnly.FromDateTime(r.GetDateTime(5)), r.GetString(6), r.GetString(7),
            r.GetDateTime(8), DateOrNull(r, 9), r.GetInt32(10), r.GetInt32(11),
            r.GetInt32(12), r.GetInt32(13), r.GetInt32(14), r.GetInt32(15),
            r.GetDecimal(16), r.GetDecimal(17), r.GetInt32(18), r.GetInt32(19),
            r.GetInt32(20), Str(r, 21), DecimalOrNull(r, 22), Str(r, 23));

    private static string? Str(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? DateOrNull(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

    private static Guid? GuidOrNull(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);

    private static decimal? DecimalOrNull(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private sealed record SqlFilter(string Where, (string Name, object? Value)[] Values);
}
