#!/usr/bin/env bash
# Writes a non-production CI environment file.
#
# Credential values are generated per run by generate-test-credentials.sh — the
# single authority for ephemeral test credentials. No static credential may be
# committed to the repository.
set -euo pipefail
output="${1:-/tmp/notrelix-ci.env}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

creds_file="$(mktemp)"
trap 'rm -f "$creds_file"' EXIT
bash "$here/generate-test-credentials.sh" > "$creds_file"
# shellcheck source=/dev/null
source "$creds_file"

{
  echo "POSTGRES_USER=postgres"
  echo "POSTGRES_PASSWORD=$POSTGRES_PASSWORD"
  echo "POSTGRES_DB=notrelix_ci"
  echo "REDIS_PASSWORD=$REDIS_PASSWORD"
  echo "RABBITMQ_USER=rabbitmq"
  echo "RABBITMQ_PASSWORD=$RABBITMQ_PASSWORD"
  echo "RABBITMQ_VHOST=/"
  echo "JWT_SECRET=$JWT_SECRET"
  echo "JWT_ISSUER=http://localhost"
  echo "JWT_AUDIENCE=http://localhost"
  echo "CORS_ORIGIN=http://localhost"
  echo "RESEND_API_KEY=$EMAIL_API_KEY"
  echo "CONNECTIONSTRINGS_NOTRELIXDB=Host=postgres;Port=5432;Database=notrelix_ci;Username=postgres;Password=$POSTGRES_PASSWORD"
  echo "CONNECTIONSTRINGS_REDIS=redis:6379,password=$REDIS_PASSWORD"
  echo "SECURITY__CSRF__ENABLED=false"
  echo "HTTPS_REDIRECTION_ENABLED=false"
  echo "BACKEND_NETWORK_SUBNET=172.28.0.0/24"
  echo "HTTP_PORT=8080"
} > "$output"
printf '%s\n' "$output"
