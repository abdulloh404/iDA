CREATE TABLE IF NOT EXISTS bu.ingest_interface_config (
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    endpoint_url text,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT 'system',
    PRIMARY KEY(hospital_id,dataset_code)
);

DO $interface_cleanup$
DECLARE has_values boolean;
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='bu'
      AND table_name='ingest_interface_config' AND column_name='auth_kind') THEN
    EXECUTE 'SELECT EXISTS (SELECT 1 FROM bu.ingest_interface_config
      WHERE auth_kind <> ''None'' OR secret_ref IS NOT NULL)' INTO has_values;
    IF has_values THEN
      RAISE EXCEPTION 'Legacy interface AUTH values must be reviewed before removing the obsolete columns';
    END IF;
    ALTER TABLE bu.ingest_interface_config
      DROP COLUMN IF EXISTS auth_kind, DROP COLUMN IF EXISTS secret_ref;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='bu'
      AND table_name='ingest_interface_config' AND column_name='enabled') THEN
    EXECUTE 'SELECT EXISTS (SELECT 1 FROM bu.ingest_interface_config WHERE NOT enabled)'
      INTO has_values;
    IF has_values THEN
      RAISE EXCEPTION 'Disabled legacy interfaces must be reviewed before removing the obsolete status';
    END IF;
    ALTER TABLE bu.ingest_interface_config DROP COLUMN IF EXISTS enabled;
  END IF;
END $interface_cleanup$;

CREATE TABLE IF NOT EXISTS bu.ingest_schedule (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    name text NOT NULL,
    interval_value integer NOT NULL CHECK (interval_value > 0),
    interval_unit text NOT NULL CHECK (interval_unit IN ('minute','hour','day')),
    next_run_at timestamptz NOT NULL,
    enabled boolean NOT NULL DEFAULT true,
    revision integer NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL,
    cancelled_at timestamptz,
    cancelled_by text,
    UNIQUE(hospital_id,id)
);
ALTER TABLE bu.ingest_schedule ADD COLUMN IF NOT EXISTS cancelled_at timestamptz;
ALTER TABLE bu.ingest_schedule ADD COLUMN IF NOT EXISTS cancelled_by text;
ALTER TABLE bu.ingest_schedule DROP CONSTRAINT IF EXISTS ingest_schedule_cancelled_disabled_check;
ALTER TABLE bu.ingest_schedule ADD CONSTRAINT ingest_schedule_cancelled_disabled_check
    CHECK (cancelled_at IS NULL OR NOT enabled);
DO $schedule_unit_upgrade$
BEGIN
  IF EXISTS (SELECT 1 FROM bu.ingest_schedule WHERE interval_unit='second') THEN
    RAISE EXCEPTION 'Review second-based schedules before upgrading to a minimum of one minute';
  END IF;
END $schedule_unit_upgrade$;
ALTER TABLE bu.ingest_schedule DROP CONSTRAINT IF EXISTS ingest_schedule_interval_unit_check;
ALTER TABLE bu.ingest_schedule ADD CONSTRAINT ingest_schedule_interval_unit_check
    CHECK (interval_unit IN ('minute','hour','day'));
CREATE INDEX IF NOT EXISTS ix_ingest_schedule_due
    ON bu.ingest_schedule(hospital_id,next_run_at) WHERE enabled;
CREATE INDEX IF NOT EXISTS ix_ingest_schedule_cancelled
    ON bu.ingest_schedule(hospital_id,cancelled_at DESC,id DESC)
    WHERE cancelled_at IS NOT NULL;

CREATE TABLE IF NOT EXISTS bu.ingest_schedule_dataset (
    hospital_id varchar(20) NOT NULL,
    schedule_id uuid NOT NULL,
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    PRIMARY KEY(hospital_id,schedule_id,dataset_code),
    FOREIGN KEY(hospital_id,schedule_id) REFERENCES bu.ingest_schedule(hospital_id,id)
);

