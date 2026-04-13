# CMC Cloud Migration Specification

## Overview

This document specifies the migration plan from Google Cloud Platform (GCP) to CMC Cloud for the KLC EV Charging Station Management System. The migration covers all backend services, databases, and supporting infrastructure while keeping Firebase (FCM + Phone Auth) on Google.

**Current hosting:** GCP (Cloud Run, Cloud SQL, Memorystore Redis, GCS)
**Target hosting:** CMC Cloud (Kubernetes, RDS, Redis, S3)
**Estimated effort:** 3-5 days
**Downtime:** ~30 minutes (DNS propagation + DB migration)

---

## 1. Service Mapping

| # | Component | GCP Current | CMC Cloud Target | Migration Type |
|---|-----------|-------------|------------------|----------------|
| 1 | Admin API | Cloud Run (`klc-admin-api`) | K8s Deployment | Container reuse |
| 2 | OCPP Gateway | Cloud Run (`klc-ocpp-gateway`) | K8s Deployment | Container reuse |
| 3 | Driver BFF | Cloud Run (`klc-driver-bff`) | K8s Deployment | Container reuse |
| 4 | Admin Portal | Cloud Run (`klc-admin-portal`) | K8s Deployment | Container reuse |
| 5 | PostgreSQL | Cloud SQL (`db-g1-small`) | CMC RDS PostgreSQL | Data migration |
| 6 | Redis | Memorystore (1GB BASIC) | CMC Redis Database | Config only |
| 7 | File Storage | GCS (`klc-ev-charging-uploads`) | CMC S3 Standard | Data + code change |
| 8 | Docker Images | Artifact Registry | CMC Container Registry | Image push |
| 9 | Secrets | GCP Secret Manager | K8s Secrets | Config migration |
| 10 | Load Balancer | Cloud Run built-in | CMC Elastic Load Balancer | New setup |
| 11 | Networking | VPC Connector | CMC VPC + Security Group | New setup |
| 12 | Monitoring | Cloud Run logs | CMC Cloud Monitoring | New setup |
| 13 | CI/CD | GitHub Actions + GCP auth | GitHub Actions + CMC auth | Pipeline update |
| 14 | Firebase (FCM) | Google Firebase | Google Firebase (no change) | None |
| 15 | Firebase Phone Auth | Google Firebase | Google Firebase (no change) | None |

---

## 2. Kubernetes Architecture

### 2.1 Cluster Specification

| Setting | Value | Notes |
|---------|-------|-------|
| Cluster type | Managed Kubernetes | CMC managed control plane |
| Worker nodes | 2 nodes (production), 1 node (staging) | Can scale to 3-5 |
| Node spec | 2 vCPU, 4 GB RAM each | Sufficient for 500 users |
| Node OS | Ubuntu 22.04 | Standard K8s node |
| K8s version | 1.28+ | Latest stable |
| CNI | Default (Calico/Flannel) | Per CMC default |

### 2.2 Namespace Layout

```
klc-production/
  ├── admin-api (Deployment + Service)
  ├── ocpp-gateway (Deployment + Service + WebSocket config)
  ├── driver-bff (Deployment + Service)
  ├── admin-portal (Deployment + Service)
  ├── ingress (Ingress resource with TLS)
  ├── configmaps (app settings)
  └── secrets (connection strings, API keys, certs)
```

### 2.3 Deployment Specs

#### admin-api
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: admin-api
  namespace: klc-production
spec:
  replicas: 1
  selector:
    matchLabels:
      app: admin-api
  template:
    metadata:
      labels:
        app: admin-api
    spec:
      containers:
      - name: admin-api
        image: <CMC_REGISTRY>/klc-backend/backend-api:latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            cpu: 500m
            memory: 512Mi
          limits:
            cpu: "1"
            memory: 1Gi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        envFrom:
        - configMapRef:
            name: app-config
        - secretRef:
            name: app-secrets
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
---
apiVersion: v1
kind: Service
metadata:
  name: admin-api
spec:
  selector:
    app: admin-api
  ports:
  - port: 80
    targetPort: 8080
