#!/usr/bin/env bash
set -euo pipefail
hex(){ openssl rand -hex "$1"; }
printf 'POSTGRES_PASSWORD=%s\n' "$(hex 18)"
printf 'REDIS_PASSWORD=%s\n' "$(hex 18)"
printf 'RABBITMQ_PASSWORD=%s\n' "$(hex 18)"
printf 'JWT_SECRET=%s\n' "$(hex 32)"
printf 'EMAIL_API_KEY=ci_%s\n' "$(hex 16)"
