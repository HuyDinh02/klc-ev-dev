#!/usr/bin/env bash
# Maps custom domains to Cloud Run services.
# Run AFTER: (1) domain odcall.com is verified in Google Search Console,
#            (2) Cloud Run services are deployed at least once.
#
# Usage: ./scripts/setup-domain-mappings.sh

set -euo pipefail

PROJECT="klc-ev-charging"
REGION="asia-southeast1"

echo "==> Mapping ev.odcall.com → klc-admin-portal"
gcloud beta run domain-mappings create \
  --service=klc-admin-portal \
  --domain=ev.odcall.com \
  --region="${REGION}" \
  --project="${PROJECT}"

echo "==> Mapping api.ev.odcall.com → klc-admin-api"
gcloud beta run domain-mappings create \
  --service=klc-admin-api \
  --domain=api.ev.odcall.com \
  --region="${REGION}" \
  --project="${PROJECT}"

echo "==> Mapping ocpp.ev.odcall.com → klc-admin-api"
gcloud beta run domain-mappings create \
  --service=klc-admin-api \
  --domain=ocpp.ev.odcall.com \
  --region="${REGION}" \
  --project="${PROJECT}"

echo "==> Mapping bff.ev.odcall.com → klc-driver-bff"
gcloud beta run domain-mappings create \
  --service=klc-driver-bff \
  --domain=bff.ev.odcall.com \
  --region="${REGION}" \
  --project="${PROJECT}"

echo ""
echo "Done! Now add the following DNS CNAME records:"
echo ""
echo "  ev.odcall.com        → ghs.googlehosted.com."
echo "  api.ev.odcall.com    → ghs.googlehosted.com."
echo "  ocpp.ev.odcall.com   → ghs.googlehosted.com."
echo "  bff.ev.odcall.com    → ghs.googlehosted.com."
echo ""
echo "SSL certificates will be provisioned automatically by Google."
