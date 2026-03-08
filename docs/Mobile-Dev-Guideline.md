# Mobile Developer Integration Guide

> Driver App (React Native / Expo) — KLC EV Charging Platform

## Quick Start

| Item | Value |
|------|-------|
| **BFF Base URL (Production)** | `https://bff.ev.odcall.com` |
| **BFF Base URL (Local)** | `http://localhost:5001` |
| **SignalR Hub** | `{BASE_URL}/hubs/driver` |
| **Auth** | Bearer JWT in `Authorization` header |
| **Pagination** | Cursor-based (never offset) |
| **Currency** | VND (integer, no decimals) |
| **Date format** | ISO 8601 UTC (`2026-03-08T14:30:00Z`) |
| **Language** | Vietnamese (default), English |

---

## 1. Authentication

### 1.1 Registration Flow

```
POST /api/v1/auth/register
POST /api/v1/auth/verify-phone   (OTP from SMS)
POST /api/v1/auth/login
```

**Register:**
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "phoneNumber": "0912345678",
  "password": "MyPass@123",
  "fullName": "Nguyen Van A"
}
```
Response: `{ "success": true, "userId": "guid", "message": "OTP sent" }`

**Verify Phone (6-digit OTP, 5 min TTL):**
```http
POST /api/v1/auth/verify-phone
{ "phoneNumber": "0912345678", "otp": "123456" }
```

**Resend OTP:**
```http
POST /api/v1/auth/resend-otp
{ "phoneNumber": "0912345678" }
```

### 1.2 Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "phoneNumber": "0912345678",
  "password": "MyPass@123"
}
```

Response:
```json
{
  "success": true,
  "accessToken": "eyJhbG...",
  "refreshToken": "abc123...",
  "expiresIn": 86400,
  "user": {
    "userId": "guid",
    "fullName": "Nguyen Van A",
    "phoneNumber": "0912345678",
    "email": null,
    "avatarUrl": null,
    "isPhoneVerified": true,
    "membershipTier": 0,
    "walletBalance": 0
  }
}
```

**Store both tokens securely** (e.g., `expo-secure-store`). Access token expires in 24h.

### 1.3 Refresh Token

```http
POST /api/v1/auth/refresh-token
{ "refreshToken": "abc123..." }
```

Returns same shape as login. Refresh token valid 30 days. Old token is revoked.

### 1.4 Logout

```http
POST /api/v1/auth/logout
Authorization: Bearer <accessToken>

{ "refreshToken": "abc123..." }
```

### 1.5 Password Reset

```
POST /api/v1/auth/forgot-password     { "phoneNumber": "0912345678" }
POST /api/v1/auth/reset-password       { "phoneNumber": "...", "otp": "123456", "newPassword": "..." }
```

### 1.6 Change Password (authenticated)

```http
POST /api/v1/auth/change-password
Authorization: Bearer <token>

{ "currentPassword": "OldPass@123", "newPassword": "NewPass@456" }
```

### 1.7 Rate Limiting

Auth endpoints: **10 requests/minute per IP**. All other API endpoints: **60 requests/minute per IP**.
HTTP 429 when exceeded.

---

## 2. Station Discovery

**No auth required** — public endpoints for map and search.

### 2.1 Nearby Stations

```http
GET /api/v1/stations/nearby?lat=21.0285&lon=105.8542&radius=10&limit=20
```

Response:
```json
{
  "data": [
    {
      "id": "guid",
      "name": "KLC Times City",
      "address": "458 Minh Khai, Hai Ba Trung",
      "latitude": 20.9935,
      "longitude": 105.8677,
      "status": 1,
      "availableConnectors": 2,
      "totalConnectors": 4,
      "distance": 2.3
    }
  ]
}
```

`distance` is in **kilometers**. Default radius: 10 km (max 50). Default limit: 20 (max 100).

### 2.2 Search Stations

```http
GET /api/v1/stations/search?q=vincom&limit=20
```

Searches name, address, station code. Same response shape as nearby (without `distance`).

### 2.3 Station Detail

