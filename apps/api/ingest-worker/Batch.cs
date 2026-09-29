using Npgsql;

internal static class Batch
{
    public static async Task<Guid> Start(NpgsqlConnection db, string hospital,
        DateOnly day, string source, Guid? scheduleId = null, string? triggeredBy = null)
    {
        var id = Guid.CreateVersion7();
        await Db.Exec(db, null, """
            INSERT INTO bu.ingest_batch(id,hospital_id,business_date,source_filter,status,
                schedule_id,trigger_kind,triggered_by)
            VALUES(@id,@h,@day,@source,'Running',@schedule::uuid,@kind,@actor)
            """, ("id", id), ("h", hospital), ("day", day), ("source", source),
            ("schedule", scheduleId?.ToString()),
            ("kind", scheduleId.HasValue ? "Scheduled" : "Manual"),
            ("actor", triggeredBy));
        return id;
    }

    public static Task Publish(NpgsqlConnection db, string hospital, Guid id) =>
        Db.Exec(db, null, """
            UPDATE bu.ingest_batch SET status='Published',finished_at=now()
            WHERE id=@id AND hospital_id=@h AND status='Running'
            """, ("id", id), ("h", hospital));

    public static Task Fail(NpgsqlConnection db, string hospital, Guid id, string message) =>
        Db.Exec(db, null, """
            UPDATE bu.ingest_batch SET status='Failed',finished_at=now(),error_message=@error
            WHERE id=@id AND hospital_id=@h AND status='Running'
            """, ("id", id), ("h", hospital), ("error", message[..Math.Min(message.Length, 500)]));
}

