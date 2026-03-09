#!/usr/bin/env bash
# =============================================================================
# KLC EV Charging CSMS — GCP Infrastructure Provisioning
# =============================================================================
#
# Provisions production infrastructure on Google Cloud Platform:
#   1. Cloud SQL PostgreSQL 16 (with PostGIS)
#   2. Memorystore Redis 7
#   3. Serverless VPC Connector
#   4. Secret Manager secrets
#   5. IAM bindings
#
# Prerequisites:
#   - gcloud CLI installed and authenticated
#   - Sufficient IAM permissions (Owner or Editor + Secret Manager Admin)
#   - APIs enabled: sqladmin, redis, vpcaccess, secretmanager, run
#
# Usage: ./scripts/provision-gcp-infrastructure.sh
#
# This script is idempotent — it checks whether resources exist before creating
# them. Safe to re-run if a previous execution was interrupted.
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
PROJECT="klc-ev-charging"
REGION="asia-southeast1"

# Cloud SQL
SQL_INSTANCE="klc-postgres"
SQL_TIER="db-f1-micro"
SQL_VERSION="POSTGRES_16"
SQL_STORAGE_SIZE="10"
SQL_DB_NAME="KLC"
SQL_USER="klc_app"
SQL_MAX_CONNECTIONS="100"
SQL_BACKUP_RETENTION="7"

# Memorystore Redis
REDIS_INSTANCE="klc-redis"
REDIS_TIER="BASIC"
REDIS_SIZE_GB="1"
REDIS_VERSION="REDIS_7_0"

# VPC Connector
VPC_CONNECTOR="klc-vpc-connector"
VPC_CONNECTOR_RANGE="10.8.0.0/28"

# Service Accounts
DEPLOY_SA="github-actions-deploy@${PROJECT}.iam.gserviceaccount.com"
BACKEND_SA="klc-backend@${PROJECT}.iam.gserviceaccount.com"

# Cloud Run services (for VPC connector binding)
CLOUD_RUN_SERVICES=("klc-admin-api" "klc-driver-bff")

# ---------------------------------------------------------------------------
# Helper functions
# ---------------------------------------------------------------------------
log() {
  echo "==> $*"
}

error() {
  echo "ERROR: $*" >&2
  exit 1
}

generate_password() {
  local length="${1:-32}"
  LC_ALL=C tr -dc 'A-Za-z0-9!@#%^*_+~' < /dev/urandom | head -c "${length}" || true
}

generate_alphanum() {
  local length="${1:-64}"
  LC_ALL=C tr -dc 'A-Za-z0-9' < /dev/urandom | head -c "${length}" || true
}

resource_exists() {
  # Returns 0 if the gcloud command succeeds (resource exists), 1 otherwise.
  # Usage: resource_exists gcloud sql instances describe ...
  "$@" > /dev/null 2>&1
}

create_or_update_secret() {
  local secret_name="$1"
  local secret_value="$2"

  if resource_exists gcloud secrets describe "${secret_name}" --project="${PROJECT}"; then
    log "Secret '${secret_name}' exists — adding new version"
    printf '%s' "${secret_value}" | gcloud secrets versions add "${secret_name}" \
      --data-file=- \
      --project="${PROJECT}" \
      --quiet
  else
    log "Creating secret '${secret_name}'"
    printf '%s' "${secret_value}" | gcloud secrets create "${secret_name}" \
      --data-file=- \
      --replication-policy="automatic" \
      --project="${PROJECT}" \
      --quiet
  fi
}

# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------
log "Running pre-flight checks..."

if ! command -v gcloud &> /dev/null; then
  error "gcloud CLI is not installed. Install it from https://cloud.google.com/sdk/docs/install"
fi

CURRENT_PROJECT=$(gcloud config get-value project 2>/dev/null || true)
if [[ "${CURRENT_PROJECT}" != "${PROJECT}" ]]; then
  log "Setting active project to ${PROJECT}"
  gcloud config set project "${PROJECT}" --quiet
fi

ACTIVE_ACCOUNT=$(gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>/dev/null || true)
if [[ -z "${ACTIVE_ACCOUNT}" ]]; then
  error "No active gcloud account. Run: gcloud auth login"
fi
log "Authenticated as: ${ACTIVE_ACCOUNT}"

# Enable required APIs
log "Enabling required GCP APIs..."
gcloud services enable \
  sqladmin.googleapis.com \
  redis.googleapis.com \
  vpcaccess.googleapis.com \
  secretmanager.googleapis.com \
  run.googleapis.com \
  servicenetworking.googleapis.com \
  --project="${PROJECT}" \
  --quiet

