# Operations Runbook — EV Charging Management System (DEV)

> **Environment**: DEV
> **GCP Project**: `klc-ev-charging`
> **Region**: `asia-southeast1`
> **Naming Prefix**: `evcms-dev`
> **GitHub Repo**: `howard-tech/klc-ev-charging`

---

## Table of Contents

1. [Bootstrap DEV in 30 Minutes](#1-bootstrap-dev-in-30-minutes)
2. [Onboard a New Charger Vendor](#2-onboard-a-new-charger-vendor)
3. [Troubleshooting](#3-troubleshooting)
4. [Maintenance](#4-maintenance)

---

## Resource Inventory

| Resource | Name | Notes |
|----------|------|-------|
| Cloud Run — Admin Portal | `evcms-dev-admin-portal` | Next.js, `ev.odcall.com` |
| Cloud Run — Backend API | `evcms-dev-backend-api` | .NET Admin API, `api.ev.odcall.com` |
| Cloud Run — BFF Socket | `evcms-dev-bff-socket` | Driver BFF, `bff.ev.odcall.com` |
| Cloud Run — OCPP Gateway | `evcms-dev-ocpp-gateway` | OCPP WebSocket, `ocpp.ev.odcall.com` |
| Cloud SQL (PostgreSQL 16) | `evcms-dev-postgres` | DB: `klc_dev`, User: `klc_app` |
| Memorystore (Redis 7) | `evcms-dev-redis` | Private IP, VPC connector required |
| Cloud Armor Policy | `evcms-dev-ocpp-armor` | IP allowlist for OCPP endpoint |
| Static IP (Global) | `evcms-dev-ocpp-ip` | Reserved for OCPP load balancer |
| VPC Connector | `evcms-dev-vpc-connector` | Serverless VPC Access for Cloud SQL/Redis |
| Artifact Registry | `asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend` | Docker images |
| Service Account — Deployer | `evcms-dev-deployer@klc-ev-charging.iam.gserviceaccount.com` | CI/CD deploys |
| Service Account — Runtime | `klc-backend@klc-ev-charging.iam.gserviceaccount.com` | Cloud Run runtime |

---

## 1. Bootstrap DEV in 30 Minutes

Follow this checklist top-to-bottom. Steps marked with a time estimate are the long poles.

### Prerequisites

- [ ] `gcloud` CLI installed and up-to-date (`gcloud components update`)
- [ ] Authenticated: `gcloud auth login`
- [ ] Project set: `gcloud config set project klc-ev-charging`
- [ ] Billing enabled on project `klc-ev-charging`
- [ ] Domain `odcall.com` DNS managed via Cloud DNS or external registrar
- [ ] GitHub CLI (`gh`) installed and authenticated

### Step 1 — Clone the Repository

```bash
git clone git@github.com:howard-tech/klc-ev-charging.git
cd klc-ev-charging
```

### Step 2 — Run the Bootstrap Script

```bash
chmod +x scripts/bootstrap-dev.sh
./scripts/bootstrap-dev.sh
```

This script provisions all GCP resources in a single pass. Expect it to take **~15 minutes** because Cloud SQL instance creation is slow (~10 min). The script creates:

- Cloud SQL instance (`evcms-dev-postgres`) with database `klc_dev` and user `klc_app`
- Memorystore Redis instance (`evcms-dev-redis`)
- Serverless VPC Access connector (`evcms-dev-vpc-connector`)
- Artifact Registry repository
- Secret Manager secrets (prefixed `evcms-dev-`)
- Cloud Run services (all four)
- Cloud Armor security policy (`evcms-dev-ocpp-armor`)
- Global static IP (`evcms-dev-ocpp-ip`)
- HTTPS load balancer for OCPP with managed SSL certificate
- Workload Identity Federation pool and provider for GitHub Actions
- Service accounts with minimal IAM bindings
- Cloud Run domain mappings for `*.ev.odcall.com`

### Step 3 — Note the Bootstrap Output

The script prints critical values at the end. Save them:

```
=== Bootstrap Complete ===
WIF Provider:          projects/493799105026/locations/global/workloadIdentityPools/...
Static OCPP IP:        x.x.x.x
Admin Portal URL:      https://ev.odcall.com
Backend API URL:       https://api.ev.odcall.com
Driver BFF URL:        https://bff.ev.odcall.com
OCPP Gateway URL:      wss://ocpp.ev.odcall.com
```

### Step 4 — Configure DNS Records

If DNS is managed externally (not Cloud DNS), add these records at your registrar:

| Type  | Name                   | Value                                              |
|-------|------------------------|----------------------------------------------------|
| CNAME | `ev.odcall.com`        | `ghs.googlehosted.com.`                            |
| CNAME | `api.ev.odcall.com`    | `ghs.googlehosted.com.`                            |
| CNAME | `bff.ev.odcall.com`    | `ghs.googlehosted.com.`                            |
| A     | `ocpp.ev.odcall.com`   | `<STATIC_IP from evcms-dev-ocpp-ip>`               |

Cloud Run domain mappings use `ghs.googlehosted.com`. The OCPP gateway uses a global HTTPS load balancer with a static IP.

### Step 5 — Configure GitHub Repository Secrets

```bash
# Set secrets via gh CLI (preferred)
gh secret set GCP_PROJECT_ID --repo howard-tech/klc-ev-charging --body "klc-ev-charging"
gh secret set GCP_REGION --repo howard-tech/klc-ev-charging --body "asia-southeast1"
gh secret set GCP_WORKLOAD_IDENTITY_PROVIDER --repo howard-tech/klc-ev-charging --body "<WIF_PROVIDER_FROM_BOOTSTRAP_OUTPUT>"
gh secret set GCP_SA_EMAIL --repo howard-tech/klc-ev-charging --body "evcms-dev-deployer@klc-ev-charging.iam.gserviceaccount.com"
```

Or set them manually in GitHub: **Settings > Secrets and variables > Actions > New repository secret**

| Secret Name | Value |
|-------------|-------|
| `GCP_PROJECT_ID` | `klc-ev-charging` |
| `GCP_REGION` | `asia-southeast1` |
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | *(from bootstrap output)* |
| `GCP_SA_EMAIL` | `evcms-dev-deployer@klc-ev-charging.iam.gserviceaccount.com` |

### Step 6 — Trigger First Deploy

```bash
git checkout -b develop
git push -u origin develop
```

This triggers the CI/CD workflow which builds Docker images, pushes to Artifact Registry, and deploys all four Cloud Run services.

### Step 7 — Wait for TLS Provisioning (~5 min)

Cloud Run managed certificates and the OCPP load balancer managed certificate need time to provision. Monitor status:

```bash
# Cloud Run domain mapping cert status
gcloud run domain-mappings describe --domain=ev.odcall.com \
  --region=asia-southeast1 \
  --format="value(status.conditions)"

# OCPP LB managed cert status
gcloud compute ssl-certificates describe evcms-dev-ocpp-cert \
  --format="value(managed.status,managed.domainStatus)"
```

Repeat every 60 seconds until status shows `ACTIVE` / `PROVISIONING_COMPLETE`.

### Step 8 — Verify All Endpoints

```bash
# Admin Portal — should return HTML
curl -s https://ev.odcall.com | head -5

# Backend API — should return {"status":"Healthy",...}
curl -s https://api.ev.odcall.com/health

# Driver BFF — should return {"status":"Healthy",...}
curl -s https://bff.ev.odcall.com/health

# OCPP Gateway — verify TLS handshake succeeds
openssl s_client -connect ocpp.ev.odcall.com:443 \
  -servername ocpp.ev.odcall.com < /dev/null 2>/dev/null | head -10
```

All four must return valid responses before proceeding.

### Step 9 — Seed Demo Data

```bash
# Connect to Cloud SQL via gcloud proxy
gcloud sql connect evcms-dev-postgres --user=klc_app --database=klc_dev

# At the psql prompt, run the seed script
\i scripts/seed-demo-data.sql

# Verify seed data
SELECT count(*) FROM "ChargingStations";
SELECT count(*) FROM "AbpUsers";
\q
```

Demo credentials after seeding:
- Admin: `admin` / `Admin@123`
- Operator: `operator` / `Admin@123`
- Viewer: `viewer` / `Admin@123`

### Step 10 — Test OCPP with Simulator

```bash
# Install wscat if not already available
npm install -g wscat

# Connect to OCPP endpoint with the ocpp1.6 subprotocol
npx wscat -c wss://ocpp.ev.odcall.com/ocpp/SIMULATOR-001 -s ocpp1.6

# Once connected, send a BootNotification
> [2,"boot-001","BootNotification",{"chargePointVendor":"Simulator","chargePointModel":"Virtual","chargePointSerialNumber":"SIM-001"}]

# Expected response:
# [3,"boot-001",{"status":"Accepted","currentTime":"2026-03-08T12:00:00.000Z","interval":300}]

# Send a Heartbeat
> [2,"hb-001","Heartbeat",{}]

# Expected response:
# [3,"hb-001",{"currentTime":"2026-03-08T12:00:05.000Z"}]
```

If the BootNotification returns `Accepted`, the entire stack is working end-to-end.

---

## 2. Onboard a New Charger Vendor

Use this procedure every time a new hardware vendor needs to connect their charge points to the OCPP gateway.

### 2.1 Collect Information

Gather the following from the vendor before starting:

| Item | Example | Notes |
|------|---------|-------|
| Vendor name | VendorX Corp | Official legal name |
| Technical contact | engineer@vendorx.com | For troubleshooting |
| Outbound IP CIDRs | `203.0.113.0/24`, `198.51.100.0/24` | Their NAT/egress IPs — they MUST provide these |
| Charger model(s) | VX-50DC | For documentation |
| OCPP version | 1.6J | Must be 1.6J; 2.0/2.1 support is limited |
| Expected charge points | 50 | For capacity planning |
| Serial number prefix | `VX-` | For charge point ID convention |

### 2.2 Update IP Allowlist

The OCPP gateway is protected by Cloud Armor. Only explicitly allowed IPs can connect.

```bash
# Edit the vendor allowlist file
vi infra/vendor-allowlist.txt
```

Add the vendor's CIDRs with a comment header:

```
# --- VendorX Corp (added 2026-03-15, contact: engineer@vendorx.com) ---
# Approved by: [your name]
203.0.113.0/24
198.51.100.0/24
```

Commit the change to maintain an audit trail:

```bash
git add infra/vendor-allowlist.txt
git commit -m "chore: add VendorX CIDRs to OCPP allowlist"
git push
```

### 2.3 Apply Cloud Armor Policy Update

```bash
# Read CIDRs from file (strips comments and blank lines)
CIDRS=$(grep -v '^#' infra/vendor-allowlist.txt | grep -v '^$' | paste -sd, -)

# Update the allow rule (priority 1000 is the vendor allowlist rule)
gcloud compute security-policies rules update 1000 \
  --security-policy=evcms-dev-ocpp-armor \
  --src-ip-ranges="${CIDRS}" \
  --project=klc-ev-charging

# Verify the updated policy
gcloud compute security-policies describe evcms-dev-ocpp-armor \
  --project=klc-ev-charging \
  --format="yaml(rules)"
```

Cloud Armor changes propagate globally within **~60 seconds**.

### 2.4 Provide Vendor with Connection Details

Send the vendor the following connection information. Provide OCPP passwords separately via a secure channel.

```
=== OCPP Connection Details ===

WebSocket Endpoint:     wss://ocpp.ev.odcall.com/ocpp/{chargePointId}
OCPP Protocol:          OCPP 1.6J
WebSocket Subprotocol:  ocpp1.6
TLS:                    Required (TLS 1.2 minimum)
Authentication:         HTTP Basic Auth (credentials provided separately per charge point)

Inbound IP (for your firewall allowlist):
  <STATIC_IP from evcms-dev-ocpp-ip>
  Run: gcloud compute addresses describe evcms-dev-ocpp-ip --global --format="value(address)"

Charge Point ID Format: VENDOR-SERIALNUMBER
  Example: JUHANG-CP001, VX-SN0042

Heartbeat Interval:     300 seconds (server-configured)
Connection Timeout:     3600 seconds (1 hour idle timeout)

=== Required WebSocket Headers ===
Sec-WebSocket-Protocol: ocpp1.6
Authorization: Basic <base64(chargePointId:password)>   (if Basic Auth is enabled)
```

### 2.5 Test OCPP Handshake

Before going live, verify the vendor can connect:

```bash
# From a machine with an IP in the vendor's allowed range, test the connection
npx wscat -c wss://ocpp.ev.odcall.com/ocpp/TEST-VENDORX-001 -s ocpp1.6

# Send BootNotification
> [2,"test-001","BootNotification",{"chargePointVendor":"VendorX","chargePointModel":"VX-50DC","chargePointSerialNumber":"SN001"}]

# Expected: [3,"test-001",{"status":"Accepted","currentTime":"...","interval":300}]

# Send Heartbeat
> [2,"test-002","Heartbeat",{}]

# Expected: [3,"test-002",{"currentTime":"..."}]

# Send StatusNotification
> [2,"test-003","StatusNotification",{"connectorId":1,"errorCode":"NoError","status":"Available"}]

# Expected: [3,"test-003",{}]
```

If the vendor gets a `403 Forbidden`, their source IP is not in the Cloud Armor allowlist. See [Section 3.3](#33-cloud-armor-blocking-legitimate-traffic).

### 2.6 Register Charge Points in System

1. Log into the admin portal at **https://ev.odcall.com**
2. Navigate to **Stations > New Station**
3. Fill in station details:
   - **Station Code**: Use the agreed charge point ID (e.g., `VX-SN0042`)
   - **Vendor**: Select or create the vendor profile
   - **Connectors**: Add connector(s) with correct type (CCS2, CHAdeMO, Type2, etc.)
   - **OCPP Password**: Set if Basic Auth is enabled for this charge point
   - **Location**: Set GPS coordinates and address
4. Save the station
5. The charge point can now connect and will be recognized by the system

For bulk registration, use the seed SQL approach:

```bash
gcloud sql connect evcms-dev-postgres --user=klc_app --database=klc_dev

-- Example: Register a batch of VendorX charge points
INSERT INTO "ChargingStations" ("Id", "StationCode", "Name", ...)
VALUES
  (gen_random_uuid(), 'VX-SN0001', 'VendorX Station 1', ...),
  (gen_random_uuid(), 'VX-SN0002', 'VendorX Station 2', ...);
```

---

## 3. Troubleshooting

### 3.1 DNS Propagation / Certificate Issuance

**Symptom**: HTTPS not working, browser shows `ERR_SSL_PROTOCOL_ERROR` or `ERR_CONNECTION_REFUSED`.

**Diagnosis**:

```bash
# Check DNS resolution for all domains
dig ev.odcall.com +short
dig api.ev.odcall.com +short
dig bff.ev.odcall.com +short
dig ocpp.ev.odcall.com +short

# Check Cloud Run domain mapping status (for portal, API, BFF)
gcloud run domain-mappings describe --domain=ev.odcall.com \
  --region=asia-southeast1 \
  --format="value(status)"

gcloud run domain-mappings describe --domain=api.ev.odcall.com \
  --region=asia-southeast1 \
  --format="value(status)"

gcloud run domain-mappings describe --domain=bff.ev.odcall.com \
  --region=asia-southeast1 \
  --format="value(status)"

# Check managed cert status for OCPP LB
gcloud compute ssl-certificates describe evcms-dev-ocpp-cert \
  --format="value(managed.status,managed.domainStatus)"
```

**Fix**:

- DNS propagation takes **5-30 minutes**. Verify records are correct with `dig`.
- Cloud Run managed certificates provision automatically once DNS resolves to `ghs.googlehosted.com`. Takes **5-15 minutes** typically, up to 24 hours in rare cases.
- OCPP LB managed certificate requires `ocpp.ev.odcall.com` to resolve to the static IP (`evcms-dev-ocpp-ip`) BEFORE the cert can provision.
- If stuck in `PROVISIONING_FAILED_PERMANENTLY`, delete and recreate the certificate:
  ```bash
  gcloud compute ssl-certificates delete evcms-dev-ocpp-cert --quiet
  gcloud compute ssl-certificates create evcms-dev-ocpp-cert \
    --domains=ocpp.ev.odcall.com \
    --global
  # Re-attach to target HTTPS proxy
  gcloud compute target-https-proxies update evcms-dev-ocpp-https-proxy \
    --ssl-certificates=evcms-dev-ocpp-cert \
    --global
  ```

### 3.2 WebSocket Upgrade Issues

**Symptom**: OCPP charger connects but immediately disconnects, or HTTP 101 upgrade fails.

**Diagnosis**:

```bash
# Test WebSocket connection directly
npx wscat -c wss://ocpp.ev.odcall.com/ocpp/TEST-001 -s ocpp1.6

# Check OCPP Gateway Cloud Run logs
gcloud logging read \
  'resource.type="cloud_run_revision" AND resource.labels.service_name="evcms-dev-ocpp-gateway"' \
  --limit=20 \
  --format="table(timestamp,jsonPayload.message)" \
  --project=klc-ev-charging

# Check Load Balancer logs for OCPP traffic
gcloud logging read \
  'resource.type="http_load_balancer" AND httpRequest.requestUrl:"ocpp"' \
  --limit=20 \
  --project=klc-ev-charging

# Check Cloud Run service timeout setting
gcloud run services describe evcms-dev-ocpp-gateway \
  --region=asia-southeast1 \
  --format="value(spec.template.spec.timeoutSeconds)"
```

**Common Causes & Fixes**:

| Cause | Fix |
|-------|-----|
| Missing `Sec-WebSocket-Protocol: ocpp1.6` header | Charger firmware must send the subprotocol header. Contact vendor. |
| Cloud Run timeout too low | Must be 3600s: `gcloud run services update evcms-dev-ocpp-gateway --timeout=3600 --region=asia-southeast1` |
| Cloud Armor blocking the IP | Check Section 3.3 |
| Load balancer backend timeout mismatch | Verify backend service timeout: `gcloud compute backend-services describe evcms-dev-ocpp-backend --global --format="value(timeoutSec)"` — must be `3600` |
| Session affinity not configured | WebSocket requires session affinity: `gcloud compute backend-services update evcms-dev-ocpp-backend --global --session-affinity=GENERATED_COOKIE` |

### 3.3 Cloud Armor Blocking Legitimate Traffic

**Symptom**: Vendor charger gets `403 Forbidden` when connecting to OCPP endpoint.

**Diagnosis**:

```bash
# Check Cloud Armor logs for denied requests
gcloud logging read \
  'resource.type="http_load_balancer" AND jsonPayload.enforcedSecurityPolicy.outcome="DENY"' \
  --limit=20 \
  --format="table(timestamp,httpRequest.remoteIp,jsonPayload.enforcedSecurityPolicy.name)" \
  --project=klc-ev-charging

# View the current Cloud Armor policy rules
gcloud compute security-policies describe evcms-dev-ocpp-armor \
  --project=klc-ev-charging \
  --format="yaml(rules)"

# Ask the vendor to confirm their actual outbound IP
# Have them run from their network:
# curl -s https://api.ipify.org
```

**Fix**:

1. Identify the blocked IP from the logs
2. Confirm with the vendor it is their legitimate egress IP
3. Add to `infra/vendor-allowlist.txt`
4. Re-apply Cloud Armor rule (see [Section 2.3](#23-apply-cloud-armor-policy-update))

```bash
# Quick one-liner to add a single IP without editing the file
gcloud compute security-policies rules update 1000 \
  --security-policy=evcms-dev-ocpp-armor \
  --src-ip-ranges="$(gcloud compute security-policies rules describe 1000 \
    --security-policy=evcms-dev-ocpp-armor \
    --format='value(match.config.srcIpRanges)'),NEW.IP.ADDR/32" \
  --project=klc-ev-charging
```

> **Warning**: Always update `infra/vendor-allowlist.txt` as the source of truth after any ad-hoc Cloud Armor change. The next full re-apply from the file will overwrite manual additions.

### 3.4 Cloud Run Revision Rollback

**Symptom**: New deployment breaks functionality, 5xx errors, or unexpected behavior.

```bash
# List the 5 most recent revisions for a service
gcloud run revisions list \
  --service=evcms-dev-backend-api \
  --region=asia-southeast1 \
  --limit=5 \
  --format="table(REVISION,ACTIVE,DEPLOYED_AT)"

# Rollback to a specific previous revision
gcloud run services update-traffic evcms-dev-backend-api \
  --to-revisions=evcms-dev-backend-api-XXXXX=100 \
  --region=asia-southeast1
```

To rollback **all services** to their previous revisions at once:

```bash
for svc in backend-api bff-socket ocpp-gateway admin-portal; do
  echo "Rolling back evcms-dev-${svc}..."
  PREV=$(gcloud run revisions list \
    --service=evcms-dev-${svc} \
    --region=asia-southeast1 \
    --limit=2 \
    --format="value(REVISION)" | tail -1)

  if [ -n "${PREV}" ]; then
    gcloud run services update-traffic evcms-dev-${svc} \
      --to-revisions=${PREV}=100 \
      --region=asia-southeast1
    echo "  -> Rolled back to ${PREV}"
  else
    echo "  -> No previous revision found, skipping"
  fi
done
```

After rollback, investigate the failing revision's logs:

```bash
# Get the failing revision name
FAILING_REV=$(gcloud run revisions list \
  --service=evcms-dev-backend-api \
  --region=asia-southeast1 \
  --limit=1 \
  --format="value(REVISION)")

# Read its logs
gcloud logging read \
  "resource.type=\"cloud_run_revision\" AND resource.labels.revision_name=\"${FAILING_REV}\"" \
  --limit=50 \
  --format="table(timestamp,severity,jsonPayload.message)" \
  --project=klc-ev-charging
```

### 3.5 Database Connection Issues

**Symptom**: Services return 500 errors, logs show `could not connect to server`, `connection refused`, or `timeout expired`.

```bash
# 1. Check Cloud SQL instance is running
gcloud sql instances describe evcms-dev-postgres \
  --format="table(state,ipAddresses,settings.ipConfiguration.privateNetwork)" \
  --project=klc-ev-charging

# 2. Check VPC connector status (required for Cloud Run -> Cloud SQL private IP)
gcloud compute networks vpc-access connectors describe evcms-dev-vpc-connector \
  --region=asia-southeast1 \
  --format="table(state,network,ipCidrRange,minThroughput,maxThroughput)"

# 3. Test connection interactively from Cloud Shell
gcloud sql connect evcms-dev-postgres --user=klc_app --database=klc_dev

# 4. Verify the connection string secret is correct
gcloud secrets versions access latest --secret=evcms-dev-db-connection-string

# 5. Verify Cloud Run service has VPC connector attached
gcloud run services describe evcms-dev-backend-api \
  --region=asia-southeast1 \
  --format="value(spec.template.metadata.annotations['run.googleapis.com/vpc-access-connector'])"
```

**Common Causes & Fixes**:

| Cause | Fix |
|-------|-----|
| Cloud SQL instance stopped | `gcloud sql instances patch evcms-dev-postgres --activation-policy=ALWAYS` |
| VPC connector in ERROR state | Delete and recreate: see Maintenance section |
| Wrong password in secret | Update secret: `echo -n "correct-password" \| gcloud secrets versions add evcms-dev-db-connection-string --data-file=-` then redeploy |
| Max connections exceeded | Check with `SELECT count(*) FROM pg_stat_activity;` — increase if needed or fix connection leaks |
| Cloud SQL storage full | `gcloud sql instances patch evcms-dev-postgres --storage-auto-increase` |

### 3.6 Redis Connection Issues

**Symptom**: BFF returns stale data or 500 errors, logs show `Redis connection failed` or `SocketException`.

```bash
# 1. Check Memorystore Redis instance status
gcloud redis instances describe evcms-dev-redis \
  --region=asia-southeast1 \
  --format="table(state,host,port,memorySizeGb,redisVersion)"

# 2. Verify VPC connector is attached to the BFF service
gcloud run services describe evcms-dev-bff-socket \
  --region=asia-southeast1 \
  --format="value(spec.template.metadata.annotations['run.googleapis.com/vpc-access-connector'])"

# 3. Verify Redis connection string secret
gcloud secrets versions access latest --secret=evcms-dev-redis-connection-string

# 4. Check Redis memory usage (connect via Compute Engine VM in same VPC)
# From a VM in the same network:
redis-cli -h <REDIS_HOST> -p 6379 INFO memory
```

**Key points**:

- Memorystore Redis uses **private IP only** — services MUST use the VPC connector to reach it
- If the VPC connector is missing or broken, Redis connections will time out
- The BFF is designed to be cache-first with lazy Redis connections — if Redis is down, the BFF should still serve requests from the database (with degraded performance), not crash

### 3.7 Common gcloud Commands Reference

```bash
# ============================================================
# LOGS
# ============================================================

# View logs for any Cloud Run service (last 30 min, adjust timestamp)
gcloud logging read \
  'resource.type="cloud_run_revision" AND resource.labels.service_name="evcms-dev-backend-api" AND timestamp>="2026-03-08T00:00:00Z"' \
  --limit=50 \
  --format="table(timestamp,severity,jsonPayload.message)" \
  --project=klc-ev-charging

# View error logs only
gcloud logging read \
  'resource.type="cloud_run_revision" AND resource.labels.service_name="evcms-dev-backend-api" AND severity>=ERROR' \
  --limit=20 \
  --project=klc-ev-charging

# View OCPP gateway logs (WebSocket connections)
gcloud logging read \
  'resource.type="cloud_run_revision" AND resource.labels.service_name="evcms-dev-ocpp-gateway"' \
  --limit=30 \
  --format="table(timestamp,jsonPayload.message)" \
  --project=klc-ev-charging

# Tail logs in real time (requires gcloud beta)
gcloud beta run services logs tail evcms-dev-backend-api --region=asia-southeast1

# ============================================================
# SERVICE INSPECTION
# ============================================================

# View Cloud Run service details
gcloud run services describe evcms-dev-backend-api \
  --region=asia-southeast1 \
  --project=klc-ev-charging

# List all Cloud Run services in the project
gcloud run services list --region=asia-southeast1 --project=klc-ev-charging

# Check current revision and traffic split
gcloud run services describe evcms-dev-backend-api \
  --region=asia-southeast1 \
  --format="value(status.traffic)"

# ============================================================
# SECRETS
# ============================================================

# List all secrets
gcloud secrets list --filter="name:evcms-dev" --project=klc-ev-charging

# Read a secret value
gcloud secrets versions access latest --secret=evcms-dev-db-connection-string

# Update a secret (creates a new version)
echo -n "new-value" | gcloud secrets versions add evcms-dev-jwt-secret-key --data-file=-

# ============================================================
# FORCE REDEPLOY (picks up new secret versions)
# ============================================================

gcloud run services update evcms-dev-backend-api --region=asia-southeast1

# ============================================================
# STATIC IP
# ============================================================

# Check OCPP static IP address
gcloud compute addresses describe evcms-dev-ocpp-ip \
  --global \
  --format="value(address)"

# ============================================================
# CLOUD SQL
# ============================================================

# Connect interactively
gcloud sql connect evcms-dev-postgres --user=klc_app --database=klc_dev

# Check instance status
gcloud sql instances describe evcms-dev-postgres \
  --format="table(state,settings.tier,settings.dataDiskSizeGb)" \
  --project=klc-ev-charging

# ============================================================
# CLOUD ARMOR
# ============================================================

# View all rules
gcloud compute security-policies describe evcms-dev-ocpp-armor \
  --project=klc-ev-charging \
  --format="yaml(rules)"

# Check recent blocked requests
gcloud logging read \
  'resource.type="http_load_balancer" AND jsonPayload.enforcedSecurityPolicy.outcome="DENY"' \
  --limit=10 \
  --project=klc-ev-charging
```

---

## 4. Maintenance

### 4.1 Rotating Secrets

Secrets should be rotated periodically or immediately if a compromise is suspected.

#### Rotate JWT Secret

```bash
# Generate a new 64-character random JWT secret
NEW_JWT=$(openssl rand -base64 48 | tr -dc 'a-zA-Z0-9' | head -c 64)

# Store as a new secret version
echo -n "${NEW_JWT}" | gcloud secrets versions add evcms-dev-jwt-secret-key --data-file=-

# Force redeploy all services to pick up the new secret
for svc in backend-api bff-socket ocpp-gateway; do
  gcloud run services update evcms-dev-${svc} --region=asia-southeast1
  echo "Redeployed evcms-dev-${svc}"
done
```

> **Note**: Rotating the JWT secret invalidates all existing JWT tokens. Mobile app users will need to re-authenticate. Plan rotations during low-traffic windows.

#### Rotate Database Password

```bash
# Generate new password
NEW_DB_PASS=$(openssl rand -base64 32 | tr -dc 'a-zA-Z0-9' | head -c 32)

# Update Cloud SQL user password
gcloud sql users set-password klc_app \
  --instance=evcms-dev-postgres \
  --password="${NEW_DB_PASS}"

# Update the connection string secret
# Format: Host=<private_ip>;Port=5432;Database=klc_dev;Username=klc_app;Password=<new_password>;
PRIVATE_IP=$(gcloud sql instances describe evcms-dev-postgres \
  --format="value(ipAddresses[0].ipAddress)")

echo -n "Host=${PRIVATE_IP};Port=5432;Database=klc_dev;Username=klc_app;Password=${NEW_DB_PASS};" | \
  gcloud secrets versions add evcms-dev-db-connection-string --data-file=-

# Redeploy all services
for svc in backend-api bff-socket ocpp-gateway; do
  gcloud run services update evcms-dev-${svc} --region=asia-southeast1
done
```

#### Rotate OpenIddict Client Secret

```bash
NEW_OIDC_SECRET=$(openssl rand -base64 32 | tr -dc 'a-zA-Z0-9' | head -c 48)
echo -n "${NEW_OIDC_SECRET}" | gcloud secrets versions add evcms-dev-oidc-client-secret --data-file=-

# Update the secret in the database (OpenIddict stores client secrets hashed)
# This requires running a migration or admin command — coordinate with the backend team

# Redeploy
for svc in backend-api admin-portal; do
  gcloud run services update evcms-dev-${svc} --region=asia-southeast1
done
```

### 4.2 Database Migrations

#### Option A: Connect Directly and Run SQL

For simple or emergency migrations:

```bash
gcloud sql connect evcms-dev-postgres --user=klc_app --database=klc_dev < migration.sql
```

#### Option B: EF Core Migrations via Cloud Run Job (Recommended)

This is the standard approach for schema changes driven by code-first migrations.

```bash
# Build the migration runner image
docker build -t asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/db-migrator:latest \
  -f src/backend/src/KLC.DbMigrator/Dockerfile \
  src/backend/

# Push to Artifact Registry
docker push asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/db-migrator:latest

# Create a Cloud Run Job (first time only)
gcloud run jobs create evcms-dev-db-migrate \
  --image=asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/db-migrator:latest \
  --set-secrets=ConnectionStrings__Default=evcms-dev-db-connection-string:latest \
  --vpc-connector=evcms-dev-vpc-connector \
  --region=asia-southeast1 \
  --project=klc-ev-charging

# Execute the migration job
gcloud run jobs execute evcms-dev-db-migrate \
  --region=asia-southeast1 \
  --project=klc-ev-charging

# Monitor execution
gcloud run jobs executions list \
  --job=evcms-dev-db-migrate \
  --region=asia-southeast1 \
  --limit=3

# Check logs if migration fails
gcloud logging read \
  'resource.type="cloud_run_job" AND resource.labels.job_name="evcms-dev-db-migrate"' \
  --limit=30 \
  --project=klc-ev-charging
```

#### Pre-Migration Checklist

- [ ] Test migration locally against a copy of the DEV database
- [ ] Ensure migration is backward-compatible (no column drops without a deprecation period)
- [ ] Take a database backup before running:
  ```bash
  gcloud sql backups create --instance=evcms-dev-postgres --project=klc-ev-charging
  ```
- [ ] Run during low-traffic window if migration locks tables

### 4.3 Scaling for Load Testing

```bash
# Temporarily increase max instances for load testing
gcloud run services update evcms-dev-backend-api \
  --max-instances=10 \
  --region=asia-southeast1

gcloud run services update evcms-dev-bff-socket \
  --max-instances=10 \
  --region=asia-southeast1

gcloud run services update evcms-dev-ocpp-gateway \
  --max-instances=10 \
  --region=asia-southeast1

# Optionally increase Cloud SQL tier for the test
gcloud sql instances patch evcms-dev-postgres \
  --tier=db-custom-4-16384 \
  --project=klc-ev-charging
```

After load testing, reset to DEV defaults:

```bash
# Reset Cloud Run scaling
gcloud run services update evcms-dev-backend-api \
  --max-instances=3 \
  --region=asia-southeast1

gcloud run services update evcms-dev-bff-socket \
  --max-instances=3 \
  --region=asia-southeast1

gcloud run services update evcms-dev-ocpp-gateway \
  --max-instances=3 \
  --region=asia-southeast1

# Reset Cloud SQL tier
gcloud sql instances patch evcms-dev-postgres \
  --tier=db-custom-1-3840 \
  --project=klc-ev-charging
```

### 4.4 Database Backups

```bash
# Create an on-demand backup
gcloud sql backups create --instance=evcms-dev-postgres --project=klc-ev-charging

# List existing backups
gcloud sql backups list --instance=evcms-dev-postgres --project=klc-ev-charging

# Restore from a specific backup (DESTRUCTIVE — replaces current data)
gcloud sql backups restore BACKUP_ID \
  --restore-instance=evcms-dev-postgres \
  --project=klc-ev-charging
```

Automated daily backups are configured on the Cloud SQL instance. Retention: 7 days.

### 4.5 VPC Connector Recreation

If the VPC connector enters an `ERROR` state, delete and recreate it:

```bash
# Delete the broken connector
gcloud compute networks vpc-access connectors delete evcms-dev-vpc-connector \
  --region=asia-southeast1 \
  --quiet

# Recreate it
gcloud compute networks vpc-access connectors create evcms-dev-vpc-connector \
  --region=asia-southeast1 \
  --network=default \
  --range=10.8.0.0/28 \
  --min-instances=2 \
  --max-instances=3 \
  --project=klc-ev-charging

# Redeploy all services (they reference the connector by name)
for svc in backend-api bff-socket ocpp-gateway; do
  gcloud run services update evcms-dev-${svc} \
    --vpc-connector=evcms-dev-vpc-connector \
    --region=asia-southeast1
done
```

### 4.6 Cleaning Up DEV Environment

To tear down the entire DEV environment (e.g., to rebuild from scratch):

```bash
# WARNING: This destroys all DEV data. Make sure you have backups.

# Delete Cloud Run services
for svc in admin-portal backend-api bff-socket ocpp-gateway; do
  gcloud run services delete evcms-dev-${svc} \
    --region=asia-southeast1 \
    --quiet
done

# Delete Cloud Run job
gcloud run jobs delete evcms-dev-db-migrate \
  --region=asia-southeast1 \
  --quiet

# Delete domain mappings
for domain in ev.odcall.com api.ev.odcall.com bff.ev.odcall.com; do
  gcloud run domain-mappings delete \
    --domain=${domain} \
    --region=asia-southeast1 \
    --quiet
done

# Delete load balancer components (OCPP)
gcloud compute forwarding-rules delete evcms-dev-ocpp-https-rule --global --quiet
gcloud compute target-https-proxies delete evcms-dev-ocpp-https-proxy --global --quiet
gcloud compute url-maps delete evcms-dev-ocpp-url-map --global --quiet
gcloud compute backend-services delete evcms-dev-ocpp-backend --global --quiet
gcloud compute network-endpoint-groups delete evcms-dev-ocpp-neg --region=asia-southeast1 --quiet
gcloud compute ssl-certificates delete evcms-dev-ocpp-cert --global --quiet
gcloud compute security-policies delete evcms-dev-ocpp-armor --quiet
gcloud compute addresses delete evcms-dev-ocpp-ip --global --quiet

# Delete Cloud SQL (takes a few minutes)
gcloud sql instances delete evcms-dev-postgres --quiet

# Delete Memorystore Redis
gcloud redis instances delete evcms-dev-redis --region=asia-southeast1 --quiet

# Delete VPC connector
gcloud compute networks vpc-access connectors delete evcms-dev-vpc-connector \
  --region=asia-southeast1 --quiet

# Delete secrets
for secret in $(gcloud secrets list --filter="name:evcms-dev" --format="value(name)"); do
  gcloud secrets delete ${secret} --quiet
done

# Delete Artifact Registry images (keep the repo)
gcloud artifacts docker images delete \
  asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend \
  --delete-tags --quiet

echo "DEV environment teardown complete."
```

---

## Appendix A: Environment Variables & Secrets

| Secret Name | Used By | Description |
|-------------|---------|-------------|
| `evcms-dev-db-connection-string` | backend-api, bff-socket, ocpp-gateway | PostgreSQL connection string |
| `evcms-dev-redis-connection-string` | backend-api, bff-socket | Redis connection string |
| `evcms-dev-jwt-secret-key` | backend-api, bff-socket | JWT signing key |
| `evcms-dev-string-encryption-passphrase` | backend-api | ABP string encryption |
| `evcms-dev-oidc-client-secret` | admin-portal | OpenIddict client secret |
| `evcms-dev-admin-api-url` | admin-portal | Backend API base URL |

## Appendix B: Port & URL Reference

| Service | Local Port | Production URL |
|---------|-----------|----------------|
| Admin Portal | 3001 | https://ev.odcall.com |
| Backend API | 44305 | https://api.ev.odcall.com |
| Driver BFF | 5001 | https://bff.ev.odcall.com |
| OCPP Gateway | 44305 (shared) | wss://ocpp.ev.odcall.com |
| PostgreSQL | 5433 | Cloud SQL private IP |
| Redis | 6379 | Memorystore private IP |

## Appendix C: IAM Roles Summary

| Service Account | Roles | Purpose |
|----------------|-------|---------|
| `evcms-dev-deployer@klc-ev-charging.iam.gserviceaccount.com` | `roles/run.admin`, `roles/artifactregistry.writer`, `roles/iam.serviceAccountUser`, `roles/secretmanager.secretAccessor` | CI/CD deployments via GitHub Actions + WIF |
| `klc-backend@klc-ev-charging.iam.gserviceaccount.com` | `roles/cloudsql.client`, `roles/secretmanager.secretAccessor`, `roles/storage.objectAdmin`, `roles/firebase.sdkAdminServiceAgent` | Cloud Run service runtime identity |
