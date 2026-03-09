# Production Readiness Checklist

KLC EV Charging Station Management System

## 1. Security

- [ ] All secrets stored in GCP Secret Manager (not in code or env files)
- [ ] `Jwt:SecretKey` configured via Secret Manager (no hardcoded fallback)
- [ ] OpenIddict `client_secret` served via server-side API route only
- [ ] CORS restricted to known domains (`ev.odcall.com`, `api.ev.odcall.com`)
- [ ] HTTPS enforced on all public endpoints
- [ ] Rate limiting enabled: BFF auth 10/min, BFF API 60/min, Admin API 100/min
- [ ] `[Authorize]` on all admin API controllers
- [ ] `Ocpp:AllowTestIdTags` set to `false`
- [ ] Payment callback HMAC-SHA256 signature verification enabled (MoMo/VnPay)
- [ ] Security headers configured (X-Content-Type-Options, X-Frame-Options, Strict-Transport-Security)
- [ ] Firebase service account key not committed to repository
- [ ] Pre-commit hook installed to prevent secret commits

## 2. Database

- [ ] Cloud SQL instance provisioned with automated daily backups
- [ ] Point-in-time recovery enabled (7-day retention minimum)
- [ ] Database connection pooling configured (max connections appropriate for Cloud Run instances)
- [ ] Read replica configured for Driver BFF queries
- [ ] All EF Core migrations tested against production schema
- [ ] PostGIS extension enabled and spatial indexes verified
- [ ] `klc_app` user has least-privilege permissions (no superuser)
- [ ] Backup/restore scripts tested: `scripts/db-backup.sh`, `scripts/db-restore.sh`

## 3. Monitoring

- [ ] Health check endpoints responding: `/health` on Admin API and Driver BFF
- [ ] Cloud Run health checks configured (startup, liveness)
- [ ] Structured logging enabled (JSON format for Cloud Logging)
- [ ] Error tracking configured (Sentry DSN or equivalent)
- [ ] Uptime checks for `api.ev.odcall.com`, `bff.ev.odcall.com`, `ev.odcall.com`
- [ ] OCPP WebSocket connection monitoring (heartbeat timeouts tracked)
- [ ] Alert policies: error rate > 1%, latency p95 > 2s, instance restarts

## 4. Performance

- [ ] Load tested: target 100 concurrent charging sessions
- [ ] Redis cache configured for BFF (station data, session lookups)
- [ ] Cloud Run autoscaling: min 1, max instances set per service
- [ ] Cloud Run concurrency limits configured (default 80)
- [ ] WebSocket session affinity enabled for OCPP endpoint (3600s timeout)
- [ ] Static assets served via CDN (admin portal)
- [ ] Database query performance reviewed (no N+1 queries, indexes on foreign keys)

## 5. Operations

- [ ] CI/CD pipelines passing: build, test, deploy to Cloud Run
- [ ] Rollback procedure documented and tested (Cloud Run revision rollback)
- [ ] Database migration rollback scripts prepared for latest 3 migrations
- [ ] On-call rotation established with escalation policy
- [ ] Runbook: OCPP charger reconnection procedure
- [ ] Runbook: payment gateway failover (MoMo/VnPay)
- [ ] Runbook: database restore from backup
- [ ] Incident response process documented
- [ ] Domain DNS verified: `ev.odcall.com`, `api.ev.odcall.com`, `bff.ev.odcall.com`, `ocpp.ev.odcall.com`

## 6. Compliance

- [ ] User data privacy policy implemented (Vietnamese regulations)
- [ ] Audit logging enabled for all admin actions (ABP audit log module)
- [ ] E-invoice integration tested with tax authority
- [ ] Personal data encryption at rest (Cloud SQL default encryption)
- [ ] Data retention policy defined and automated (soft delete cleanup schedule)
- [ ] User account deletion flow implemented (GDPR/PDPA compliance)
- [ ] Payment transaction records retained per financial regulations (5 years minimum)
