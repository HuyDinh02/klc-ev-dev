#!/bin/bash
# K-Charge Mobile App — Maestro Test Runner
# Run this in your terminal (requires TTY for Maestro output)

set -e

export PATH="$PATH:$HOME/.maestro/bin"
export MAESTRO_CLI_NO_ANALYTICS=1
export MAESTRO_CLI_ANALYSIS_NOTIFICATION_DISABLED=true

TESTS_DIR=".maestro"

echo "=== K-Charge Mobile Test Suite ==="
echo ""

# Single flow mode: ./run-tests.sh login
if [ -n "$1" ]; then
  echo "Running: $1"
  maestro test "$TESTS_DIR/$1.yaml" --format junit --output /tmp/maestro-$1.xml
  exit $?
fi

# Run all flows
echo "Running all test flows..."
maestro test "$TESTS_DIR" --format junit --output /tmp/maestro-all.xml

echo ""
echo "=== Done. Report saved to /tmp/maestro-all.xml ==="
