#!/bin/bash
# Connect to Cloud SQL via Auth Proxy with read-only user.
# Safe for production — does not touch service credentials.
#
# Usage:
#   ./scripts/db-debug.sh              # interactive psql shell
#   ./scripts/db-debug.sh -c "SELECT 1" # run a single query
#
# Prerequisites: brew install cloud-sql-proxy

set -e

PROJECT="klc-ev-charging"
INSTANCE="klc-ev-charging:asia-southeast1:klc-postgres"
DB="KLC"
USER="debug_readonly"
PASS="debug_readonly_2026"
PORT=5434

# Start proxy if not already running
if ! lsof -i :$PORT -sTCP:LISTEN >/dev/null 2>&1; then
  echo "Starting Cloud SQL Auth Proxy on port $PORT..."
  cloud-sql-proxy "$INSTANCE" --port=$PORT &
  PROXY_PID=$!
  trap "kill $PROXY_PID 2>/dev/null" EXIT
  sleep 2
else
  echo "Proxy already running on port $PORT"
fi

PGPASSWORD="$PASS" psql -h localhost -p $PORT -U "$USER" -d "$DB" "$@"