# ---------------------------------------------------------------------------
# 1. Cloud SQL PostgreSQL 16
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "1. Cloud SQL PostgreSQL 16"
log "============================================"

if resource_exists gcloud sql instances describe "${SQL_INSTANCE}" --project="${PROJECT}"; then
  log "Cloud SQL instance '${SQL_INSTANCE}' already exists — skipping creation"
else
  log "Creating Cloud SQL instance '${SQL_INSTANCE}' (this may take 5-10 minutes)..."
  gcloud sql instances create "${SQL_INSTANCE}" \
    --database-version="${SQL_VERSION}" \
    --tier="${SQL_TIER}" \
    --region="${REGION}" \
    --storage-type=SSD \
    --storage-size="${SQL_STORAGE_SIZE}" \
    --storage-auto-increase \
    --backup-start-time="03:00" \
    --retained-backups-count="${SQL_BACKUP_RETENTION}" \
    --enable-bin-log \
    --database-flags="max_connections=${SQL_MAX_CONNECTIONS}" \
    --assign-ip \
    --network=default \
    --no-assign-ip \
    --project="${PROJECT}" \
    --quiet \
    2>/dev/null || {
      # Fallback: private IP may fail if VPC peering is not set up.
      # Retry with public IP only.
      log "Private IP setup failed — falling back to public IP with authorized networks"
      gcloud sql instances create "${SQL_INSTANCE}" \
        --database-version="${SQL_VERSION}" \
        --tier="${SQL_TIER}" \
        --region="${REGION}" \
        --storage-type=SSD \
        --storage-size="${SQL_STORAGE_SIZE}" \
        --storage-auto-increase \
        --backup-start-time="03:00" \
        --retained-backups-count="${SQL_BACKUP_RETENTION}" \
        --database-flags="max_connections=${SQL_MAX_CONNECTIONS}" \
        --assign-ip \
        --authorized-networks="0.0.0.0/0" \
        --project="${PROJECT}" \
        --quiet
      log "WARNING: Instance created with public IP open to all. Restrict authorized networks after VPC setup."
    }
  log "Cloud SQL instance '${SQL_INSTANCE}' created"
fi

# Generate password for the application user
SQL_PASSWORD=$(generate_password 24)

# Create the database
if gcloud sql databases describe "${SQL_DB_NAME}" --instance="${SQL_INSTANCE}" --project="${PROJECT}" > /dev/null 2>&1; then
  log "Database '${SQL_DB_NAME}' already exists — skipping"
else
  log "Creating database '${SQL_DB_NAME}'..."
  gcloud sql databases create "${SQL_DB_NAME}" \
    --instance="${SQL_INSTANCE}" \
    --charset=UTF8 \
    --collation=en_US.UTF8 \
    --project="${PROJECT}" \
    --quiet
fi

# Create the application user
if gcloud sql users list --instance="${SQL_INSTANCE}" --project="${PROJECT}" --format="value(name)" 2>/dev/null | grep -q "^${SQL_USER}$"; then
  log "SQL user '${SQL_USER}' already exists — updating password"
  gcloud sql users set-password "${SQL_USER}" \
    --instance="${SQL_INSTANCE}" \
    --password="${SQL_PASSWORD}" \
    --project="${PROJECT}" \
    --quiet
else
  log "Creating SQL user '${SQL_USER}'..."
  gcloud sql users create "${SQL_USER}" \
    --instance="${SQL_INSTANCE}" \
    --password="${SQL_PASSWORD}" \
    --project="${PROJECT}" \
    --quiet
fi

# Enable PostGIS extension
# This requires connecting to the database. We use Cloud SQL proxy or gcloud sql connect.
# For now, we document it as a manual step since gcloud sql connect is interactive.
log "NOTE: PostGIS extension must be enabled manually. Connect to the database and run:"
log "  CREATE EXTENSION IF NOT EXISTS postgis;"
log "  CREATE EXTENSION IF NOT EXISTS postgis_topology;"

# Get the Cloud SQL connection name for connection strings
SQL_CONNECTION_NAME=$(gcloud sql instances describe "${SQL_INSTANCE}" \
  --project="${PROJECT}" \
  --format="value(connectionName)" 2>/dev/null || echo "${PROJECT}:${REGION}:${SQL_INSTANCE}")
log "Cloud SQL connection name: ${SQL_CONNECTION_NAME}"

# ---------------------------------------------------------------------------
# 2. Memorystore Redis 7
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "2. Memorystore Redis 7"
log "============================================"

if resource_exists gcloud redis instances describe "${REDIS_INSTANCE}" --region="${REGION}" --project="${PROJECT}"; then
  log "Memorystore Redis instance '${REDIS_INSTANCE}' already exists — skipping creation"
