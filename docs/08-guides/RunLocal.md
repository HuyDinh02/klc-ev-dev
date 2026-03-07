# Run Local — Quick Reference

> Status: PUBLISHED | Last Updated: 2026-03-07

Quick-reference guide for running the entire EV Charging CSMS locally for testing and debugging. For first-time environment setup, see [dev-setup.md](dev-setup.md).

---

## TL;DR (Experienced Developer)

```bash
# 1. Infrastructure
docker compose up -d

# 2. Apply migrations (if needed)
cd src/backend/src/KLC.EntityFrameworkCore && dotnet ef database update && cd ../../../..

# 3. Seed demo data (if fresh DB)
PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql

# 4. Start Admin API (Terminal 1)
cd src/backend/src/KLC.HttpApi.Host && dotnet run

# 5. Start Driver BFF (Terminal 2)
cd src/backend/src/KLC.Driver.BFF && dotnet run

# 6. Start Admin Portal (Terminal 3)
cd src/admin-portal && npm run dev

# 7. Verify
curl -sk https://localhost:44305/health   # → Healthy
curl -s http://localhost:5001/health       # → Healthy
open http://localhost:3001                 # → Admin Portal
```

---

## 1. Start Infrastructure

PostgreSQL (port 5433) and Redis (port 6379) run via Docker Compose.

```bash
# From project root
docker compose up -d

# Verify both containers are healthy
docker compose ps
```

Expected:
```
NAME                    STATUS
ev-charging-postgres    Up (healthy)
ev-charging-redis       Up (healthy)
```

Optional — start pgAdmin for database browsing:
```bash
docker compose --profile tools up -d
# pgAdmin at http://localhost:8080 (admin@klc.local / admin)
```

## 2. Apply Database Migrations

Only needed when there are new migrations (after pulling changes or creating new entities).

```bash
cd src/backend/src/KLC.EntityFrameworkCore
dotnet ef database update
```

If starting from a fresh database, seed demo data:
```bash
PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql
```

Demo user accounts (all password: `Admin@123`):

| Username | Role | Purpose |
|----------|------|---------|
| admin | Admin | Full access |
| operator | Operator | Station management |
| viewer | Viewer | Read-only |

## 3. Start Backend Services

You need **two terminals** — one for each backend service.

### Terminal 1: Admin API (port 44305, HTTPS)

```bash
cd src/backend/src/KLC.HttpApi.Host
dotnet run
```

Wait for:
```
[INF] Now listening on: https://localhost:44305
[INF] Application started. Press Ctrl+C to shut down.
```

Verify:
```bash
curl -sk https://localhost:44305/health
# → Healthy
```

Key URLs:
- Health check: `https://localhost:44305/health`
- Swagger UI: `https://localhost:44305/swagger`
- OCPP WebSocket: `wss://localhost:44305/ocpp/{chargePointId}`
- SignalR (Monitoring): `wss://localhost:44305/hubs/monitoring`

> **Note:** Uses self-signed HTTPS certificate. Accept the browser warning when first visiting.

### Terminal 2: Driver BFF (port 5001, HTTP)

```bash
cd src/backend/src/KLC.Driver.BFF
dotnet run
```

Wait for:
```
[INF] Now listening on: http://localhost:5001
[INF] Application started. Press Ctrl+C to shut down.
```

Verify:
```bash
curl -s http://localhost:5001/health
# → Healthy
```

Key URLs:
- Health check: `http://localhost:5001/health`
- Swagger UI: `http://localhost:5001/swagger`
- SignalR (Driver): `ws://localhost:5001/hubs/driver`

## 4. Start Admin Portal

### Terminal 3: Admin Portal (port 3001)

```bash
cd src/admin-portal
npm install   # only needed first time or after package changes
npm run dev
```

Open `http://localhost:3001` and log in with `admin` / `Admin@123`.

## 5. Get Auth Token (for API Testing)

The Admin API uses OpenIddict with password grant. Get a token:

```bash
TOKEN=$(curl -sk -X POST https://localhost:44305/connect/token \
  -d "grant_type=password&username=admin&password=Admin@123&client_id=KLC_Api&client_secret=1q2w3e*&scope=KLC" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

echo $TOKEN
```

