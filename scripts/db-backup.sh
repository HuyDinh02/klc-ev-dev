#!/usr/bin/env bash
# Database backup script for KLC EV Charging
# Usage: ./scripts/db-backup.sh [local|production]
set -euo pipefail

ENV="${1:-local}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="backups"
mkdir -p "$BACKUP_DIR"

if [ "$ENV" = "local" ]; then
    DB_HOST="localhost"
    DB_PORT="5433"
    DB_USER="postgres"
    DB_NAME="KLC"
    export PGPASSWORD="postgres"

    echo "Backing up local database..."
    pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --format=custom --compress=9 \
        -f "$BACKUP_DIR/klc_${ENV}_${TIMESTAMP}.dump"

    echo "Backup saved: $BACKUP_DIR/klc_${ENV}_${TIMESTAMP}.dump"

elif [ "$ENV" = "production" ]; then
    echo "Production backup requires Cloud SQL Auth Proxy."
    echo "1. Start proxy: cloud_sql_proxy -instances=klc-ev-charging:asia-southeast1:klc-postgres=tcp:5434"
    echo "2. Run: PGPASSWORD=\$KLC_DB_PASSWORD pg_dump -h 127.0.0.1 -p 5434 -U klc_app -d KLC --format=custom --compress=9 -f $BACKUP_DIR/klc_prod_${TIMESTAMP}.dump"
    exit 1
else
    echo "Usage: $0 [local|production]"
    exit 1
fi
