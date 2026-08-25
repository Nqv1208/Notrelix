#!/usr/bin/env bash
# Single authority for non-production CI/test credentials.
#
# Emits KEY=VALUE lines with cryptographically random values that live only in
# the consuming process/job. Never commit generated values and never log them.
set -euo pipefail

echo "POSTGRES_PASSWORD=$(openssl rand -hex 16)"
echo "REDIS_PASSWORD=$(openssl rand -hex 16)"
echo "RABBITMQ_PASSWORD=$(openssl rand -hex 16)"
echo "JWT_SECRET=$(openssl rand -hex 32)"
echo "EMAIL_API_KEY=$(openssl rand -hex 16)"
