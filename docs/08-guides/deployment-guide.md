# Deployment Guide

> Status: PUBLISHED | Last Updated: 2026-03-09

Production deployment guide for EV Charging CSMS on Google Cloud Platform (GCP) using Cloud Run, Cloud SQL, and Memorystore Redis.

## 1. Overview

### Architecture

```
                        Internet
                           |
              ┌────────────┼────────────┐
              |            |            |
        ev.odcall.com  api.ev.odcall.com  bff.ev.odcall.com
              |            |            |
              v            v            v
        ┌───────────┐ ┌───────────┐ ┌───────────┐
        │  Admin     │ │  Admin    │ │  Driver   │
        │  Portal    │ │  API      │ │  BFF      │
        │ (Next.js)  │ │ (.NET 10) │ │ (.NET 10) │
        │ port 3000  │ │ port 8080 │ │ port 8080 │
        └───────────┘ └─────┬─────┘ └─────┬─────┘
         Cloud Run      Cloud Run      Cloud Run
                             |            |
                      ┌──────┴────────────┘
                      |        VPC Connector
                      |        (klc-connector)
                ┌─────┴─────┐       ┌──────────────┐
                │ Cloud SQL │       │  Memorystore  │
                │ PostgreSQL│       │  Redis 7      │
                │ (PostGIS) │       │              │
                └───────────┘       └──────────────┘
               klc-postgres          klc-redis
             34.177.104.51        10.239.176.251:6379
```

### Services

| Service | Cloud Run Name | Domain | Port | Description |
|---------|---------------|--------|------|-------------|
| Admin API | `klc-admin-api` | `api.ev.odcall.com` | 8080 | ABP Framework API + OCPP WebSocket + SignalR |
| Driver BFF | `klc-driver-bff` | `bff.ev.odcall.com` | 8080 | Minimal API for mobile app |
| Admin Portal | `klc-admin-portal` | `ev.odcall.com` | 3000 | Next.js frontend |

### Key Configuration

| Property | Value |
|----------|-------|
| GCP Project | `klc-ev-charging` (493799105026) |
| Region | `asia-southeast1` |
| Artifact Registry | `asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend` |
| Cloud SQL Instance | `klc-ev-charging:asia-southeast1:klc-postgres` |
| Database | `KLC` (user: `klc_app`, PostGIS enabled) |
| Redis | `klc-redis` (`10.239.176.251:6379`) |
| VPC Connector | `klc-connector` |
| Deploy SA | `github-actions-deploy@klc-ev-charging.iam.gserviceaccount.com` |
| Backend SA | `klc-backend@klc-ev-charging.iam.gserviceaccount.com` |

## 2. Prerequisites

### Required Tools

