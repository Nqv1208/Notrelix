#!/usr/bin/env bash
# Restore drill: restore the latest restic snapshot into a throwaway Postgres container, count rows in core
# tables, print the elapsed time, tear everything down. Never touches the running stack or its database.
# Uses the same env file as backup.sh. Optional: DRILL_TABLES (space separated), PG_IMAGE (default: the image
# of the running postgres container), RESTORE_SNAPSHOT (default latest).
# Exit 0 only when every table exists and has at least one row.
set -euo pipefail

DEPLOY_PATH="${DEPLOY_PATH:-/opt/notrelix}"
BACKUP_ENV_FILE="${BACKUP_ENV_FILE:-$DEPLOY_PATH/.backup.env}"
DRILL_TABLES="${DRILL_TABLES:-accounts users workspaces boards}"
SNAPSHOT="${RESTORE_SNAPSHOT:-latest}"

set -a
# shellcheck source=/dev/null
. "$BACKUP_ENV_FILE"
set +a
: "${RESTIC_REPOSITORY:?}" "${RESTIC_PASSWORD:?}"
if [[ -z "${PG_IMAGE:-}" ]]; then
  pg="$(docker ps -q --filter label=com.docker.compose.service=postgres | head -n1)"
  [[ -n "$pg" ]] || { echo "no running postgres container; set PG_IMAGE" >&2; exit 1; }
  PG_IMAGE="$(docker inspect -f '{{.Config.Image}}' "$pg")"
fi

work="$(mktemp -d)"; name="notrelix-drill-$$"
cleanup() { docker rm -f "$name" >/dev/null 2>&1 || true; rm -rf "$work"; }
trap cleanup EXIT
start="$(date +%s)"

restic restore "$SNAPSHOT" --tag staging --target "$work"
dump="$(find "$work" -name notrelix.dump | head -n1)"
[[ -s "$dump" ]] || { echo "snapshot has no dump" >&2; exit 1; }

docker run -d --name "$name" -e POSTGRES_PASSWORD=drill -e POSTGRES_DB=drill "$PG_IMAGE" >/dev/null
for _ in $(seq 1 60); do docker exec "$name" pg_isready -h 127.0.0.1 -U postgres -d drill >/dev/null 2>&1 && break; sleep 1; done
docker exec "$name" pg_isready -h 127.0.0.1 -U postgres -d drill >/dev/null
# Roles from the source cluster do not exist here, so ownership and grants are skipped; row data is what we prove.
docker exec -i "$name" pg_restore -U postgres -d drill --no-owner --no-privileges < "$dump" || echo "pg_restore reported errors (see above); judging by the row counts" >&2

fail=0
for t in $DRILL_TABLES; do
  n="$(docker exec "$name" psql -U postgres -d drill -Atc "select count(*) from \"$t\"" 2>/dev/null || echo MISSING)"
  printf 'rows %-14s %s\n' "$t" "$n"
  [[ "$n" =~ ^[0-9]+$ && "$n" -gt 0 ]] || fail=1
done
echo "drill time: $(( $(date +%s) - start ))s snapshot=$SNAPSHOT"
[[ "$fail" -eq 0 ]] || { echo "drill FAILED" >&2; exit 1; }
echo "drill ok"
