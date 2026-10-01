using Ida.Infrastructure.Databases;
using Npgsql;

internal static class Scheduler
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private sealed record Due(Guid Id, Guid ScheduleId, DateTime ScheduledFor,
        string Hospital, string[] DatasetCodes);
    private sealed record ManualJob(Guid Id, Guid BatchId, DateOnly BusinessDate,
        string Source, string[] DatasetCodes, bool SimulateFailureAfterCapture, int Attempt, int MaxAttempts,
        Guid LeaseToken);

    public static async Task Run(DatabaseRegistry registry, IReadOnlyList<Dataset> catalogue)
    {
        var endpoint = registry.FixedBranch;
        var hospital = endpoint.HospitalId;
        if (string.IsNullOrWhiteSpace(hospital))
            throw new InvalidOperationException($"Ingest branch {endpoint.ConnectionKey} has no hospital id.");
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.WriteLine($"Mock ingest scheduler polling DB every 15 seconds " +
            $"(hospital={hospital}). Ctrl+C to stop.");
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await using var db = await registry.OpenAsync(endpoint, stop.Token);
                var bypassesRls = await Db.Scalar(db, null, """
                    SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user
                    """);
                if (Convert.ToBoolean(bypassesRls))
                    throw new InvalidOperationException(
                        "Scheduler connection must not have SUPERUSER or BYPASSRLS.");
                await Db.Exec(db, null, "SELECT set_config('app.hospital_id',@h,false)",
                    ("h", hospital));
                await Db.Exec(db, null, """
                    INSERT INTO bu.ingest_worker_heartbeat(hospital_id,last_seen_at)
                    VALUES(@h,now()) ON CONFLICT(hospital_id) DO UPDATE
                    SET last_seen_at=EXCLUDED.last_seen_at
                    """, ("h", hospital));
                try
                {
                    await ProcessManual(db, hospital, catalogue, stop.Token);
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(
                        $"Manual ingest branch {endpoint.ConnectionKey} failed: {error.Message}");
                }
                try
                {
                    var due = await Claim(db, hospital);
                    if (due is not null)
                        await Execute(db, due, catalogue);
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(
                        $"Scheduled ingest branch {endpoint.ConnectionKey} failed: {error.Message}");
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                Console.Error.WriteLine($"Ingest branch {endpoint.ConnectionKey} failed: {error.Message}");
            }
            try { await Task.Delay(PollInterval, stop.Token); }
            catch (OperationCanceledException) { break; }
        }
    }

    private static async Task ProcessManual(NpgsqlConnection db, string hospital,
        IReadOnlyList<Dataset> catalogue, CancellationToken cancellationToken)
    {
        await RecoverExhausted(db, hospital);
        var job = await ClaimManual(db, hospital);
        if (job is null) return;
        try
        {
            IEnumerable<Dataset> selected = job.Source switch
            {
                "all" => catalogue,
                "his" => catalogue.Where(x => x.Code.StartsWith("his_", StringComparison.Ordinal)),
                "oracle" => catalogue.Where(x => x.Code == "oracle_ar"),
                "custom" => catalogue.Where(x => job.DatasetCodes.Contains(x.Code, StringComparer.Ordinal)),
                _ => throw new InvalidOperationException($"Unknown ingest source: {job.Source}.")
            };
            var fixtures = selected
                .Select(x => MockFixture.Create(x, hospital, job.BusinessDate, 1, 1))
                .ToArray();
            if (fixtures.Length == 0)
                throw new InvalidOperationException(
                    $"No ingest fixtures found for source {job.Source}.");
            if (job.Source == "custom" && fixtures.Length != job.DatasetCodes.Length)
                throw new InvalidOperationException("Custom ingest selection contains unknown or duplicate dataset codes.");
            var anyChanged = false;
            foreach (var fixture in fixtures)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RenewManualLease(db, hospital, job);
                var outcome = await Ingest.One(db, fixture, hospital, job.BusinessDate, 1,
                    job.BatchId, job.SimulateFailureAfterCapture);
                anyChanged |= outcome.Changed > 0;
            }
            if (anyChanged)
            {
                await RenewManualLease(db, hospital, job);
                await Calculation.Recompute(db, hospital, job.BusinessDate);
            }
            await RenewManualLease(db, hospital, job);
            await CompleteManual(db, hospital, job);
            Console.WriteLine($"Manual ingest job {job.Id} published batch={job.BatchId}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            await RetryOrFailManual(db, hospital, job, error.Message);
            Console.Error.WriteLine($"Manual ingest job {job.Id} failed: {error.Message}");
        }
    }

    private static async Task<ManualJob?> ClaimManual(NpgsqlConnection db, string hospital)
    {
        await using var tx = await db.BeginTransactionAsync();
        var leaseToken = Guid.CreateVersion7();
        await using var cmd = Db.Command(db, tx, """
            WITH candidate AS (
                SELECT id FROM bu.ingest_job
                WHERE hospital_id=@h AND attempts < max_attempts
                    AND ((status='Pending' AND available_at<=now())
                        OR (status='Running' AND leased_until<now()))
                ORDER BY available_at,created_at,id
                FOR UPDATE SKIP LOCKED LIMIT 1
            )
            UPDATE bu.ingest_job j
            SET status='Running',attempts=j.attempts+1,lease_token=@lease,
                leased_until=now()+interval '10 minutes',
                started_at=COALESCE(j.started_at,now()),error_message=NULL
            FROM candidate c WHERE j.id=c.id AND j.hospital_id=@h
            RETURNING j.id,j.batch_id,j.business_date,j.source_filter,j.dataset_codes,
                j.simulate_failure_after_capture,j.attempts,j.max_attempts
            """, ("h", hospital), ("lease", leaseToken));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            await reader.DisposeAsync();
            await tx.CommitAsync();
            return null;
        }
        var job = new ManualJob(reader.GetGuid(0), reader.GetGuid(1),
            DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetString(3),
            reader.GetFieldValue<string[]>(4), reader.GetBoolean(5), reader.GetInt32(6),
            reader.GetInt32(7), leaseToken);
        await reader.DisposeAsync();
        await tx.CommitAsync();
        return job;
    }

    private static async Task RenewManualLease(NpgsqlConnection db, string hospital,
        ManualJob job)
    {
        var renewed = await Db.Exec(db, null, """
            UPDATE bu.ingest_job SET leased_until=now()+interval '10 minutes'
            WHERE id=@id AND hospital_id=@h AND status='Running' AND lease_token=@lease
            """, ("id", job.Id), ("h", hospital), ("lease", job.LeaseToken));
        if (renewed != 1)
            throw new InvalidOperationException($"Ingest job lease was lost: {job.Id}.");
    }

    private static async Task CompleteManual(NpgsqlConnection db, string hospital, ManualJob job)
    {
        await using var tx = await db.BeginTransactionAsync();
        try
        {
            var completed = await Db.Exec(db, tx, """
                UPDATE bu.ingest_job
                SET status='Published',finished_at=now(),leased_until=NULL,lease_token=NULL,
                    error_message=NULL
                WHERE id=@id AND hospital_id=@h AND status='Running' AND lease_token=@lease
                """, ("id", job.Id), ("h", hospital), ("lease", job.LeaseToken));
            if (completed != 1)
                throw new InvalidOperationException($"Ingest job lease was lost: {job.Id}.");
            await Db.Exec(db, tx, """
                UPDATE bu.ingest_batch SET status='Published',finished_at=now(),error_message=NULL
                WHERE id=@batch AND hospital_id=@h AND status='Running'
                """, ("batch", job.BatchId), ("h", hospital));
            await Db.Exec(db, tx, """
                UPDATE bu.ingest_manual_request
                SET status='Published',finished_at=now(),error_message=NULL
                WHERE hospital_id=@h AND batch_id=@batch
                """, ("h", hospital), ("batch", job.BatchId));
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task RetryOrFailManual(NpgsqlConnection db, string hospital,
        ManualJob job, string error)
    {
        var message = error[..Math.Min(error.Length, 500)];
        if (job.Attempt < job.MaxAttempts)
        {
            await Db.Exec(db, null, """
                UPDATE bu.ingest_job
                SET status='Pending',available_at=now()+make_interval(secs => LEAST(300,@delay)),
                    lease_token=NULL,leased_until=NULL,error_message=@error
                WHERE id=@id AND hospital_id=@h AND status='Running' AND lease_token=@lease
                """, ("delay", 5 * (1 << Math.Min(job.Attempt - 1, 6))),
                ("error", message), ("id", job.Id), ("h", hospital),
                ("lease", job.LeaseToken));
            return;
        }

        await using var tx = await db.BeginTransactionAsync();
        try
        {
            var failed = await Db.Exec(db, tx, """
                UPDATE bu.ingest_job
                SET status='Failed',finished_at=now(),lease_token=NULL,leased_until=NULL,
                    error_message=@error
                WHERE id=@id AND hospital_id=@h AND status='Running' AND lease_token=@lease
                """, ("error", message), ("id", job.Id), ("h", hospital),
                ("lease", job.LeaseToken));
            if (failed == 1)
            {
                await Db.Exec(db, tx, """
                    UPDATE bu.ingest_batch SET status='Failed',finished_at=now(),error_message=@error
                    WHERE id=@batch AND hospital_id=@h AND status='Running'
                    """, ("error", message), ("batch", job.BatchId), ("h", hospital));
                await Db.Exec(db, tx, """
                    UPDATE bu.ingest_manual_request
                    SET status='Failed',finished_at=now(),error_message=@error
                    WHERE hospital_id=@h AND batch_id=@batch
                    """, ("error", message), ("h", hospital), ("batch", job.BatchId));
            }
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static Task RecoverExhausted(NpgsqlConnection db, string hospital) =>
        Db.Exec(db, null, """
            WITH failed_jobs AS (
                UPDATE bu.ingest_job
                SET status='Failed',finished_at=now(),lease_token=NULL,leased_until=NULL,
                    error_message=COALESCE(error_message,'Ingest worker lease expired.')
                WHERE hospital_id=@h AND status='Running' AND leased_until<now()
                    AND attempts>=max_attempts
                RETURNING hospital_id,batch_id,error_message
            ), failed_batches AS (
                UPDATE bu.ingest_batch b
                SET status='Failed',finished_at=now(),error_message=f.error_message
                FROM failed_jobs f
                WHERE b.hospital_id=f.hospital_id AND b.id=f.batch_id AND b.status='Running'
                RETURNING b.id
            )
            UPDATE bu.ingest_manual_request r
            SET status='Failed',finished_at=now(),error_message=f.error_message
            FROM failed_jobs f
            WHERE r.hospital_id=f.hospital_id AND r.batch_id=f.batch_id
            """, ("h", hospital));

    private static async Task<Due?> Claim(NpgsqlConnection db, string hospital)
    {
        await using var tx = await db.BeginTransactionAsync();
        Guid scheduleId;
        DateTime scheduledFor;
        int value;
        string unit;
        await using (var cmd = Db.Command(db, tx, """
            SELECT id,next_run_at,interval_value,interval_unit
            FROM bu.ingest_schedule
            WHERE hospital_id=@h AND enabled AND cancelled_at IS NULL
                AND next_run_at<=now()
            ORDER BY next_run_at,id FOR UPDATE SKIP LOCKED LIMIT 1
            """, ("h", hospital)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) return null;
            scheduleId = reader.GetGuid(0);
            scheduledFor = reader.GetDateTime(1);
            value = reader.GetInt32(2);
            unit = reader.GetString(3);
        }

        var codes = new List<string>();
        await using (var cmd = Db.Command(db, tx, """
            SELECT d.dataset_code FROM bu.ingest_schedule_dataset d
            WHERE d.hospital_id=@h AND d.schedule_id=@id ORDER BY d.dataset_code
            """, ("h", hospital), ("id", scheduleId)))
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) codes.Add(reader.GetString(0));

        var interval = unit switch
        {
            "minute" => TimeSpan.FromMinutes(value),
            "hour" => TimeSpan.FromHours(value),
            "day" => TimeSpan.FromDays(value),
            _ => throw new InvalidDataException($"Unknown interval unit: {unit}")
        };

        var next = scheduledFor + interval;
        if (next <= DateTime.UtcNow) next = DateTime.UtcNow + interval;
        var executionId = Guid.CreateVersion7();
        await Db.Exec(db, tx, """
            UPDATE bu.ingest_schedule SET next_run_at=@next
            WHERE hospital_id=@h AND id=@id
            """, ("next", next), ("h", hospital), ("id", scheduleId));
        await Db.Exec(db, tx, """
            INSERT INTO bu.ingest_schedule_execution
                (id,hospital_id,schedule_id,scheduled_for,status)
            VALUES(@id,@h,@schedule,@due,'Running')
            """, ("id", executionId), ("h", hospital),
            ("schedule", scheduleId), ("due", scheduledFor));
        await tx.CommitAsync();
        return new Due(executionId, scheduleId, scheduledFor, hospital, codes.ToArray());
    }

    private static async Task Execute(NpgsqlConnection db, Due due,
        IReadOnlyList<Dataset> catalogue)
    {
        var day = DateOnly.FromDateTime(DateTimeOffset.UtcNow
            .ToOffset(TimeSpan.FromHours(7)).DateTime);
        Guid? batchId = null;
        try
        {
            if (due.DatasetCodes.Length == 0)
                throw new InvalidOperationException("No enabled interface selected for this schedule.");
            batchId = await Batch.Start(db, due.Hospital, day, "custom", due.ScheduleId,
                $"schedule:{due.ScheduleId}");
            await Db.Exec(db, null, """
                UPDATE bu.ingest_schedule_execution SET batch_id=@batch
                WHERE hospital_id=@h AND id=@id
                """, ("batch", batchId.Value), ("h", due.Hospital), ("id", due.Id));
            var changed = false;
            foreach (var code in due.DatasetCodes)
            {
                var dataset = catalogue.SingleOrDefault(x => x.Code == code) ??
                    throw new InvalidOperationException($"No mock adapter for {code}.");
                var fixture = MockFixture.Create(dataset, due.Hospital, day, 1, 1);
                var outcome = await Ingest.One(db, fixture, due.Hospital, day, 1, batchId.Value);
                changed |= outcome.Changed > 0;
            }
            await Batch.Publish(db, due.Hospital, batchId.Value);
            if (changed) await Calculation.Recompute(db, due.Hospital, day);
            await Db.Exec(db, null, """
                UPDATE bu.ingest_schedule_execution
                SET status='Published',finished_at=now()
                WHERE hospital_id=@h AND id=@id
                """, ("h", due.Hospital), ("id", due.Id));
            Console.WriteLine($"Schedule {due.ScheduleId} published batch={batchId}");
        }
        catch (Exception error)
        {
            if (batchId.HasValue) await Batch.Fail(db, due.Hospital, batchId.Value, error.Message);
            await Db.Exec(db, null, """
                UPDATE bu.ingest_schedule_execution
                SET status='Failed',finished_at=now(),error_message=@message
                WHERE hospital_id=@h AND id=@id
                """, ("message", error.Message[..Math.Min(error.Message.Length, 500)]),
                ("h", due.Hospital), ("id", due.Id));
            Console.Error.WriteLine($"Schedule {due.ScheduleId} failed: {error.Message}");
        }
    }
}
