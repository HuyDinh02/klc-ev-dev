# DevOps Plan -- EVCMS DEV Environment on GCP

> **Project:** EV Charging Management System (EVCMS)
> **Client:** KLC | **Developer:** EmeSoft
> **Environment:** DEV
> **GCP Project:** `klc-ev-charging`
> **Region:** `asia-southeast1`
> **Last Updated:** 2026-03-08

---

## Table of Contents

1. [Target Architecture](#1-target-architecture)
2. [GCP Resources List](#2-gcp-resources-list)
3. [Domain Strategy](#3-domain-strategy)
4. [Security Checklist](#4-security-checklist)
5. [Observability Checklist](#5-observability-checklist)
6. [Cost Guardrails for DEV](#6-cost-guardrails-for-dev)
7. [Production Recommendations](#7-production-recommendations)

---

## 1. Target Architecture

### Naming Conventions

| Constant   | Value                        |
|------------|------------------------------|
| PROJECT_ID | `klc-ev-charging`            |
| REGION     | `asia-southeast1`            |
| ENV        | `dev`                        |
| PREFIX     | `evcms-dev`                  |
| DOMAIN     | `odcall.com`                 |
| GitHub     | `howard-tech/klc-ev-charging`|

### Cloud Run Services

| Service Name              | Image Source     | Port | Purpose                              |
|---------------------------|------------------|------|--------------------------------------|
| `evcms-dev-admin-portal`  | Next.js          | 3000 | Admin dashboard frontend             |
| `evcms-dev-backend-api`   | ASP.NET (.NET 10)| 8080 | Admin API (ABP, OpenIddict, SignalR)  |
| `evcms-dev-bff-socket`    | ASP.NET (.NET 10)| 8080 | Driver BFF + SignalR (DriverHub)      |
| `evcms-dev-ocpp-gateway`  | Same as backend-api | 8080 | Dedicated OCPP 1.6J/2.0 WebSocket  |

### Architecture Diagram

```
                              ┌─────────────────────────────────────────────────┐
                              │                   INTERNET                      │
                              └──────────┬───────────┬──────────────┬───────────┘
                                         │           │              │
                              ┌──────────▼──┐  ┌─────▼─────┐  ┌────▼──────────┐
                              │  Cloud DNS   │  │ Cloud DNS  │  │   Cloud DNS   │
                              │  (CNAME)     │  │ (CNAME)    │  │   (A record)  │
                              └──────────┬───┘  └─────┬──────┘  └────┬──────────┘
                                         │           │              │
              ┌──────────────────────────┐│           │              │
              │  ev.odcall.com           ││           │              │
              │  api.ev.odcall.com       │◄───────────┘              │
              │  bff.ev.odcall.com       │                           │
              │                          │               ┌───────────▼───────────┐
              │  Cloud Run Domain        │               │  ocpp.ev.odcall.com   │
              │  Mapping (managed TLS)   │               │                       │
              └──────┬──────┬──────┬─────┘               │  Static IP            │
                     │      │      │                     │  evcms-dev-ocpp-ip    │
                     │      │      │                     └───────────┬───────────┘
                     │      │      │                                 │
              ┌──────▼──┐ ┌─▼────┐ ┌▼─────────┐          ┌──────────▼──────────┐
              │ admin-  │ │backend│ │bff-      │          │  HTTPS Load Balancer │
              │ portal  │ │-api   │ │socket    │          │  evcms-dev-ocpp-*    │
              │ (Next)  │ │(ABP)  │ │(BFF+WS)  │          └──────────┬──────────┘
              │ :3000   │ │:8080  │ │:8080     │                     │
              └─────────┘ └───┬───┘ └──┬───────┘          ┌──────────▼──────────┐
                              │        │                  │  Cloud Armor         │
                              │        │                  │  evcms-dev-ocpp-armor│
                              │        │                  │  (IP allowlist)      │
                              │        │                  └──────────┬──────────┘
                              │        │                             │
                              │        │                  ┌──────────▼──────────┐
                              │        │                  │  Serverless NEG      │
                              │        │                  │  evcms-dev-ocpp-neg  │
                              │        │                  └──────────┬──────────┘
                              │        │                             │
                              │        │                  ┌──────────▼──────────┐
                              │        │                  │  ocpp-gateway        │
                              │        │                  │  (Cloud Run)         │
                              │        │                  │  :8080               │
                              │        │                  └──────────┬──────────┘
                              │        │                             │
              ┌───────────────▼────────▼─────────────────────────────▼──────────┐
              │                   VPC Connector                                 │
              │                   evcms-dev-vpc-connector (10.8.0.0/28)         │
              └───────────────────────┬────────────────────────┬───────────────┘
                                      │                        │
                           ┌──────────▼──────────┐  ┌─────────▼──────────┐
                           │  Cloud SQL           │  │  Memorystore Redis │
                           │  evcms-dev-postgres  │  │  evcms-dev-redis   │
                           │  PostgreSQL 16       │  │  BASIC 1GB         │
                           │  + PostGIS           │  │  Redis 7           │
                           │  db: klc_dev         │  │                    │
                           │  user: klc_app       │  │                    │
                           └──────────────────────┘  └────────────────────┘
```

### OCPP WebSocket Path (Detail)

```
  EV Charger
      │
      │  wss://ocpp.ev.odcall.com/ocpp/{chargePointId}
      │  subprotocol: ocpp1.6
      │
      ▼
  Static IP (evcms-dev-ocpp-ip)
      │
      ▼
  HTTPS LB (evcms-dev-ocpp-forwarding-rule)
      │
      ▼
  SSL Proxy (evcms-dev-ocpp-https-proxy)
      │  managed certificate: evcms-dev-ocpp-cert
      │
      ▼
  URL Map (evcms-dev-ocpp-urlmap)
      │
      ▼
  Backend Service (evcms-dev-ocpp-backend)
      │  timeout: 3600s (WebSocket keep-alive)
      │  session affinity: GENERATED_COOKIE
      │  Cloud Armor policy: evcms-dev-ocpp-armor
      │  logging: enabled (sample rate 1.0)
      │
      ▼
  Serverless NEG (evcms-dev-ocpp-neg)
      │
      ▼
  Cloud Run: evcms-dev-ocpp-gateway
      │  min-instances: 1 (always warm for chargers)
      │  timeout: 3600s
      │
      ▼
  VPC Connector → Cloud SQL + Redis
```

### Data Flow Summary

| Flow                        | Path                                                                |
|-----------------------------|---------------------------------------------------------------------|
| Admin browser               | `ev.odcall.com` -> Cloud Run `admin-portal` -> calls `api.ev.odcall.com` |
| Admin API call              | `api.ev.odcall.com` -> Cloud Run `backend-api` -> VPC -> SQL/Redis |
| Mobile app API call         | `bff.ev.odcall.com` -> Cloud Run `bff-socket` -> VPC -> SQL/Redis  |
| Mobile SignalR              | `bff.ev.odcall.com` -> Cloud Run `bff-socket` (WebSocket upgrade)  |
| Admin SignalR               | `api.ev.odcall.com` -> Cloud Run `backend-api` (WebSocket upgrade) |
| Charger OCPP                | `ocpp.ev.odcall.com` -> Static IP -> LB -> Armor -> NEG -> `ocpp-gateway` -> VPC -> SQL/Redis |

---

## 2. GCP Resources List

### Compute

| Resource Type       | Name                        | Purpose                                    | Est. Monthly Cost (DEV) |
|---------------------|-----------------------------|--------------------------------------------|------------------------|
| Cloud Run Service   | `evcms-dev-admin-portal`    | Next.js admin dashboard                    | ~$0-5 (scale to 0)    |
| Cloud Run Service   | `evcms-dev-backend-api`     | Admin API, OpenIddict, MonitoringHub       | ~$0-5 (scale to 0)    |
| Cloud Run Service   | `evcms-dev-bff-socket`      | Driver BFF, DriverHub SignalR              | ~$0-5 (scale to 0)    |
| Cloud Run Service   | `evcms-dev-ocpp-gateway`    | OCPP WebSocket endpoint (charger comms)    | ~$5-10 (min=1)        |

### Data

| Resource Type       | Name                        | Purpose                                    | Est. Monthly Cost (DEV) |
|---------------------|-----------------------------|--------------------------------------------|------------------------|
| Cloud SQL (PG 16)   | `evcms-dev-postgres`        | Primary database (PostGIS), db: `klc_dev`  | ~$7 (db-f1-micro)     |
| Memorystore Redis   | `evcms-dev-redis`           | Cache, session store, BFF cache-first      | ~$35 (BASIC 1GB)      |

### Networking

| Resource Type       | Name                             | Purpose                                | Est. Monthly Cost (DEV) |
|---------------------|----------------------------------|----------------------------------------|------------------------|
| VPC Connector       | `evcms-dev-vpc-connector`        | Cloud Run -> private SQL/Redis         | ~$0 (included)         |
| Static IP (global)  | `evcms-dev-ocpp-ip`              | Stable IP for OCPP charger vendors     | $0 (free while in use) |
| Serverless NEG      | `evcms-dev-ocpp-neg`             | NEG pointing to ocpp-gateway           | $0 (no charge)         |
| Backend Service     | `evcms-dev-ocpp-backend`         | LB backend (3600s timeout, logging)    | $0 (part of LB)        |
| URL Map             | `evcms-dev-ocpp-urlmap`          | Route all traffic to backend service   | $0 (part of LB)        |
| SSL Certificate     | `evcms-dev-ocpp-cert`            | Managed TLS for ocpp.ev.odcall.com     | $0 (free managed)      |
| HTTPS Proxy         | `evcms-dev-ocpp-https-proxy`     | HTTPS termination for OCPP LB          | $0 (part of LB)        |
| Forwarding Rule     | `evcms-dev-ocpp-forwarding-rule` | Global forwarding rule (443->proxy)    | ~$18/mo                |

### Security

| Resource Type       | Name                        | Purpose                                    | Est. Monthly Cost (DEV) |
|---------------------|-----------------------------|--------------------------------------------|------------------------|
| Cloud Armor Policy  | `evcms-dev-ocpp-armor`      | IP allowlist for OCPP charger vendors      | ~$5/mo                 |

### Container Registry

| Resource Type       | Name                        | Purpose                                    | Est. Monthly Cost (DEV) |
|---------------------|-----------------------------|--------------------------------------------|------------------------|
| Artifact Registry   | `evcms-dev-ar`              | Docker image repository                    | ~$1-2 (storage)        |

### Identity & Secrets

| Resource Type       | Name                                   | Purpose                             | Est. Monthly Cost (DEV) |
|---------------------|----------------------------------------|-------------------------------------|------------------------|
| Service Account     | `evcms-dev-deployer`                   | CI/CD via Workload Identity Federation | $0                   |
| Service Account     | `evcms-dev-sa-portal`                  | admin-portal Cloud Run runtime SA   | $0                     |
| Service Account     | `evcms-dev-sa-api`                     | backend-api Cloud Run runtime SA    | $0                     |
| Service Account     | `evcms-dev-sa-bff`                     | bff-socket Cloud Run runtime SA     | $0                     |
| Service Account     | `evcms-dev-sa-ocpp`                    | ocpp-gateway Cloud Run runtime SA   | $0                     |
| Secret Manager      | `evcms-dev-db-connection-string`       | PostgreSQL connection string        | ~$0.06/secret/mo       |
| Secret Manager      | `evcms-dev-db-password`                | PostgreSQL klc_app user password    | (included)             |
| Secret Manager      | `evcms-dev-jwt-secret-key`             | JWT signing key for BFF auth        | (included)             |
| Secret Manager      | `evcms-dev-string-encryption-passphrase`| ABP string encryption passphrase   | (included)             |
| Secret Manager      | `evcms-dev-oidc-client-secret`         | OpenIddict client secret            | (included)             |
| Secret Manager      | `evcms-dev-redis-auth-string`          | Redis AUTH password                 | (included)             |
| Secret Manager      | `evcms-dev-ocpp-auth-key`              | OCPP HTTP Basic Auth shared key     | (included)             |
| Secret Manager      | `evcms-dev-momo-partner-code`          | MoMo payment partner code           | (included)             |
| Secret Manager      | `evcms-dev-momo-access-key`            | MoMo payment access key             | (included)             |
| Secret Manager      | `evcms-dev-momo-secret-key`            | MoMo payment secret key             | (included)             |
| Secret Manager      | `evcms-dev-vnpay-tmn-code`             | VnPay terminal code                 | (included)             |
| Secret Manager      | `evcms-dev-vnpay-hash-secret`          | VnPay HMAC hash secret              | (included)             |

### Summary

| Category         | Est. Monthly Cost |
|------------------|-------------------|
| Cloud Run (4)    | $10-25            |
| Cloud SQL        | $7                |
| Memorystore      | $35               |
| Load Balancer    | $18               |
| Cloud Armor      | $5                |
| Artifact Registry| $1-2              |
| Secret Manager   | ~$1               |
| **Total**        | **~$77-93/month** |

---

## 3. Domain Strategy

### Overview

Three services use native Cloud Run domain mapping (simple, managed TLS). The OCPP gateway uses a dedicated HTTPS Load Balancer with a static IP to provide a stable endpoint for charger vendors.

### DNS Records (Cloud DNS zone for odcall.com)

| Record                    | Type  | Value                                  | Purpose                           |
|---------------------------|-------|----------------------------------------|-----------------------------------|
| `ev.odcall.com`           | CNAME | `ghs.googlehosted.com.`               | Admin Portal via Cloud Run mapping|
| `api.ev.odcall.com`       | CNAME | `ghs.googlehosted.com.`               | Backend API via Cloud Run mapping |
| `bff.ev.odcall.com`       | CNAME | `ghs.googlehosted.com.`               | Driver BFF via Cloud Run mapping  |
| `ocpp.ev.odcall.com`      | A     | `<evcms-dev-ocpp-ip>`                 | OCPP gateway via HTTPS LB         |

### Cloud Run Domain Mappings

```bash
# Map custom domains to Cloud Run services (managed TLS provisioned automatically)
gcloud run domain-mappings create \
  --service=evcms-dev-admin-portal \
  --domain=ev.odcall.com \
  --region=asia-southeast1

gcloud run domain-mappings create \
  --service=evcms-dev-backend-api \
  --domain=api.ev.odcall.com \
  --region=asia-southeast1

gcloud run domain-mappings create \
  --service=evcms-dev-bff-socket \
  --domain=bff.ev.odcall.com \
  --region=asia-southeast1
```

### OCPP HTTPS Load Balancer Setup

The OCPP gateway requires a dedicated HTTPS LB because:

1. **Stable IP address** -- Charger vendors (Chargecore, JUHANG, etc.) require a fixed IP to allowlist in their firmware configuration.
2. **Cloud Armor** -- IP-based allowlisting restricts access to known charger networks only.
3. **Long-lived WebSocket** -- 3600s backend timeout with session affinity for persistent OCPP connections.
4. **Access logging** -- Full request/response logging for OCPP protocol debugging.

```bash
# 1. Reserve a global static IP
gcloud compute addresses create evcms-dev-ocpp-ip --global

# 2. Create a Serverless NEG pointing to the Cloud Run ocpp-gateway service
gcloud compute network-endpoint-groups create evcms-dev-ocpp-neg \
  --region=asia-southeast1 \
  --network-endpoint-type=serverless \
  --cloud-run-service=evcms-dev-ocpp-gateway

# 3. Create the backend service with WebSocket-friendly settings
gcloud compute backend-services create evcms-dev-ocpp-backend \
  --global \
  --load-balancing-scheme=EXTERNAL_MANAGED \
  --timeout=3600 \
  --session-affinity=GENERATED_COOKIE \
  --enable-logging \
  --logging-sample-rate=1.0

gcloud compute backend-services add-backend evcms-dev-ocpp-backend \
  --global \
  --network-endpoint-group=evcms-dev-ocpp-neg \
  --network-endpoint-group-region=asia-southeast1

# 4. Create Cloud Armor security policy
gcloud compute security-policies create evcms-dev-ocpp-armor \
  --description="OCPP charger IP allowlist"

# Default deny all
gcloud compute security-policies rules update 2147483647 \
  --security-policy=evcms-dev-ocpp-armor \
  --action=deny-403

# Allow known charger vendor IP ranges (update as vendors are onboarded)
gcloud compute security-policies rules create 1000 \
  --security-policy=evcms-dev-ocpp-armor \
  --action=allow \
  --src-ip-ranges="0.0.0.0/0" \
  --description="DEV: allow all (restrict per-vendor in production)"

# Attach policy to backend service
gcloud compute backend-services update evcms-dev-ocpp-backend \
  --global \
  --security-policy=evcms-dev-ocpp-armor

# 5. Create URL map
gcloud compute url-maps create evcms-dev-ocpp-urlmap \
  --default-service=evcms-dev-ocpp-backend

# 6. Create managed SSL certificate
gcloud compute ssl-certificates create evcms-dev-ocpp-cert \
  --domains=ocpp.ev.odcall.com \
  --global

# 7. Create HTTPS proxy
gcloud compute target-https-proxies create evcms-dev-ocpp-https-proxy \
  --url-map=evcms-dev-ocpp-urlmap \
  --ssl-certificates=evcms-dev-ocpp-cert

# 8. Create forwarding rule
gcloud compute forwarding-rules create evcms-dev-ocpp-forwarding-rule \
  --global \
  --target-https-proxy=evcms-dev-ocpp-https-proxy \
  --ports=443 \
  --address=evcms-dev-ocpp-ip
```

### TLS Certificate Management

| Domain              | Certificate Type          | Managed By        |
|---------------------|---------------------------|-------------------|
| `ev.odcall.com`     | Google-managed (auto)     | Cloud Run         |
| `api.ev.odcall.com` | Google-managed (auto)     | Cloud Run         |
| `bff.ev.odcall.com` | Google-managed (auto)     | Cloud Run         |
| `ocpp.ev.odcall.com`| Google-managed (LB cert)  | HTTPS Load Balancer|

All certificates are automatically provisioned and renewed by Google. No manual certificate management is required.

---

## 4. Security Checklist

### IAM -- Least Privilege

- [ ] **Separate service accounts per service** -- Each Cloud Run service runs under its own SA to limit blast radius.
- [ ] **Workload Identity Federation** for CI/CD -- `evcms-dev-deployer` authenticates via GitHub OIDC tokens; no long-lived service account keys.
- [ ] **No SA key export** -- All authentication is via WIF or attached SAs. No JSON key files in CI.

| Service Account          | Roles                                                              |
|--------------------------|--------------------------------------------------------------------|
| `evcms-dev-deployer`     | `roles/run.admin`, `roles/artifactregistry.writer`, `roles/iam.serviceAccountUser`, `roles/secretmanager.secretAccessor` |
| `evcms-dev-sa-portal`    | `roles/run.invoker` (minimal, frontend only)                       |
| `evcms-dev-sa-api`       | `roles/cloudsql.client`, `roles/secretmanager.secretAccessor`, `roles/storage.objectAdmin` |
| `evcms-dev-sa-bff`       | `roles/cloudsql.client`, `roles/secretmanager.secretAccessor`      |
| `evcms-dev-sa-ocpp`      | `roles/cloudsql.client`, `roles/secretmanager.secretAccessor`      |

### Secret Manager

- [ ] **All secrets stored in Secret Manager** -- No secrets in environment variables, code, or Docker images.
- [ ] **Secrets are rotatable** -- Application reads secrets at startup (or via secret volume mounts) so rotation requires only a redeploy.
- [ ] **Secret versions** -- Use `latest` alias in DEV; pin specific versions in production.
- [ ] **Access audit** -- `roles/secretmanager.secretAccessor` granted only to service accounts that need each specific secret.

| Secret                                     | Accessed By                          |
|--------------------------------------------|--------------------------------------|
| `evcms-dev-db-connection-string`           | `evcms-dev-sa-api`, `evcms-dev-sa-bff`, `evcms-dev-sa-ocpp` |
| `evcms-dev-db-password`                    | `evcms-dev-sa-api` (migration only)  |
| `evcms-dev-jwt-secret-key`                 | `evcms-dev-sa-api`, `evcms-dev-sa-bff` |
| `evcms-dev-string-encryption-passphrase`   | `evcms-dev-sa-api`                   |
| `evcms-dev-oidc-client-secret`             | `evcms-dev-sa-portal`, `evcms-dev-sa-api` |
| `evcms-dev-redis-auth-string`              | `evcms-dev-sa-api`, `evcms-dev-sa-bff`, `evcms-dev-sa-ocpp` |
| `evcms-dev-ocpp-auth-key`                  | `evcms-dev-sa-ocpp`                  |
| `evcms-dev-momo-partner-code`              | `evcms-dev-sa-bff`                   |
| `evcms-dev-momo-access-key`               | `evcms-dev-sa-bff`                   |
| `evcms-dev-momo-secret-key`               | `evcms-dev-sa-bff`                   |
| `evcms-dev-vnpay-tmn-code`                | `evcms-dev-sa-bff`                   |
| `evcms-dev-vnpay-hash-secret`             | `evcms-dev-sa-bff`                   |

### Database Security

- [ ] **Private IP only** -- Cloud SQL instance has no public IP; accessed exclusively via VPC Connector.
- [ ] **Dedicated user** -- Application connects as `klc_app` (not `postgres`); the `postgres` superuser password is stored separately and used only for migrations.
- [ ] **SSL required** -- `sslmode=verify-ca` in connection string.
- [ ] **Automated backups** -- Daily backups enabled with 7-day retention (even in DEV for data recovery).

### Network Security

- [ ] **CORS** -- Restrictive origin allowlists per service:
  - `backend-api`: `https://ev.odcall.com` only
  - `bff-socket`: mobile app schemes + `https://ev.odcall.com`
  - `ocpp-gateway`: no CORS (WebSocket only)
- [ ] **Rate limiting** -- Enforced at application level:
  - Auth endpoints: 10 requests/minute
  - General API: 60 requests/minute
  - Admin API: 100 requests/minute global
- [ ] **Cloud Armor** -- IP allowlist on OCPP gateway; default deny rule blocks all unknown sources.
- [ ] **Ingress** -- Cloud Run services set to `--ingress=all` (required for domain mapping); OCPP gateway additionally protected by Cloud Armor.

### Application Security

- [ ] **JWT secret** -- Application throws on startup if `Jwt:SecretKey` is not configured (no hardcoded fallback).
- [ ] **OpenIddict client_secret** -- Proxied through Next.js server-side API route (`/api/auth/token`); never exposed to browser.
- [ ] **OCPP test idTags** -- Gated behind `Ocpp:AllowTestIdTags` config flag (default: `false` in all deployed environments).
- [ ] **Payment callbacks** -- HMAC-SHA256 signature verification for MoMo and VnPay callbacks.
- [ ] **`[Authorize]` on all admin controllers** -- Verified across AlertController, DeviceController, AdminSessionController, etc.

---

## 5. Observability Checklist

### Cloud Logging

- [ ] **Structured JSON logs** -- All .NET services configured with `Microsoft.Extensions.Logging` JSON console provider; Next.js uses structured `console.log` with JSON serialization.
- [ ] **Log severity levels** -- `Information` for normal operations, `Warning` for recoverable issues, `Error` for failures requiring attention.
- [ ] **Correlation IDs** -- `X-Request-Id` header propagated through all services for request tracing.
- [ ] **OCPP message logging** -- All OCPP request/response pairs logged at `Information` level with `chargePointId` and `messageId` fields.
- [ ] **Log exclusions** -- Health check endpoints (`/health`, `/healthz`) excluded from logging to reduce noise.

### Cloud Monitoring -- Uptime Checks

| Endpoint                          | Protocol | Check Interval | Timeout | Alert Channel       |
|-----------------------------------|----------|----------------|---------|---------------------|
| `https://ev.odcall.com`           | HTTPS    | 5 min          | 10s     | Email + Slack (dev) |
| `https://api.ev.odcall.com/health`| HTTPS    | 1 min          | 10s     | Email + Slack (dev) |
| `https://bff.ev.odcall.com/health`| HTTPS    | 1 min          | 10s     | Email + Slack (dev) |
| `https://ocpp.ev.odcall.com/health`| HTTPS   | 1 min          | 10s     | Email + Slack (dev) |

### Load Balancer Logging

- [ ] **Access logs enabled** on `evcms-dev-ocpp-backend` with sample rate `1.0` (log every request in DEV).
- [ ] **Log fields captured** -- Client IP, latency, response code, backend service, WebSocket upgrade status.
- [ ] **Log-based metrics** -- Create custom metrics for OCPP connection duration and message throughput.

### Cloud Armor Logging

- [ ] **Deny event logging** -- All denied requests logged automatically by Cloud Armor.
- [ ] **Log filter** -- `resource.type="http_load_balancer" AND jsonPayload.enforcedSecurityPolicy.outcome="DENY"`.
- [ ] **Dashboard** -- Cloud Armor deny events visualized in a dedicated Monitoring dashboard panel.

### Alerting Policies

| Metric                                       | Condition                      | Severity | Window |
|----------------------------------------------|--------------------------------|----------|--------|
| Cloud Run error rate (`evcms-dev-backend-api`)| > 5% of requests return 5xx   | Warning  | 5 min  |
| Cloud Run error rate (`evcms-dev-bff-socket`) | > 5% of requests return 5xx   | Warning  | 5 min  |
| Cloud Run error rate (`evcms-dev-ocpp-gateway`)| > 1% of requests return 5xx  | Critical | 5 min  |
| Cloud SQL CPU utilization                     | > 80% sustained               | Warning  | 10 min |
| Cloud SQL disk utilization                    | > 80%                          | Critical | 5 min  |
| Cloud SQL connections                         | > 90% of max                   | Warning  | 5 min  |
| Memorystore Redis memory usage                | > 80%                          | Warning  | 10 min |
| Memorystore Redis connected clients           | > 100                          | Warning  | 5 min  |
| OCPP gateway instance count                   | < 1 (should never scale to 0) | Critical | 1 min  |
| Cloud Armor deny events                       | > 100 in window                | Info     | 15 min |
| Uptime check failure                          | Any endpoint down              | Critical | 1 check|

### Dashboards

Create a single Cloud Monitoring dashboard named `EVCMS DEV Overview` with the following panels:

1. **Cloud Run** -- Request count, latency (p50/p95/p99), error rate, instance count (per service)
2. **Cloud SQL** -- CPU, memory, disk, connections, query latency
3. **Memorystore** -- Memory usage, connected clients, hit/miss ratio, evictions
4. **OCPP** -- Active WebSocket connections, messages/sec, connection duration histogram
5. **Cloud Armor** -- Allowed vs. denied requests, top blocked IPs
6. **LB** -- Request count, latency, error rate, backend health

---

## 6. Cost Guardrails for DEV

### Cloud Run Configuration

| Service                   | min-instances | max-instances | CPU  | Memory | Timeout | Rationale                              |
|---------------------------|---------------|---------------|------|--------|---------|----------------------------------------|
| `evcms-dev-admin-portal`  | 0             | 3             | 1    | 512Mi  | 300s    | Scale to zero when no admin activity   |
| `evcms-dev-backend-api`   | 0             | 3             | 1    | 1Gi    | 300s    | Scale to zero; ABP startup ~5s cold    |
| `evcms-dev-bff-socket`    | 0             | 3             | 1    | 1Gi    | 3600s   | Scale to zero; SignalR reconnects OK   |
| `evcms-dev-ocpp-gateway`  | 1             | 3             | 1    | 1Gi    | 3600s   | Always warm: chargers expect 24/7 uptime|

### Cloud SQL Configuration

| Setting                | Value          | Rationale                                     |
|------------------------|----------------|-----------------------------------------------|
| Machine type           | `db-f1-micro`  | Shared-core, sufficient for DEV (~$7/mo)      |
| Storage                | 10 GB SSD      | Minimal; auto-increase enabled                |
| High availability      | Disabled       | Not needed for DEV                            |
| Backups                | Daily, 7-day   | Cheap insurance for dev data                  |
| Maintenance window     | Sun 03:00 UTC  | Low-impact for Vietnam dev team               |
| Point-in-time recovery | Disabled       | Not needed for DEV                            |

### Memorystore Redis Configuration

| Setting         | Value      | Rationale                                    |
|-----------------|------------|----------------------------------------------|
| Tier            | BASIC      | No replication needed for DEV (~$35/mo)      |
| Memory          | 1 GB       | Sufficient for cache + sessions in DEV       |
| Redis version   | 7.x        | Matches local Docker redis:7-alpine          |
| AUTH            | Enabled    | Always use AUTH, even in DEV                 |

### Cost Optimization Strategies

1. **Scale to zero** -- All Cloud Run services except `ocpp-gateway` scale to 0 instances when idle. DEV traffic is sporadic, so most hours incur no compute cost.
2. **Shared-core SQL** -- `db-f1-micro` is the smallest Cloud SQL tier. Upgrade to `db-custom-1-3840` only if query performance is insufficient.
3. **No read replicas in DEV** -- The BFF cache-first pattern reduces SQL load; a single instance is sufficient.
4. **Artifact Registry cleanup** -- Configure a lifecycle policy to delete images older than 30 days (keep last 10 tagged versions).
5. **Log retention** -- Set Cloud Logging retention to 30 days for DEV (default is 30 days; do not increase).

### Estimated Monthly Cost Breakdown

| Resource                | Monthly Estimate | Notes                                   |
|-------------------------|------------------|-----------------------------------------|
| Cloud Run (4 services)  | $10-25           | Mostly idle; OCPP min=1 is the baseline |
| Cloud SQL (db-f1-micro) | ~$7              | Shared-core, 10GB SSD                   |
| Memorystore (BASIC 1GB) | ~$35             | Largest single cost item in DEV         |
| HTTPS LB forwarding rule| ~$18             | Fixed monthly cost for global LB        |
| Cloud Armor policy      | ~$5              | Per-policy monthly fee                  |
| Artifact Registry       | ~$1-2            | Docker image storage                    |
| Secret Manager (13)     | ~$1              | $0.06/secret/mo + access charges        |
| Cloud DNS               | ~$0.50           | Hosted zone + queries                   |
| Static IP               | $0               | Free while attached to forwarding rule  |
| Egress                  | ~$1-3            | Minimal for DEV traffic                 |
| **Total**               | **~$79-97/month**| Conservative estimate for DEV workload  |

---

## 7. Production Recommendations

The DEV architecture on GCP Cloud Run is optimized for cost and simplicity. For production, the following alternatives provide higher availability, better performance, and compliance with enterprise requirements.

### Option A: GCP Production (Recommended for Continuity)

Scale the existing GCP architecture for production:

| DEV Setting                  | Production Setting                           |
|------------------------------|----------------------------------------------|
| Cloud Run min=0              | min=2 (API, BFF), min=2 (OCPP)              |
| Cloud Run max=3              | max=10 (auto-scale based on CPU/concurrency) |
| db-f1-micro                  | db-custom-2-7680 (2 vCPU, 7.5GB RAM)        |
| No read replica              | 1 read replica (asia-southeast1)             |
| Memorystore BASIC 1GB        | Memorystore STANDARD 5GB (HA with replica)  |
| Cloud Armor allow-all (DEV)  | Cloud Armor strict IP allowlist per vendor   |
| 30-day log retention         | 90-day log retention + BigQuery export       |
| No CDN                       | Cloud CDN on admin-portal                    |
| Single region                | Multi-region failover (optional Phase 2)     |

**Estimated production cost: $300-500/month** (scales with traffic).

### Option B: AWS (ECS Fargate)

If the client requires AWS or multi-cloud:

```
Internet
    │
    ├── CloudFront (CDN) ──► ALB ──► ECS Fargate (admin-portal)
    │
    ├── Route 53 ──► ALB ──► ECS Fargate (backend-api)
    │                  │
    │                  ├──► ECS Fargate (bff-socket)
    │                  │
    │                  └──► ECS Fargate (ocpp-gateway)
    │                         │  NLB (static IP for chargers)
    │                         │  WAF (IP allowlist)
    │
    └── VPC
         ├── RDS PostgreSQL 16 (Multi-AZ, PostGIS)
         └── ElastiCache Redis 7 (Multi-AZ)
```

| GCP Resource       | AWS Equivalent                          |
|--------------------|-----------------------------------------|
| Cloud Run          | ECS Fargate                             |
| Cloud SQL          | RDS PostgreSQL (Multi-AZ)               |
| Memorystore        | ElastiCache Redis (Multi-AZ)            |
| HTTPS LB           | ALB + NLB (for static IP)               |
| Cloud Armor        | AWS WAF                                 |
| Secret Manager     | AWS Secrets Manager                     |
| Artifact Registry  | ECR (Elastic Container Registry)        |
| Cloud Monitoring   | CloudWatch                              |
| Workload Identity  | OIDC Provider + IAM Roles for GitHub    |

**Key differences:**
- **NLB** provides static IPs natively (no separate IP reservation needed).
- **WAF** offers managed rule groups (OWASP, Bot Control) in addition to IP allowlists.
- **Multi-AZ** is built into RDS and ElastiCache; no manual replica configuration.
- **CloudFront** can serve the Next.js frontend from edge locations for lower latency.

**Estimated AWS production cost: $400-700/month** (Fargate pricing is slightly higher than Cloud Run).

### Option C: On-Premises / Self-Hosted VMs

For clients requiring full infrastructure control or data sovereignty:

```
Internet
    │
    ├── Nginx / Traefik (reverse proxy + TLS termination)
    │     ├── /              → Docker: admin-portal (Next.js)
    │     ├── /api/          → Docker: backend-api (ASP.NET)
    │     ├── /bff/          → Docker: bff-socket (ASP.NET)
    │     └── /ocpp/         → Docker: ocpp-gateway (ASP.NET)
    │
    └── Internal Network
          ├── PostgreSQL 16 + PostGIS (dedicated VM or Docker)
          └── Redis 7 (dedicated VM or Docker)
```

**Infrastructure requirements:**
- 2x VMs minimum (app server + database server), 4 vCPU / 8GB RAM each.
- Docker Compose or Docker Swarm for container orchestration.
- Traefik or Nginx for reverse proxy, TLS termination, and WebSocket support.
- Let's Encrypt for automated certificate management.
- Separate firewall rules for OCPP port (restrict to charger vendor IPs).
- Manual backups (pg_dump cron) or use pgBackRest for continuous archiving.

**Key differences:**
- **No auto-scaling** -- Must provision for peak load or use Docker Swarm scaling.
- **Manual certificate management** -- Let's Encrypt + certbot/Traefik ACME.
- **No managed WAF** -- Use Nginx rate limiting + fail2ban for basic protection.
- **Operational overhead** -- Patching, monitoring, backups are the team's responsibility.

**Estimated cost: $50-150/month** (2x cloud VMs) or $0 incremental (existing on-prem hardware).

### Architecture Portability

The EVCMS architecture is designed for easy migration between platforms:

| Concern                | Portability Approach                                              |
|------------------------|-------------------------------------------------------------------|
| Containerization       | All services are Docker containers; no cloud-specific runtime deps|
| Configuration          | Environment variables + secret injection (works on any platform) |
| Database               | Standard PostgreSQL 16 + PostGIS (available everywhere)          |
| Cache                  | Standard Redis 7 (available everywhere)                          |
| File storage           | `IFileUploadService` abstraction (swap GCS for S3/local/MinIO)  |
| Push notifications     | Firebase FCM (works regardless of hosting platform)              |
| Secret management      | Injected via env vars at deploy time (any secret store works)    |
| Reverse proxy          | Application handles routing internally; any proxy works          |

**Migration effort estimate:** 1-2 days to switch between GCP, AWS, or on-prem deployment targets. The primary work is infrastructure provisioning and DNS cutover; no application code changes are required.

---

## Appendix A: Service Account Setup Commands

```bash
PROJECT_ID="klc-ev-charging"
PREFIX="evcms-dev"

# Create service accounts
for SA in deployer sa-portal sa-api sa-bff sa-ocpp; do
  gcloud iam service-accounts create ${PREFIX}-${SA} \
    --project=${PROJECT_ID} \
    --display-name="EVCMS DEV ${SA}"
done

# Deployer: CI/CD permissions
gcloud projects add-iam-policy-binding ${PROJECT_ID} \
  --member="serviceAccount:${PREFIX}-deployer@${PROJECT_ID}.iam.gserviceaccount.com" \
  --role="roles/run.admin"

gcloud projects add-iam-policy-binding ${PROJECT_ID} \
  --member="serviceAccount:${PREFIX}-deployer@${PROJECT_ID}.iam.gserviceaccount.com" \
  --role="roles/artifactregistry.writer"

gcloud projects add-iam-policy-binding ${PROJECT_ID} \
  --member="serviceAccount:${PREFIX}-deployer@${PROJECT_ID}.iam.gserviceaccount.com" \
  --role="roles/iam.serviceAccountUser"

gcloud projects add-iam-policy-binding ${PROJECT_ID} \
  --member="serviceAccount:${PREFIX}-deployer@${PROJECT_ID}.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# API SA: database + secrets + storage
for ROLE in roles/cloudsql.client roles/secretmanager.secretAccessor roles/storage.objectAdmin; do
  gcloud projects add-iam-policy-binding ${PROJECT_ID} \
    --member="serviceAccount:${PREFIX}-sa-api@${PROJECT_ID}.iam.gserviceaccount.com" \
    --role="${ROLE}"
done

# BFF SA: database + secrets
for ROLE in roles/cloudsql.client roles/secretmanager.secretAccessor; do
  gcloud projects add-iam-policy-binding ${PROJECT_ID} \
    --member="serviceAccount:${PREFIX}-sa-bff@${PROJECT_ID}.iam.gserviceaccount.com" \
    --role="${ROLE}"
done

# OCPP SA: database + secrets
for ROLE in roles/cloudsql.client roles/secretmanager.secretAccessor; do
  gcloud projects add-iam-policy-binding ${PROJECT_ID} \
    --member="serviceAccount:${PREFIX}-sa-ocpp@${PROJECT_ID}.iam.gserviceaccount.com" \
    --role="${ROLE}"
done

# Workload Identity Federation for GitHub Actions
gcloud iam workload-identity-pools create github-actions \
  --project=${PROJECT_ID} \
  --location=global \
  --display-name="GitHub Actions"

gcloud iam workload-identity-pools providers create-oidc github \
  --project=${PROJECT_ID} \
  --location=global \
  --workload-identity-pool=github-actions \
  --issuer-uri="https://token.actions.githubusercontent.com" \
  --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository"

gcloud iam service-accounts add-iam-policy-binding \
  ${PREFIX}-deployer@${PROJECT_ID}.iam.gserviceaccount.com \
  --project=${PROJECT_ID} \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/projects/493799105026/locations/global/workloadIdentityPools/github-actions/attribute.repository/howard-tech/klc-ev-charging"
```

## Appendix B: Secret Manager Setup Commands

```bash
PROJECT_ID="klc-ev-charging"
PREFIX="evcms-dev"

# Create all secrets (values to be populated separately)
SECRETS=(
  "db-connection-string"
  "db-password"
  "jwt-secret-key"
  "string-encryption-passphrase"
  "oidc-client-secret"
  "redis-auth-string"
  "ocpp-auth-key"
  "momo-partner-code"
  "momo-access-key"
  "momo-secret-key"
  "vnpay-tmn-code"
  "vnpay-hash-secret"
)

for SECRET in "${SECRETS[@]}"; do
  gcloud secrets create ${PREFIX}-${SECRET} \
    --project=${PROJECT_ID} \
    --replication-policy=user-managed \
    --locations=asia-southeast1
done

# Add a secret version (example for db-connection-string)
echo -n "Host=/cloudsql/${PROJECT_ID}:asia-southeast1:${PREFIX}-postgres;Database=klc_dev;Username=klc_app;Password=CHANGE_ME;SSL Mode=Disable" | \
  gcloud secrets versions add ${PREFIX}-db-connection-string \
    --project=${PROJECT_ID} \
    --data-file=-
```

## Appendix C: Cloud Run Deployment Commands

```bash
PROJECT_ID="klc-ev-charging"
REGION="asia-southeast1"
PREFIX="evcms-dev"
AR="${REGION}-docker.pkg.dev/${PROJECT_ID}/${PREFIX}-ar"

# Create Artifact Registry repository
gcloud artifacts repositories create ${PREFIX}-ar \
  --project=${PROJECT_ID} \
  --repository-format=docker \
  --location=${REGION} \
  --description="EVCMS DEV Docker images"

# Build and push images (from CI/CD or locally)
docker build -t ${AR}/admin-portal:latest -f src/admin-portal/Dockerfile src/admin-portal/
docker build -t ${AR}/backend-api:latest -f src/backend/src/KLC.HttpApi.Host/Dockerfile src/backend/
docker build -t ${AR}/bff-socket:latest -f src/backend/src/KLC.Driver.BFF/Dockerfile src/backend/

docker push ${AR}/admin-portal:latest
docker push ${AR}/backend-api:latest
docker push ${AR}/bff-socket:latest

# Deploy admin-portal
gcloud run deploy ${PREFIX}-admin-portal \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --image=${AR}/admin-portal:latest \
  --port=3000 \
  --service-account=${PREFIX}-sa-portal@${PROJECT_ID}.iam.gserviceaccount.com \
  --min-instances=0 \
  --max-instances=3 \
  --cpu=1 \
  --memory=512Mi \
  --timeout=300 \
  --allow-unauthenticated \
  --set-env-vars="NEXT_PUBLIC_API_URL=https://api.ev.odcall.com"

# Deploy backend-api
gcloud run deploy ${PREFIX}-backend-api \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --image=${AR}/backend-api:latest \
  --port=8080 \
  --service-account=${PREFIX}-sa-api@${PROJECT_ID}.iam.gserviceaccount.com \
  --min-instances=0 \
  --max-instances=3 \
  --cpu=1 \
  --memory=1Gi \
  --timeout=300 \
  --allow-unauthenticated \
  --vpc-connector=${PREFIX}-vpc-connector \
  --set-secrets="ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,Jwt__SecretKey=${PREFIX}-jwt-secret-key:latest,StringEncryption__DefaultPassPhrase=${PREFIX}-string-encryption-passphrase:latest,Redis__Configuration=${PREFIX}-redis-auth-string:latest"

# Deploy bff-socket
gcloud run deploy ${PREFIX}-bff-socket \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --image=${AR}/bff-socket:latest \
  --port=8080 \
  --service-account=${PREFIX}-sa-bff@${PROJECT_ID}.iam.gserviceaccount.com \
  --min-instances=0 \
  --max-instances=3 \
  --cpu=1 \
  --memory=1Gi \
  --timeout=3600 \
  --allow-unauthenticated \
  --vpc-connector=${PREFIX}-vpc-connector \
  --session-affinity \
  --set-secrets="ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,Jwt__SecretKey=${PREFIX}-jwt-secret-key:latest,Redis__Configuration=${PREFIX}-redis-auth-string:latest"

# Deploy ocpp-gateway (same image as backend-api, different config)
gcloud run deploy ${PREFIX}-ocpp-gateway \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --image=${AR}/backend-api:latest \
  --port=8080 \
  --service-account=${PREFIX}-sa-ocpp@${PROJECT_ID}.iam.gserviceaccount.com \
  --min-instances=1 \
  --max-instances=3 \
  --cpu=1 \
  --memory=1Gi \
  --timeout=3600 \
  --allow-unauthenticated \
  --vpc-connector=${PREFIX}-vpc-connector \
  --session-affinity \
  --set-secrets="ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,Ocpp__AuthKey=${PREFIX}-ocpp-auth-key:latest,Redis__Configuration=${PREFIX}-redis-auth-string:latest" \
  --set-env-vars="ASPNETCORE_URLS=http://+:8080,Ocpp__AllowTestIdTags=false"
```

## Appendix D: VPC Connector and Cloud SQL Setup

```bash
PROJECT_ID="klc-ev-charging"
REGION="asia-southeast1"
PREFIX="evcms-dev"

# Create VPC connector for Cloud Run -> private services
gcloud compute networks vpc-access connectors create ${PREFIX}-vpc-connector \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --range="10.8.0.0/28" \
  --min-instances=2 \
  --max-instances=3

# Create Cloud SQL instance (PostgreSQL 16 with PostGIS)
gcloud sql instances create ${PREFIX}-postgres \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --database-version=POSTGRES_16 \
  --tier=db-f1-micro \
  --storage-size=10GB \
  --storage-auto-increase \
  --no-assign-ip \
  --network=default \
  --backup-start-time=20:00 \
  --maintenance-window-day=SUN \
  --maintenance-window-hour=3

# Create database
gcloud sql databases create klc_dev \
  --project=${PROJECT_ID} \
  --instance=${PREFIX}-postgres

# Create application user
gcloud sql users create klc_app \
  --project=${PROJECT_ID} \
  --instance=${PREFIX}-postgres \
  --password="$(gcloud secrets versions access latest --secret=${PREFIX}-db-password)"

# Enable PostGIS extension (connect via Cloud SQL Auth Proxy or psql)
# psql -h 127.0.0.1 -U postgres -d klc_dev -c "CREATE EXTENSION IF NOT EXISTS postgis;"

# Create Memorystore Redis instance
gcloud redis instances create ${PREFIX}-redis \
  --project=${PROJECT_ID} \
  --region=${REGION} \
  --tier=basic \
  --size=1 \
  --redis-version=redis_7_0 \
  --auth-enabled
```
