#!/usr/bin/env bash
# Database restore script
# Usage: ./scripts/db-restore.sh <backup-file> [local]
set -euo pipefail

BACKUP_FILE="${1:?Usage: $0 <backup-file> [local]}"
ENV="${2:-local}"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Backup file not found: $BACKUP_FILE"
    exit 1
fi

if [ "$ENV" = "local" ]; then
    DB_HOST="localhost"
    DB_PORT="5433"
    DB_USER="postgres"
    DB_NAME="KLC"
    export PGPASSWORD="postgres"

    echo "WARNING: This will drop and recreate the '$DB_NAME' database!"
    read -p "Continue? (y/N) " confirm
    [ "$confirm" = "y" ] || exit 0

    echo "Dropping existing database..."
    dropdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" --if-exists "$DB_NAME"

    echo "Creating database..."
    createdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME"

    echo "Restoring from backup..."
    pg_restore -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --no-owner --no-privileges "$BACKUP_FILE"

    echo "Restore complete!"
else
    echo "Production restore must be done manually via Cloud SQL."
    exit 1
fi
