#!/bin/sh
# Creates the two runtime LOGIN roles on a fresh data directory (the entrypoint runs this directory only
# when the volume is empty). Passwords come from the same variables the backend's connection strings are
# built from. Idempotent; `--rls-apply` re-asserts the same attributes and password from the connection
# strings, which is the authoritative step for existing volumes and external databases.
set -eu

app_password="${NOTRELIX_APP_DB_PASSWORD:-}"
worker_password="${NOTRELIX_WORKER_DB_PASSWORD:-}"

# The entrypoint may source this file, so no branch calls `exit` for the skip case.
if [ -z "$app_password" ] && [ -z "$worker_password" ]; then
  echo "NOTRELIX_APP_DB_PASSWORD and NOTRELIX_WORKER_DB_PASSWORD are not set; runtime logins are provisioned by --rls-apply."
elif [ -z "$app_password" ] || [ -z "$worker_password" ]; then
  echo "Both NOTRELIX_APP_DB_PASSWORD and NOTRELIX_WORKER_DB_PASSWORD must be set to create the runtime logins." >&2
  exit 1
else
  # The passwords are read by psql from its own environment so they never appear in an argument list.
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
\set app_password `printenv NOTRELIX_APP_DB_PASSWORD`
\set worker_password `printenv NOTRELIX_WORKER_DB_PASSWORD`

SELECT format('CREATE ROLE %I', r) FROM (VALUES ('notrelix_app'), ('notrelix_worker')) AS t(r)
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = t.r) \gexec

SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', 'notrelix_app', :'app_password') \gexec
SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', 'notrelix_worker', :'worker_password') \gexec
SQL
  echo "Runtime logins notrelix_app and notrelix_worker are provisioned."
fi
