#!/usr/bin/env bash
# =============================================================================
# EV Charging Management System (EVCMS) — DEV Environment Bootstrap
# =============================================================================
#
# Provisions the complete DEV infrastructure on Google Cloud Platform:
#   A. Enable GCP APIs
#   B. Artifact Registry (Docker repo)
#   C. Service Accounts + IAM bindings
#   D. Workload Identity Federation (GitHub Actions OIDC)
#   E. Cloud SQL PostgreSQL 16 (with PostGIS)
#   F. Memorystore Redis 7
#   G. Serverless VPC Connector
#   H. Secret Manager secrets
#   I. Cloud Run services (4 services, placeholder images)
#   J. Cloud Run domain mappings
#   K. OCPP Load Balancer + Cloud Armor
#   L. Cloud DNS records
#   M. Summary output
#
# Prerequisites:
#   - gcloud CLI installed and authenticated
#   - Sufficient IAM permissions (Owner or Editor + Secret Manager Admin)
#   - Domain odcall.com verified in Google Search Console
#   - infra/vendor-allowlist.txt exists with vendor CIDRs
#
# Usage: ./scripts/bootstrap-dev.sh
#
# This script is idempotent — it checks whether resources exist before
# creating them. Safe to re-run if a previous execution was interrupted.
# =============================================================================

set -euo pipefail

# =============================================================================
# Configuration
# =============================================================================
PROJECT_ID="klc-ev-charging"
REGION="asia-southeast1"
ENV="dev"
PREFIX="evcms-dev"
DOMAIN="odcall.com"
DNS_ZONE="odcall-com"
GITHUB_REPO="howard-tech/klc-ev-charging"

# Cloud Run services
SVC_PORTAL="${PREFIX}-admin-portal"
SVC_API="${PREFIX}-backend-api"
SVC_BFF="${PREFIX}-bff-socket"
SVC_OCPP="${PREFIX}-ocpp-gateway"

# Service accounts (short names, without @...iam.gserviceaccount.com)
SA_DEPLOYER="${PREFIX}-deployer"
SA_PORTAL="${PREFIX}-sa-portal"
SA_API="${PREFIX}-sa-api"
SA_BFF="${PREFIX}-sa-bff"
SA_OCPP="${PREFIX}-sa-ocpp"

# Full SA emails
SA_DEPLOYER_EMAIL="${SA_DEPLOYER}@${PROJECT_ID}.iam.gserviceaccount.com"
SA_PORTAL_EMAIL="${SA_PORTAL}@${PROJECT_ID}.iam.gserviceaccount.com"
SA_API_EMAIL="${SA_API}@${PROJECT_ID}.iam.gserviceaccount.com"
SA_BFF_EMAIL="${SA_BFF}@${PROJECT_ID}.iam.gserviceaccount.com"
SA_OCPP_EMAIL="${SA_OCPP}@${PROJECT_ID}.iam.gserviceaccount.com"

# Artifact Registry
AR_REPO="${PREFIX}-ar"

# Cloud SQL
SQL_INSTANCE="${PREFIX}-postgres"
SQL_TIER="db-f1-micro"
SQL_VERSION="POSTGRES_16"
SQL_STORAGE_SIZE="10"
SQL_DB_NAME="klc_dev"
SQL_USER="klc_app"

# Memorystore Redis
REDIS_INSTANCE="${PREFIX}-redis"
REDIS_TIER="BASIC"
REDIS_SIZE_GB="1"
REDIS_VERSION="REDIS_7_0"

# VPC Connector
VPC_CONNECTOR="${PREFIX}-vpc-connector"
VPC_CONNECTOR_RANGE="10.8.0.0/28"

# Workload Identity Federation
WIF_POOL="${PREFIX}-github-pool"
WIF_PROVIDER="${PREFIX}-github-provider"

# Load Balancer / Cloud Armor (OCPP)
LB_IP_NAME="${PREFIX}-ocpp-ip"
NEG_NAME="${PREFIX}-ocpp-neg"
HEALTH_CHECK="${PREFIX}-ocpp-health-check"
BACKEND_SVC="${PREFIX}-ocpp-backend"
URL_MAP="${PREFIX}-ocpp-urlmap"
SSL_CERT="${PREFIX}-ocpp-cert"
HTTPS_PROXY="${PREFIX}-ocpp-https-proxy"
FWD_RULE="${PREFIX}-ocpp-forwarding-rule"
ARMOR_POLICY="${PREFIX}-ocpp-armor"

# Domains
DOMAIN_PORTAL="ev.${DOMAIN}"
DOMAIN_API="api.ev.${DOMAIN}"
DOMAIN_BFF="bff.ev.${DOMAIN}"
DOMAIN_OCPP="ocpp.ev.${DOMAIN}"

# Vendor allowlist file
VENDOR_ALLOWLIST="infra/vendor-allowlist.txt"

# Placeholder image for initial Cloud Run deployment
PLACEHOLDER_IMAGE="gcr.io/cloudrun/hello"

# =============================================================================
# Helper Functions
# =============================================================================

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_step() {
  echo -e "${GREEN}==> $*${NC}"
}

log_info() {
  echo -e "${CYAN}    $*${NC}"
}

log_warn() {
  echo -e "${YELLOW}==> WARNING: $*${NC}"
}

log_error() {
  echo -e "${RED}==> ERROR: $*${NC}" >&2
  exit 1
}

# Check if a resource exists by running a gcloud describe command.
# Usage: resource_exists gcloud sql instances describe INSTANCE ...
resource_exists() {
  "$@" > /dev/null 2>&1
}

# Generate a random alphanumeric string of specified length.
generate_random() {
  local len="${1:-32}"
  openssl rand -base64 48 | tr -dc 'a-zA-Z0-9' | head -c "${len}"
}