CREATE TABLE IF NOT EXISTS bu.ingest_schedule_execution (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL,
    schedule_id uuid NOT NULL,
    scheduled_for timestamptz NOT NULL,
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    status text NOT NULL CHECK (status IN ('Running','Published','Failed')),
    batch_id uuid,
    error_message text,
    UNIQUE(hospital_id,schedule_id,scheduled_for),
    FOREIGN KEY(hospital_id,schedule_id) REFERENCES bu.ingest_schedule(hospital_id,id)
);

CREATE TABLE IF NOT EXISTS bu.ingest_worker_heartbeat (
    hospital_id varchar(20) PRIMARY KEY REFERENCES core.hospital(id),
    last_seen_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS bu.ingest_config_event (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    entity_type text NOT NULL CHECK (entity_type IN ('interface','schedule')),
    entity_id text NOT NULL,
    changed_at timestamptz NOT NULL DEFAULT now(),
    changed_by text NOT NULL,
    old_value jsonb,
    new_value jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_ingest_config_event_entity
    ON bu.ingest_config_event(hospital_id,entity_type,entity_id,changed_at DESC);

CREATE TABLE IF NOT EXISTS bu.ingest_batch (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    business_date date NOT NULL,
    source_mode text NOT NULL DEFAULT 'MOCK' CHECK (source_mode = 'MOCK'),
    source_filter text NOT NULL CHECK (source_filter IN ('all','his','oracle','custom')),
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    status text NOT NULL CHECK (status IN ('Running','Published','Failed')),
    error_message text,
    UNIQUE(hospital_id,id)
);
ALTER TABLE bu.ingest_batch DROP CONSTRAINT IF EXISTS ingest_batch_source_filter_check;
ALTER TABLE bu.ingest_batch ADD CONSTRAINT ingest_batch_source_filter_check
    CHECK (source_filter IN ('all','his','oracle','custom'));
ALTER TABLE bu.ingest_batch ADD COLUMN IF NOT EXISTS schedule_id uuid;
ALTER TABLE bu.ingest_batch ADD COLUMN IF NOT EXISTS trigger_kind text NOT NULL DEFAULT 'Manual';
ALTER TABLE bu.ingest_batch ADD COLUMN IF NOT EXISTS triggered_by text;
DO $batch_schedule_fk$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='bu.ingest_batch'::regclass
      AND conname='fk_ingest_batch_schedule') THEN
    ALTER TABLE bu.ingest_batch ADD CONSTRAINT fk_ingest_batch_schedule
      FOREIGN KEY(hospital_id,schedule_id) REFERENCES bu.ingest_schedule(hospital_id,id);
  END IF;
END $batch_schedule_fk$;
DO $execution_batch_fk$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='bu.ingest_schedule_execution'::regclass
      AND conname='fk_ingest_schedule_execution_batch') THEN
    ALTER TABLE bu.ingest_schedule_execution ADD CONSTRAINT fk_ingest_schedule_execution_batch
      FOREIGN KEY(hospital_id,batch_id) REFERENCES bu.ingest_batch(hospital_id,id);
  END IF;
END $execution_batch_fk$;
CREATE INDEX IF NOT EXISTS ix_ingest_batch_hospital_started
    ON bu.ingest_batch(hospital_id,started_at DESC);

