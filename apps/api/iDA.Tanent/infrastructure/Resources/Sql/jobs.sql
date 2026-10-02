CREATE TABLE IF NOT EXISTS bu.ingest_job (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL,
    batch_id uuid NOT NULL,
    idempotency_key text NOT NULL,
    business_date date NOT NULL,
    source_filter text NOT NULL CHECK (source_filter IN ('all','his','oracle','custom')),
    dataset_codes text[] NOT NULL DEFAULT '{}',
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
ALTER TABLE bu.ingest_job ADD COLUMN IF NOT EXISTS dataset_codes text[] NOT NULL DEFAULT '{}';

CREATE INDEX IF NOT EXISTS ix_ingest_job_claim
    ON bu.ingest_job(hospital_id,available_at,created_at)
    WHERE status IN ('Pending','Running');