# Create a Secret Manager secret if it does not already exist.
# If it exists, skip (do NOT overwrite — idempotent for generated values).
create_secret_if_not_exists() {
  local secret_name="$1"
  local secret_value="$2"

  if resource_exists gcloud secrets describe "${secret_name}" --project="${PROJECT_ID}"; then
    log_info "Secret '${secret_name}' already exists — skipping"
  else
    log_info "Creating secret '${secret_name}'"
    printf '%s' "${secret_value}" | gcloud secrets create "${secret_name}" \
      --data-file=- \
      --replication-policy="automatic" \
      --project="${PROJECT_ID}" \
      --quiet
  fi
}

# Create a service account if it does not already exist.
create_sa_if_not_exists() {
  local sa_name="$1"
  local display_name="$2"

  if resource_exists gcloud iam service-accounts describe "${sa_name}@${PROJECT_ID}.iam.gserviceaccount.com" --project="${PROJECT_ID}"; then
    log_info "Service account '${sa_name}' already exists — skipping"
  else
    log_info "Creating service account '${sa_name}'"
    gcloud iam service-accounts create "${sa_name}" \
      --display-name="${display_name}" \
      --project="${PROJECT_ID}" \
      --quiet
  fi
}

# Grant an IAM role to a service account (project-level binding).
grant_role() {
  local sa_email="$1"
  local role="$2"

  gcloud projects add-iam-policy-binding "${PROJECT_ID}" \
    --member="serviceAccount:${sa_email}" \
    --role="${role}" \
    --condition=None \
    --quiet > /dev/null 2>&1
  log_info "  ${role} -> ${sa_email}"
}

# =============================================================================
# Pre-flight Checks
# =============================================================================
log_step "Running pre-flight checks..."

if ! command -v gcloud &> /dev/null; then
  log_error "gcloud CLI is not installed. Install from https://cloud.google.com/sdk/docs/install"
fi

CURRENT_PROJECT=$(gcloud config get-value project 2>/dev/null || true)
if [[ "${CURRENT_PROJECT}" != "${PROJECT_ID}" ]]; then
  log_step "Setting active project to ${PROJECT_ID}"
  gcloud config set project "${PROJECT_ID}" --quiet
fi

ACTIVE_ACCOUNT=$(gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>/dev/null || true)
if [[ -z "${ACTIVE_ACCOUNT}" ]]; then
  log_error "No active gcloud account. Run: gcloud auth login"
fi
log_info "Authenticated as: ${ACTIVE_ACCOUNT}"
log_info "Project: ${PROJECT_ID}"
log_info "Region: ${REGION}"
log_info "Environment: ${ENV}"

# =============================================================================
# A. Enable APIs
# =============================================================================
log_step ""
log_step "============================================"
log_step "A. Enabling GCP APIs"
log_step "============================================"

APIS=(
  run.googleapis.com
  sqladmin.googleapis.com
  artifactregistry.googleapis.com
  secretmanager.googleapis.com
  iam.googleapis.com
  dns.googleapis.com
  compute.googleapis.com
  certificatemanager.googleapis.com
  vpcaccess.googleapis.com
  redis.googleapis.com
  logging.googleapis.com
  monitoring.googleapis.com
  iamcredentials.googleapis.com
  sts.googleapis.com
)

log_info "Enabling ${#APIS[@]} APIs (this may take a minute)..."
gcloud services enable "${APIS[@]}" \
  --project="${PROJECT_ID}" \
  --quiet
log_info "All APIs enabled"

# =============================================================================
# B. Artifact Registry
# =============================================================================
log_step ""
log_step "============================================"
log_step "B. Artifact Registry"
log_step "============================================"

if resource_exists gcloud artifacts repositories describe "${AR_REPO}" \
    --location="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Artifact Registry repo '${AR_REPO}' already exists — skipping"
else
  log_info "Creating Docker repository '${AR_REPO}' in ${REGION}..."
  gcloud artifacts repositories create "${AR_REPO}" \
    --repository-format=docker \
    --location="${REGION}" \
    --description="EVCMS DEV Docker images" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Artifact Registry repo '${AR_REPO}' created"
fi

AR_REGISTRY="${REGION}-docker.pkg.dev/${PROJECT_ID}/${AR_REPO}"
log_info "Registry URL: ${AR_REGISTRY}"

# =============================================================================
# C. Service Accounts + IAM
# =============================================================================
log_step ""
log_step "============================================"
log_step "C. Service Accounts + IAM Bindings"
log_step "============================================"

# Create service accounts
log_step "Creating service accounts..."
create_sa_if_not_exists "${SA_DEPLOYER}" "EVCMS DEV Deployer (CI/CD)"
create_sa_if_not_exists "${SA_PORTAL}"   "EVCMS DEV Admin Portal"
create_sa_if_not_exists "${SA_API}"      "EVCMS DEV Backend API"
create_sa_if_not_exists "${SA_BFF}"      "EVCMS DEV BFF Socket"
create_sa_if_not_exists "${SA_OCPP}"     "EVCMS DEV OCPP Gateway"

# Grant IAM roles
log_step "Granting IAM roles..."

# Deployer: CI/CD pipeline
log_info "Deployer roles:"
grant_role "${SA_DEPLOYER_EMAIL}" "roles/run.admin"
grant_role "${SA_DEPLOYER_EMAIL}" "roles/artifactregistry.writer"
grant_role "${SA_DEPLOYER_EMAIL}" "roles/iam.serviceAccountUser"
grant_role "${SA_DEPLOYER_EMAIL}" "roles/secretmanager.secretAccessor"

# Backend API: full access to SQL, secrets, storage
log_info "Backend API roles:"
grant_role "${SA_API_EMAIL}" "roles/cloudsql.client"
grant_role "${SA_API_EMAIL}" "roles/secretmanager.secretAccessor"
grant_role "${SA_API_EMAIL}" "roles/storage.objectAdmin"