- [Google Cloud SDK (gcloud)](https://cloud.google.com/sdk/docs/install) v450+
- Docker v24+
- Git
- Access to the `klc-ev-charging` GCP project with at minimum `roles/run.admin` and `roles/artifactregistry.writer`

### Initial Setup

```bash
# Authenticate to GCP
gcloud auth login
gcloud config set project klc-ev-charging
gcloud config set run/region asia-southeast1

# Configure Docker for Artifact Registry
gcloud auth configure-docker asia-southeast1-docker.pkg.dev --quiet

# Verify access
gcloud run services list --region asia-southeast1
```

## 3. Infrastructure

### Cloud Run Services

All three services run in `asia-southeast1` with auto-scaling:

| Service | Memory | CPU | Min Instances | Max Instances | Timeout | Session Affinity |
|---------|--------|-----|---------------|---------------|---------|-----------------|
| `klc-admin-api` | 1Gi | 1 | 0 | 3 | 3600s | Yes (WebSocket) |
| `klc-driver-bff` | 512Mi | 1 | 0 | 5 | 300s | No |
| `klc-admin-portal` | 512Mi | 1 | 0 | 3 | 300s | No |

The Admin API has session affinity and a 3600s (1 hour) timeout to support long-lived OCPP WebSocket connections.

### Cloud SQL (PostgreSQL)

- **Instance**: `klc-postgres` (connection name: `klc-ev-charging:asia-southeast1:klc-postgres`)
- **IP**: `34.177.104.51`
- **Database**: `KLC`
- **User**: `klc_app`
- **Extensions**: PostGIS (spatial queries via `UseNetTopologySuite()`)
- **Connection**: Via Cloud SQL Auth Proxy sidecar (automatically configured with `--add-cloudsql-instances` flag)

### Memorystore Redis

- **Instance**: `klc-redis`
- **IP**: `10.239.176.251:6379`
- **Access**: Via VPC connector `klc-connector` (private IP only)
- **Purpose**: Session cache, real-time OCPP status, Driver BFF cache-first reads

### Artifact Registry

- **Repository**: `asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend`
- **Images**:
  - `admin-api:<tag>` -- Admin API
  - `driver-bff:<tag>` -- Driver BFF
  - `admin-portal:<tag>` -- Admin Portal
- **Tags**: Images are tagged with both the Git SHA and `latest`

### Secret Manager

All sensitive configuration is stored in GCP Secret Manager and injected into Cloud Run services at runtime. See section 5 for the full list.

### VPC Connector

- **Name**: `klc-connector`
- **Egress**: `private-ranges-only` (only traffic to private IPs goes through the connector)
- **Purpose**: Allows Cloud Run services to reach Memorystore Redis on private IP

## 4. CI/CD Pipeline

### Workflows

Three GitHub Actions workflows handle CI and deployment:

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI | `ci.yml` | Push/PR to `develop`, `main` | Build, type check, test |
| Deploy (Prod) | `deploy.yml` | Push to `main` | Deploy to production Cloud Run |
| Deploy (Dev) | `deploy-dev.yml` | Push to `develop` | Deploy to dev Cloud Run |

### Branch Strategy

```
develop  ──push──>  Deploy Dev workflow  ──>  evcms-dev-* Cloud Run services
main     ──push──>  Deploy (Prod) workflow ──>  klc-* Cloud Run services
PR       ──open──>  CI workflow (tests only, no deploy)
```

### Production Deploy Pipeline (`deploy.yml`)

```
  ┌──────────┐
  │ Detect   │──> Which paths changed? (backend / admin-portal)
  │ Changes  │
  └────┬─────┘
       │
  ┌────v─────┐
  │ Backend  │──> dotnet test (PostGIS service container)
  │ Tests    │
  └────┬─────┘
       │
  ┌────v────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
  │ Deploy Admin API    │  │ Deploy Driver BFF   │  │ Deploy Admin Portal │
  │ (needs test pass)   │  │ (needs test pass)   │  │ (no test gate)      │
  └─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

Each deploy job:
1. Authenticates to GCP via Workload Identity Federation (keyless, no service account keys)
2. Builds the Docker image
3. Pushes to Artifact Registry with both `:<sha>` and `:latest` tags
4. Deploys to Cloud Run via `google-github-actions/deploy-cloudrun@v2`

### Authentication: Workload Identity Federation

The CI/CD pipeline uses **Workload Identity Federation** instead of service account keys:

- **Identity Pool**: `github-actions`
- **Service Account**: `github-actions-deploy@klc-ev-charging.iam.gserviceaccount.com`
- **GitHub Secrets Required**:
  - `GCP_WORKLOAD_IDENTITY_PROVIDER` -- The full provider resource name
  - `GCP_SA_EMAIL` -- The deploy service account email
  - `GCP_PROJECT_ID` -- `klc-ev-charging`
  - `GCP_REGION` -- `asia-southeast1`

### Change Detection

The deploy workflow uses `dorny/paths-filter@v3` to detect which parts of the codebase changed. Only affected services are rebuilt and redeployed:

- `src/backend/**` changes trigger Admin API and Driver BFF deploys
- `src/admin-portal/**` changes trigger Admin Portal deploy
- Admin Portal does not require backend tests to pass

## 5. Environment Variables & Secrets

### Admin API (`klc-admin-api`)

**Environment Variables** (set via `--set-env-vars`):

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `App__CorsOrigins` | `https://ev.odcall.com,https://api.ev.odcall.com,http://localhost:3001` |

**Secrets** (injected from Secret Manager via `--set-secrets`):

| Env Variable | Secret Manager Key | Description |
|--------------|--------------------|-------------|
| `ConnectionStrings__Default` | `db-connection-string` | Cloud SQL PostgreSQL connection string |
| `ConnectionStrings__Redis` | `redis-connection-string` | Memorystore Redis connection string |
| `Jwt__SecretKey` | `jwt-secret-key` | JWT signing key |
| `StringEncryption__DefaultPassPhrase` | `string-encryption-passphrase` | ABP string encryption passphrase |
| `Payment__MoMo__PartnerCode` | `momo-partner-code` | MoMo payment gateway partner code |
| `Payment__MoMo__AccessKey` | `momo-access-key` | MoMo payment gateway access key |
| `Payment__MoMo__SecretKey` | `momo-secret-key` | MoMo payment gateway secret key |
| `Payment__VnPay__TmnCode` | `vnpay-tmn-code` | VnPay terminal code |
| `Payment__VnPay__HashSecret` | `vnpay-hash-secret` | VnPay hash secret |
| `OPENIDDICT_SIGNING_CERT` | `openiddict-signing-cert` | OpenIddict signing certificate (base64) |
| `OPENIDDICT_SIGNING_PASSWORD` | `openiddict-signing-password` | OpenIddict signing cert password |
| `OPENIDDICT_ENCRYPTION_CERT` | `openiddict-encryption-cert` | OpenIddict encryption certificate (base64) |
| `OPENIDDICT_ENCRYPTION_PASSWORD` | `openiddict-encryption-password` | OpenIddict encryption cert password |

### Driver BFF (`klc-driver-bff`)

**Environment Variables**:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `EnableApiDocs` | `true` |

**Secrets**: Same as Admin API except without the OpenIddict certificates and CORS settings.

### Admin Portal (`klc-admin-portal`)

**Environment Variables**:

| Variable | Value |
|----------|-------|
| `NODE_ENV` | `production` |
| `BACKEND_API_URL` | `https://api.ev.odcall.com` |
| `OIDC_CLIENT_ID` | `KLC_Api` |

**Build Args** (set at Docker build time):

| Arg | Value |
|-----|-------|
| `NEXT_PUBLIC_API_URL` | `https://api.ev.odcall.com` |

**Secrets**:

| Env Variable | Secret Manager Key | Description |
|--------------|--------------------|-------------|
| `OIDC_CLIENT_SECRET` | `oidc-client-secret` | OpenIddict client secret |

### Managing Secrets

```bash
# Create a new secret
echo -n "my-secret-value" | gcloud secrets create my-secret-name \
    --data-file=- --replication-policy=automatic

# Update an existing secret
echo -n "new-value" | gcloud secrets versions add my-secret-name --data-file=-

# View secret metadata (not the value)
gcloud secrets describe db-connection-string

# List all secrets
gcloud secrets list

# Grant Cloud Run service account access to a secret
gcloud secrets add-iam-policy-binding my-secret-name \
    --member="serviceAccount:493799105026-compute@developer.gserviceaccount.com" \
    --role="roles/secretmanager.secretAccessor"
```

## 6. Manual Deployment

### Build and Push Images

```bash
# Set variables
export PROJECT_ID=klc-ev-charging
export REGION=asia-southeast1
export AR_REPO=${REGION}-docker.pkg.dev/${PROJECT_ID}/klc-backend
export TAG=$(git rev-parse --short HEAD)

# Authenticate
gcloud auth configure-docker ${REGION}-docker.pkg.dev --quiet
```

**Admin API**:

```bash
docker build -t ${AR_REPO}/admin-api:${TAG} \
    -f src/backend/src/KLC.HttpApi.Host/Dockerfile \
    src/backend

docker push ${AR_REPO}/admin-api:${TAG}
```

**Driver BFF**:

```bash
docker build -t ${AR_REPO}/driver-bff:${TAG} \
    -f src/backend/src/KLC.Driver.BFF/Dockerfile \
    src/backend

docker push ${AR_REPO}/driver-bff:${TAG}
```

**Admin Portal**:

```bash
docker build -t ${AR_REPO}/admin-portal:${TAG} \
    --build-arg NEXT_PUBLIC_API_URL=https://api.ev.odcall.com \
    -f src/admin-portal/Dockerfile \
    src/admin-portal

docker push ${AR_REPO}/admin-portal:${TAG}
```

### Deploy to Cloud Run

**Admin API**:

```bash
gcloud run deploy klc-admin-api \
    --image ${AR_REPO}/admin-api:${TAG} \
    --region ${REGION} \
    --port=8080 \
    --memory=1Gi \
    --cpu=1 \
    --min-instances=0 \
    --max-instances=3 \
    --timeout=3600 \
    --session-affinity \
    --add-cloudsql-instances=klc-ev-charging:asia-southeast1:klc-postgres \
    --vpc-connector=klc-connector \
    --vpc-egress=private-ranges-only \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,App__CorsOrigins=https://ev.odcall.com" \
    --set-secrets="ConnectionStrings__Default=db-connection-string:latest,ConnectionStrings__Redis=redis-connection-string:latest,Jwt__SecretKey=jwt-secret-key:latest,StringEncryption__DefaultPassPhrase=string-encryption-passphrase:latest"
```

**Driver BFF**:

```bash
gcloud run deploy klc-driver-bff \
    --image ${AR_REPO}/driver-bff:${TAG} \
    --region ${REGION} \
    --port=8080 \
    --memory=512Mi \
    --cpu=1 \
    --min-instances=0 \
    --max-instances=5 \
    --add-cloudsql-instances=klc-ev-charging:asia-southeast1:klc-postgres \
    --vpc-connector=klc-connector \
    --vpc-egress=private-ranges-only \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" \
    --set-secrets="ConnectionStrings__Default=db-connection-string:latest,ConnectionStrings__Redis=redis-connection-string:latest,Jwt__SecretKey=jwt-secret-key:latest"
```

**Admin Portal**:

```bash
gcloud run deploy klc-admin-portal \
    --image ${AR_REPO}/admin-portal:${TAG} \
    --region ${REGION} \
    --port=3000 \
    --memory=512Mi \
    --cpu=1 \
    --min-instances=0 \
    --max-instances=3 \
    --set-env-vars="NODE_ENV=production,BACKEND_API_URL=https://api.ev.odcall.com,OIDC_CLIENT_ID=KLC_Api" \
    --set-secrets="OIDC_CLIENT_SECRET=oidc-client-secret:latest"
```

### Database Migrations

Migrations are run manually via the `deploy-dev.yml` workflow dispatch or from a local machine with Cloud SQL Auth Proxy:

```bash
# Install Cloud SQL Auth Proxy
gcloud components install cloud-sql-proxy

# Start the proxy (connects to Cloud SQL via IAM auth)
cloud-sql-proxy klc-ev-charging:asia-southeast1:klc-postgres &
sleep 3

# Run migrations (use the proxy's local port)
dotnet ef database update \
    --project src/backend/src/KLC.EntityFrameworkCore \
    --startup-project src/backend/src/KLC.HttpApi.Host
```

Set the connection string environment variable to point to `127.0.0.1` when using the proxy:

```bash
export ConnectionStrings__Default="Host=127.0.0.1;Port=5432;Database=KLC;Username=klc_app;Password=<password>"
```

## 7. Domain Configuration

### Custom Domain Mappings

Three domains are mapped to Cloud Run services:

| Domain | Cloud Run Service | Purpose |
|--------|------------------|---------|
| `ev.odcall.com` | `klc-admin-portal` | Admin Portal (Next.js) |
| `api.ev.odcall.com` | `klc-admin-api` | Admin API + OCPP WebSocket |
| `bff.ev.odcall.com` | `klc-driver-bff` | Driver BFF (mobile API) |

### Setting Up Domain Mappings

```bash
# Map custom domains to Cloud Run services
gcloud run domain-mappings create \
    --service klc-admin-portal \
    --domain ev.odcall.com \
    --region asia-southeast1

gcloud run domain-mappings create \
    --service klc-admin-api \
    --domain api.ev.odcall.com \
    --region asia-southeast1

gcloud run domain-mappings create \
    --service klc-driver-bff \
    --domain bff.ev.odcall.com \
    --region asia-southeast1
```

### DNS Configuration

After creating domain mappings, configure DNS records at your domain registrar:

```
# Verify the mapping status and get required DNS records
gcloud run domain-mappings describe \
    --domain ev.odcall.com \
    --region asia-southeast1
```

Typically, Cloud Run requires:
- A `CNAME` record pointing to `ghs.googlehosted.com.` for each subdomain
- Domain ownership verification via Google Search Console

### OCPP WebSocket Access

OCPP chargers connect via WebSocket at:

```
wss://api.ev.odcall.com/ocpp/{chargePointId}
```

The Admin API Cloud Run service is configured with:
- **Session affinity**: Ensures WebSocket connections stick to the same instance
- **Timeout**: 3600s (1 hour) to support long-lived WebSocket connections
- **Subprotocol**: `ocpp1.6`

## 8. Monitoring & Health Checks

### Health Endpoints

| Service | Health URL |
|---------|-----------|
| Admin API | `https://api.ev.odcall.com/health` |
| Driver BFF | `https://bff.ev.odcall.com/health` |
| Admin Portal | `https://ev.odcall.com/` (HTTP 200 check) |

### Quick Health Check

```bash
# Check all services
curl -sf https://api.ev.odcall.com/health && echo "Admin API: OK" || echo "Admin API: FAIL"
curl -sf https://bff.ev.odcall.com/health && echo "Driver BFF: OK" || echo "Driver BFF: FAIL"
curl -sf https://ev.odcall.com/ -o /dev/null && echo "Admin Portal: OK" || echo "Admin Portal: FAIL"
```

### Cloud Run Metrics

View service metrics in the GCP Console or via CLI:

```bash
# View recent logs for a service
gcloud run services logs read klc-admin-api --region asia-southeast1 --limit 50

# View logs for a specific revision
gcloud run revisions logs read klc-admin-api-00042-abc --region asia-southeast1

# Stream logs in real-time
gcloud run services logs tail klc-admin-api --region asia-southeast1
```

### Cloud Logging Queries

Access structured logs via Cloud Logging in the GCP Console. Useful queries:

```
# All errors from Admin API
resource.type="cloud_run_revision"
resource.labels.service_name="klc-admin-api"
severity>=ERROR

# OCPP WebSocket connections
resource.type="cloud_run_revision"
resource.labels.service_name="klc-admin-api"
textPayload=~"ocpp"

# Slow requests (>5s)
resource.type="cloud_run_revision"
resource.labels.service_name="klc-admin-api"
httpRequest.latency>"5s"
```

### Key Metrics to Monitor

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| Request count | Total requests per service | Spike > 10x normal |
| Request latency (p95) | 95th percentile response time | > 2s for API, > 5s for portal |
| Container instance count | Active instances | Max instances sustained |
| Memory utilization | Container memory usage | > 80% |
| Error rate (5xx) | Server error percentage | > 1% |

## 9. Rollback Procedures

Cloud Run maintains a history of revisions, making rollbacks straightforward.

### View Revision History

```bash
# List revisions for a service
gcloud run revisions list --service klc-admin-api --region asia-southeast1

# Output shows revision name, traffic %, and status
# Example:
#   klc-admin-api-00045-xyz   100%   READY
#   klc-admin-api-00044-abc   0%     READY
#   klc-admin-api-00043-def   0%     READY
```

### Rollback to Previous Revision

```bash
# Route 100% traffic to a specific previous revision
gcloud run services update-traffic klc-admin-api \
    --to-revisions=klc-admin-api-00044-abc=100 \
    --region asia-southeast1
```

### Gradual Rollback (Canary)

```bash
# Split traffic: 90% to old revision, 10% to new
gcloud run services update-traffic klc-admin-api \
    --to-revisions=klc-admin-api-00044-abc=90,klc-admin-api-00045-xyz=10 \
    --region asia-southeast1

# After verification, fully shift
gcloud run services update-traffic klc-admin-api \
    --to-revisions=klc-admin-api-00044-abc=100 \
    --region asia-southeast1
```

### Redeploy a Known-Good Image

```bash
# Deploy a specific image by SHA tag
gcloud run deploy klc-admin-api \
    --image asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend/admin-api:<known-good-sha> \
    --region asia-southeast1
```

### Database Rollback

If a migration needs to be reverted:

```bash
# Revert to a specific migration (via Cloud SQL Auth Proxy)
cloud-sql-proxy klc-ev-charging:asia-southeast1:klc-postgres &
sleep 3

dotnet ef database update <PreviousMigrationName> \
    --project src/backend/src/KLC.EntityFrameworkCore \
    --startup-project src/backend/src/KLC.HttpApi.Host
```

**Important**: Always deploy the application code rollback _before_ reverting the database migration, since the old code expects the old schema.

## 10. Troubleshooting

### Common Issues

#### Service fails to start (CrashLoopBackOff)

**Symptoms**: Cloud Run revision shows `FAILED` status, container restarts repeatedly.

**Diagnosis**:
```bash
gcloud run revisions describe <revision-name> --region asia-southeast1
gcloud run services logs read klc-admin-api --region asia-southeast1 --limit 100
```

**Common causes**:
- Missing or invalid secrets (check Secret Manager access)
- Database connection failure (check Cloud SQL instance status and VPC connector)
- Port mismatch (service must listen on the port specified in `--port`)

#### Cannot connect to Redis

**Symptoms**: `RedisConnectionException` in logs.

**Fix**:
1. Verify VPC connector is active: `gcloud compute networks vpc-access connectors describe klc-connector --region asia-southeast1`
2. Verify Redis instance is running: `gcloud redis instances describe klc-redis --region asia-southeast1`
3. Confirm the `redis-connection-string` secret contains the correct private IP (`10.239.176.251:6379`)

Note: Redis connections in the BFF are configured as lazy and non-fatal. The service will start even if Redis is temporarily unavailable.

#### Cannot connect to Cloud SQL

**Symptoms**: `Npgsql.NpgsqlException` in logs.

**Fix**:
1. Verify the Cloud SQL instance is running: `gcloud sql instances describe klc-postgres`
2. Ensure `--add-cloudsql-instances` flag is set in the deploy command
3. Verify the `db-connection-string` secret uses the Cloud SQL socket path format:
   ```
   Host=/cloudsql/klc-ev-charging:asia-southeast1:klc-postgres;Database=KLC;Username=klc_app;Password=<password>
   ```

#### OCPP WebSocket connections dropping

**Symptoms**: Chargers disconnect and reconnect frequently.

**Fix**:
1. Verify `--timeout=3600` is set on `klc-admin-api`
2. Verify `--session-affinity` is enabled
3. Check Cloud Run logs for idle timeout messages
4. Ensure the charger's heartbeat interval is shorter than the Cloud Run timeout

#### Admin Portal shows 502/503 errors

**Symptoms**: `ev.odcall.com` returns Bad Gateway.

**Fix**:
1. Check if the portal container is running: `gcloud run services describe klc-admin-portal --region asia-southeast1`
2. Verify `BACKEND_API_URL` points to `https://api.ev.odcall.com`
3. Check if the Admin API itself is healthy (the portal proxies auth requests server-side)

#### Deployment fails with permission errors

**Symptoms**: GitHub Actions workflow fails at GCP authentication step.

**Fix**:
1. Verify Workload Identity Federation is configured correctly
2. Check that the deploy SA has required roles:
   - `roles/run.admin`
   - `roles/artifactregistry.writer`
   - `roles/iam.serviceAccountUser`
   - `roles/secretmanager.secretAccessor`
3. Verify GitHub secrets `GCP_WORKLOAD_IDENTITY_PROVIDER` and `GCP_SA_EMAIL` are set

#### Secret access denied

**Symptoms**: `PermissionDenied` error when Cloud Run tries to access secrets.

**Fix**:
```bash
# Grant the compute service account access to all secrets
for secret in db-connection-string redis-connection-string jwt-secret-key \
    string-encryption-passphrase oidc-client-secret momo-partner-code \
    momo-access-key momo-secret-key vnpay-tmn-code vnpay-hash-secret; do
    gcloud secrets add-iam-policy-binding ${secret} \
        --member="serviceAccount:493799105026-compute@developer.gserviceaccount.com" \
        --role="roles/secretmanager.secretAccessor"
done
```

### Useful Diagnostic Commands

```bash
# List all Cloud Run services and their URLs
gcloud run services list --region asia-southeast1

# Describe a specific service (shows URL, revisions, env vars)
gcloud run services describe klc-admin-api --region asia-southeast1

# Check domain mapping status
gcloud run domain-mappings list --region asia-southeast1

# List images in Artifact Registry
gcloud artifacts docker images list \
    asia-southeast1-docker.pkg.dev/klc-ev-charging/klc-backend

# Check Cloud SQL status
gcloud sql instances describe klc-postgres

# Check Redis status
gcloud redis instances describe klc-redis --region asia-southeast1

# Check VPC connector status
gcloud compute networks vpc-access connectors describe klc-connector \
    --region asia-southeast1
```

### Dev Environment

The dev environment uses a separate set of Cloud Run services and secrets with the `evcms-dev-` prefix:

| Production | Dev |
|-----------|-----|
| `klc-admin-api` | `evcms-dev-backend-api` |
| `klc-driver-bff` | `evcms-dev-bff-socket` |
| `klc-admin-portal` | `evcms-dev-admin-portal` |
| `klc-connector` | `evcms-dev-vpc-connector` |
| `db-connection-string` | `evcms-dev-db-connection-string` |
| `redis-connection-string` | `evcms-dev-redis-auth-string` |

Dev also has an additional `evcms-dev-ocpp-gateway` service (separate Cloud Run instance for OCPP WebSocket handling).

The dev environment deploys automatically on push to `develop` branch via the `deploy-dev.yml` workflow. Database migrations can be triggered manually via `workflow_dispatch` with `run_migrations: true`.
