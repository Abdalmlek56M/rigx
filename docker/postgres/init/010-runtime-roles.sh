#!/usr/bin/env bash
set -euo pipefail

: "${APP_DB_PASSWORD:?APP_DB_PASSWORD must be set for the application runtime role}"

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=app_role=hardware_store_app \
  --set=audit_owner=hardware_store_audit_owner \
  --set=audit_writer=hardware_store_audit_writer \
  --set=migration_role="$POSTGRES_USER" \
  --set=app_database="$POSTGRES_DB" \
  --set=app_password="$APP_DB_PASSWORD" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'app_role', :'app_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_role')
\gexec

SELECT format('CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE', :'audit_owner')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'audit_owner')
\gexec

SELECT format('CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE', :'audit_writer')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'audit_writer')
\gexec

GRANT :"audit_writer" TO :"app_role";
GRANT :"audit_owner" TO :"migration_role";
GRANT CONNECT ON DATABASE :"app_database" TO :"app_role";
GRANT USAGE ON SCHEMA public TO :"app_role";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"app_role";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO :"app_role";
ALTER DEFAULT PRIVILEGES FOR ROLE :"migration_role" IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_role";
ALTER DEFAULT PRIVILEGES FOR ROLE :"migration_role" IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO :"app_role";
SQL