# BFF Socket: SQL, secrets, read-only storage
log_info "BFF Socket roles:"
grant_role "${SA_BFF_EMAIL}" "roles/cloudsql.client"
grant_role "${SA_BFF_EMAIL}" "roles/secretmanager.secretAccessor"
grant_role "${SA_BFF_EMAIL}" "roles/storage.objectViewer"

# OCPP Gateway: SQL and secrets
log_info "OCPP Gateway roles:"
grant_role "${SA_OCPP_EMAIL}" "roles/cloudsql.client"
grant_role "${SA_OCPP_EMAIL}" "roles/secretmanager.secretAccessor"

# Admin Portal: secrets only
log_info "Admin Portal roles:"
grant_role "${SA_PORTAL_EMAIL}" "roles/secretmanager.secretAccessor"

# =============================================================================
# D. Workload Identity Federation
# =============================================================================
log_step ""
log_step "============================================"
log_step "D. Workload Identity Federation"
log_step "============================================"

PROJECT_NUMBER=$(gcloud projects describe "${PROJECT_ID}" --format="value(projectNumber)")
WIF_POOL_FULL="projects/${PROJECT_NUMBER}/locations/global/workloadIdentityPools/${WIF_POOL}"
WIF_PROVIDER_FULL="${WIF_POOL_FULL}/providers/${WIF_PROVIDER}"

# Create WIF pool
if resource_exists gcloud iam workload-identity-pools describe "${WIF_POOL}" \
    --location="global" --project="${PROJECT_ID}"; then
  log_info "WIF pool '${WIF_POOL}' already exists — skipping"
else
  log_info "Creating Workload Identity Pool '${WIF_POOL}'..."
  gcloud iam workload-identity-pools create "${WIF_POOL}" \
    --location="global" \
    --display-name="EVCMS DEV GitHub Actions Pool" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "WIF pool created"
fi

# Create WIF provider
if resource_exists gcloud iam workload-identity-pools providers describe "${WIF_PROVIDER}" \
    --workload-identity-pool="${WIF_POOL}" --location="global" --project="${PROJECT_ID}"; then
  log_info "WIF provider '${WIF_PROVIDER}' already exists — skipping"
else
  log_info "Creating Workload Identity Provider '${WIF_PROVIDER}'..."
  gcloud iam workload-identity-pools providers create-oidc "${WIF_PROVIDER}" \
    --workload-identity-pool="${WIF_POOL}" \
    --location="global" \
    --issuer-uri="https://token.actions.githubusercontent.com" \
    --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "WIF provider created"
fi

# Bind deployer SA to the WIF pool with repository condition
log_info "Binding deployer SA to WIF pool (repo: ${GITHUB_REPO})..."
gcloud iam service-accounts add-iam-policy-binding "${SA_DEPLOYER_EMAIL}" \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/${WIF_POOL_FULL}/attribute.repository/${GITHUB_REPO}" \
  --project="${PROJECT_ID}" \
  --quiet > /dev/null 2>&1

log_info "WIF provider full name:"
log_info "  ${WIF_PROVIDER_FULL}"

# =============================================================================
# E. Cloud SQL PostgreSQL
# =============================================================================
log_step ""
log_step "============================================"
log_step "E. Cloud SQL PostgreSQL 16"
log_step "============================================"

# Generate database password
DB_PASSWORD=$(generate_random 32)

if resource_exists gcloud sql instances describe "${SQL_INSTANCE}" --project="${PROJECT_ID}"; then
  log_info "Cloud SQL instance '${SQL_INSTANCE}' already exists — skipping creation"
else
  log_step "Creating Cloud SQL instance '${SQL_INSTANCE}' (this takes 5-10 minutes)..."
  gcloud sql instances create "${SQL_INSTANCE}" \
    --database-version="${SQL_VERSION}" \
    --tier="${SQL_TIER}" \
    --region="${REGION}" \
    --storage-type=SSD \
    --storage-size="${SQL_STORAGE_SIZE}" \
    --storage-auto-increase \
    --backup-start-time="03:00" \
    --retained-backups-count=7 \
    --database-flags="max_connections=100" \
    --assign-ip \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Cloud SQL instance '${SQL_INSTANCE}' created"
fi

# Get the Cloud SQL connection name
SQL_CONNECTION_NAME=$(gcloud sql instances describe "${SQL_INSTANCE}" \
  --project="${PROJECT_ID}" \
  --format="value(connectionName)" 2>/dev/null || echo "${PROJECT_ID}:${REGION}:${SQL_INSTANCE}")
log_info "Cloud SQL connection name: ${SQL_CONNECTION_NAME}"

# Create the database
if gcloud sql databases describe "${SQL_DB_NAME}" \
    --instance="${SQL_INSTANCE}" --project="${PROJECT_ID}" > /dev/null 2>&1; then
  log_info "Database '${SQL_DB_NAME}' already exists — skipping"
else
  log_info "Creating database '${SQL_DB_NAME}'..."
  gcloud sql databases create "${SQL_DB_NAME}" \
    --instance="${SQL_INSTANCE}" \
    --charset=UTF8 \
    --collation=en_US.UTF8 \
    --project="${PROJECT_ID}" \
    --quiet
fi

# Create or update the application user
if gcloud sql users list --instance="${SQL_INSTANCE}" --project="${PROJECT_ID}" \
    --format="value(name)" 2>/dev/null | grep -q "^${SQL_USER}$"; then
  log_info "SQL user '${SQL_USER}' already exists — updating password"
  gcloud sql users set-password "${SQL_USER}" \
    --instance="${SQL_INSTANCE}" \
    --password="${DB_PASSWORD}" \
    --project="${PROJECT_ID}" \
    --quiet
else
  log_info "Creating SQL user '${SQL_USER}'..."
  gcloud sql users create "${SQL_USER}" \
    --instance="${SQL_INSTANCE}" \
    --password="${DB_PASSWORD}" \
    --project="${PROJECT_ID}" \
    --quiet
