#!/usr/bin/env bash
set -euo pipefail
out="${1:?usage: write-test-env.sh OUTPUT}"
tmp="$(mktemp)";trap 'rm -f "$tmp"' EXIT
scripts/ci/generate-test-credentials.sh > "$tmp"
# shellcheck source=/dev/null
source "$tmp"
cat > "$out" <<EOF
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$POSTGRES_PASSWORD
POSTGRES_DB=notrelix
REDIS_PASSWORD=$REDIS_PASSWORD
RABBITMQ_USER=rabbitmq
RABBITMQ_PASSWORD=$RABBITMQ_PASSWORD
RABBITMQ_VHOST=/
JWT_SECRET=$JWT_SECRET
JWT_ISSUER=https://ci.notrelix.invalid
JWT_AUDIENCE=https://ci.notrelix.invalid
CORS_ORIGIN=https://ci.notrelix.invalid
RESEND_API_KEY=$EMAIL_API_KEY
BACKEND_NETWORK_SUBNET=172.28.0.0/24
HTTPS_REDIRECTION_ENABLED=false
HTTP_PORT=8080
EOF
chmod 600 "$out"