else
  log "Creating Memorystore Redis instance '${REDIS_INSTANCE}' (this may take 3-5 minutes)..."
  gcloud redis instances create "${REDIS_INSTANCE}" \
    --region="${REGION}" \
    --tier="${REDIS_TIER}" \
    --size="${REDIS_SIZE_GB}" \
    --redis-version="${REDIS_VERSION}" \
    --enable-auth \
    --network=default \
    --project="${PROJECT}" \
    --quiet
  log "Memorystore Redis instance '${REDIS_INSTANCE}' created"
fi

# Retrieve Redis host and auth string
REDIS_HOST=$(gcloud redis instances describe "${REDIS_INSTANCE}" \
  --region="${REGION}" \
  --project="${PROJECT}" \
  --format="value(host)" 2>/dev/null || echo "REDIS_HOST_PENDING")

REDIS_PORT=$(gcloud redis instances describe "${REDIS_INSTANCE}" \
  --region="${REGION}" \
  --project="${PROJECT}" \
  --format="value(port)" 2>/dev/null || echo "6379")

REDIS_AUTH=$(gcloud redis instances get-auth-string "${REDIS_INSTANCE}" \
  --region="${REGION}" \
  --project="${PROJECT}" \
  --format="value(authString)" 2>/dev/null || echo "REDIS_AUTH_PENDING")

log "Redis host: ${REDIS_HOST}:${REDIS_PORT}"

# ---------------------------------------------------------------------------
# 3. Serverless VPC Connector
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "3. Serverless VPC Connector"
log "============================================"

if resource_exists gcloud compute networks vpc-access connectors describe "${VPC_CONNECTOR}" --region="${REGION}" --project="${PROJECT}"; then
  log "VPC Connector '${VPC_CONNECTOR}' already exists — skipping creation"
else
  log "Creating Serverless VPC Connector '${VPC_CONNECTOR}'..."
  gcloud compute networks vpc-access connectors create "${VPC_CONNECTOR}" \
    --region="${REGION}" \
    --range="${VPC_CONNECTOR_RANGE}" \
    --network=default \
    --project="${PROJECT}" \
    --quiet
  log "VPC Connector '${VPC_CONNECTOR}' created"
fi

# ---------------------------------------------------------------------------
# 4. Secret Manager
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "4. Secret Manager Secrets"
log "============================================"

# Generate secrets for JWT, encryption, and OIDC
JWT_SECRET=$(generate_alphanum 64)
ENCRYPTION_PASSPHRASE=$(generate_alphanum 32)
OIDC_SECRET=$(generate_alphanum 32)

# Build connection strings
DB_CONNECTION_STRING="Host=/cloudsql/${SQL_CONNECTION_NAME};Database=${SQL_DB_NAME};Username=${SQL_USER};Password=${SQL_PASSWORD}"
REDIS_CONNECTION_STRING="${REDIS_HOST}:${REDIS_PORT},password=${REDIS_AUTH}"
ADMIN_API_URL="https://api.ev.odcall.com"

create_or_update_secret "db-connection-string" "${DB_CONNECTION_STRING}"
create_or_update_secret "redis-connection-string" "${REDIS_CONNECTION_STRING}"
create_or_update_secret "jwt-secret-key" "${JWT_SECRET}"
create_or_update_secret "string-encryption-passphrase" "${ENCRYPTION_PASSPHRASE}"
create_or_update_secret "oidc-client-secret" "${OIDC_SECRET}"
create_or_update_secret "admin-api-url" "${ADMIN_API_URL}"

# ---------------------------------------------------------------------------
# 5. IAM Bindings
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "5. IAM Bindings"
log "============================================"

# Grant klc-backend SA access to Cloud SQL Client
log "Granting Cloud SQL Client role to backend SA..."
gcloud projects add-iam-policy-binding "${PROJECT}" \
  --member="serviceAccount:${BACKEND_SA}" \
  --role="roles/cloudsql.client" \
  --condition=None \
  --quiet > /dev/null 2>&1
log "  roles/cloudsql.client -> ${BACKEND_SA}"

# Grant klc-backend SA access to Secret Manager
log "Granting Secret Manager Accessor role to backend SA..."
gcloud projects add-iam-policy-binding "${PROJECT}" \
  --member="serviceAccount:${BACKEND_SA}" \
  --role="roles/secretmanager.secretAccessor" \
  --condition=None \
  --quiet > /dev/null 2>&1
log "  roles/secretmanager.secretAccessor -> ${BACKEND_SA}"

