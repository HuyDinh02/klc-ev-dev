# April 15, 2026 — Production Release Plan

## Target
- 10 real charging stations connected via OCPP
- 500 registered users (est. 50-100 concurrent)
- Full core flow: register → topup VnPay → scan QR → charge → stop → billing

## Timeline

### Week 1: Apr 3-6 (Code Complete)

| Date | Task | Owner | Status |
|------|------|-------|--------|
| Apr 3 | Session timeout (Pending/Stopping 5min) | Done | ✅ |
| Apr 3 | OCPP idTag fix (20-char session ID) | Done | ✅ |
| Apr 3 | Cloud Run scaling fix (max-instances) | Done | ✅ |
| Apr 3 | Graceful WebSocket close on shutdown | Done | ✅ |
| Apr 3 | OCPP page improvements (filter, badges, live) | Done | ✅ |
| Apr 3 | VnPay Query API fix | Done | ✅ |
| Apr 3 | Auto TriggerMessage after BootNotification | Done | ✅ |
| Apr 3 | K6 load test (50 VU=233ms, 500 VU=1.4s) | Done | ✅ |
| Apr 4 | Mobile: Firebase Phone Auth integration | Mobile dev | 🔲 |
| Apr 4 | Register real stations (chargePointIds) | Ops/KLC | 🔲 |
| Apr 5 | Test full flow on real charger (QR→charge→stop) | QA | 🔲 |
| Apr 6 | Bug fixes from real charger testing | Dev | 🔲 |

### Week 2: Apr 7-11 (Integration Testing)

| Date | Task | Owner | Status |
|------|------|-------|--------|
| Apr 7 | Connect 10 real stations to OCPP gateway | Ops/KLC | 🔲 |
| Apr 7 | Seed station data (names, addresses, tariffs) | Dev | 🔲 |
| Apr 8 | VnPay production credentials (if different from sandbox) | KLC/VnPay | 🔲 |
| Apr 8 | Test VnPay topup with real money (small amounts) | QA | 🔲 |
| Apr 9 | Multi-station charging test (5 users, 5 stations) | QA | 🔲 |
| Apr 10 | Mobile app build (TestFlight/Internal Testing) | Mobile dev | 🔲 |
| Apr 11 | Fix bugs from integration testing | Dev | 🔲 |

### Week 3: Apr 12-15 (Launch)

| Date | Task | Owner | Status |
|------|------|-------|--------|
| Apr 12 | Final regression test (1,329 automated tests) | Dev | 🔲 |
| Apr 12 | Load test with 10 stations connected | Dev | 🔲 |
| Apr 13 | Staging freeze — no code changes | All | 🔲 |
| Apr 13 | Backup database, snapshot Cloud SQL | Ops | 🔲 |
| Apr 14 | Smoke test all endpoints | QA | 🔲 |
| Apr 14 | Monitor dashboards setup (Sentry alerts, uptime) | Dev | 🔲 |
| **Apr 15** | **Go Live** 🚀 | All | 🔲 |

## Checklist: Go/No-Go Decision (Apr 14)

### Must Have (Go)
- [ ] 10 stations connected and sending heartbeats
- [ ] Full charging flow works on real charger (QR→start→meter→stop→billing)
- [ ] VnPay topup works (user can add money)
- [ ] Firebase Phone Auth works (user can register/login)
- [ ] Admin portal shows sessions, monitoring, OCPP events
- [ ] No critical errors in Sentry/Cloud Logging for 24h
- [ ] All 1,329 automated tests pass
- [ ] Load test: p95 < 2s at 50 concurrent users

### Nice to Have (won't block launch)
- [ ] Mobile app in Play Store / TestFlight
- [ ] SMS fallback for non-Firebase auth
- [ ] Station detail real-time SignalR refresh
- [ ] Mobile specific error messages

## Infrastructure

| Service | Config | URL |
|---------|--------|-----|
| Admin API | min=1, max=2, 1Gi, session-affinity | api.ev.odcall.com |
| Driver BFF | min=1, max=5, 512Mi | bff.ev.odcall.com |
| Admin Portal | min=0, max=3, 512Mi | ev.odcall.com |
| OCPP Simulator | min=0, max=1, nginx | sim.ev.odcall.com |
| PostgreSQL | Cloud SQL, PostGIS 16, klc-postgres | Cloud SQL proxy |
| Redis | Memorystore 7 | VPC internal |

## Rollback Plan
1. Revert to previous Cloud Run revision: `gcloud run services update-traffic klc-admin-api --to-revisions=PREV_REVISION=100`
2. Database: restore from Cloud SQL automated backup (point-in-time recovery)
3. Notify users via app push notification if downtime > 5 minutes

## Post-Launch (Apr 16+)
- Monitor error rates for 48h
- Optimize DB queries if p95 > 500ms
- Implement Redis Pub/Sub for multi-instance OCPP (if scaling needed)
- Migrate BFF to ABP repositories
- Add domain events for cross-aggregate consistency
