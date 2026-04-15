# Pre-Launch Testing Plan

**Target**: 10 stations, 500 users, April 2026 launch

---

## 1. Penetration Testing

### 1.1 Tools

```bash
brew install nuclei k6 websocat
brew install --cask zap
pip install sslyze
```

### 1.2 OWASP Top 10 Checklist

| # | Test | Command | Pass Criteria | Blocker? |
|---|------|---------|---------------|----------|
| A01 | Broken Access Control — IDOR | curl other user's session/wallet with your token | 404 (not 200 with data) | YES |
| A01 | Role enforcement | Viewer token → POST stations | 403 | YES |
| A01 | No-auth on protected endpoints | curl /wallet/balance without token | 401 | YES |
| A02 | TLS config | `sslyze api.ev.odcall.com` | TLS 1.2+ only | YES |
| A02 | Security headers | `curl -sI api.ev.odcall.com` | HSTS, X-Content-Type, X-Frame | YES |
| A03 | SQL injection | `sqlmap -u ".../stations/search?q=test"` | Not injectable | YES |
| A04 | Negative amount topup | POST /wallet/topup `{"amount":-50000}` | 400 | YES |
| A04 | Zero amount topup | POST /wallet/topup `{"amount":0}` | 400 | YES |
| A04 | VnPay IPN tampered hash | Send IPN with fake vnp_SecureHash | RspCode 97 | YES |
| A04 | VnPay IPN replay (idempotency) | Send same valid IPN twice | Second returns "already confirmed" | YES |
| A04 | VnPay IPN from wrong IP | Send from non-whitelisted IP | RspCode 99 | YES |
| A05 | Swagger in production | GET /swagger/index.html | 404 | NO |
| A05 | Nuclei scan | `nuclei -u api.ev.odcall.com` | 0 critical/high | YES |
| A06 | Vulnerable packages | `dotnet list package --vulnerable` | 0 known vulns | YES |
| A06 | npm audit | `cd admin-portal && npm audit` | 0 critical/high | YES |
| A07 | Login brute force | 15 rapid login attempts | 429 after ~10 | YES |
| A07 | OTP brute force | 15 rapid verify-phone attempts | Rate limited | YES |
| A07 | JWT alg:none attack | Forge token with alg:none | 401 | YES |
| A08 | Payment race condition | 10 concurrent IPNs for same txn | Only 1 wallet credit | YES |

### 1.3 Payment-Specific Tests

```bash
# Double-credit prevention (10 concurrent IPN callbacks)
for i in $(seq 1 10); do
  curl -s "https://bff.ev.odcall.com/api/v1/wallet/topup/vnpay-ipn?vnp_TxnRef=RACE_TEST&vnp_Amount=100000&vnp_ResponseCode=00&vnp_SecureHash=fake" &
done
wait
# Verify: only 1 credit in DB

# Double-session prevention (2 concurrent start requests)
curl -s -X POST ".../sessions/start" -d '{"connectorId":"A"}' &
curl -s -X POST ".../sessions/start" -d '{"connectorId":"B"}' &
wait
# Verify: only 1 session created (unique constraint)
```

---

## 2. Load Testing (k6)

### 2.1 Target Metrics

| Metric | Target | Blocker? |
|--------|--------|----------|
| p95 latency (API) | < 2,000ms | YES |
| p99 latency (API) | < 5,000ms | YES |
| Error rate | < 1% | YES |
| Station search p95 | < 1,000ms | NO |
| Wallet balance p95 | < 500ms | NO |
| Session start p95 | < 3,000ms | NO |
| Throughput | > 50 req/s sustained | NO |
| 429 rate limit errors | < 0.1% at target load | YES |

### 2.2 User Scenarios

| Scenario | Virtual Users | Duration | Behavior |
|----------|--------------|----------|----------|
| Browsing | 300 VU | 10 min | Search stations, view details, check wallet |
| Charging | 150 VU | 10 min | Start session, poll active, stop session |
| Top-up | 50 VU | 10 min | Initiate VnPay top-up, check status |
| **Total** | **500 VU** | **10 min** | |

### 2.3 Run Load Test

```bash
# Install k6
brew install k6

# Run with 500 virtual users
k6 run tests/load/driver-flow.js \
  --env BFF_URL=https://bff.ev.odcall.com \
  --out json=results/load-test.json

# Or quick Newman-based test (already available)
./postman/scripts/load-test.sh 10 50  # 10 users x 50 iterations
```

### 2.4 Quick Load Test (Newman — available now)

```bash
# Light load (already proven: 0% errors)
./postman/scripts/load-test.sh 5 20    # 4,700 requests

# Medium load
./postman/scripts/load-test.sh 10 30   # 14,100 requests

# Heavy load
./postman/scripts/load-test.sh 20 50   # 47,000 requests
```

---

## 3. Stress Testing

### 3.1 Goal

Find the breaking point — what happens beyond 500 users.

### 3.2 Ramp-Up Test