Use the token in subsequent requests:
```bash
curl -sk https://localhost:44305/api/v1/ocpp/connections \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

### Auth Parameters Reference

| Parameter | Value |
|-----------|-------|
| `grant_type` | `password` |
| `client_id` | `KLC_Api` |
| `client_secret` | `1q2w3e*` |
| `scope` | `KLC` |
| `username` | `admin` (or `operator`, `viewer`) |
| `password` | `Admin@123` |

## 6. Test OCPP Integration

### 6a. Accept Self-Signed Certificate

Before the WebSocket simulator can connect, your browser must trust the self-signed certificate.

1. Open Chrome and navigate to: `https://localhost:44305/health`
2. Click **Advanced** → **Proceed to localhost (unsafe)**
3. You should see `Healthy`

### 6b. Open the OCPP Simulator

Open the simulator HTML file in Chrome:

```bash
open "ocpp-simulator/Simulators/simple simulator1.6_mod.html"
```

Or navigate to the file manually:
```
ocpp-simulator/Simulators/simple simulator1.6_mod.html
```

### 6c. Connect a Simulated Charger

In the simulator page:

1. **Central Station** field should be: `wss://localhost:44305/ocpp/KC-HN-001`
   - Change `KC-HN-001` to any charge point ID you want to simulate
   - Format must match: `[A-Za-z0-9-_.]{1,64}`
2. **Vendor** dropdown: Select a vendor to test profile detection
   - `JUHANG` — Vietnamese market charger (orange badge in admin)
   - `Chargecore` — AU/APAC charger (blue badge in admin)
   - `AVT-Company` — Unknown vendor (gray Generic badge)
3. **Model**: Any value (e.g., `JH-DC120`)
4. Click **Connect**

You should see in the simulator console:
```
ws connected
Response: {"status":"Accepted","currentTime":"...","interval":60}
```

### 6d. Verify in Admin Portal

1. Open `http://localhost:3001/ocpp` (OCPP Management page)
2. The charger should appear in **Connected Chargers** list
3. Click the charger to see detail panel (vendor, model, firmware, profile badge)
4. The **OCPP Event Log** at the bottom shows all received messages

### 6e. Simulate Charging Flow

In the simulator, execute these in order:

| Step | Button | What happens |
|------|--------|-------------|
| 1 | **Connect** | WebSocket connects, sends BootNotification automatically |
| 2 | **Authorize** | Validates the ID Tag |
| 3 | **Start Transaction** | Begins a charging session |
| 4 | Set **Meter value** to `5000`, click **Send Meter Values** | Reports energy reading (5000 Wh) |
| 5 | **Send Meter Values** again with `10000` | Reports updated energy |
| 6 | **Stop Transaction** | Ends the charging session |
| 7 | **Heartbeat** | Sends periodic alive signal |
| 8 | **Status Notification** | Reports connector status |

Each action appears in the admin portal's OCPP Event Log with timestamp, vendor profile, and processing latency.

### 6f. Test Multiple Vendor Profiles

To compare vendor behavior:

1. Connect charger 1 with `wss://localhost:44305/ocpp/JUHANG-001`, Vendor = `JUHANG`
2. Disconnect (click Connect/Disconnect toggle)
3. Change URL to `wss://localhost:44305/ocpp/CCG-001`, Vendor = `Chargecore`
4. Connect again

Both chargers appear in the admin portal with different vendor profile badges (orange for JUHANG, blue for Chargecore).

### 6g. Test OCPP via curl

```bash
# List connected chargers
curl -sk https://localhost:44305/api/v1/ocpp/connections \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# Get specific charger detail
curl -sk https://localhost:44305/api/v1/ocpp/connections/KC-HN-001 \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# View raw OCPP event log (last 10)
curl -sk "https://localhost:44305/api/v1/ocpp/events?limit=10" \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# Filter events by charger
curl -sk "https://localhost:44305/api/v1/ocpp/events?chargePointId=KC-HN-001&limit=10" \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# Remote start transaction
curl -sk -X POST https://localhost:44305/api/v1/ocpp/connections/KC-HN-001/remote-start \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"connectorId": 1, "idTag": "B4A63CDF"}'

# Remote stop transaction
curl -sk -X POST https://localhost:44305/api/v1/ocpp/connections/KC-HN-001/remote-stop \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"transactionId": 1}'
```