```http
GET /api/v1/stations/{id}
```

Response:
```json
{
  "id": "guid",
  "stationCode": "KLC-HN-001",
  "name": "KLC Times City",
  "address": "458 Minh Khai, Hai Ba Trung",
  "latitude": 20.9935,
  "longitude": 105.8677,
  "status": 1,
  "isEnabled": true,
  "vendor": "Chargecore",
  "model": "DC-60kW",
  "ratePerKwh": 3500,
  "taxRatePercent": 8,
  "connectors": [
    {
      "id": "guid",
      "connectorNumber": 1,
      "type": 1,
      "status": 0,
      "maxPowerKw": 60.0,
      "isEnabled": true
    }
  ]
}
```

### 2.4 Connector Status (real-time)

```http
GET /api/v1/stations/{id}/connectors
```

30-second cache. Use for checking availability before starting session.

---

## 3. Charging Session

**All require auth.**

### 3.1 Start Session

```http
POST /api/v1/sessions/start
Authorization: Bearer <token>

{
  "stationId": "guid",
  "connectorNumber": 1,
  "vehicleId": "guid"           // optional
}
```

Response:
```json
{ "success": true, "sessionId": "guid", "status": 1 }
```

**Pre-checks (handled by server):**
- Connector must be Available
- User must not have another active session
- Station must be enabled

### 3.2 Stop Session

```http
POST /api/v1/sessions/{id}/stop
Authorization: Bearer <token>
```

### 3.3 Active Session

```http
GET /api/v1/sessions/active
Authorization: Bearer <token>
```

Returns current active session or **204 No Content**. Cached 10 seconds.

Response:
```json
{
  "sessionId": "guid",
  "stationId": "guid",
  "stationName": "KLC Times City",
  "stationAddress": "458 Minh Khai",
  "connectorNumber": 1,
  "status": 2,
  "startTime": "2026-03-08T10:00:00Z",
  "energyKwh": 15.3,
  "currentCost": 53550,
  "ratePerKwh": 3500
}
```

### 3.4 Session Detail

```http
GET /api/v1/sessions/{id}
```

### 3.5 Session History

```http
GET /api/v1/sessions/history?pageSize=20
GET /api/v1/sessions/history?cursor={lastSessionId}&pageSize=20
```

Response:
```json
{
  "data": [ ... ],
  "pagination": {
    "nextCursor": "guid-or-null",
    "hasMore": true,
    "pageSize": 20
  }
}
```

---

## 4. Payments

### 4.1 Process Payment (after session completes)

```http
POST /api/v1/payments/process
Authorization: Bearer <token>

{
  "sessionId": "guid",
  "gateway": 1,                  // 1=MoMo, 4=VnPay, 3=Wallet
  "paymentMethodId": "guid",    // optional, saved method
  "voucherCode": "SAVE10"       // optional, apply discount
}
```

Response:
```json
{
  "success": true,
  "paymentId": "guid",
  "status": 2,
  "redirectUrl": "https://momo.vn/pay/...",   // null if paid via wallet/voucher
  "voucherDiscount": 50000                      // null if no voucher
}
```

**Voucher discount logic:**
- `FixedAmount`: discount = min(voucher.value, sessionCost)
- `Percentage`: discount = min(cost * value/100, maxDiscountAmount)
- `FreeCharging`: discount = sessionCost (100% off)

If `redirectUrl` is returned, open it in a WebView/browser for the user to complete payment.

### 4.2 Payment History & Detail

```http
GET /api/v1/payments/history?pageSize=20
GET /api/v1/payments/{id}
```

### 4.3 Payment Methods (saved cards/wallets)

```http
GET /api/v1/payment-methods
POST /api/v1/payment-methods
  { "gateway": 1, "displayName": "MoMo ***1234", "tokenReference": "momo-token", "lastFourDigits": "1234" }
DELETE /api/v1/payment-methods/{id}
POST /api/v1/payment-methods/{id}/set-default
```

---

## 5. Wallet

### 5.1 Get Balance

