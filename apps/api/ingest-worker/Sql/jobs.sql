CREATE TABLE IF NOT EXISTS bu.ingest_job (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL,
    batch_id uuid NOT NULL,
    idempotency_key text NOT NULL,
    business_date date NOT NULL,
    source_filter text NOT NULL CHECK (source_filter IN ('all','his','oracle')),
    simulate_failure_after_capture boolean NOT NULL DEFAULT false,
    status text NOT NULL CHECK (status IN ('Pending','Running','Published','Failed')),
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    max_attempts integer NOT NULL DEFAULT 5 CHECK (max_attempts > 0),
    available_at timestamptz NOT NULL DEFAULT now(),
    lease_token uuid,
    leased_until timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    started_at timestamptz,
    finished_at timestamptz,
    error_message text,
    UNIQUE(hospital_id,idempotency_key),
    UNIQUE(hospital_id,batch_id),
    FOREIGN KEY(hospital_id,batch_id) REFERENCES bu.ingest_batch(hospital_id,id)
);

CREATE INDEX IF NOT EXISTS ix_ingest_job_claim
    ON bu.ingest_job(hospital_id,available_at,created_at)
    WHERE status IN ('Pending','Running');

ALTER TABLE bu.ingest_job ENABLE ROW LEVEL SECURITY;
ALTER TABLE bu.ingest_job FORCE ROW LEVEL SECURITY;

DO $policy$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname=current_schema() AND tablename='ingest_job' AND policyname='p_tenant'
    ) THEN
        CREATE POLICY p_tenant ON bu.ingest_job
            USING (hospital_id = current_setting('app.hospital_id', true))
            WITH CHECK (hospital_id = current_setting('app.hospital_id', true));
    END IF;
END $policy$;
