# KLC EV Charging — Load Tests

Load testing scripts for the KLC EV Charging Station Management System, built with [k6](https://k6.io/) by Grafana Labs.

## Prerequisites

### Install k6

**macOS (Homebrew):**

```bash
brew install grafana/k6/k6
```

**Windows (Chocolatey):**

```bash
choco install k6
```

**Linux (Debian/Ubuntu):**

```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D68
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
  | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6
```

**Docker:**

```bash
docker run --rm -i grafana/k6 run - < api-load-test.js
```

Verify installation:

```bash
k6 version
```

## Test Scripts

### 1. API Load Test (`api-load-test.js`)

Tests both the Admin API and Driver BFF endpoints across three scenarios:

| Scenario | VUs | Duration | Purpose |
|----------|-----|----------|---------|
| **Smoke** | 1 | 30s | Verify endpoints are reachable |
| **Load** | 0 -> 50 | 6min | Normal expected traffic |
| **Stress** | 0 -> 200 | 7min | Find breaking points |

**Endpoints tested:**
- `GET /health` — Admin API and BFF health checks
- `POST /connect/token` — OpenIddict authentication
- `GET /api/app/charging-station` — Station list (authenticated)
- `GET /api/app/dashboard` — Dashboard stats (authenticated)
- `GET /api/v1/stations/nearby` — BFF nearby stations

### 2. OCPP WebSocket Load Test (`ocpp-ws-load-test.js`)

Simulates OCPP 1.6J charge points connecting via WebSocket:

| Phase | Chargers | Duration |
|-------|----------|----------|
| Ramp up | 1 -> 10 | 1min |
| Ramp up | 10 -> 50 | 2min |
| Hold | 50 | 3min |
| Ramp up | 50 -> 100 | 1min |
| Hold | 100 | 2min |
| Ramp down | 100 -> 0 | 1min |

**OCPP messages sent per charger:**
- `BootNotification` — on connect
- `Heartbeat` — every 30 seconds
- `StatusNotification` — every 60 seconds

Each simulated charger stays connected for 2-5 minutes.

## Running Tests

### Against production

```bash
# API load test (all scenarios: smoke + load + stress)
k6 run api-load-test.js

# OCPP WebSocket test
k6 run ocpp-ws-load-test.js
```

### Against local development

```bash
# API load test against local services
k6 run \
  --env ADMIN_API_URL=https://localhost:44305 \
  --env BFF_URL=http://localhost:5001 \
  api-load-test.js

# OCPP WebSocket test against local
k6 run \
  --env OCPP_URL=ws://localhost:44305/ocpp \
  ocpp-ws-load-test.js
```

### Run a single scenario

```bash
# Smoke test only (quick validation)
k6 run --env ADMIN_API_URL=https://api.ev.odcall.com api-load-test.js \
  --scenario smoke

# Load test only (skip smoke and stress)
k6 run api-load-test.js --scenario load
```

### With custom credentials

```bash
k6 run \
  --env ADMIN_USER=operator \
  --env ADMIN_PASS='Admin@123' \
  --env CLIENT_SECRET='your-secret' \
  api-load-test.js
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ADMIN_API_URL` | `https://api.ev.odcall.com` | Admin API base URL |
| `BFF_URL` | `https://bff.ev.odcall.com` | Driver BFF base URL |
| `OCPP_URL` | `wss://api.ev.odcall.com/ocpp` | OCPP WebSocket base URL |
| `CLIENT_ID` | `KLC_Api` | OpenIddict client ID |
| `CLIENT_SECRET` | (empty) | OpenIddict client secret |
| `ADMIN_USER` | `admin` | Admin username for auth tests |
| `ADMIN_PASS` | `Admin@123` | Admin password for auth tests |

## Interpreting Results

### Key Metrics

After each run, k6 outputs a summary. Focus on these metrics:

```
http_req_duration ........: avg=120ms  min=10ms  med=80ms  max=2500ms  p(90)=300ms  p(95)=500ms
http_req_failed ..........: 2.50%  (25 out of 1000)
auth_success .............: 97.00% (97 out of 100)
api_errors ...............: 1.50%  (15 out of 1000)
```

| Metric | Threshold | Meaning |
|--------|-----------|---------|
| `http_req_duration p(95)` | < 2000ms | 95th percentile response time |
| `http_req_failed` | < 5% | HTTP error rate |
| `auth_success` | > 95% | Token endpoint success rate |
| `ws_response_time p(95)` | < 5000ms | OCPP message response time |
| `ws_connections` | > 0 | Total WebSocket connections established |
| `ws_messages_sent` | - | Total OCPP messages sent |

### Pass/Fail

k6 exits with code 0 if all thresholds pass, non-zero otherwise. Thresholds are defined in each script's `options.thresholds` block.

### Common Issues

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| All auth requests fail | Wrong credentials or client secret | Check `CLIENT_SECRET` env var |
| High p(95) latency | Server under-provisioned or cold start | Run smoke test first to warm up |
| WebSocket 403 | OCPP auth enabled on server | Set HTTP Basic Auth credentials |
| Connection refused | Service not running or wrong URL | Verify URLs with `curl` first |

## Output Formats

### JSON output (for CI/CD pipelines)

```bash
k6 run --out json=results.json api-load-test.js
```

### CSV output

```bash
k6 run --out csv=results.csv api-load-test.js
```

### Grafana Cloud k6 (remote reporting)

```bash
K6_CLOUD_TOKEN=your-token k6 cloud api-load-test.js
```

## Recommended Test Plan

1. **Before deployment**: Run smoke test to verify endpoints
2. **After deployment**: Run full load test to validate performance
3. **Periodically**: Run stress test to find capacity limits
4. **Before go-live**: Run OCPP WebSocket test to validate charger connection capacity

Target capacity for MVP (June 2026):
- Admin API: 50 concurrent users, p(95) < 2s
- Driver BFF: 200 concurrent users, p(95) < 1s
- OCPP WebSocket: 100 simultaneous charger connections
