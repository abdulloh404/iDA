#!/usr/bin/env bash
set -Eeuo pipefail

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<-'EOSQL'
\getenv app_db_user APP_DB_USER
\getenv app_db_password APP_DB_PASSWORD
SELECT format(
  'CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L',
  :'app_db_user',
  :'app_db_password'
)
WHERE NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = :'app_db_user')
\gexec
SELECT format('ALTER DATABASE %I OWNER TO %I', current_database(), :'app_db_user')
\gexec
DROP SCHEMA IF EXISTS public RESTRICT;
EOSQL