```

#### ocpp-gateway
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ocpp-gateway
spec:
  replicas: 1
  strategy:
    type: Recreate  # Single instance for WebSocket consistency
  template:
    metadata:
      labels:
        app: ocpp-gateway
    spec:
      terminationGracePeriodSeconds: 30  # Allow WebSocket cleanup
      containers:
      - name: ocpp-gateway
        image: <CMC_REGISTRY>/klc-backend/backend-api:latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            cpu: 500m
            memory: 512Mi
          limits:
            cpu: "1"
            memory: 1Gi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        envFrom:
        - configMapRef:
            name: app-config
        - secretRef:
            name: app-secrets
---
apiVersion: v1
kind: Service
metadata:
  name: ocpp-gateway
spec:
  selector:
    app: ocpp-gateway
  ports:
  - port: 80
    targetPort: 8080
  sessionAffinity: ClientIP  # WebSocket sticky sessions
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 3600
```

#### driver-bff
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: driver-bff
spec:
  replicas: 1
  template:
    metadata:
      labels:
        app: driver-bff
    spec:
      containers:
      - name: driver-bff
        image: <CMC_REGISTRY>/klc-backend/bff-socket:latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            cpu: 250m
            memory: 256Mi
          limits:
            cpu: "1"
            memory: 512Mi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: Ocpp__GatewayUrl
          value: http://ocpp-gateway  # Internal K8s DNS
        envFrom:
        - configMapRef:
            name: app-config
        - secretRef:
            name: app-secrets
```

#### admin-portal
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: admin-portal
spec:
  replicas: 1
  template:
    metadata:
      labels:
        app: admin-portal
    spec:
      containers:
      - name: admin-portal
        image: <CMC_REGISTRY>/klc-backend/admin-portal:latest
        ports:
        - containerPort: 3000
        resources:
          requests:
            cpu: 250m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi
        env:
        - name: NODE_ENV
          value: production
        - name: BACKEND_API_URL
          value: http://admin-api  # Internal K8s DNS
```

### 2.4 Ingress (Load Balancer + TLS)

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: klc-ingress
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"      # WebSocket timeout
    nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"
    nginx.ingress.kubernetes.io/websocket-services: "ocpp-gateway"
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.ev.odcall.com
    - ocpp.ev.odcall.com
    - bff.ev.odcall.com
    - ev.odcall.com
    secretName: klc-tls
  rules:
  - host: api.ev.odcall.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: admin-api
            port:
              number: 80
  - host: ocpp.ev.odcall.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: ocpp-gateway
            port:
              number: 80
  - host: bff.ev.odcall.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: driver-bff
            port:
              number: 80
  - host: ev.odcall.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: admin-portal
            port:
              number: 80
```

### 2.5 ConfigMap & Secrets

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  App__CorsOrigins: "https://ev.odcall.com,https://api.ev.odcall.com"
  Ocpp__GatewayUrl: "http://ocpp-gateway"
  Firebase__CredentialPath: "/secrets/firebase/service-account.json"
  EnableApiDocs: "true"
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
stringData:
  ConnectionStrings__Default: "Host=<CMC_RDS_HOST>;Port=5432;Database=KLC;Username=klc_app;Password=<PASSWORD>"
  ConnectionStrings__Redis: "<CMC_REDIS_HOST>:6379"
  Jwt__SecretKey: "<JWT_SECRET>"
  Internal__ApiKey: "<INTERNAL_KEY>"
  StringEncryption__DefaultPassPhrase: "<PASSPHRASE>"
  # Payment gateway secrets
  Payment__MoMo__PartnerCode: "<VALUE>"
  Payment__MoMo__AccessKey: "<VALUE>"
  Payment__MoMo__SecretKey: "<VALUE>"
  Payment__VnPay__TmnCode: "<VALUE>"
  Payment__VnPay__HashSecret: "<VALUE>"
  Payment__VnPay__BaseUrl: "<VALUE>"
  Payment__VnPay__QueryApiUrl: "<VALUE>"
  # VnPay IPN IP whitelist (Case 13 compliance)
  Payment__VnPay__IpnWhitelist: "113.52.45.78,116.97.245.130,42.118.107.252,113.20.97.250,203.171.19.146,103.220.87.4,103.220.86.4,103.220.86.10,103.220.87.10,103.220.86.139,103.220.87.139"
  # OpenIddict certs (base64 encoded)
  OPENIDDICT_SIGNING_CERT: "<BASE64>"
  OPENIDDICT_SIGNING_PASSWORD: "<VALUE>"
  OPENIDDICT_ENCRYPTION_CERT: "<BASE64>"
  OPENIDDICT_ENCRYPTION_PASSWORD: "<VALUE>"
---
# Firebase service account mounted as file
apiVersion: v1
kind: Secret
metadata:
  name: firebase-credentials
type: Opaque
stringData:
  service-account.json: |
    {
      "type": "service_account",
      "project_id": "klc-ev-charging",
      ...
    }
```

