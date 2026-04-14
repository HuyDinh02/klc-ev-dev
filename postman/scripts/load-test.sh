#!/bin/bash
# Load test — runs collection with concurrent virtual users.
# Usage: ./postman/scripts/load-test.sh [USERS] [ITERATIONS]
# Example: ./postman/scripts/load-test.sh 5 50
set -e

CONCURRENT_USERS=${1:-5}
ITERATIONS_PER_USER=${2:-20}

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
POSTMAN_DIR="$(dirname "$SCRIPT_DIR")"
RESULTS_DIR="$POSTMAN_DIR/results/load-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$RESULTS_DIR"

COLLECTION="$POSTMAN_DIR/KLC-EV-Charging.postman_collection.json"
ENVIRONMENT="$POSTMAN_DIR/KLC-EV-Charging.postman_environment.json"

TOTAL_REQUESTS=$((CONCURRENT_USERS * ITERATIONS_PER_USER * 47))

echo "=== KLC Load Test ==="
echo "Time: $(date)"
echo "Virtual Users: $CONCURRENT_USERS"
echo "Iterations per user: $ITERATIONS_PER_USER"
echo "Estimated total requests: ~$TOTAL_REQUESTS"
echo "Results dir: $RESULTS_DIR"
echo ""
echo "Starting $CONCURRENT_USERS parallel runners..."

START_TIME=$(date +%s)

for i in $(seq 1 $CONCURRENT_USERS); do
  npx newman run "$COLLECTION" \
    -e "$ENVIRONMENT" \
    --iteration-count "$ITERATIONS_PER_USER" \
    --delay-request 50 \
    --timeout-request 15000 \
    --reporters json \
    --reporter-json-export "$RESULTS_DIR/user-${i}.json" \
    --suppress-exit-code \
    --silent &
  echo "  Started user $i (PID: $!)"
done

echo ""
echo "Waiting for all runners to complete..."
wait

END_TIME=$(date +%s)
WALL_TIME=$((END_TIME - START_TIME))

echo ""
echo "All runners complete in ${WALL_TIME}s. Aggregating results..."
echo ""

# Aggregate results from all runners
node -e "
const fs = require('fs');
const files = fs.readdirSync('$RESULTS_DIR').filter(f => f.startsWith('user-') && f.endsWith('.json'));

let allTimes = [];
let totalReqs = 0, failedReqs = 0, totalTests = 0, failedTests = 0;
let perEndpoint = {};

files.forEach(f => {
  const data = JSON.parse(fs.readFileSync('$RESULTS_DIR/' + f, 'utf8'));
  const run = data.run;
  totalReqs += run.stats.requests.total;
  failedReqs += run.stats.requests.failed;
  totalTests += run.stats.tests.total;
  failedTests += run.stats.tests.failed;

  run.executions.filter(e => e.response).forEach(e => {
    const time = e.response.responseTime;
    allTimes.push(time);
    const name = e.item.name;
    if (!perEndpoint[name]) perEndpoint[name] = { times: [], errors: 0 };
    perEndpoint[name].times.push(time);
    if (e.response.code >= 400) perEndpoint[name].errors++;
  });
});

allTimes.sort((a, b) => a - b);
const p = (arr, pct) => arr[Math.floor(arr.length * pct / 100)] || 0;
const avg = (arr) => arr.length ? Math.round(arr.reduce((a,b) => a+b, 0) / arr.length) : 0;

console.log('=== Load Test Results ===');
console.log('Virtual Users:', files.length);
console.log('Total Requests:', totalReqs);
console.log('Failed Requests:', failedReqs, '(' + ((failedReqs/totalReqs)*100).toFixed(2) + '%)');
console.log('Wall Time:', $WALL_TIME + 's');
console.log('Throughput:', (totalReqs / $WALL_TIME).toFixed(1), 'req/s');
console.log('');
console.log('=== Latency (ms) ===');
console.log('Min:', allTimes[0]);
console.log('Avg:', avg(allTimes));
console.log('p50:', p(allTimes, 50));
console.log('p90:', p(allTimes, 90));
console.log('p95:', p(allTimes, 95));
console.log('p99:', p(allTimes, 99));
console.log('Max:', allTimes[allTimes.length - 1]);
console.log('');
console.log('=== Tests ===');
console.log('Total:', totalTests, '| Passed:', totalTests - failedTests, '| Failed:', failedTests);
console.log('');

// Top 5 slowest endpoints
console.log('=== Slowest Endpoints (by p95) ===');
const sorted = Object.entries(perEndpoint)
  .map(([name, d]) => ({ name, p95: p(d.times.sort((a,b)=>a-b), 95), avg: avg(d.times), errors: d.errors, count: d.times.length }))
  .sort((a, b) => b.p95 - a.p95);
sorted.slice(0, 10).forEach(e => {
  console.log('  ' + e.name.padEnd(30) + ' avg:' + String(e.avg).padStart(5) + 'ms  p95:' + String(e.p95).padStart(5) + 'ms  errors:' + e.errors + '/' + e.count);
});
console.log('');
console.log('Results dir:', '$RESULTS_DIR');
"