```http
GET /api/v1/wallet/balance
```

Response:
```json
{
  "balance": 500000,
  "currency": "VND",
  "lastTransactionType": 0,
  "lastTransactionAmount": 100000,
  "lastTransactionAt": "2026-03-08T10:00:00Z"
}
```

### 5.2 Top Up

```http
POST /api/v1/wallet/topup
{ "amount": 100000, "gateway": 1 }
```

**Constraints:**
- Min: 10,000 VND per transaction
- Max: 10,000,000 VND per transaction
- Monthly limit: 100,000,000 VND (SBV Circular 41/2025)

Response includes `redirectUrl` → open in WebView for payment.

### 5.3 Top-Up Status

```http
GET /api/v1/wallet/topup/{transactionId}/status
```

Poll this after returning from payment WebView until status changes from `Pending(0)` to `Completed(1)` or `Failed(2)`.

### 5.4 Transaction History

```http
GET /api/v1/wallet/transactions?pageSize=20&type=0
```

Optional `type` filter: `0`=TopUp, `1`=SessionPayment, `2`=Refund, `3`=Adjustment, `4`=VoucherCredit

### 5.5 Transaction Summary

```http
GET /api/v1/wallet/transactions/summary
```

Returns: `currentBalance`, `totalTopUp`, `totalSpent`, `totalRefunded`, `totalVoucherCredit`, `transactionCount`

---

## 6. Vouchers & Promotions

### 6.1 Available Vouchers

```http
GET /api/v1/vouchers
```

Returns vouchers the user hasn't used yet, sorted by expiry date.

### 6.2 Validate Voucher (before applying)

```http
POST /api/v1/vouchers/validate
{ "code": "SAVE10" }
```

Response: `{ "isValid": true, "voucher": { ... } }` or `{ "isValid": false, "error": "..." }`

### 6.3 Apply Voucher (wallet credit)

```http
POST /api/v1/vouchers/apply
{ "code": "WELCOME50K" }
```

Response:
```json
{ "success": true, "newBalance": 550000, "creditAmount": 50000 }
```

### 6.4 Active Promotions

```http
GET /api/v1/promotions
GET /api/v1/promotions/{id}
```

---

## 7. Vehicles

```http
GET    /api/v1/vehicles                    // list
GET    /api/v1/vehicles/default            // default vehicle (or 204)
GET    /api/v1/vehicles/{id}
POST   /api/v1/vehicles                    // create
  { "make": "VinFast", "model": "VF8", "year": 2025, "licensePlate": "30A-12345",
    "color": "White", "nickname": "My VF8", "batteryCapacityKwh": 87.7,
    "preferredConnectorType": 1 }
PUT    /api/v1/vehicles/{id}               // update
DELETE /api/v1/vehicles/{id}               // soft delete
POST   /api/v1/vehicles/{id}/set-default
```

First vehicle added is auto-set as default.

---

## 8. User Profile

```http
GET    /api/v1/profile
PUT    /api/v1/profile       { "fullName": "...", "preferredLanguage": "vi" }
GET    /api/v1/profile/statistics
POST   /api/v1/profile/avatar              // multipart/form-data, max 5MB
POST   /api/v1/profile/change-phone        { "newPhoneNumber": "0987654321" }
POST   /api/v1/profile/verify-phone-change { "newPhoneNumber": "...", "otp": "123456" }
DELETE /api/v1/profile                     // deactivate account
```

Statistics response: `totalSessions`, `totalEnergyKwh`, `totalSpentVnd`, `totalChargingMinutes`, `co2SavedKg`

---

## 9. Favorites

```http
GET    /api/v1/favorites                   // list favorite stations
POST   /api/v1/favorites/{stationId}       // add
DELETE /api/v1/favorites/{stationId}       // remove
```

---

## 10. Notifications

### 10.1 List & Read

```http
GET /api/v1/notifications?pageSize=20
GET /api/v1/notifications/unread-count
PUT /api/v1/notifications/{id}/read
PUT /api/v1/notifications/read-all
```

