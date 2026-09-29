using Npgsql;

internal static class Calculation
{

    public static async Task Recompute(NpgsqlConnection db, string hospital, DateOnly day,
        NpgsqlTransaction? outer = null)
    {
        var ownsTransaction = outer is null;
        await using var own = ownsTransaction ? await db.BeginTransactionAsync() : null;
        var tx = outer ?? own!;
        try
        {
            var invoices = new List<(Guid Id, string Invoice, string Doctor, string Treatment, decimal Amount)>();
            await using (var cmd = Db.Command(db, tx, """
                SELECT source_record_id,invoice_no,df_doctor_code,
                    treatment_code,amount_after_discount
                FROM bu.trn_his_invoice
                WHERE hospital_id=@h AND is_current AND invoice_date=@day
                """, ("h", hospital), ("day", day)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    invoices.Add((reader.GetGuid(0), reader.GetString(1),
                        reader.GetString(2), reader.GetString(3), reader.GetDecimal(4)));
            }

            foreach (var invoice in invoices)
            {

                var masterCount = await Db.Scalar(db, tx, """
                    SELECT count(*) FROM bu.ingest_record t
                    JOIN bu.ingest_record cat ON cat.hospital_id=t.hospital_id
                        AND cat.dataset_code='his_treatment_category' AND cat.active
                        AND cat.payload#>>'{results,0,TREATMENT_CATEGORY_CODE}'=
                            t.payload#>>'{results,0,TREATMENT_CATEGORY_CODE}'
                    JOIN bu.ingest_record sched ON sched.hospital_id=t.hospital_id
                        AND sched.dataset_code='his_examination_schedule' AND sched.active
                        AND sched.payload#>>'{results,0,DOCTOR_CODE}'=@doctor
                    JOIN bu.ingest_record clinic ON clinic.hospital_id=t.hospital_id
                        AND clinic.dataset_code='his_clinic' AND clinic.active
                        AND clinic.payload#>>'{results,0,CLINIC_CODE}'=
                            sched.payload#>>'{results,0,CLINIC_CODE}'
                    WHERE t.hospital_id=@h AND t.dataset_code='his_treatment' AND t.active
                        AND t.payload#>>'{results,0,TREATMENT_CODE}'=@treatment
                    """, ("h", hospital), ("doctor", invoice.Doctor),
                    ("treatment", invoice.Treatment));
                if (Convert.ToInt64(masterCount) != 1)
                    throw new InvalidOperationException($"Demo master chain missing or ambiguous for invoice {invoice.Invoice}.");
            }

            var receipts = new Dictionary<string, decimal>(StringComparer.Ordinal);
            await using (var cmd = Db.Command(db, tx, """
                SELECT invoice_no,SUM(receipt_amount)
                FROM bu.trn_oracle_ar
                WHERE hospital_id=@h AND is_current AND receipt_date=@day
                    AND is_void IS DISTINCT FROM 'Y' AND doc_type='R'
                GROUP BY invoice_no
                """, ("h", hospital), ("day", day)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    receipts[reader.GetString(0)] = reader.GetDecimal(1);
            }

            var runIds = new List<Guid>();
            await using (var cmd = Db.Command(db, tx, """
                SELECT DISTINCT last_run_id FROM bu.ingest_record
                WHERE hospital_id=@h AND active AND last_run_id IS NOT NULL
                """, ("h", hospital)))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) runIds.Add(reader.GetGuid(0));
            }

            await Db.Exec(db, tx, """
                UPDATE bu.calc_run SET status='Superseded'
                WHERE hospital_id=@h AND business_date=@day AND status='Published'
                """, ("h", hospital), ("day", day));
            var calcId = Guid.CreateVersion7();
            await Db.Exec(db, tx, """
                INSERT INTO bu.calc_run(id,hospital_id,business_date,rule_version,status,input_run_ids)
                VALUES(@id,@h,@day,'DEMO-20PCT-MIN-AR-v1','Published',@runs)
                """, ("id", calcId), ("h", hospital), ("day", day),
                ("runs", runIds.ToArray()));

            decimal total = 0;
            foreach (var row in invoices)
            {
                var receipt = receipts.GetValueOrDefault(row.Invoice);
                var fee = Math.Round(Math.Min(row.Amount, receipt) * .20m, 2,
                    MidpointRounding.AwayFromZero);
                total += fee;
                await Db.Exec(db, tx, """
                    INSERT INTO bu.calc_result(id,hospital_id,calc_run_id,invoice_no,doctor_code,
                        invoice_amount,receipt_amount,demo_share_percent,demo_doctor_fee,input_record_id)
                    VALUES(@id,@h,@run,@invoice,@doctor,@amount,@receipt,20,@fee,@record)
                    """, ("id", Guid.CreateVersion7()), ("h", hospital), ("run", calcId),
                    ("invoice", row.Invoice), ("doctor", row.Doctor),
                    ("amount", row.Amount), ("receipt", receipt), ("fee", fee),
                    ("record", row.Id));
            }
            if (ownsTransaction) await tx.CommitAsync();
            Console.WriteLine($"Daily DEMO calc {day:yyyy-MM-dd}: {invoices.Count} invoice(s), fee={total:N2}, calc_run={calcId}");
        }
        catch
        {
            if (ownsTransaction) await tx.RollbackAsync();
            throw;
        }
    }
}