```bash
# k6 ramp-up: 0 → 100 → 500 → 1000 → 0 users
k6 run tests/load/stress-test.js \
  --env BFF_URL=https://bff.ev.odcall.com
```

Stages:
1. **Warm-up**: 0 → 100 VU over 2 min
2. **Target load**: 100 → 500 VU over 5 min
3. **Stress**: 500 → 1000 VU over 5 min
4. **Recovery**: 1000 → 0 VU over 2 min

### 3.3 What to Monitor

| Metric | Tool | Alert Threshold |
|--------|------|-----------------|
| Cloud Run instance count | `gcloud run services describe` | > 10 instances |
| Cloud Run CPU/Memory | Cloud Console Metrics | CPU > 80% |
| DB connections | `SELECT count(*) FROM pg_stat_activity` | > 80 |
| DB connection pool | Cloud SQL Insights | Pool exhausted |
| Redis memory | `redis-cli info memory` | > 80% |
| Error rate (5xx) | Sentry + k6 | > 1% |
| p99 latency | k6 | > 10s |
| Rate limit (429) | k6 | > 5% |

### 3.4 Cloud Run Scaling Limits

```bash
# Check current limits
gcloud run services describe klc-driver-bff --region=asia-southeast1 --project=klc-ev-charging \
  --format="get(spec.template.metadata.annotations)"

# Key settings to review:
# autoscaling.knative.dev/maxScale: 100 (max instances)
# autoscaling.knative.dev/minScale: 1 (min instances)
# containerConcurrency: 80 (requests per instance)
```

### 3.5 Database Stress

```bash
# Check current connection limits
./scripts/db-debug.sh -c "SHOW max_connections;"
./scripts/db-debug.sh -c "SELECT count(*) FROM pg_stat_activity;"

# During stress test, monitor connections
watch -n 5 './scripts/db-debug.sh -c "SELECT count(*), state FROM pg_stat_activity GROUP BY state;"'
```

---

## 4. EV Charging-Specific Tests

### 4.1 Concurrent OCPP Sessions

| Test | What | Pass |
|------|------|------|
| 10 chargers heartbeat simultaneously | All 10 respond within 5s | No timeouts |
| Start 10 sessions across 10 stations | All start successfully | 0 failures |
| Stop all 10 sessions at once | All complete with correct billing | Billing matches energy |
| Charger reconnect during session | Session resumes, no duplicate billing | Meter values continuous |
| MeterValues every 10s from 10 chargers | All processed, no backlog | DB writes < 100ms |

### 4.2 Payment Race Conditions

| Test | What | Pass |
|------|------|------|
| 2 VnPay IPNs for same transaction | Only 1 wallet credit | Check wallet balance |
| Top-up + session payment simultaneously | Correct final balance | Balance = topup - session |
| 5 users top-up at same time | All 5 independent | No cross-contamination |
| Session billing during wallet top-up | No double-charge or missed charge | Verify with DB query |

### 4.3 SignalR Under Load

```bash
# Test SignalR with many subscribers (admin monitoring)
# Use websocat to simulate 50 monitoring connections
for i in $(seq 1 50); do
  websocat "wss://api.ev.odcall.com/hubs/monitoring?access_token=$ADMIN_TOKEN" &
done
# Then trigger status changes — all 50 should receive updates
```

---

## 5. Execution Timeline

| Day | Activity | Duration |
|-----|----------|----------|
| Day 1 | Security scan: nuclei, sslyze, npm audit, dotnet audit | 2h |
| Day 1 | OWASP Top 10 manual tests (access control, injection, payment) | 4h |
| Day 2 | Load test: 500 VU x 10 min (Newman + k6) | 2h |
| Day 2 | Stress test: ramp to 1000 VU, find breaking point | 2h |
| Day 2 | OCPP concurrent session test | 2h |
| Day 3 | Fix any blockers found | TBD |
| Day 3 | Re-test after fixes | 2h |
| Day 3 | Sign-off report | 1h |

---

## 6. Pass/Fail Summary (Go/No-Go)

### Must Pass (Blockers)

- [ ] 0 SQL injection vulnerabilities
- [ ] 0 IDOR (cross-user data access)
- [ ] All role enforcement tests pass
- [ ] JWT manipulation rejected
- [ ] VnPay IPN signature validation works
- [ ] VnPay IPN replay produces single credit
- [ ] Rate limiting active on auth endpoints
- [ ] TLS 1.2+ only
- [ ] p95 latency < 2s at 500 VU
- [ ] Error rate < 1% at 500 VU
- [ ] 0 critical/high CVEs in dependencies

### Should Pass (Fix before launch if possible)

- [ ] Security headers (HSTS, X-Frame, X-Content-Type)
- [ ] Swagger disabled in production
- [ ] Station search p95 < 1s
- [ ] SignalR handles 50 concurrent subscribers
- [ ] Cloud Run auto-scales to handle 1000 VU spike