CREATE TABLE IF NOT EXISTS bu.ingest_manual_request (
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    idempotency_key text NOT NULL,
    batch_id uuid,
    requested_by text NOT NULL,
    requested_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    business_date date NOT NULL,
    source_filter text NOT NULL CHECK (source_filter IN ('all','his','oracle','custom')),
    dataset_codes text[] NOT NULL DEFAULT '{}',
    status text NOT NULL CHECK (status IN ('Running','Published','Failed')),
    error_message text,
    PRIMARY KEY(hospital_id,idempotency_key),
    FOREIGN KEY(hospital_id,batch_id) REFERENCES bu.ingest_batch(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_manual_request_batch
    ON bu.ingest_manual_request(hospital_id,batch_id);
ALTER TABLE bu.ingest_manual_request ADD COLUMN IF NOT EXISTS dataset_codes text[] NOT NULL DEFAULT '{}';

CREATE TABLE IF NOT EXISTS bu.ingest_run (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    batch_id uuid,
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    fixture_version text NOT NULL,
    business_date date NOT NULL,
    source_mode text NOT NULL DEFAULT 'MOCK' CHECK (source_mode = 'MOCK'),
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    status text NOT NULL CHECK (status IN ('Running','Published','Failed','Withdrawn')),
    received_count integer NOT NULL DEFAULT 0,
    changed_count integer NOT NULL DEFAULT 0,
    duplicate_count integer NOT NULL DEFAULT 0,
    staged_count integer NOT NULL DEFAULT 0,
    rejected_count integer NOT NULL DEFAULT 0,
    pending_count integer NOT NULL DEFAULT 0,
    source_total numeric(15,2) NOT NULL DEFAULT 0,
    loaded_total numeric(15,2) NOT NULL DEFAULT 0,
    error_message text,
    UNIQUE(hospital_id,id)
);
ALTER TABLE bu.ingest_run ADD COLUMN IF NOT EXISTS batch_id uuid;
ALTER TABLE bu.ingest_run ADD COLUMN IF NOT EXISTS staged_count integer NOT NULL DEFAULT 0;
ALTER TABLE bu.ingest_run ADD COLUMN IF NOT EXISTS rejected_count integer NOT NULL DEFAULT 0;
ALTER TABLE bu.ingest_run ADD COLUMN IF NOT EXISTS pending_count integer NOT NULL DEFAULT 0;
DO $fk$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid='bu.ingest_run'::regclass
      AND conname='fk_ingest_run_batch') THEN
    ALTER TABLE bu.ingest_run ADD CONSTRAINT fk_ingest_run_batch
      FOREIGN KEY(hospital_id,batch_id) REFERENCES bu.ingest_batch(hospital_id,id);
  END IF;
END $fk$;
CREATE INDEX IF NOT EXISTS ix_ingest_run_hospital_date ON bu.ingest_run(hospital_id,business_date,started_at DESC);

CREATE TABLE IF NOT EXISTS bu.ingest_response_page (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    page_number integer NOT NULL CHECK (page_number > 0),

    raw_body text,
    raw_sha256 text,
    payload jsonb,
    received_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(hospital_id,run_id,page_number),
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id)
);

ALTER TABLE bu.ingest_response_page ADD COLUMN IF NOT EXISTS raw_body text;
ALTER TABLE bu.ingest_response_page ADD COLUMN IF NOT EXISTS raw_sha256 text;
ALTER TABLE bu.ingest_response_page ALTER COLUMN payload DROP NOT NULL;
ALTER TABLE bu.ingest_response_page DROP CONSTRAINT IF EXISTS ingest_response_page_payload_check;
CREATE OR REPLACE FUNCTION bu.reject_ingest_response_page_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'ingest_response_page is append-only';
END $$;
DROP TRIGGER IF EXISTS trg_ingest_response_page_append_only ON bu.ingest_response_page;
CREATE TRIGGER trg_ingest_response_page_append_only
BEFORE UPDATE OR DELETE ON bu.ingest_response_page
FOR EACH ROW EXECUTE FUNCTION bu.reject_ingest_response_page_mutation();

