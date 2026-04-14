#!/bin/bash
# Production smoke test — verifies all critical flows work.
# Usage: ./postman/scripts/smoke-test.sh
# Exit code 0 = all pass, 1 = failures found
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
POSTMAN_DIR="$(dirname "$SCRIPT_DIR")"
RESULTS_DIR="$POSTMAN_DIR/results"
mkdir -p "$RESULTS_DIR"

COLLECTION="$POSTMAN_DIR/KLC-EV-Charging.postman_collection.json"
ENVIRONMENT="$POSTMAN_DIR/KLC-EV-Charging.postman_environment.json"

echo "=== KLC Production Smoke Test ==="
echo "Time: $(date)"
echo ""

npx newman run "$COLLECTION" \
  -e "$ENVIRONMENT" \
  --bail \
  --timeout-request 15000 \
  --delay-request 100 \
  --reporters cli,json,htmlextra \
  --reporter-json-export "$RESULTS_DIR/smoke-test.json" \
  --reporter-htmlextra-export "$RESULTS_DIR/smoke-test.html" \
  --color on

EXIT_CODE=$?

# Parse results
node -e "
const fs = require('fs');
const data = JSON.parse(fs.readFileSync('$RESULTS_DIR/smoke-test.json', 'utf8'));
const run = data.run;
const times = run.executions.filter(e => e.response).map(e => e.response.responseTime).sort((a,b) => a-b);
const p = (pct) => times[Math.floor(times.length * pct / 100)] || 0;
const total = run.stats.requests.total;
const failed = run.stats.requests.failed;
const duration = ((run.timings.completed - run.timings.started) / 1000).toFixed(1);

console.log('');
console.log('=== Summary ===');
console.log('Requests:', total, '| Failed:', failed, '| Error Rate:', ((failed/total)*100).toFixed(1) + '%');
console.log('Duration:', duration + 's | Throughput:', (total / duration).toFixed(1), 'req/s');
console.log('Latency — p50:', p(50) + 'ms | p95:', p(95) + 'ms | p99:', p(99) + 'ms | Max:', times[times.length-1] + 'ms');
console.log('Tests:', run.stats.tests.total, '| Passed:', run.stats.tests.total - run.stats.tests.failed, '| Failed:', run.stats.tests.failed);
console.log('');
console.log('HTML report:', '$RESULTS_DIR/smoke-test.html');
"

exit $EXIT_CODE