## 7. Run Tests

```bash
# All backend tests
dotnet test src/backend

# Domain tests only
dotnet test src/backend/test/KLC.Domain.Tests

# Application tests only
dotnet test src/backend/test/KLC.Application.Tests

# EF Core integration tests (requires PostgreSQL running)
dotnet test src/backend/test/KLC.EntityFrameworkCore.Tests

# Run with verbose output
dotnet test src/backend --verbosity normal

# Run a single test class
dotnet test src/backend/test/KLC.Domain.Tests --filter "FullyQualifiedName~VendorProfileTests"
```

## 8. Common Admin API Endpoints

```bash
# Stations
curl -sk https://localhost:44305/api/app/charging-station \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# Sessions
curl -sk https://localhost:44305/api/app/charging-session \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# Tariffs
curl -sk https://localhost:44305/api/app/tariff-plan \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

## 9. Port Reference

| Service | Port | Protocol | URL |
|---------|------|----------|-----|
| Admin API | 44305 | HTTPS | `https://localhost:44305` |
| Driver BFF | 5001 | HTTP | `http://localhost:5001` |
| Admin Portal | 3001 | HTTP | `http://localhost:3001` |
| PostgreSQL | 5433 | TCP | `localhost:5433` |
| Redis | 6379 | TCP | `localhost:6379` |
| pgAdmin | 8080 | HTTP | `http://localhost:8080` |
| OCPP WebSocket | 44305 | WSS | `wss://localhost:44305/ocpp/{cpId}` |
| SignalR (Admin) | 44305 | WSS | `wss://localhost:44305/hubs/monitoring` |
| SignalR (Driver) | 5001 | WS | `ws://localhost:5001/hubs/driver` |

## 10. Troubleshooting

### Port already in use

```bash
# Find and kill process on a port (macOS/Linux)
lsof -ti :44305 | xargs kill -9
lsof -ti :5001 | xargs kill -9
lsof -ti :3001 | xargs kill -9
```

### Docker containers not running

```bash
docker compose ps               # Check status
docker compose logs postgres     # Check PostgreSQL logs
docker compose logs redis        # Check Redis logs
docker compose down && docker compose up -d   # Restart all
```

### Database migration error

```bash
# Check current migration status
cd src/backend/src/KLC.EntityFrameworkCore
dotnet ef migrations list

# Re-apply from scratch (WARNING: drops all data)
dotnet ef database drop --force
dotnet ef database update
PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql
```

### Corrupted bin/obj directories

If you see deeply nested `bin/Debug/net10.0/bin/Debug/...` paths or path-too-long errors:

```bash
# Clean build artifacts for affected project
cd src/backend/src/KLC.HttpApi.Host
rm -rf bin obj
dotnet build
```

### OCPP simulator won't connect

1. **Certificate not accepted**: Visit `https://localhost:44305/health` in Chrome first, accept the warning
2. **Wrong URL format**: Must be `wss://localhost:44305/ocpp/{chargePointId}` — the cpId must match `[A-Za-z0-9-_.]{1,64}`
3. **Admin API not running**: Check Terminal 1 for the API process
4. **Browser console errors**: Open DevTools (F12) → Console tab to see WebSocket errors

### Admin portal login fails

1. Verify Admin API is running: `curl -sk https://localhost:44305/health`
2. Verify demo data was seeded: check that users exist in database
3. Check browser DevTools Network tab for the `/connect/token` request and response

### Hot reload

- **Backend**: `dotnet watch run` instead of `dotnet run` for auto-restart on code changes
- **Admin Portal**: Already enabled by default with `npm run dev`

## Notes for AI Agents

When an AI agent needs to run the system locally:

1. Always start Docker first: `docker compose up -d`
2. Check if migrations are pending before starting the API
3. Use `dotnet run` (not `dotnet watch`) for stability during automated testing
4. Get auth token using the curl command in Section 5 — store in a variable
5. After testing, clean up: `lsof -ti :44305 | xargs kill -9` to free the port
6. Run `dotnet test src/backend` to verify all tests pass before committing