fi

# Enable PostGIS extension via gcloud sql connect
log_info "Enabling PostGIS extension..."
gcloud sql connect "${SQL_INSTANCE}" --user=postgres --project="${PROJECT_ID}" --quiet \
  <<< "CREATE EXTENSION IF NOT EXISTS postgis;" 2>/dev/null || {
  log_warn "Could not enable PostGIS automatically. Enable it manually:"
  log_warn "  gcloud sql connect ${SQL_INSTANCE} --user=postgres --project=${PROJECT_ID}"
  log_warn "  CREATE EXTENSION IF NOT EXISTS postgis;"
}

# Build connection string
DB_CONNECTION_STRING="Host=/cloudsql/${SQL_CONNECTION_NAME};Database=${SQL_DB_NAME};Username=${SQL_USER};Password=${DB_PASSWORD}"

# =============================================================================
# F. Memorystore Redis
# =============================================================================
log_step ""
log_step "============================================"
log_step "F. Memorystore Redis 7"
log_step "============================================"

if resource_exists gcloud redis instances describe "${REDIS_INSTANCE}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Memorystore Redis instance '${REDIS_INSTANCE}' already exists — skipping creation"
else
  log_step "Creating Memorystore Redis instance '${REDIS_INSTANCE}' (this takes 3-5 minutes)..."
  gcloud redis instances create "${REDIS_INSTANCE}" \
    --region="${REGION}" \
    --tier="${REDIS_TIER}" \
    --size="${REDIS_SIZE_GB}" \
    --redis-version="${REDIS_VERSION}" \
    --enable-auth \
    --network=default \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Memorystore Redis instance '${REDIS_INSTANCE}' created"
fi

# Retrieve Redis connection info
REDIS_HOST=$(gcloud redis instances describe "${REDIS_INSTANCE}" \
  --region="${REGION}" --project="${PROJECT_ID}" \
  --format="value(host)" 2>/dev/null || echo "REDIS_HOST_PENDING")

REDIS_PORT=$(gcloud redis instances describe "${REDIS_INSTANCE}" \
  --region="${REGION}" --project="${PROJECT_ID}" \
  --format="value(port)" 2>/dev/null || echo "6379")

REDIS_AUTH_STRING=$(gcloud redis instances get-auth-string "${REDIS_INSTANCE}" \
  --region="${REGION}" --project="${PROJECT_ID}" \
  --format="value(authString)" 2>/dev/null || echo "REDIS_AUTH_PENDING")

log_info "Redis host: ${REDIS_HOST}:${REDIS_PORT}"

REDIS_CONNECTION_STRING="${REDIS_HOST}:${REDIS_PORT},password=${REDIS_AUTH_STRING}"

# =============================================================================
# G. VPC Connector
# =============================================================================
log_step ""
log_step "============================================"
log_step "G. Serverless VPC Connector"
log_step "============================================"

if resource_exists gcloud compute networks vpc-access connectors describe "${VPC_CONNECTOR}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "VPC Connector '${VPC_CONNECTOR}' already exists — skipping"
else
  log_info "Creating Serverless VPC Connector '${VPC_CONNECTOR}'..."
  gcloud compute networks vpc-access connectors create "${VPC_CONNECTOR}" \
    --region="${REGION}" \
    --range="${VPC_CONNECTOR_RANGE}" \
    --network=default \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "VPC Connector '${VPC_CONNECTOR}' created"
fi

# =============================================================================
# H. Secret Manager
# =============================================================================
log_step ""
log_step "============================================"
log_step "H. Secret Manager Secrets"
log_step "============================================"

# Generate random secrets
JWT_SECRET=$(generate_random 64)
ENCRYPTION_PASSPHRASE=$(generate_random 32)
OIDC_CLIENT_SECRET=$(generate_random 32)
OCPP_AUTH_KEY=$(generate_random 32)

# Create secrets (idempotent — skips if already exists)
create_secret_if_not_exists "${PREFIX}-db-connection-string"          "${DB_CONNECTION_STRING}"
create_secret_if_not_exists "${PREFIX}-db-password"                   "${DB_PASSWORD}"
create_secret_if_not_exists "${PREFIX}-jwt-secret-key"                "${JWT_SECRET}"
create_secret_if_not_exists "${PREFIX}-string-encryption-passphrase"  "${ENCRYPTION_PASSPHRASE}"
create_secret_if_not_exists "${PREFIX}-oidc-client-secret"            "${OIDC_CLIENT_SECRET}"
create_secret_if_not_exists "${PREFIX}-redis-auth-string"             "${REDIS_AUTH_STRING}"
create_secret_if_not_exists "${PREFIX}-ocpp-auth-key"                 "${OCPP_AUTH_KEY}"
create_secret_if_not_exists "${PREFIX}-momo-partner-code"             "CHANGE_ME"
create_secret_if_not_exists "${PREFIX}-momo-access-key"               "CHANGE_ME"
create_secret_if_not_exists "${PREFIX}-momo-secret-key"               "CHANGE_ME"
create_secret_if_not_exists "${PREFIX}-vnpay-tmn-code"                "CHANGE_ME"
create_secret_if_not_exists "${PREFIX}-vnpay-hash-secret"             "CHANGE_ME"

log_info "All 12 secrets created/verified"

# =============================================================================
# I. Deploy Cloud Run Services (placeholder images)
# =============================================================================
log_step ""
log_step "============================================"
log_step "I. Cloud Run Services (placeholder deploys)"
log_step "============================================"