---

## 3. Database Migration

### 3.1 PostgreSQL (Cloud SQL → CMC RDS)

**Pre-requisites:**
- CMC RDS PostgreSQL 16 instance with PostGIS extension
- Network connectivity between GCP and CMC (or use pg_dump/pg_restore)

**Steps:**
```bash
# 1. Export from Cloud SQL
pg_dump -h <CLOUD_SQL_IP> -U postgres -d KLC \
  --format=custom --no-owner --no-acl \
  -f klc-backup.dump

# 2. Create database on CMC RDS
psql -h <CMC_RDS_HOST> -U postgres -c "CREATE DATABASE \"KLC\";"
psql -h <CMC_RDS_HOST> -U postgres -d KLC -c "CREATE EXTENSION postgis;"

# 3. Restore to CMC RDS
pg_restore -h <CMC_RDS_HOST> -U postgres -d KLC \
  --no-owner --no-acl \
  klc-backup.dump

# 4. Create app user
psql -h <CMC_RDS_HOST> -U postgres -d KLC <<SQL
CREATE USER klc_app WITH PASSWORD '<PASSWORD>';
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO klc_app;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO klc_app;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
  GRANT ALL ON TABLES TO klc_app;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
  GRANT ALL ON SEQUENCES TO klc_app;
SQL

# 5. Verify
psql -h <CMC_RDS_HOST> -U klc_app -d KLC -c "SELECT count(*) FROM \"AppChargingStations\";"
```

### 3.2 Redis

No data migration needed — Redis is used as cache + pub/sub. Data rebuilds automatically on first request.

**CMC Redis setup:**
- Instance: 1 GB, BASIC tier
- Note the host:port for connection string

---

## 4. File Storage Migration

### 4.1 GCS → CMC S3

**ALREADY IMPLEMENTED** — Cloud-agnostic file storage provider system.

Three providers available (config-driven, no code change):
- `GcsFileUploadService` — Google Cloud Storage (default)
- `S3FileUploadService` — S3-compatible (CMC Cloud, AWS, MinIO)
- `LocalFileUploadService` — Local filesystem (development)

**To switch to CMC S3, change config only:**
```json
{
  "FileStorage": {
    "Provider": "s3",
    "S3Endpoint": "https://s3.cmccloud.vn",
    "S3Bucket": "klc-ev-charging-uploads",
    "S3AccessKey": "<CMC_S3_ACCESS_KEY>",
    "S3SecretKey": "<CMC_S3_SECRET_KEY>",
    "S3Region": "ap-southeast-1"
  }
}
```

**Data migration:**
```bash
# Sync files from GCS to CMC S3
gsutil ls gs://klc-ev-charging-uploads/
gsutil -m cp -r gs://klc-ev-charging-uploads/* s3://klc-ev-charging-uploads/
```

**No code changes required.** The provider is selected at startup based on `FileStorage:Provider` config.
```

---

## 5. CI/CD Pipeline

### 5.1 GitHub Actions → CMC Kubernetes

Replace GCP-specific steps with K8s deployment:

```yaml
# .github/workflows/deploy-cmc.yml
name: Deploy to CMC Cloud

on:
  push:
    branches: [main]

env:
  CMC_REGISTRY: registry.cmccloud.vn/klc-ev-charging
  K8S_NAMESPACE: klc-production

