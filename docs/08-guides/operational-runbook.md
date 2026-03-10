# Operational Runbook

## Service URLs

| Service | Production URL | Health Check |
|---------|---------------|--------------|
| Admin API | https://api.ev.odcall.com | `/health`, `/health/ready` |
| Driver BFF | https://bff.ev.odcall.com | `/health`, `/health/ready` |
| Admin Portal | https://ev.odcall.com | `/` (200 OK) |

## Service Health Checks

```bash
# Admin API
curl -s https://api.ev.odcall.com/health
curl -s https://api.ev.odcall.com/health/ready

# Driver BFF
curl -s https://bff.ev.odcall.com/health
curl -s https://bff.ev.odcall.com/health/ready
```

## Common Issues & Resolution

### Container won't start

1. Check recent logs:
   ```bash
   gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=klc-admin-api" \
     --project=klc-ev-charging --limit=50 --format="table(timestamp,textPayload)"
   ```
2. Verify secrets are accessible:
   ```bash
   gcloud secrets list --project=klc-ev-charging
   gcloud secrets versions access latest --secret=db-connection-string --project=klc-ev-charging
   ```
3. Check Cloud Run revision status:
   ```bash
   gcloud run revisions list --service=klc-admin-api --region=asia-southeast1 --project=klc-ev-charging
   ```

### OCPP charger disconnected

- Check WebSocket endpoint: `wss://api.ev.odcall.com/ocpp/{chargePointId}` (subprotocol: `ocpp1.6`)
- Verify Admin API min-instances >= 1 (cold starts kill WebSocket connections)
- Check charger-side logs for connection errors
- Restart the charge point from the admin portal (Stations > [station] > Restart)
- Check Cloud Run logs for WebSocket timeout or connection reset:
  ```bash
  gcloud logging read "resource.labels.service_name=klc-admin-api AND textPayload=~\"WebSocket|OCPP|chargePoint\"" \
    --project=klc-ev-charging --limit=100 --format="table(timestamp,textPayload)"
  ```

### Payment callback failure

- Verify HMAC-SHA256 signature matches expected (check `Payment__MoMo__SecretKey` or `Payment__VnPay__HashSecret`)
- Check gateway status pages: [MoMo](https://business.momo.vn), [VnPay](https://vnpay.vn)
- Check BFF payment logs:
  ```bash
  gcloud logging read "resource.labels.service_name=klc-driver-bff AND textPayload=~\"payment|callback|HMAC\"" \
    --project=klc-ev-charging --limit=50 --format="table(timestamp,textPayload)"
  ```
- Verify callback URL is reachable from gateway (must be public HTTPS)

### Database connection issues

- Check Cloud SQL instance status:
  ```bash
  gcloud sql instances describe klc-postgres --project=klc-ev-charging --format="value(state)"
  ```
- Verify VPC connector is healthy:
  ```bash
  gcloud compute networks vpc-access connectors describe klc-connector \
    --region=asia-southeast1 --project=klc-ev-charging
  ```
- Check connection count (max 100 per instance):
  ```bash
  gcloud sql operations list --instance=klc-postgres --project=klc-ev-charging --limit=10
  ```
- Verify `db-connection-string` secret is correct

### Redis connection issues

- Check Memorystore instance:
  ```bash
  gcloud redis instances describe klc-redis --region=asia-southeast1 --project=klc-ev-charging
  ```
- Verify Redis IP is reachable from VPC connector (expected: `10.239.176.251:6379`)
- BFF Redis connections are lazy -- health check failures are non-fatal
- Check `redis-connection-string` secret matches Memorystore IP

## Rollback Procedure

1. Find the previous working image tag (use commit SHA):
   ```bash
   gcloud artifacts docker images list \
     asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/admin-api \
     --sort-by=~CREATE_TIME --limit=5
   ```

2. Redeploy with previous image:
   ```bash
   # Admin API
   gcloud run deploy klc-admin-api \
     --image=asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/admin-api:<PREVIOUS_SHA> \
     --region=asia-southeast1 --project=klc-ev-charging

   # Driver BFF
   gcloud run deploy klc-driver-bff \
     --image=asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/driver-bff:<PREVIOUS_SHA> \
     --region=asia-southeast1 --project=klc-ev-charging

   # Admin Portal
   gcloud run deploy klc-admin-portal \
     --image=asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/admin-portal:<PREVIOUS_SHA> \
     --region=asia-southeast1 --project=klc-ev-charging
   ```

3. Verify rollback:
   ```bash
   curl -s https://api.ev.odcall.com/health
   curl -s https://bff.ev.odcall.com/health
   ```

## Log Access

```bash
# Admin API logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=klc-admin-api" \
  --project=klc-ev-charging --limit=100 --format="table(timestamp,textPayload)" --freshness=1h

# Driver BFF logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=klc-driver-bff" \
  --project=klc-ev-charging --limit=100 --format="table(timestamp,textPayload)" --freshness=1h

# Admin Portal logs
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=klc-admin-portal" \
  --project=klc-ev-charging --limit=100 --format="table(timestamp,textPayload)" --freshness=1h

# Filter by severity
gcloud logging read "resource.labels.service_name=klc-admin-api AND severity>=ERROR" \
  --project=klc-ev-charging --limit=50 --freshness=1h

# OCPP-specific logs
gcloud logging read "resource.labels.service_name=klc-admin-api AND textPayload=~\"OCPP\"" \
  --project=klc-ev-charging --limit=50 --freshness=1h
```

## Scaling

```bash
# View current config
gcloud run services describe klc-admin-api --region=asia-southeast1 --project=klc-ev-charging \
  --format="value(spec.template.spec.containerConcurrency,spec.template.metadata.annotations)"

# Adjust min/max instances
gcloud run services update klc-admin-api \
  --min-instances=1 --max-instances=5 \
  --region=asia-southeast1 --project=klc-ev-charging

gcloud run services update klc-driver-bff \
  --min-instances=0 --max-instances=10 \
  --region=asia-southeast1 --project=klc-ev-charging

# Adjust memory/CPU
gcloud run services update klc-admin-api \
  --memory=2Gi --cpu=2 \
  --region=asia-southeast1 --project=klc-ev-charging
```

**Note**: Admin API should always have `--min-instances=1` to maintain OCPP WebSocket connections.

## Database

### Backup

```bash
# On-demand backup
gcloud sql backups create --instance=klc-postgres --project=klc-ev-charging

# List backups
gcloud sql backups list --instance=klc-postgres --project=klc-ev-charging

# Automated backups are configured on Cloud SQL instance
```

### Restore

```bash
# Restore from backup (creates downtime)
gcloud sql backups restore <BACKUP_ID> --restore-instance=klc-postgres --project=klc-ev-charging

# Point-in-time recovery (if binary logging enabled)
gcloud sql instances clone klc-postgres klc-postgres-recovery \
  --point-in-time="2026-03-09T00:00:00Z" --project=klc-ev-charging
```

### Migrations

- Migrations run automatically on Admin API startup via `KLC.DbMigrator`
- Manual migration: `dotnet ef database update -p src/backend/src/KLC.EntityFrameworkCore`
- Never run manual SQL DDL in production -- use code-first migrations only