# --- I.1 Admin Portal ---
log_step "Deploying ${SVC_PORTAL}..."
if resource_exists gcloud run services describe "${SVC_PORTAL}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Service '${SVC_PORTAL}' already exists — skipping"
else
  gcloud run deploy "${SVC_PORTAL}" \
    --image="${PLACEHOLDER_IMAGE}" \
    --region="${REGION}" \
    --platform=managed \
    --service-account="${SA_PORTAL_EMAIL}" \
    --port=3000 \
    --vpc-connector="${VPC_CONNECTOR}" \
    --vpc-egress=private-ranges-only \
    --min-instances=0 \
    --max-instances=3 \
    --memory=512Mi \
    --cpu=1 \
    --allow-unauthenticated \
    --set-secrets="OIDC_CLIENT_SECRET=${PREFIX}-oidc-client-secret:latest" \
    --set-env-vars="NODE_ENV=production,BACKEND_API_URL=https://${DOMAIN_API}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Service '${SVC_PORTAL}' deployed"
fi

# --- I.2 Backend API ---
log_step "Deploying ${SVC_API}..."
if resource_exists gcloud run services describe "${SVC_API}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Service '${SVC_API}' already exists — skipping"
else
  gcloud run deploy "${SVC_API}" \
    --image="${PLACEHOLDER_IMAGE}" \
    --region="${REGION}" \
    --platform=managed \
    --service-account="${SA_API_EMAIL}" \
    --port=8080 \
    --vpc-connector="${VPC_CONNECTOR}" \
    --vpc-egress=private-ranges-only \
    --min-instances=0 \
    --max-instances=5 \
    --memory=1Gi \
    --cpu=1 \
    --allow-unauthenticated \
    --set-secrets="\
ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,\
ConnectionStrings__Redis=${PREFIX}-redis-auth-string:latest,\
Jwt__SecretKey=${PREFIX}-jwt-secret-key:latest,\
StringEncryption__DefaultPassPhrase=${PREFIX}-string-encryption-passphrase:latest,\
Payment__MoMo__PartnerCode=${PREFIX}-momo-partner-code:latest,\
Payment__MoMo__AccessKey=${PREFIX}-momo-access-key:latest,\
Payment__MoMo__SecretKey=${PREFIX}-momo-secret-key:latest,\
Payment__VnPay__TmnCode=${PREFIX}-vnpay-tmn-code:latest,\
Payment__VnPay__HashSecret=${PREFIX}-vnpay-hash-secret:latest" \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Development" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Service '${SVC_API}' deployed"
fi

# --- I.3 BFF Socket ---
log_step "Deploying ${SVC_BFF}..."
if resource_exists gcloud run services describe "${SVC_BFF}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Service '${SVC_BFF}' already exists — skipping"
else
  gcloud run deploy "${SVC_BFF}" \
    --image="${PLACEHOLDER_IMAGE}" \
    --region="${REGION}" \
    --platform=managed \
    --service-account="${SA_BFF_EMAIL}" \
    --port=8080 \
    --vpc-connector="${VPC_CONNECTOR}" \
    --vpc-egress=private-ranges-only \
    --min-instances=0 \
    --max-instances=5 \
    --memory=1Gi \
    --cpu=1 \
    --allow-unauthenticated \
    --set-secrets="\
ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,\
ConnectionStrings__Redis=${PREFIX}-redis-auth-string:latest,\
Jwt__SecretKey=${PREFIX}-jwt-secret-key:latest,\
StringEncryption__DefaultPassPhrase=${PREFIX}-string-encryption-passphrase:latest,\
Payment__MoMo__PartnerCode=${PREFIX}-momo-partner-code:latest,\
Payment__MoMo__AccessKey=${PREFIX}-momo-access-key:latest,\
Payment__MoMo__SecretKey=${PREFIX}-momo-secret-key:latest,\
Payment__VnPay__TmnCode=${PREFIX}-vnpay-tmn-code:latest,\
Payment__VnPay__HashSecret=${PREFIX}-vnpay-hash-secret:latest" \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Development" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Service '${SVC_BFF}' deployed"
fi

# --- I.4 OCPP Gateway ---
log_step "Deploying ${SVC_OCPP}..."
if resource_exists gcloud run services describe "${SVC_OCPP}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Service '${SVC_OCPP}' already exists — skipping"
else
  gcloud run deploy "${SVC_OCPP}" \
    --image="${PLACEHOLDER_IMAGE}" \
    --region="${REGION}" \
    --platform=managed \
    --service-account="${SA_OCPP_EMAIL}" \
    --port=8080 \
    --vpc-connector="${VPC_CONNECTOR}" \
    --vpc-egress=private-ranges-only \
    --timeout=3600 \
    --session-affinity \
    --min-instances=1 \
    --max-instances=5 \
    --memory=1Gi \
    --cpu=1 \
    --allow-unauthenticated \
    --set-secrets="\
ConnectionStrings__Default=${PREFIX}-db-connection-string:latest,\
ConnectionStrings__Redis=${PREFIX}-redis-auth-string:latest,\
Jwt__SecretKey=${PREFIX}-jwt-secret-key:latest,\
StringEncryption__DefaultPassPhrase=${PREFIX}-string-encryption-passphrase:latest,\
Payment__MoMo__PartnerCode=${PREFIX}-momo-partner-code:latest,\
Payment__MoMo__AccessKey=${PREFIX}-momo-access-key:latest,\
Payment__MoMo__SecretKey=${PREFIX}-momo-secret-key:latest,\
Payment__VnPay__TmnCode=${PREFIX}-vnpay-tmn-code:latest,\
Payment__VnPay__HashSecret=${PREFIX}-vnpay-hash-secret:latest,\
Ocpp__AuthKey=${PREFIX}-ocpp-auth-key:latest" \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Development" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Service '${SVC_OCPP}' deployed"
fi

# =============================================================================
# J. Cloud Run Domain Mappings
# =============================================================================
log_step ""
log_step "============================================"
log_step "J. Cloud Run Domain Mappings"
log_step "============================================"