### 10.2 FCM Device Registration

Call after login and whenever FCM token refreshes:

```http
POST /api/v1/devices/register
{ "fcmToken": "firebase-token-string", "platform": 0 }
```

Platform: `0` = iOS, `1` = Android

On logout:
```http
DELETE /api/v1/devices/{fcmToken}
```

### 10.3 Notification Preferences

```http
GET /api/v1/notifications/preferences
PUT /api/v1/notifications/preferences
{
  "chargingComplete": true,
  "paymentAlerts": true,
  "faultAlerts": true,
  "promotions": false
}
```

---

## 11. Feedback & Support

```http
POST /api/v1/feedback
  { "type": 2, "subject": "Charger stuck", "message": "Connector 1 at Times City won't release" }
GET  /api/v1/feedback?pageSize=20
GET  /api/v1/support/faq
GET  /api/v1/support/about
```

Feedback types: `0`=Bug, `1`=FeatureRequest, `2`=ChargingIssue, `3`=PaymentIssue, `4`=General

---

## 12. Real-Time Updates (SignalR)

### 12.1 Connection

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${BASE_URL}/hubs/driver`, {
    accessTokenFactory: () => getAccessToken(),
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### 12.2 Subscribe to Events

```typescript
// Subscribe to charging session updates
await connection.invoke("SubscribeToSession", sessionId);

// Subscribe to station connector changes
await connection.invoke("SubscribeToStation", stationId);

// Unsubscribe when leaving
await connection.invoke("UnsubscribeFromSession", sessionId);
await connection.invoke("UnsubscribeFromStation", stationId);
```

### 12.3 Listen for Events

```typescript
// Real-time charging metrics (every 10-30 seconds)
connection.on("OnSessionUpdate", (msg) => {
  // msg: { sessionId, energyKwh, currentCost, durationMinutes, powerKw, socPercent, timestamp }
});

// Session status changed
connection.on("OnSessionStatusChanged", (msg) => {
  // msg: { sessionId, status, message, timestamp }
});

// Session completed
connection.on("OnSessionCompleted", (msg) => {
  // msg: { sessionId, totalEnergyKwh, totalCost, durationMinutes, completedAt }
  // → Navigate to payment screen
});

// Wallet balance changed (top-up, voucher, deduction)
connection.on("OnWalletBalanceChanged", (msg) => {
  // msg: { userId, newBalance, changeAmount, reason, timestamp }
});

// Connector availability changed
connection.on("OnConnectorStatusChanged", (msg) => {
  // msg: { stationId, connectorNumber, status, timestamp }
});

// New notification
connection.on("OnNotification", (msg) => {
  // msg: { notificationId, type, title, body, actionUrl, timestamp }
});

// Payment status update
connection.on("OnPaymentStatusChanged", (msg) => {
  // msg: { paymentId, sessionId, status, error, timestamp }
});

