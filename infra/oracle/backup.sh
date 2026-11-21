#!/usr/bin/env bash
# Nightly Postgres backup for the single-host staging stack, run as the deploy user (see notrelix-backup.timer).
# pg_dump (custom format, uncompressed so restic can deduplicate and compress) -> restic -> OCI Object Storage
# (S3-compatible endpoint), then retention. Settings come from an env file (mode 600, never in git):
#   RESTIC_REPOSITORY      s3:https://<namespace>.compat.objectstorage.<region>.oraclecloud.com/<bucket>
#   RESTIC_PASSWORD        encrypts the repository; losing it loses the backups
#   AWS_ACCESS_KEY_ID      OCI customer secret key id
#   AWS_SECRET_ACCESS_KEY  OCI customer secret key
#   AWS_DEFAULT_REGION     OCI region, for example ap-singapore-1
# Optional: KEEP_DAILY (7), KEEP_WEEKLY (4), PG_CONTAINER (found by compose label), BACKUP_DIR.
set -euo pipefail

DEPLOY_PATH="${DEPLOY_PATH:-/opt/notrelix}"
BACKUP_ENV_FILE="${BACKUP_ENV_FILE:-$DEPLOY_PATH/.backup.env}"
BACKUP_DIR="${BACKUP_DIR:-$DEPLOY_PATH/backups/restic-staging}"
KEEP_DAILY="${KEEP_DAILY:-7}"
KEEP_WEEKLY="${KEEP_WEEKLY:-4}"

[[ -f "$BACKUP_ENV_FILE" ]] || { echo "missing $BACKUP_ENV_FILE" >&2; exit 1; }
[[ "$(stat -c %a "$BACKUP_ENV_FILE" 2>/dev/null || stat -f %Lp "$BACKUP_ENV_FILE")" == 600 ]] || { echo "$BACKUP_ENV_FILE must be mode 600" >&2; exit 1; }
set -a
# shellcheck source=/dev/null
. "$BACKUP_ENV_FILE"
set +a
: "${RESTIC_REPOSITORY:?}" "${RESTIC_PASSWORD:?}"

pg="${PG_CONTAINER:-$(docker ps -q --filter label=com.docker.compose.service=postgres | head -n1)}"
[[ -n "$pg" ]] || { echo "no running postgres container" >&2; exit 1; }

umask 077; mkdir -p "$BACKUP_DIR"
dump="$BACKUP_DIR/notrelix.dump"
docker exec "$pg" sh -c 'pg_dump -Fc -Z0 -U "$POSTGRES_USER" "$POSTGRES_DB"' > "$dump.tmp"
[[ -s "$dump.tmp" ]] # a failed or empty dump must never become a snapshot
mv "$dump.tmp" "$dump"

restic cat config >/dev/null 2>&1 || restic init
restic backup "$dump" --tag staging --host notrelix-staging
restic forget --tag staging --keep-daily "$KEEP_DAILY" --keep-weekly "$KEEP_WEEKLY" --prune
echo "backup ok: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