# Map ev.odcall.com -> admin-portal
if gcloud beta run domain-mappings describe --domain="${DOMAIN_PORTAL}" \
    --region="${REGION}" --project="${PROJECT_ID}" > /dev/null 2>&1; then
  log_info "Domain mapping '${DOMAIN_PORTAL}' already exists — skipping"
else
  log_info "Mapping ${DOMAIN_PORTAL} -> ${SVC_PORTAL}"
  gcloud beta run domain-mappings create \
    --service="${SVC_PORTAL}" \
    --domain="${DOMAIN_PORTAL}" \
    --region="${REGION}" \
    --project="${PROJECT_ID}" \
    --quiet
fi

# Map api.ev.odcall.com -> backend-api
if gcloud beta run domain-mappings describe --domain="${DOMAIN_API}" \
    --region="${REGION}" --project="${PROJECT_ID}" > /dev/null 2>&1; then
  log_info "Domain mapping '${DOMAIN_API}' already exists — skipping"
else
  log_info "Mapping ${DOMAIN_API} -> ${SVC_API}"
  gcloud beta run domain-mappings create \
    --service="${SVC_API}" \
    --domain="${DOMAIN_API}" \
    --region="${REGION}" \
    --project="${PROJECT_ID}" \
    --quiet
fi

# Map bff.ev.odcall.com -> bff-socket
if gcloud beta run domain-mappings describe --domain="${DOMAIN_BFF}" \
    --region="${REGION}" --project="${PROJECT_ID}" > /dev/null 2>&1; then
  log_info "Domain mapping '${DOMAIN_BFF}' already exists — skipping"
else
  log_info "Mapping ${DOMAIN_BFF} -> ${SVC_BFF}"
  gcloud beta run domain-mappings create \
    --service="${SVC_BFF}" \
    --domain="${DOMAIN_BFF}" \
    --region="${REGION}" \
    --project="${PROJECT_ID}" \
    --quiet
fi

# NOTE: OCPP uses a dedicated LB (section K), not a Cloud Run domain mapping.

# =============================================================================
# K. OCPP Load Balancer + Cloud Armor
# =============================================================================
log_step ""
log_step "============================================"
log_step "K. OCPP Load Balancer + Cloud Armor"
log_step "============================================"

# K.1 Reserve global static IP
log_step "K.1 Reserving global static IP '${LB_IP_NAME}'..."
if resource_exists gcloud compute addresses describe "${LB_IP_NAME}" --global --project="${PROJECT_ID}"; then
  log_info "Static IP '${LB_IP_NAME}' already exists — skipping"
else
  gcloud compute addresses create "${LB_IP_NAME}" \
    --global \
    --ip-version=IPV4 \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Static IP '${LB_IP_NAME}' reserved"
fi

OCPP_STATIC_IP=$(gcloud compute addresses describe "${LB_IP_NAME}" \
  --global --project="${PROJECT_ID}" \
  --format="value(address)" 2>/dev/null || echo "IP_PENDING")
log_info "OCPP static IP: ${OCPP_STATIC_IP}"

# K.2 Create serverless NEG
log_step "K.2 Creating serverless NEG '${NEG_NAME}'..."
if resource_exists gcloud compute network-endpoint-groups describe "${NEG_NAME}" \
    --region="${REGION}" --project="${PROJECT_ID}"; then
  log_info "Serverless NEG '${NEG_NAME}' already exists — skipping"
else
  gcloud compute network-endpoint-groups create "${NEG_NAME}" \
    --region="${REGION}" \
    --network-endpoint-type=serverless \
    --cloud-run-service="${SVC_OCPP}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Serverless NEG '${NEG_NAME}' created"
fi

# K.3 Create health check
log_step "K.3 Creating health check '${HEALTH_CHECK}'..."
if resource_exists gcloud compute health-checks describe "${HEALTH_CHECK}" --project="${PROJECT_ID}"; then
  log_info "Health check '${HEALTH_CHECK}' already exists — skipping"
else
  gcloud compute health-checks create http "${HEALTH_CHECK}" \
    --port=8080 \
    --request-path="/health" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Health check '${HEALTH_CHECK}' created"
fi

# K.4 Create backend service
log_step "K.4 Creating backend service '${BACKEND_SVC}'..."
if resource_exists gcloud compute backend-services describe "${BACKEND_SVC}" --global --project="${PROJECT_ID}"; then
  log_info "Backend service '${BACKEND_SVC}' already exists — skipping"
else
  gcloud compute backend-services create "${BACKEND_SVC}" \
    --global \
    --load-balancing-scheme=EXTERNAL_MANAGED \
    --protocol=HTTP2 \
    --timeout-sec=3600 \
    --session-affinity=CLIENT_IP \
    --health-checks="${HEALTH_CHECK}" \
    --enable-logging \
    --logging-sample-rate=1.0 \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Backend service '${BACKEND_SVC}' created"

  # Add NEG to backend service
  log_info "Adding NEG '${NEG_NAME}' to backend service..."
  gcloud compute backend-services add-backend "${BACKEND_SVC}" \
    --global \
    --network-endpoint-group="${NEG_NAME}" \
    --network-endpoint-group-region="${REGION}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "NEG added to backend service"
fi

# K.5 Create URL map
log_step "K.5 Creating URL map '${URL_MAP}'..."
if resource_exists gcloud compute url-maps describe "${URL_MAP}" --project="${PROJECT_ID}"; then
  log_info "URL map '${URL_MAP}' already exists — skipping"
else
  gcloud compute url-maps create "${URL_MAP}" \
    --default-service="${BACKEND_SVC}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "URL map '${URL_MAP}' created"
fi

# K.6 Create managed SSL certificate
log_step "K.6 Creating managed SSL certificate '${SSL_CERT}'..."
if resource_exists gcloud compute ssl-certificates describe "${SSL_CERT}" --project="${PROJECT_ID}"; then
  log_info "SSL certificate '${SSL_CERT}' already exists — skipping"