// Charging error
connection.on("OnChargingError", (msg) => {
  // msg: { sessionId, errorCode, message, timestamp }
});
```

### 12.4 User-Level Events (auto-subscribed on connect)

These are delivered to the `User:{userId}` group automatically:
- `OnWalletBalanceChanged`
- `OnNotification`
- `OnPaymentStatusChanged`

No explicit subscribe needed.

---

## 13. Enums Reference

### StationStatus
| Value | Name | Description |
|-------|------|-------------|
| 0 | Offline | Not communicating |
| 1 | Available | Ready for charging |
| 2 | Occupied | All connectors in use |
| 3 | Unavailable | Under maintenance |
| 4 | Faulted | Error state |
| 5 | Decommissioned | Permanently offline |

### ConnectorStatus
| Value | Name |
|-------|------|
| 0 | Available |
| 1 | Preparing |
| 2 | Charging |
| 3 | SuspendedEV |
| 4 | SuspendedEVSE |
| 5 | Finishing |
| 6 | Reserved |
| 7 | Unavailable |
| 8 | Faulted |

### ConnectorType
| Value | Name |
|-------|------|
| 0 | Type2 |
| 1 | CCS2 |
| 2 | CHAdeMO |
| 3 | GBT |
| 4 | Type1 |

### SessionStatus
| Value | Name |
|-------|------|
| 0 | Pending |
| 1 | Starting |
| 2 | InProgress |
| 3 | Suspended |
| 4 | Stopping |
| 5 | Completed |
| 6 | Failed |

### PaymentGateway
| Value | Name | Notes |
|-------|------|-------|
| 0 | ZaloPay | |
| 1 | MoMo | Primary |
| 2 | OnePay | |
| 3 | Wallet | Pay from balance |
| 4 | VnPay | Primary |
| 5 | QrPayment | |
| 6 | Voucher | Auto-set for full voucher payments |
| 7 | Urbox | |

### PaymentStatus
| Value | Name |
|-------|------|
| 0 | Pending |
| 1 | Processing |
| 2 | Completed |
| 3 | Failed |
| 4 | Refunded |
| 5 | Cancelled |

### WalletTransactionType
| Value | Name |
|-------|------|
| 0 | TopUp |
| 1 | SessionPayment |
| 2 | Refund |
| 3 | Adjustment |
| 4 | VoucherCredit |

### VoucherType
| Value | Name | Discount logic |
|-------|------|----------------|
| 0 | FixedAmount | Subtract fixed VND amount |
| 1 | Percentage | % of cost, capped by maxDiscountAmount |
| 2 | FreeCharging | 100% off session |

### FeedbackType
| Value | Name |
|-------|------|
| 0 | Bug |
| 1 | FeatureRequest |
| 2 | ChargingIssue |
| 3 | PaymentIssue |
| 4 | General |

### DevicePlatform
| Value | Name |
|-------|------|
| 0 | iOS |
| 1 | Android |

---

## 14. Typical User Flows

### Flow 1: First-Time User
```
Register → Verify OTP → Login → Add Vehicle → Browse Stations Map
```

### Flow 2: Charge & Pay
```
Search/Browse Stations → View Detail → Check Connector Availability
→ Start Session → Subscribe SignalR → Monitor Real-Time
→ Stop Session → OnSessionCompleted
→ (Optional) Validate Voucher → Process Payment
→ View Receipt
```

### Flow 3: Top Up Wallet
```
View Balance → Top Up (MoMo/VnPay)
→ Open Redirect URL in WebView → Complete Payment
→ Poll /topup/{id}/status until Completed
→ OnWalletBalanceChanged via SignalR
```

### Flow 4: Apply Voucher
```
View Available Vouchers → Validate Code → Apply Voucher
→ Wallet Balance Updated → OnWalletBalanceChanged
```

---

## 15. Error Handling

All error responses follow this format:

```json
{
  "error": {
    "code": "MODULE_ERROR",
    "message": "Human-readable message in English"
  }
}
```

**Common status codes:**
| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Process response |
| 201 | Created | Resource created |
| 204 | No Content | Success, no body |
| 400 | Bad Request | Show error message |
| 401 | Unauthorized | Refresh token or re-login |
| 404 | Not Found | Show "not found" |
| 429 | Too Many Requests | Wait and retry |
| 500 | Server Error | Show generic error |

**Token refresh pattern:**
```typescript
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;
      const newToken = await refreshToken();
      error.config.headers.Authorization = `Bearer ${newToken}`;
      return api(error.config);
    }
    return Promise.reject(error);
  }
);
```

---

## 16. Default Map Region

```typescript
const DEFAULT_REGION = {
  latitude: 21.0285,    // Hanoi
  longitude: 105.8542,
  latitudeDelta: 0.05,
  longitudeDelta: 0.05,
};
```

---

## 17. Recommended Libraries

| Purpose | Library |
|---------|---------|
| HTTP Client | `axios` |
| Secure Storage | `expo-secure-store` |
| Maps | `react-native-maps` |
| SignalR | `@microsoft/signalr` |
| Navigation | `@react-navigation/native` |
| State | `zustand` |
| Push Notifications | `expo-notifications` + Firebase |