jobs:
  detect-changes:
    # Same as current deploy.yml
    ...

  build-and-deploy:
    needs: detect-changes
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      # Login to CMC Container Registry
      - name: Login to CMC Registry
        run: docker login registry.cmccloud.vn -u ${{ secrets.CMC_REGISTRY_USER }} -p ${{ secrets.CMC_REGISTRY_PASS }}

      # Build & push images (same Docker builds, different registry)
      - name: Build Backend API
        if: needs.detect-changes.outputs.backend == 'true'
        run: |
          docker build -t $CMC_REGISTRY/backend-api:${{ github.sha }} -f src/backend/src/KLC.HttpApi.Host/Dockerfile src/backend
          docker push $CMC_REGISTRY/backend-api:${{ github.sha }}

      - name: Build BFF
        if: needs.detect-changes.outputs.backend == 'true'
        run: |
          docker build -t $CMC_REGISTRY/bff-socket:${{ github.sha }} -f src/backend/src/KLC.Driver.BFF/Dockerfile src/backend
          docker push $CMC_REGISTRY/bff-socket:${{ github.sha }}

      - name: Build Admin Portal
        if: needs.detect-changes.outputs.admin-portal == 'true'
        run: |
          docker build -t $CMC_REGISTRY/admin-portal:${{ github.sha }} -f src/admin-portal/Dockerfile src/admin-portal
          docker push $CMC_REGISTRY/admin-portal:${{ github.sha }}

      # Deploy to K8s
      - name: Setup kubectl
        uses: azure/setup-kubectl@v3

      - name: Configure kubeconfig
        run: echo "${{ secrets.CMC_KUBECONFIG }}" | base64 -d > kubeconfig
        env:
          KUBECONFIG: kubeconfig

      - name: Deploy backend services
        if: needs.detect-changes.outputs.backend == 'true'
        run: |
          kubectl set image deployment/admin-api admin-api=$CMC_REGISTRY/backend-api:${{ github.sha }} -n $K8S_NAMESPACE
          kubectl set image deployment/ocpp-gateway ocpp-gateway=$CMC_REGISTRY/backend-api:${{ github.sha }} -n $K8S_NAMESPACE
          kubectl set image deployment/driver-bff driver-bff=$CMC_REGISTRY/bff-socket:${{ github.sha }} -n $K8S_NAMESPACE
          kubectl rollout status deployment/admin-api -n $K8S_NAMESPACE --timeout=120s
          kubectl rollout status deployment/ocpp-gateway -n $K8S_NAMESPACE --timeout=120s
          kubectl rollout status deployment/driver-bff -n $K8S_NAMESPACE --timeout=120s

      - name: Deploy admin portal
        if: needs.detect-changes.outputs.admin-portal == 'true'
        run: |
          kubectl set image deployment/admin-portal admin-portal=$CMC_REGISTRY/admin-portal:${{ github.sha }} -n $K8S_NAMESPACE
          kubectl rollout status deployment/admin-portal -n $K8S_NAMESPACE --timeout=120s

      # DB Migration (manual trigger)
      - name: Run DB Migration
        if: github.event.inputs.run_migrations == 'true'
        run: |
          kubectl run migration --rm -i --restart=Never \
            --image=$CMC_REGISTRY/backend-api:${{ github.sha }} \
            -n $K8S_NAMESPACE \
            --env="ConnectionStrings__Default=${{ secrets.DB_CONNECTION_STRING }}" \
            -- dotnet ef database update \
              --project src/KLC.EntityFrameworkCore \
              --startup-project src/KLC.HttpApi.Host