else
  gcloud compute ssl-certificates create "${SSL_CERT}" \
    --domains="${DOMAIN_OCPP}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "SSL certificate '${SSL_CERT}' created (provisioning may take 15-60 minutes)"
fi

# K.7 Create target HTTPS proxy
log_step "K.7 Creating HTTPS proxy '${HTTPS_PROXY}'..."
if resource_exists gcloud compute target-https-proxies describe "${HTTPS_PROXY}" --project="${PROJECT_ID}"; then
  log_info "HTTPS proxy '${HTTPS_PROXY}' already exists — skipping"
else
  gcloud compute target-https-proxies create "${HTTPS_PROXY}" \
    --url-map="${URL_MAP}" \
    --ssl-certificates="${SSL_CERT}" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "HTTPS proxy '${HTTPS_PROXY}' created"
fi

# K.8 Create forwarding rule
log_step "K.8 Creating forwarding rule '${FWD_RULE}'..."
if resource_exists gcloud compute forwarding-rules describe "${FWD_RULE}" --global --project="${PROJECT_ID}"; then
  log_info "Forwarding rule '${FWD_RULE}' already exists — skipping"
else
  gcloud compute forwarding-rules create "${FWD_RULE}" \
    --global \
    --address="${LB_IP_NAME}" \
    --target-https-proxy="${HTTPS_PROXY}" \
    --ports=443 \
    --load-balancing-scheme=EXTERNAL_MANAGED \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Forwarding rule '${FWD_RULE}' created"
fi

# K.9 Create Cloud Armor policy
log_step "K.9 Creating Cloud Armor policy '${ARMOR_POLICY}'..."

# Read vendor CIDRs from allowlist file
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "${SCRIPT_DIR}")"
ALLOWLIST_PATH="${PROJECT_ROOT}/${VENDOR_ALLOWLIST}"

if [[ ! -f "${ALLOWLIST_PATH}" ]]; then
  log_warn "Vendor allowlist file not found at ${ALLOWLIST_PATH}"
  log_warn "Using 0.0.0.0/0 as fallback (allow all). Update ${VENDOR_ALLOWLIST} and re-run."
  VENDOR_CIDRS="0.0.0.0/0"
else
  VENDOR_CIDRS=$(grep -v '^#' "${ALLOWLIST_PATH}" | grep -v '^$' | paste -sd, - || echo "0.0.0.0/0")
  if [[ -z "${VENDOR_CIDRS}" ]]; then
    log_warn "Vendor allowlist is empty. Using 0.0.0.0/0 as fallback."
    VENDOR_CIDRS="0.0.0.0/0"
  fi
fi
log_info "Vendor CIDRs: ${VENDOR_CIDRS}"

if resource_exists gcloud compute security-policies describe "${ARMOR_POLICY}" --project="${PROJECT_ID}"; then
  log_info "Cloud Armor policy '${ARMOR_POLICY}' already exists — skipping creation"
else
  # Create the security policy
  gcloud compute security-policies create "${ARMOR_POLICY}" \
    --description="EVCMS DEV OCPP endpoint protection" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "Cloud Armor policy '${ARMOR_POLICY}' created"

  # Rule 1000: Allow vendor CIDRs
  log_info "Adding rule 1000: allow vendor CIDRs"
  gcloud compute security-policies rules create 1000 \
    --security-policy="${ARMOR_POLICY}" \
    --src-ip-ranges="${VENDOR_CIDRS}" \
    --action=allow \
    --description="Allow OCPP charger vendor IPs" \
    --project="${PROJECT_ID}" \
    --quiet

  # Default rule (priority 2147483647): deny all
  log_info "Updating default rule: deny 403"
  gcloud compute security-policies rules update 2147483647 \
    --security-policy="${ARMOR_POLICY}" \
    --action=deny-403 \
    --description="Default deny" \
    --project="${PROJECT_ID}" \
    --quiet
fi

# K.10 Attach Cloud Armor to backend service
log_step "K.10 Attaching Cloud Armor to backend service..."
gcloud compute backend-services update "${BACKEND_SVC}" \
  --global \
  --security-policy="${ARMOR_POLICY}" \
  --project="${PROJECT_ID}" \
  --quiet
log_info "Cloud Armor policy '${ARMOR_POLICY}' attached to '${BACKEND_SVC}'"

# =============================================================================
# L. Cloud DNS Records
# =============================================================================
log_step ""
log_step "============================================"
log_step "L. Cloud DNS Records"
log_step "============================================"

# Check if the DNS zone exists
if ! resource_exists gcloud dns managed-zones describe "${DNS_ZONE}" --project="${PROJECT_ID}"; then
  log_warn "Cloud DNS zone '${DNS_ZONE}' does not exist. Creating it..."
  gcloud dns managed-zones create "${DNS_ZONE}" \
    --dns-name="${DOMAIN}." \
    --description="EVCMS domain zone" \
    --project="${PROJECT_ID}" \
    --quiet
  log_info "DNS zone '${DNS_ZONE}' created"
  log_warn "You must update your domain registrar NS records to point to Google Cloud DNS."
  log_warn "Get the NS records with: gcloud dns managed-zones describe ${DNS_ZONE} --project=${PROJECT_ID}"
fi

