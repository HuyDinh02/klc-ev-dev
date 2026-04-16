# Firebase Phone Auth — Production Setup Guide

## GCP APIs (Done)

- ✅ Play Integrity API — enabled
- ✅ Firebase App Check API — enabled
- ❌ Android Device Verification — permission denied (not critical, Play Integrity is the replacement)

## Firebase Console Settings (Admin to do)

URL: https://console.firebase.google.com/u/0/project/klc-ev-charging/authentication/settings

### 1. Phone enforcement mode
- Current: `AUDIT`
- **Change to: `ENFORCE`**

### 2. SMS region policy
- Go to **SMS region policy**
- Select **Allow only selected regions**
- Check only: **Vietnam (+84)**

### 3. Sign-up quota
- Set to **100 accounts/day** (increase after launch)

---

## Mobile Dev Tasks

### Android — Tắt reCAPTCHA

**Step 1: Add SHA fingerprints to Firebase**

```bash
# From production keystore (NOT debug)
keytool -list -v -keystore path/to/production.keystore -alias your-alias

# Copy both SHA-1 and SHA-256
```

Go to **Firebase Console → Project Settings → General → Android app (vn.klc.driver)** → Add fingerprint → add both SHA-1 and SHA-256.

**Step 2: Download `google-services.json`**

Firebase Console → Project Settings → General → Android app → Download `google-services.json`

Place in: `src/driver-app/google-services.json`

**Step 3: Update `app.json`**

```json
{
  "expo": {
    "android": {
      "package": "vn.klc.driver",
      "googleServicesFile": "./google-services.json"
    }
  }
}
```

**Step 4: Rebuild**

```bash
eas build --platform android
```

After SHA fingerprints + Play Integrity, Android will silently verify — no reCAPTCHA popup.

---

### iOS — Tắt reCAPTCHA

**Step 1: Create APNs Key**

1. Go to https://developer.apple.com → Certificates, Identifiers & Profiles → Keys
2. Click **+** → check **Apple Push Notifications service (APNs)** → Continue → Register
3. Download the `.p8` file (only downloadable ONCE — save it safely)
4. Note the **Key ID** (shown on the key page)

**Step 2: Upload APNs Key to Firebase**

Firebase Console → Project Settings → Cloud Messaging → Apple app → Upload APNs Authentication Key:
- Upload `.p8` file
- Enter **Key ID**
- Enter **Team ID** (from Apple Developer → Membership)

**Step 3: Download `GoogleService-Info.plist`**

Firebase Console → Project Settings → General → iOS app (vn.klc.driver) → Download `GoogleService-Info.plist`

Place in: `src/driver-app/GoogleService-Info.plist`

**Step 4: Update `app.json`**

```json
{
  "expo": {
    "ios": {
      "bundleIdentifier": "vn.klc.driver",
      "googleServicesFile": "./GoogleService-Info.plist"
    }
  }
}
```

**Step 5: Rebuild**

```bash
eas build --platform ios
```

After APNs key + GoogleService-Info.plist, iOS will use silent push verification — no reCAPTCHA.

---

### App Check (Optional — post-launch)

Protects Firebase APIs from abuse (bots, modified apps).

**After app is published to stores:**

1. Firebase Console → App Check → Register apps
2. Android: Select **Play Integrity** provider
3. iOS: Select **App Attest** provider
4. Enforce App Check on: Authentication, Firestore (if used)

---

## Backend (Already Done)

The BFF already verifies Firebase ID tokens:

```csharp
// Program.cs line 76-100
FirebaseAdmin.FirebaseApp.Create(new AppOptions {
    Credential = GoogleCredential.FromFile(firebaseCredPath),
    ProjectId = "klc-ev-charging"
});
```

Endpoints that verify Firebase tokens:
- `POST /api/v1/auth/firebase-verify-phone`
- `POST /api/v1/auth/firebase-phone`
- `POST /api/v1/auth/firebase-reset-password`

No backend changes needed.

---

## Verification Checklist

| # | Task | Owner | Status |
|---|------|-------|--------|
| 1 | Play Integrity API on GCP | Backend | ✅ Done |
| 2 | Firebase App Check API on GCP | Backend | ✅ Done |
| 3 | Phone enforcement → ENFORCE | Admin | Pending |
| 4 | SMS region → Vietnam only | Admin | Pending |
| 5 | Sign-up quota → 100/day | Admin | Pending |
| 6 | Android SHA-1 + SHA-256 | Mobile dev | Pending |
| 7 | `google-services.json` in build | Mobile dev | Pending |
| 8 | iOS APNs key (.p8) uploaded | Mobile dev | Pending |
| 9 | `GoogleService-Info.plist` in build | Mobile dev | Pending |
| 10 | App Check registration | Mobile dev | Post-launch |

## How to Test

After setup, phone verification should:
- **Android**: Show NO reCAPTCHA — silent verification via Play Integrity
- **iOS**: Show NO reCAPTCHA — silent verification via APNs push
- **Fallback**: If silent verification fails, reCAPTCHA appears as backup (this is OK)

Test with a real device (not emulator) on production build.
