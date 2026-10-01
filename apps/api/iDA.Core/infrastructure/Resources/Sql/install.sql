

DO $role$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='ida_app'
      AND NOT rolsuper AND NOT rolbypassrls) THEN
    RAISE EXCEPTION 'ida_app must exist without SUPERUSER or BYPASSRLS';
  END IF;
END $role$;

CREATE TABLE IF NOT EXISTS core.ingest_interface_definition (
    id uuid PRIMARY KEY,
    code text NOT NULL UNIQUE,
    source_system text NOT NULL,
    legacy_contract_version text NOT NULL,
    fields jsonb NOT NULL
);
ALTER TABLE core.ingest_interface_definition ADD COLUMN IF NOT EXISTS display_name text;
ALTER TABLE core.ingest_interface_definition ADD COLUMN IF NOT EXISTS data_category text NOT NULL DEFAULT 'Unclassified';
UPDATE core.ingest_interface_definition SET data_category='Master'
WHERE code IN ('his_clinic','his_treatment_category','his_treatment','his_specialty',
    'his_sub_specialty','his_examination_schedule','his_refrain_schedule','his_department')
    AND data_category='Unclassified';
UPDATE core.ingest_interface_definition SET data_category='Transaction'
WHERE code IN ('his_invoice','his_xray','his_none_df','his_accrual_no_invoice','oracle_ar')
    AND data_category='Unclassified';
DO $data_category_constraint$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
        WHERE conrelid='core.ingest_interface_definition'::regclass
            AND conname='ck_ingest_interface_data_category') THEN
        ALTER TABLE core.ingest_interface_definition ADD CONSTRAINT ck_ingest_interface_data_category
            CHECK (data_category IN ('Master','Transaction','Unclassified'));
    END IF;
END $data_category_constraint$;

