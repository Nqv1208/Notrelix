#!/bin/sh
set -eu

n8n_database="${N8N_POSTGRES_DB:-notrelix_n8n}"

if [ "$n8n_database" = "$POSTGRES_DB" ]; then
  echo "n8n database is the same as POSTGRES_DB; skipping separate database creation."
  exit 0
fi

if psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -tAc "SELECT 1 FROM pg_database WHERE datname = '$n8n_database'" | grep -q 1; then
  echo "n8n database '$n8n_database' already exists."
else
  createdb --username "$POSTGRES_USER" "$n8n_database"
  echo "Created n8n database '$n8n_database'."
fi