```

---

## 6. DNS Migration

### 6.1 Domain Configuration

| Domain | Current (GCP) | Target (CMC) |
|--------|---------------|--------------|
| `api.ev.odcall.com` | Cloud Run URL | CMC Load Balancer IP |
| `ocpp.ev.odcall.com` | Cloud Run URL | CMC Load Balancer IP |
| `bff.ev.odcall.com` | Cloud Run URL | CMC Load Balancer IP |
| `ev.odcall.com` | Cloud Run URL | CMC Load Balancer IP |

**Steps:**
1. Deploy all services on CMC Cloud
2. Verify services work via CMC Load Balancer IP
3. Update DNS A records to point to CMC Load Balancer IP
4. Wait for propagation (~5-30 minutes)
5. Verify TLS certificates (Let's Encrypt via cert-manager)

---

## 7. What Stays on Google

| Service | Why |
|---------|-----|
| **Firebase Cloud Messaging** | FCM is Google-only, no CMC equivalent. Push notifications require Firebase. |
| **Firebase Phone Auth** | OTP verification via Firebase SDK. Could replace with Twilio/custom SMS later. |
| **Google Maps API** | Used by mobile app for maps. Could switch to OpenStreetMap/Mapbox later. |

These services are consumed via API keys — no infrastructure dependency. The `firebase-service-account.json` is mounted as a K8s Secret.

---

## 8. Migration Checklist

### Pre-Migration
- [ ] Create CMC Cloud account and project
- [ ] Set up CMC VPC and Security Groups
- [ ] Provision K8s cluster (2 nodes, 2vCPU/4GB each)
- [ ] Provision CMC RDS PostgreSQL 16 with PostGIS
- [ ] Provision CMC Redis 1GB
- [ ] Create CMC S3 bucket (`klc-ev-charging-uploads`)
- [ ] Set up CMC Container Registry
- [ ] Install cert-manager + nginx-ingress on K8s cluster
- [ ] Implement S3FileUploadService (replace GCS)

### Migration Day
- [ ] Put GCP system in maintenance mode
- [ ] pg_dump from Cloud SQL
- [ ] pg_restore to CMC RDS
- [ ] Verify data integrity (row counts match)
- [ ] Sync GCS files to CMC S3
- [ ] Push Docker images to CMC Registry
- [ ] Apply K8s manifests (deployments, services, ingress, secrets)
- [ ] Verify all services healthy (`kubectl get pods`)
- [ ] Test internal connectivity (BFF → Gateway, API → DB, API → Redis)
- [ ] Test OCPP WebSocket (charger connects to new endpoint)
- [ ] Update DNS records
- [ ] Verify TLS certificates
- [ ] Test full charging flow (scan QR → start → meter → stop → payment)
- [ ] Test admin portal (login, stations, sessions, OCPP management)
- [ ] Test mobile app (login, charging, notifications)

### Post-Migration
- [ ] Monitor logs for 24 hours
- [ ] Verify charger reconnection after DNS propagation
- [ ] Set up CMC Cloud Monitoring alerts
- [ ] Update `CLAUDE.md` with CMC-specific commands
- [ ] Decommission GCP resources (after 1 week stability)

---

## 9. Cost Comparison

| Service | GCP Current | CMC Cloud Est. | Saving |
|---------|---:|---:|---:|
| Compute (4 services) | $66/mo | $40-60/mo | ~$10-25 |
| Database (PostgreSQL) | $35/mo | $30-50/mo | ~$0 |
| Redis | $44/mo | $20-30/mo | ~$15-25 |
| Networking (VPC, LB) | $15/mo | $10-15/mo | ~$0-5 |
| Storage + Registry | $1/mo | $1/mo | $0 |
| Firebase (stays on Google) | $0/mo | $0/mo | $0 |
| **Total** | **~$161/mo** | **~$101-156/mo** | **~$5-60/mo** |

*Note: CMC pricing varies by contract. Contact CMC sales for exact quotes.*

---

## 10. Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| PostGIS not supported on CMC RDS | High — spatial queries break | Verify before migration; fallback: self-managed PostgreSQL on Elastic Compute |
| WebSocket stability on K8s Ingress | Medium — charger disconnections | Use nginx-ingress with proper timeout annotations; Redis pub/sub as fallback |
| CMC S3 API compatibility | Low — minor SDK differences | Use standard AWS S3 SDK which works with any S3-compatible storage |
| DNS propagation delay | Low — brief downtime | Keep GCP running in parallel for 24h; use low TTL (60s) before migration |
| CI/CD pipeline differences | Low — different auth mechanism | Test pipeline on staging K8s cluster first |
