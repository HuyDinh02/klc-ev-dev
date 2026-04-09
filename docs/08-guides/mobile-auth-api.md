# Mobile App — Auth & OTP API Guide

## Base URL
```
Production: https://bff.ev.odcall.com/api/v1
Local:      http://localhost:5001/api/v1
```

## Flow

```
1. Register (phone + name + password) → OTP sent via SMS
2. Verify OTP (phone + 6-digit code) → phone verified
3. Login (phone + password) → JWT token
4. Use JWT for all authenticated API calls
```

---

## 1. Register

```http
POST /auth/register
Content-Type: application/json

{
  "phoneNumber": "0983987986",
  "fullName": "Nguyễn Văn An",
  "password": "MyPassword@123"
}
```

**Response 201:**
```json
{
  "success": true,
  "userId": "2d8d654f-f6a5-45f2-bfef-d8fac0c94144",
  "message": "Registration successful. Please verify your phone number."
}
```

**Response 400 (phone taken):**
```json
{
  "error": {
    "code": "REGISTRATION_FAILED",
    "message": "KLC:Auth:PhoneAlreadyRegistered"
  }
}
```

> After register, user receives SMS with 6-digit OTP code.
> In dev/staging: OTP is logged to Cloud Logging (no real SMS yet).

---

## 2. Verify Phone (OTP)

```http
POST /auth/verify-phone
Content-Type: application/json

{
  "phoneNumber": "0983987986",
  "otp": "522497"
}
```

**Response 200:**
```json
{
  "success": true
}
```

**Response 400 (wrong OTP):**
```json
{
  "error": {
    "code": "VERIFY_FAILED",
    "message": "KLC:Auth:InvalidOtp"
  }
}
```

**Response 400 (OTP expired — 5 min):**
```json
{
  "error": {
    "code": "VERIFY_FAILED",
    "message": "KLC:Auth:OtpExpired"
  }
}
```

---

## 3. Resend OTP

```http
POST /auth/resend-otp
Content-Type: application/json

{
  "phoneNumber": "0983987986"
}
```

**Response 200:**
```json
{
  "message": "OTP sent successfully"
}
```

---

## 4. Login

```http
POST /auth/login
Content-Type: application/json

{
  "phoneNumber": "0983987986",
  "password": "MyPassword@123"
}
```

**Response 200:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiI...",
  "refreshToken": "AaMRD7cN+nDl...",
  "expiresIn": 3600,
  "user": {
    "userId": "2d8d654f-f6a5-45f2-bfef-d8fac0c94144",
    "fullName": "Nguyễn Văn An",
    "phoneNumber": "0983987986",
    "email": null,
    "avatarUrl": null,
    "isPhoneVerified": true,
    "membershipTier": "Standard",
    "walletBalance": 0
  }
}
```

**Response 401 (wrong password or user not found):**
Empty body, HTTP 401.

> Note: `isPhoneVerified: false` means user registered but hasn't verified OTP yet.
> User CAN login before verification, but some features may be restricted.

---

## 5. Refresh Token

```http
POST /auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "AaMRD7cN+nDl..."
}
```

**Response 200:** Same as login response (new tokens).

---

## 6. Logout

```http
POST /auth/logout
Content-Type: application/json
Authorization: Bearer {accessToken}

{
  "refreshToken": "AaMRD7cN+nDl..."
}
```

---

## Using JWT Token

All authenticated endpoints require:
```http
Authorization: Bearer {accessToken}
```

Token expires in 1 hour. Use refresh token to get new access token.

---

## Mobile Implementation (React Native)

### API Client (`api/auth.ts`)

```typescript
const BFF_URL = 'https://bff.ev.odcall.com/api/v1';

export const authApi = {
  register: async (phoneNumber: string, fullName: string, password: string) => {
    const res = await fetch(`${BFF_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ phoneNumber, fullName, password }),
    });
    return res.json();
  },

  verifyPhone: async (phoneNumber: string, otp: string) => {
    const res = await fetch(`${BFF_URL}/auth/verify-phone`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ phoneNumber, otp }),
    });
    return res.json();
  },

  resendOtp: async (phoneNumber: string) => {
    const res = await fetch(`${BFF_URL}/auth/resend-otp`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ phoneNumber }),
    });
    return res.json();
  },

  login: async (phoneNumber: string, password: string) => {
    const res = await fetch(`${BFF_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ phoneNumber, password }),
    });
    return res.json();
  },
};
```

### Screen Flow

```
RegisterScreen
  → Input: phone, fullName, password
  → Call: authApi.register()
  → Navigate to OtpScreen

OtpScreen
  → Input: 6-digit OTP code
  → Call: authApi.verifyPhone()
  → On success: navigate to LoginScreen (or auto-login)
  → "Resend OTP" button: authApi.resendOtp()

LoginScreen
  → Input: phone, password
  → Call: authApi.login()
  → Store accessToken + refreshToken in SecureStore
  → Navigate to HomeScreen
```

---

## Test Accounts (Staging)

| Phone | Password | Name | Balance |
|-------|----------|------|---------|
| 0901234001 | Admin@123 | Nguyễn Văn An | 5,347,712đ |
| 0901234002 | Admin@123 | Trần Thị Bình | 1,850,000đ |
| 0901234003 | Admin@123 | Lê Văn Cường | 3,300,000đ |

These accounts are pre-verified (no OTP needed for login).

## OTP in Dev/Staging

SMS is in **log-only mode** — OTP is not sent via real SMS.
To get the OTP code, check Cloud Logging:

```bash
gcloud logging read 'textPayload:("0983987986")' --limit 1 --format="value(textPayload)" --freshness=5m
```

Output: `[SMS] Provider=Log — OTP logged only. To: 0983987986, Message: Your KLC verification code is: 522497`

For production: configure eSMS.vn credentials and real SMS will be sent.