# Grant deploy SA access to Secret Manager (for CI/CD secret reading)
log "Granting Secret Manager Accessor role to deploy SA..."
gcloud projects add-iam-policy-binding "${PROJECT}" \
  --member="serviceAccount:${DEPLOY_SA}" \
  --role="roles/secretmanager.secretAccessor" \
  --condition=None \
  --quiet > /dev/null 2>&1
log "  roles/secretmanager.secretAccessor -> ${DEPLOY_SA}"

# Grant deploy SA access to VPC connector
log "Granting VPC Access User role to deploy SA..."
gcloud projects add-iam-policy-binding "${PROJECT}" \
  --member="serviceAccount:${DEPLOY_SA}" \
  --role="roles/vpcaccess.user" \
  --condition=None \
  --quiet > /dev/null 2>&1
log "  roles/vpcaccess.user -> ${DEPLOY_SA}"

# Grant backend SA access to Redis (needed for Memorystore from Cloud Run)
log "Granting Redis Editor role to backend SA..."
gcloud projects add-iam-policy-binding "${PROJECT}" \
  --member="serviceAccount:${BACKEND_SA}" \
  --role="roles/redis.editor" \
  --condition=None \
  --quiet > /dev/null 2>&1
log "  roles/redis.editor -> ${BACKEND_SA}"

# ---------------------------------------------------------------------------
# 6. Update Cloud Run services with VPC connector (if they exist)
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "6. Cloud Run VPC Connector Binding"
log "============================================"

for service in "${CLOUD_RUN_SERVICES[@]}"; do
  if gcloud run services describe "${service}" --region="${REGION}" --project="${PROJECT}" > /dev/null 2>&1; then
    log "Updating '${service}' with VPC connector..."
    gcloud run services update "${service}" \
      --region="${REGION}" \
      --vpc-connector="${VPC_CONNECTOR}" \
      --vpc-egress=private-ranges-only \
      --project="${PROJECT}" \
      --quiet
  else
    log "Cloud Run service '${service}' not deployed yet — skipping VPC connector binding"
    log "  (VPC connector will be set during first deployment via deploy flags)"
  fi
done

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
log ""
log "============================================"
log "PROVISIONING COMPLETE"
log "============================================"
log ""
log "Resources created/verified:"
log "  Cloud SQL:       ${SQL_INSTANCE} (${SQL_VERSION}, ${SQL_TIER})"
log "  Connection Name: ${SQL_CONNECTION_NAME}"
log "  Database:        ${SQL_DB_NAME}"
log "  SQL User:        ${SQL_USER}"
log "  Redis:           ${REDIS_INSTANCE} (${REDIS_VERSION}, ${REDIS_HOST}:${REDIS_PORT})"
log "  VPC Connector:   ${VPC_CONNECTOR} (${VPC_CONNECTOR_RANGE})"
log "  Secrets:         db-connection-string, redis-connection-string, jwt-secret-key,"
log "                   string-encryption-passphrase, oidc-client-secret, admin-api-url"
log ""
log "IAM bindings:"
log "  ${BACKEND_SA}:"
log "    - roles/cloudsql.client"
log "    - roles/secretmanager.secretAccessor"
log "    - roles/redis.editor"
log "  ${DEPLOY_SA}:"
log "    - roles/secretmanager.secretAccessor"
log "    - roles/vpcaccess.user"
log ""
log "============================================"
log "NEXT STEPS"
log "============================================"
log ""
log "1. Enable PostGIS extension on the database:"
log "   gcloud sql connect ${SQL_INSTANCE} --user=postgres --project=${PROJECT}"
log "   Then run: CREATE EXTENSION IF NOT EXISTS postgis;"
log "             CREATE EXTENSION IF NOT EXISTS postgis_topology;"
log ""
log "2. Update deploy.yml to include VPC connector flags:"
log "   --vpc-connector=${VPC_CONNECTOR}"
log "   --vpc-egress=private-ranges-only"
log ""
log "3. Update Cloud Run service flags to mount the Cloud SQL connection:"
log "   --add-cloudsql-instances=${SQL_CONNECTION_NAME}"
log ""
log "4. Run domain mappings (if not already done):"
log "   ./scripts/setup-domain-mappings.sh"
log ""
log "5. Restrict Cloud SQL authorized networks (if using public IP fallback):"
log "   gcloud sql instances patch ${SQL_INSTANCE} --authorized-networks='' --project=${PROJECT}"
log ""
log "6. Verify secrets are accessible:"
log "   gcloud secrets versions access latest --secret=db-connection-string --project=${PROJECT}"
log ""
log "IMPORTANT: Generated passwords and secrets have been stored in Secret Manager."
log "           They are NOT printed here for security. Access them via:"
log "           gcloud secrets versions access latest --secret=<SECRET_NAME> --project=${PROJECT}"
