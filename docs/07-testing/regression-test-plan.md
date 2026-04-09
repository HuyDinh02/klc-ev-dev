# Regression Test Plan

Run this checklist after any large refactoring or code change.

## 1. Automated Test Suites

| Suite | Command | Expected | Covers |
|-------|---------|----------|--------|
| Domain Tests | `dotnet test src/backend/test/KLC.Domain.Tests` | 632 pass | Entities, domain services, OCPP logic, sessions, payments, wallet |
| Application Tests | `dotnet test src/backend/test/KLC.Application.Tests` | 179 pass | App services, MediatR handlers, payment gateways, VnPay/MoMo signatures |
| EF Core Tests | `dotnet test src/backend/test/KLC.EntityFrameworkCore.Tests` (requires PostgreSQL) | 290 pass | BFF services, OCPP integration, session lifecycle, wallet cache |
| Driver App Tests | `cd src/driver-app && npx jest --ci --coverage` | 228 pass | All screens, stores, QR scanner, API mocking |
| Admin Portal TypeScript | `cd src/admin-portal && npx tsc --noEmit` | 0 errors | Type safety across all pages and components |
| Admin Portal Tests | `cd src/admin-portal && npx vitest run` | All pass | UI component tests |

**Total: 1,329+ automated tests**

## 2. Manual E2E Verification (Staging)

### 2.1 Charging Flow
- [ ] Open simulator at `https://klc-ocpp-simulator-25b6rq52da-as.a.run.app/simulator16.html`
- [ ] Connect to `wss://ocpp.ev.odcall.com/ocpp/{stationCode}`, send BootNotification
- [ ] Mobile app: scan QR code → station detail shows connectors
- [ ] Mobile app: tap Start Charging → session created (status: Pending)
- [ ] Simulator auto-responds to RemoteStartTransaction
- [ ] Session transitions: Pending → InProgress
- [ ] Connector status: Available → Preparing → Charging
- [ ] MeterValues auto-increment (energy, SoC, power)
- [ ] Admin portal: session appears in Sessions list with correct start time (UTC+7)
- [ ] Admin portal: duration updates in real-time
- [ ] Mobile app: tap Stop Charging → RemoteStopTransaction sent
- [ ] Simulator sends StopTransaction → session Completed
- [ ] Connector status: Charging → Available
- [ ] Wallet auto-deducted for session cost
- [ ] Session appears in History with correct energy, cost, duration

### 2.2 Session Resilience
- [ ] Refresh simulator page during active session → session stays InProgress (grace period)
- [ ] Session without OCPP transaction (Pending) + disconnect → marked Failed immediately
- [ ] After 10 min offline with active session → OrphanedSessionCleanupService marks Failed

### 2.3 VnPay Wallet TopUp
- [ ] Mobile app: open Wallet → tap amount → select VnPay
- [ ] Redirects to VnPay payment page
- [ ] After payment: VnPay IPN callback hits `/api/v1/wallet/topup/vnpay-ipn`
- [ ] Wallet balance updated
- [ ] Transaction appears in wallet history

### 2.4 Admin Portal
- [ ] Login with admin/Admin@123
- [ ] Dashboard shows correct stats (active sessions, energy, revenue)
- [ ] Sessions page: all sessions visible, sorted by CreationTime DESC
- [ ] Sessions page: start time shows in Vietnam timezone (UTC+7)
- [ ] Sessions page: duration calculates correctly for active sessions
- [ ] Stations page: connector statuses reflect actual state
- [ ] Real-time updates via SignalR (Live indicator)

### 2.5 Simulator Features
- [ ] Connect/BootNotification works
- [ ] RemoteStartTransaction auto-responds (Accepted → StartTransaction → MeterValues)
- [ ] RemoteStopTransaction auto-responds (Accepted → StopTransaction → Available)
- [ ] Auto MeterValues button starts/stops periodic meter values
- [ ] Manual Send MeterValues increments by configured step
- [ ] Handles ChangeConfiguration, Reset, UnlockConnector

## 3. CI/CD Pipeline Verification
- [ ] Push to `develop` → Deploy Staging workflow triggers
- [ ] Backend tests pass in CI
- [ ] Docker images built and pushed
- [ ] All 3 services deployed (klc-admin-api, klc-driver-bff, klc-admin-portal)
- [ ] Smoke tests pass (health endpoints return 200)
- [ ] PR to develop → CI workflow runs tests only (no deploy)

## 4. Changes Covered in This Session

| Change | Test Coverage |
|--------|--------------|
| BFF session linking to OCPP | `EfCoreOcppServiceTests.HandleStartTransaction*` |
| Grace period on disconnect | `EfCoreOcppServiceTests.StationDisconnect*` |
| Internal API for RemoteStart/Stop | Integration via staging E2E |
| Connector status lifecycle | `EfCoreOcppServiceTests.*`, manual E2E |
| VnPay topup flow | `VnPaySignatureTests.*`, manual E2E |
| QR scanner parser | `QRScannerScreen.test.tsx` (22 tests) |
| CreationTime audit fields | `WalletBffServiceCacheTests`, manual DB verify |
| Timezone (timestamptz migration) | Manual E2E (admin portal times) |
| PaymentCallbackValidator | `VnPaySignatureTests.*`, `PaymentGatewayServiceTests.*` |
| CacheKeys constants | `WalletBffServiceCacheTests`, all BFF integration tests |
| Mobile shared formatters | `WalletScreen.test.tsx`, `HistoryScreen.test.tsx`, `SessionScreen.test.tsx` |
| CI/CD simplification | Pipeline run verification |