CREATE TABLE IF NOT EXISTS bu.ingest_run_event (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    status text NOT NULL CHECK (status IN ('Running','Published','Failed','Withdrawn')),
    occurred_at timestamptz NOT NULL DEFAULT now(),
    detail text,
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_run_event_run
    ON bu.ingest_run_event(hospital_id,run_id,occurred_at);
CREATE TABLE IF NOT EXISTS bu.ingest_record (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    source_key text NOT NULL,
    payload jsonb NOT NULL,
    payload_hash text NOT NULL,
    revision integer NOT NULL DEFAULT 1,
    active boolean NOT NULL DEFAULT true,
    last_run_id uuid,
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(hospital_id,dataset_code,source_key),
    UNIQUE(hospital_id,id),
    FOREIGN KEY(hospital_id,last_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_record_hospital_dataset ON bu.ingest_record(hospital_id,dataset_code) WHERE active;

CREATE TABLE IF NOT EXISTS bu.ingest_staging_record (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    dataset_code text NOT NULL REFERENCES core.ingest_interface_definition(code),
    page_number integer NOT NULL,
    item_index integer NOT NULL CHECK (item_index >= 0),
    source_key_candidate text,
    payload jsonb,
    payload_hash text,
    validation_status text NOT NULL CHECK (validation_status IN ('Pending','Valid','Invalid')),
    disposition text NOT NULL CHECK (disposition IN ('Pending','Insert','Update','Duplicate','Rejected')),
    target_record_id uuid,
    received_at timestamptz NOT NULL DEFAULT now(),
    processed_at timestamptz,
    UNIQUE(hospital_id,run_id,page_number,item_index),
    UNIQUE(hospital_id,run_id,id),
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id),
    FOREIGN KEY(hospital_id,run_id,page_number)
        REFERENCES bu.ingest_response_page(hospital_id,run_id,page_number),
    FOREIGN KEY(hospital_id,target_record_id) REFERENCES bu.ingest_record(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_staging_review
    ON bu.ingest_staging_record(hospital_id,validation_status,disposition,received_at DESC);

CREATE TABLE IF NOT EXISTS bu.ingest_issue (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    staging_id uuid,
    issue_kind text NOT NULL CHECK (issue_kind IN ('Data','System','Mapping')),
    issue_code text NOT NULL,
    field_name text,
    message text NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id),
    FOREIGN KEY(hospital_id,run_id,staging_id)
        REFERENCES bu.ingest_staging_record(hospital_id,run_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_issue_run ON bu.ingest_issue(hospital_id,run_id);

CREATE TABLE IF NOT EXISTS bu.ingest_change (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    record_id uuid NOT NULL,
    action text NOT NULL CHECK (action IN ('Insert','Update','Withdraw')),
    before_payload jsonb,
    after_payload jsonb,
    before_hash text,
    after_hash text,
    before_revision integer NOT NULL,
    after_revision integer NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id),
    FOREIGN KEY(hospital_id,record_id) REFERENCES bu.ingest_record(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_ingest_change_run ON bu.ingest_change(hospital_id,run_id);

CREATE TABLE IF NOT EXISTS bu.trn_his_invoice (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    source_record_id uuid NOT NULL,
    source_key text NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    is_current boolean NOT NULL DEFAULT true,
    source_run_id uuid,
    item_hash text NOT NULL,
    projected_at timestamptz NOT NULL DEFAULT now(),
    invoice_no text,
    invoice_date date,
    request_no text,
    treatment_code text,
    df_doctor_code text,
    visit_no text,
    amount_after_discount numeric(15,2),
    UNIQUE(hospital_id,source_key,version),
    FOREIGN KEY(hospital_id,source_record_id) REFERENCES bu.ingest_record(hospital_id,id),
    FOREIGN KEY(hospital_id,source_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_trn_his_invoice_current
    ON bu.trn_his_invoice(hospital_id,source_key) WHERE is_current;
CREATE INDEX IF NOT EXISTS ix_trn_his_invoice_current_date
    ON bu.trn_his_invoice(hospital_id,invoice_date) WHERE is_current;

CREATE TABLE IF NOT EXISTS bu.trn_his_xray (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    source_record_id uuid NOT NULL,
    source_key text NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    is_current boolean NOT NULL DEFAULT true,
    source_run_id uuid,
    item_hash text NOT NULL,
    projected_at timestamptz NOT NULL DEFAULT now(),
    invoice_no text,
    request_no text,
    xray_code text,
    treatment_code text,
    df_doctor_code text,
    visit_no text,
    result_date date,
    UNIQUE(hospital_id,source_key,version),
    FOREIGN KEY(hospital_id,source_record_id) REFERENCES bu.ingest_record(hospital_id,id),
    FOREIGN KEY(hospital_id,source_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_trn_his_xray_current
    ON bu.trn_his_xray(hospital_id,source_key) WHERE is_current;
CREATE INDEX IF NOT EXISTS ix_trn_his_xray_current_date
    ON bu.trn_his_xray(hospital_id,result_date) WHERE is_current;

CREATE TABLE IF NOT EXISTS bu.trn_his_none_df (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    source_record_id uuid NOT NULL,
    source_key text NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    is_current boolean NOT NULL DEFAULT true,
    source_run_id uuid,
    item_hash text NOT NULL,
    projected_at timestamptz NOT NULL DEFAULT now(),
    invoice_no text,
    invoice_date date,
    admission_code text,
    right_code text,
    none_df_amount numeric(15,2),
    UNIQUE(hospital_id,source_key,version),
    FOREIGN KEY(hospital_id,source_record_id) REFERENCES bu.ingest_record(hospital_id,id),
    FOREIGN KEY(hospital_id,source_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_trn_his_none_df_current
    ON bu.trn_his_none_df(hospital_id,source_key) WHERE is_current;
CREATE INDEX IF NOT EXISTS ix_trn_his_none_df_current_date
    ON bu.trn_his_none_df(hospital_id,invoice_date) WHERE is_current;

CREATE TABLE IF NOT EXISTS bu.trn_his_accrual_no_invoice (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    source_record_id uuid NOT NULL,
    source_key text NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    is_current boolean NOT NULL DEFAULT true,
    source_run_id uuid,
    item_hash text NOT NULL,
    projected_at timestamptz NOT NULL DEFAULT now(),
    visit_no text,
    accrual_date date,
    request_no text,
    treatment_code text,
    doctor_code text,
    amount_after_discount numeric(15,2),
    invoice_is_void text,
    UNIQUE(hospital_id,source_key,version),
    FOREIGN KEY(hospital_id,source_record_id) REFERENCES bu.ingest_record(hospital_id,id),
    FOREIGN KEY(hospital_id,source_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_trn_his_accrual_no_invoice_current
    ON bu.trn_his_accrual_no_invoice(hospital_id,source_key) WHERE is_current;
CREATE INDEX IF NOT EXISTS ix_trn_his_accrual_no_invoice_current_date
    ON bu.trn_his_accrual_no_invoice(hospital_id,accrual_date) WHERE is_current;

CREATE TABLE IF NOT EXISTS bu.trn_oracle_ar (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    source_record_id uuid NOT NULL,
    source_key text NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    is_current boolean NOT NULL DEFAULT true,
    source_run_id uuid,
    item_hash text NOT NULL,
    projected_at timestamptz NOT NULL DEFAULT now(),
    cash_receipt_id text,
    receivable_application_id text,
    receipt_no text,
    invoice_no text,
    receipt_date date,
    receipt_amount numeric(15,2),
    doc_type text,
    is_void text,
    UNIQUE(hospital_id,source_key,version),
    FOREIGN KEY(hospital_id,source_record_id) REFERENCES bu.ingest_record(hospital_id,id),
    FOREIGN KEY(hospital_id,source_run_id) REFERENCES bu.ingest_run(hospital_id,id)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_trn_oracle_ar_current
    ON bu.trn_oracle_ar(hospital_id,source_key) WHERE is_current;
CREATE INDEX IF NOT EXISTS ix_trn_oracle_ar_current_date_invoice
    ON bu.trn_oracle_ar(hospital_id,receipt_date,invoice_no) WHERE is_current;

CREATE TABLE IF NOT EXISTS bu.ingest_control_action (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL,
    action text NOT NULL CHECK (action IN ('Withdraw')),
    actor text NOT NULL,
    reason text NOT NULL,
    acted_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id)
);

CREATE TABLE IF NOT EXISTS bu.ingest_reconciliation (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    run_id uuid NOT NULL UNIQUE,
    received_count integer NOT NULL,
    changed_count integer NOT NULL,
    duplicate_count integer NOT NULL,
    source_total numeric(15,2) NOT NULL,
    loaded_total numeric(15,2) NOT NULL,
    difference numeric(15,2) NOT NULL,
    status text NOT NULL,
    FOREIGN KEY(hospital_id,run_id) REFERENCES bu.ingest_run(hospital_id,id)
);

CREATE TABLE IF NOT EXISTS bu.calc_run (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    business_date date NOT NULL,
    source_mode text NOT NULL DEFAULT 'MOCK' CHECK (source_mode = 'MOCK'),
    rule_version text NOT NULL,
    status text NOT NULL CHECK (status IN ('Published','Superseded')),
    input_run_ids uuid[] NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(hospital_id,id)
);
CREATE INDEX IF NOT EXISTS ix_calc_run_hospital_date ON bu.calc_run(hospital_id,business_date,created_at DESC);

CREATE TABLE IF NOT EXISTS bu.calc_result (
    id uuid PRIMARY KEY,
    hospital_id varchar(20) NOT NULL REFERENCES core.hospital(id),
    calc_run_id uuid NOT NULL,
    invoice_no text NOT NULL,
    doctor_code text NOT NULL,
    invoice_amount numeric(15,2) NOT NULL,
    receipt_amount numeric(15,2) NOT NULL,
    demo_share_percent numeric(5,2) NOT NULL,
    demo_doctor_fee numeric(15,2) NOT NULL,
    input_record_id uuid NOT NULL,
    FOREIGN KEY(hospital_id,calc_run_id) REFERENCES bu.calc_run(hospital_id,id),
    FOREIGN KEY(hospital_id,input_record_id) REFERENCES bu.ingest_record(hospital_id,id)
);

CREATE OR REPLACE VIEW bu.his_source_record WITH (security_invoker=true) AS
    SELECT * FROM bu.ingest_record WHERE dataset_code LIKE 'his_%';
CREATE OR REPLACE VIEW bu.erp_source_record WITH (security_invoker=true) AS
    SELECT * FROM bu.ingest_record WHERE dataset_code = 'oracle_ar';

GRANT USAGE ON SCHEMA core, bu TO ida_app;
GRANT SELECT, INSERT, UPDATE ON core.ingest_interface_definition TO ida_app;
GRANT SELECT, INSERT, UPDATE ON bu.ingest_interface_config,bu.ingest_schedule,
    bu.ingest_schedule_dataset,bu.ingest_schedule_execution,
    bu.ingest_worker_heartbeat TO ida_app;
GRANT DELETE ON bu.ingest_schedule_dataset TO ida_app;
GRANT SELECT, INSERT ON bu.ingest_config_event TO ida_app;
GRANT SELECT, INSERT, UPDATE ON bu.ingest_run,bu.ingest_response_page,bu.ingest_record,bu.ingest_change,
    bu.ingest_reconciliation,bu.ingest_control_action,bu.calc_run,bu.calc_result TO ida_app;
GRANT SELECT, INSERT, UPDATE ON bu.trn_his_invoice,bu.trn_his_xray,
    bu.trn_his_none_df,bu.trn_his_accrual_no_invoice,bu.trn_oracle_ar TO ida_app;
REVOKE UPDATE, DELETE ON bu.ingest_response_page FROM ida_app;
GRANT SELECT, INSERT ON bu.ingest_run_event TO ida_app;
GRANT SELECT, INSERT, UPDATE ON bu.ingest_batch,bu.ingest_staging_record TO ida_app;
GRANT SELECT, INSERT, UPDATE ON bu.ingest_manual_request TO ida_app;
GRANT SELECT, INSERT ON bu.ingest_issue TO ida_app;
GRANT SELECT ON bu.his_source_record,bu.erp_source_record TO ida_app;