# Helper: add or update a DNS record via transaction
add_dns_record() {
  local record_name="$1"
  local record_type="$2"
  local record_value="$3"
  local ttl="${4:-300}"

  log_info "Setting ${record_type} record: ${record_name} -> ${record_value}"

  # Start a transaction, remove old record (if exists), add new, execute
  gcloud dns record-sets transaction start \
    --zone="${DNS_ZONE}" --project="${PROJECT_ID}" --quiet 2>/dev/null || true

  # Try to remove existing record (ignore errors if it doesn't exist)
  gcloud dns record-sets transaction remove \
    --zone="${DNS_ZONE}" \
    --name="${record_name}" \
    --type="${record_type}" \
    --ttl="${ttl}" \
    "${record_value}" \
    --project="${PROJECT_ID}" \
    --quiet 2>/dev/null || true

  gcloud dns record-sets transaction add \
    --zone="${DNS_ZONE}" \
    --name="${record_name}" \
    --type="${record_type}" \
    --ttl="${ttl}" \
    "${record_value}" \
    --project="${PROJECT_ID}" \
    --quiet

  gcloud dns record-sets transaction execute \
    --zone="${DNS_ZONE}" --project="${PROJECT_ID}" --quiet 2>/dev/null || {
    # If transaction fails (e.g., record already exists), abort and try direct approach
    gcloud dns record-sets transaction abort \
      --zone="${DNS_ZONE}" --project="${PROJECT_ID}" --quiet 2>/dev/null || true
    log_warn "DNS transaction failed for ${record_name}. Record may already exist."
  }
}

# OCPP subdomain: A record pointing to the LB static IP
add_dns_record "${DOMAIN_OCPP}." "A" "${OCPP_STATIC_IP}"

# Cloud Run domain mappings use CNAME to ghs.googlehosted.com.
add_dns_record "${DOMAIN_PORTAL}." "CNAME" "ghs.googlehosted.com."
add_dns_record "${DOMAIN_API}."    "CNAME" "ghs.googlehosted.com."
add_dns_record "${DOMAIN_BFF}."    "CNAME" "ghs.googlehosted.com."

log_info "DNS records configured"

# =============================================================================
# M. Summary Output
# =============================================================================
log_step ""
echo ""
echo -e "${GREEN}+===================================================================+${NC}"
echo -e "${GREEN}|                                                                   |${NC}"
echo -e "${GREEN}|   EVCMS DEV ENVIRONMENT — BOOTSTRAP COMPLETE                      |${NC}"
echo -e "${GREEN}|                                                                   |${NC}"
echo -e "${GREEN}+===================================================================+${NC}"
echo ""
echo -e "${CYAN}  Service URLs:${NC}"
echo "    Admin Portal:  https://${DOMAIN_PORTAL}"
echo "    Backend API:   https://${DOMAIN_API}"
echo "    Driver BFF:    https://${DOMAIN_BFF}"
echo "    OCPP Gateway:  wss://${DOMAIN_OCPP}/ocpp/{chargePointId}"
echo ""
echo -e "${CYAN}  Infrastructure:${NC}"
echo "    Cloud SQL:     ${SQL_INSTANCE} (${SQL_CONNECTION_NAME})"
echo "    Redis:         ${REDIS_INSTANCE} (${REDIS_HOST}:${REDIS_PORT})"
echo "    VPC Connector: ${VPC_CONNECTOR} (${VPC_CONNECTOR_RANGE})"
echo "    Artifact Reg:  ${AR_REGISTRY}"
echo ""
echo -e "${CYAN}  OCPP Load Balancer:${NC}"
echo "    Static IP:     ${OCPP_STATIC_IP}  (share with vendors for allowlisting)"
echo "    SSL Cert:      ${SSL_CERT} (check status: gcloud compute ssl-certificates describe ${SSL_CERT})"
echo "    Cloud Armor:   ${ARMOR_POLICY}"
echo ""
echo -e "${CYAN}  Workload Identity Federation (for GitHub Actions):${NC}"
echo "    Provider: ${WIF_PROVIDER_FULL}"
echo "    Service Account: ${SA_DEPLOYER_EMAIL}"
echo ""
echo -e "${CYAN}  Secrets (12 in Secret Manager):${NC}"
echo "    ${PREFIX}-db-connection-string"
echo "    ${PREFIX}-db-password"
echo "    ${PREFIX}-jwt-secret-key"
echo "    ${PREFIX}-string-encryption-passphrase"
echo "    ${PREFIX}-oidc-client-secret"
echo "    ${PREFIX}-redis-auth-string"
echo "    ${PREFIX}-ocpp-auth-key"
echo "    ${PREFIX}-momo-partner-code        (CHANGE_ME)"
echo "    ${PREFIX}-momo-access-key          (CHANGE_ME)"
echo "    ${PREFIX}-momo-secret-key          (CHANGE_ME)"
echo "    ${PREFIX}-vnpay-tmn-code           (CHANGE_ME)"
echo "    ${PREFIX}-vnpay-hash-secret        (CHANGE_ME)"
echo ""
echo -e "${CYAN}  Quick Verification Commands:${NC}"
echo "    curl -s https://${DOMAIN_PORTAL}"
echo "    curl -s https://${DOMAIN_API}/health"
echo "    curl -s https://${DOMAIN_BFF}/health"
echo "    openssl s_client -connect ${DOMAIN_OCPP}:443"
echo "    wscat -c wss://${DOMAIN_OCPP}/ocpp/test-charger -s ocpp1.6"
echo ""
echo -e "${CYAN}  GitHub Actions Secrets to Set:${NC}"
echo "    GCP_PROJECT_ID:          ${PROJECT_ID}"
echo "    GCP_REGION:              ${REGION}"
echo "    GCP_WIF_PROVIDER:        ${WIF_PROVIDER_FULL}"
echo "    GCP_SA_DEPLOYER:         ${SA_DEPLOYER_EMAIL}"
echo "    GCP_AR_REGISTRY:         ${AR_REGISTRY}"
echo ""
echo -e "${YELLOW}  Next Steps:${NC}"
echo "    1. Update placeholder secrets (MoMo, VnPay) with real values"
echo "    2. Wait for SSL certificate provisioning (~15-60 min)"
echo "    3. Verify DNS propagation: dig ${DOMAIN_OCPP}"
echo "    4. Deploy real images via CI/CD pipeline"
echo "    5. Update ${VENDOR_ALLOWLIST} with actual vendor CIDRs"
echo ""
echo -e "${GREEN}+===================================================================+${NC}"
