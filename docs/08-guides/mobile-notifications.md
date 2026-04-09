# Mobile App — Push Notifications Integration Guide

## Overview

KLC uses **Firebase Cloud Messaging (FCM)** for push notifications. The system sends notifications for:

| Event | Title | Example Body |
|-------|-------|-------------|
| Charging started | Đang sạc ⚡ | Phiên sạc đã bắt đầu tại cổng 1 |
| Charging completed | Sạc hoàn tất ✅ | Đã sạc 32.50 kWh — Chi phí: 130.000đ |
| Wallet topup success | Nạp ví thành công 💰 | Đã nạp 100.000đ vào ví. Số dư: 1.900.000đ |
| Admin broadcast | (custom title) | (custom body) |

## Architecture

```
Backend event (session complete, payment, etc.)
  → FirebasePushNotificationService.SendToUserAsync(userId, title, body, data)
  → Lookup DeviceTokens for user in DB
  → Firebase Cloud Messaging API
  → FCM delivers to Android/iOS
  → expo-notifications shows alert on device
```

---

## Step 1: Install Dependencies

```bash
cd src/driver-app
npx expo install expo-notifications expo-device
```

Already installed in the project. Verify in `package.json`.

## Step 2: Configure Android

In `app.json`, add notification plugin (already configured):

```json
{
  "expo": {
    "plugins": [
      ["expo-notifications", {
        "icon": "./assets/notification-icon.png",
        "color": "#2D9B3A"
      }]
    ]
  }
}
```

For FCM on Android, add `google-services.json` from Firebase Console:
- Firebase Console → Project Settings → Your apps → Android → Download
- Place in `src/driver-app/` root

## Step 3: Notification Service

File `src/driver-app/src/services/notifications.ts` is **already implemented**:

```typescript
// Already exists — key functions:

// Call after user logs in
registerForPushNotifications(): Promise<string | null>
  → Requests permission
  → Gets FCM device token
  → Registers with backend: POST /api/v1/devices/register

// Call when user logs out
unregisterPushNotifications(): Promise<void>
  → Unregisters from backend: DELETE /api/v1/devices/{token}

// Listen for notifications in foreground
addNotificationReceivedListener(handler)

// Listen for notification taps
addNotificationResponseReceivedListener(handler)
```

## Step 4: Register Device After Login

In your auth flow, call `registerForPushNotifications()` after successful login:

```typescript
import { registerForPushNotifications } from '../services/notifications';

// After login succeeds:
const handleLoginSuccess = async (accessToken: string) => {
  // Save token to storage
  await SecureStore.setItemAsync('access_token', accessToken);

  // Register device for push notifications
  await registerForPushNotifications();
};
```

## Step 5: Unregister on Logout

```typescript
import { unregisterPushNotifications } from '../services/notifications';

const handleLogout = async () => {
  await unregisterPushNotifications();
  await SecureStore.deleteItemAsync('access_token');
};
```

## Step 6: Handle Notification Taps (Deep Linking)

When user taps a notification, navigate to the relevant screen:

```typescript
import { addNotificationResponseReceivedListener } from '../services/notifications';
import { useNavigation } from '@react-navigation/native';

useEffect(() => {
  const subscription = addNotificationResponseReceivedListener((response) => {
    const data = response.notification.request.content.data;

    switch (data.type) {
      case 'session_started':
      case 'session_completed':
        navigation.navigate('Session', { sessionId: data.sessionId });
        break;
      case 'wallet_topup':
        navigation.navigate('Wallet');
        break;
      default:
        navigation.navigate('Notifications');
    }
  });

  return () => subscription.remove();
}, []);
```

## Step 7: Show Foreground Notifications

Already configured in `notifications.ts`:

```typescript
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,   // Show banner
    shouldPlaySound: true,   // Play sound
    shouldSetBadge: true,    // Update badge count
  }),
});
```

---

## Backend API Endpoints