internal static class Status
{
    public static async Task Print(NpgsqlConnection db, string hospital, DateOnly day)
    {
        await using (var cmd = Db.Command(db, null, """
            SELECT b.id,b.source_filter,b.status,b.started_at AT TIME ZONE 'Asia/Bangkok',
                COUNT(r.id),COUNT(r.id) FILTER (WHERE r.status='Failed')
            FROM bu.ingest_batch b LEFT JOIN bu.ingest_run r
                ON r.batch_id=b.id AND r.hospital_id=b.hospital_id
            WHERE b.hospital_id=@h AND b.business_date=@day
            GROUP BY b.id,b.source_filter,b.status,b.started_at
            ORDER BY b.started_at DESC LIMIT 10
            """, ("h", hospital), ("day", day)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                Console.WriteLine($"batch={reader.GetGuid(0)} source={reader.GetString(1)} " +
                    $"status={reader.GetString(2)} runs={reader.GetInt64(4)} " +
                    $"failed={reader.GetInt64(5)} started={reader.GetDateTime(3):yyyy-MM-dd HH:mm:ss} ICT");
        }
        await using (var cmd = Db.Command(db, null, """
            SELECT r.id,r.dataset_code,r.fixture_version,r.status,r.received_count,
                r.changed_count,r.duplicate_count,r.source_total,
                COALESCE(x.difference,0),r.error_message,r.batch_id,
                r.staged_count,r.pending_count,r.rejected_count
            FROM bu.ingest_run r LEFT JOIN bu.ingest_reconciliation x ON x.run_id=r.id
            WHERE r.hospital_id=@h AND r.business_date=@day
            ORDER BY r.started_at DESC,r.id DESC LIMIT 30
            """, ("h", hospital), ("day", day)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                Console.WriteLine($"{reader.GetGuid(0)} {reader.GetString(1),-27} " +
                    $"{reader.GetString(2),-8} {reader.GetString(3),-9} " +
                    $"received={reader.GetInt32(4)} changed={reader.GetInt32(5)} " +
                    $"staged={reader.GetInt32(11)} duplicate={reader.GetInt32(6)} " +
                    $"pending={reader.GetInt32(12)} rejected={reader.GetInt32(13)} " +
                    $"batch={(reader.IsDBNull(10) ? "legacy" : reader.GetGuid(10))} " +
                    $"source={reader.GetDecimal(7):N2} " +
                    $"diff={reader.GetDecimal(8):N2}" +
                    (reader.IsDBNull(9) ? "" : $" error={reader.GetString(9)}"));
        }
        await using (var cmd = Db.Command(db, null, """
            SELECT c.id,COUNT(r.id),COALESCE(SUM(r.demo_doctor_fee),0)
            FROM bu.calc_run c LEFT JOIN bu.calc_result r ON r.calc_run_id=c.id
            WHERE c.hospital_id=@h AND c.business_date=@day AND c.status='Published'
            GROUP BY c.id ORDER BY c.id DESC LIMIT 1
            """, ("h", hospital), ("day", day)))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
                Console.WriteLine($"Daily DEMO calc {reader.GetGuid(0)}: " +
                    $"{reader.GetInt64(1)} invoice(s), fee={reader.GetDecimal(2):N2}");
        }
    }
}