### Register Device Token

```http
POST /api/v1/devices/register
Authorization: Bearer {token}
Content-Type: application/json

{
  "fcmToken": "dGVzdC10b2tlbi0xMjM...",
  "platform": "Android"   // or "iOS"
}
```
Response: `204 No Content`

### Unregister Device

```http
DELETE /api/v1/devices/{fcmToken}
Authorization: Bearer {token}
```
Response: `204 No Content`

### Get In-App Notifications

```http
GET /api/v1/notifications?pageSize=20&cursor={lastId}
Authorization: Bearer {token}
```

Response:
```json
{
  "items": [
    {
      "id": "3a206...",
      "type": "WalletTopUp",
      "title": "Nạp ví thành công",
      "body": "Bạn đã nạp thành công 100.000đ vào ví.",
      "isRead": false,
      "createdAt": "2026-04-03T11:22:00Z"
    }
  ],
  "totalCount": 5,
  "hasMore": false
}
```

### Get Unread Count

```http
GET /api/v1/notifications/unread-count
Authorization: Bearer {token}
```

Response:
```json
{ "count": 3 }
```

### Mark as Read

```http
PUT /api/v1/notifications/{id}/read
Authorization: Bearer {token}
```

### Mark All as Read

```http
PUT /api/v1/notifications/read-all
Authorization: Bearer {token}
```

### Get Notification Preferences

```http
GET /api/v1/notifications/preferences
Authorization: Bearer {token}
```

Response:
```json
{
  "chargingComplete": true,
  "paymentAlerts": true,
  "faultAlerts": true,
  "promotions": true
}
```

### Update Preferences

```http
PUT /api/v1/notifications/preferences
Authorization: Bearer {token}
Content-Type: application/json

{
  "chargingComplete": true,
  "paymentAlerts": true,
  "faultAlerts": false,
  "promotions": false
}
```

---

## Push Notification Data Payload

Each push notification includes a `data` field for deep linking:

| Event | data.type | data fields |
|-------|-----------|-------------|
| Session started | `session_started` | `sessionId` |
| Session completed | `session_completed` | `sessionId`, `energyKwh`, `cost` |
| Wallet topup | `wallet_topup` | `amount`, `newBalance` |

---

## Testing

### On Expo Development Build

Push notifications **don't work on Expo Go**. Must use dev client:

```bash
npx expo run:android
# or
npx expo run:ios
```

### Test Push Manually

Use Firebase Console → Cloud Messaging → Send test message:
1. Get device token from app logs after `registerForPushNotifications()`
2. Firebase Console → Messaging → New campaign → Test on device
3. Paste device token → Send

### Test via Backend

Start a charging session via simulator — push notification will be sent automatically on session start and completion.

---

## Notification Types (Backend Enum)

```
WalletTopUp     — Wallet top-up success/failure
PaymentFailed   — Payment processing failed
ChargingComplete — Charging session completed
FaultAlert      — Station fault detected
Promotion       — Promotional offer
SystemAlert     — System-level alert
```

## Already Implemented in Codebase

| File | What | Status |
|------|------|--------|
| `src/driver-app/src/services/notifications.ts` | FCM registration, listeners | ✅ Done |
| `src/driver-app/src/api/notifications.ts` | API client for notifications | ✅ Done |
| `src/driver-app/src/screens/NotificationsScreen.tsx` | Notification list screen | ✅ Done |
| `src/driver-app/src/screens/SettingsScreen.tsx` | Notification preferences | ✅ Done |
| Backend: `FirebasePushNotificationService` | Send push via FCM | ✅ Done |
| Backend: `NotificationBffService` | CRUD, preferences, device tokens | ✅ Done |
| Backend: `NotificationEndpoints` | REST API endpoints | ✅ Done |
| Backend: Session start/stop push | Auto-send on charging events | ✅ Done |
| Backend: Wallet topup push | Auto-send on payment success | ✅ Done |
