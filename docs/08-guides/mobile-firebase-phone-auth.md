# Hướng dẫn tích hợp Firebase Phone Auth cho Mobile App

## Tổng quan

Firebase Phone Auth hoạt động **trên mobile** (không phải backend):
- Firebase SDK trên mobile gửi SMS + verify OTP
- Mobile nhận Firebase ID Token
- Mobile gửi token lên backend → backend verify → trả JWT

```
Mobile App                Firebase Cloud           Backend (BFF)
─────────                ──────────────           ─────────────
1. signInWithPhoneNumber()
     → Firebase gửi SMS ──→ User nhận OTP
2. User nhập OTP
3. confirm(otp)
     → Firebase verify  ──→ OK, trả idToken
4. POST /api/v1/auth/firebase-phone ──────────→ 5. Verify Firebase token
   { idToken: "eyJ..." }                        6. Tìm/tạo AppUser
                                                 7. Trả JWT
   ← { accessToken, user } ←────────────────── 8. App lưu JWT, navigate Home
```

## Backend — Đã sẵn sàng ✅

| Component | Endpoint | Status |
|-----------|----------|--------|
| Firebase Phone Login | `POST /api/v1/auth/firebase-phone` | ✅ Deployed |
| Firebase Admin SDK | Cloud Run (GCP default credentials) | ✅ Initialized |
| Auto-create user | Tạo AppUser nếu phone chưa đăng ký | ✅ Implemented |
| Trả JWT + user profile | accessToken, refreshToken, user | ✅ Implemented |

**Mobile dev chỉ cần implement phía client. Backend không cần thay đổi gì.**

---

## Bước 1: Cài đặt dependencies

```bash
cd src/driver-app
npx expo install @react-native-firebase/app @react-native-firebase/auth
```

## Bước 2: Cấu hình Firebase project

### 2.1 Firebase Console
1. Vào https://console.firebase.google.com → project `klc-ev-charging`
2. Authentication → Sign-in method → Enable **Phone**
3. Thêm test phone numbers (không gửi SMS thật):
   - `+84983987986` → OTP: `123456`
   - `+84901234001` → OTP: `123456`

### 2.2 Android
1. Firebase Console → Project Settings → Your apps → Add Android app
   - Package name: `vn.klc.driver`
   - SHA-1: lấy bằng `cd android && ./gradlew signingReport`
2. Tải `google-services.json` → đặt vào `src/driver-app/`

### 2.3 iOS
1. Firebase Console → Project Settings → Your apps → Add iOS app
   - Bundle ID: `vn.klc.driver`
2. Tải `GoogleService-Info.plist` → đặt vào `src/driver-app/ios/`
3. Upload APNs key (Settings → Cloud Messaging → APNs)

### 2.4 app.json
```json
{
  "expo": {
    "plugins": [
      "@react-native-firebase/app",
      "@react-native-firebase/auth",
      "expo-location",
      "expo-secure-store",
      ["expo-camera", { ... }],
      ["expo-notifications", { ... }]
    ],
    "android": {
      "googleServicesFile": "./google-services.json",
      "package": "vn.klc.driver"
    },
    "ios": {
      "googleServicesFile": "./GoogleService-Info.plist",
      "bundleIdentifier": "vn.klc.driver"
    }
  }
}
```

## Bước 3: Tạo Firebase Auth service

Tạo file `src/driver-app/src/services/firebaseAuth.ts`:

```typescript
import auth from '@react-native-firebase/auth';

/**
 * Bước 1: Gửi OTP đến số điện thoại
 * Firebase tự động gửi SMS (hoặc dùng test number)
 * Trả về confirmation object để verify OTP
 */
export async function sendOtp(phoneNumber: string) {
  // Chuẩn hóa: 0983987986 → +84983987986
  const formatted = phoneNumber.startsWith('0')
    ? '+84' + phoneNumber.slice(1)
    : phoneNumber.startsWith('+') ? phoneNumber : '+84' + phoneNumber;

  const confirmation = await auth().signInWithPhoneNumber(formatted);
  return confirmation;
}

/**
 * Bước 2: Verify OTP và lấy Firebase ID token
 * Gọi sau khi user nhập OTP 6 số
 */
export async function verifyOtpAndGetToken(
  confirmation: any,
  otp: string
): Promise<string> {
  await confirmation.confirm(otp);
  const idToken = await auth().currentUser?.getIdToken();
  if (!idToken) throw new Error('Failed to get Firebase token');
  return idToken;
}

/**
 * Đăng xuất Firebase (gọi khi user logout khỏi app)
 */
export async function signOutFirebase(): Promise<void> {
  await auth().signOut();
}
```

## Bước 4: Thêm API call

Thêm vào `src/driver-app/src/api/auth.ts`:

```typescript
export interface FirebasePhoneLoginRequest {
  idToken: string;
}

export const authApi = {
  // ... giữ nguyên login, refreshToken, logout ...

  /** Login/Register bằng Firebase Phone Auth */
  firebasePhoneLogin: async (request: FirebasePhoneLoginRequest): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/firebase-phone', request);
    return data;
  },
};
```

## Bước 5: Cập nhật LoginScreen

```typescript
import React, { useState, useRef } from 'react';
import { View, Text, TextInput, Alert, StyleSheet, KeyboardAvoidingView, Platform } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Colors } from '../constants/colors';
import { Button, Card } from '../components/common';
import { useAuthStore } from '../stores';
import { authApi, mapAuthUserToProfile } from '../api';
import { sendOtp, verifyOtpAndGetToken } from '../services/firebaseAuth';

export function LoginScreen() {
  const { t } = useTranslation();
  const { login } = useAuthStore();
  const [step, setStep] = useState<'phone' | 'otp'>('phone');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const confirmationRef = useRef<any>(null);

  // Bước 1: Gửi OTP
  const handleSendOtp = async () => {
    if (!phoneNumber || phoneNumber.length < 9) {
      Alert.alert('Lỗi', 'Nhập số điện thoại hợp lệ');
      return;
    }
    setLoading(true);
    try {
      confirmationRef.current = await sendOtp(phoneNumber);
      setStep('otp');
    } catch (error: any) {
      Alert.alert('Lỗi', error.message || 'Không gửi được OTP');
    } finally {
      setLoading(false);
    }
  };

  // Bước 2: Verify OTP → đăng nhập BFF
  const handleVerifyOtp = async () => {
    if (!otp || otp.length !== 6) {
      Alert.alert('Lỗi', 'Nhập OTP 6 số');
      return;
    }
    setLoading(true);
    try {
      // Verify OTP với Firebase → lấy idToken
      const idToken = await verifyOtpAndGetToken(confirmationRef.current, otp);

      // Gửi idToken lên BFF → nhận JWT
      const result = await authApi.firebasePhoneLogin({ idToken });

      if (result.success && result.accessToken && result.user) {
        const profile = mapAuthUserToProfile(result.user);
        if (profile) {
          await login(result.accessToken, result.refreshToken || '', profile);
          // Navigation tự động chuyển về Home
        }
      } else {
        Alert.alert('Lỗi', result.error || 'Đăng nhập thất bại');
      }
    } catch (error: any) {
      const msg = error.code === 'auth/invalid-verification-code'
        ? 'OTP không đúng. Vui lòng thử lại.'
        : error.message || 'Xác thực thất bại';
      Alert.alert('Lỗi', msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
        <Card style={styles.card}>
          <Text style={styles.logo}>⚡ K-Charge</Text>

          {step === 'phone' ? (
            <>
              <Text style={styles.subtitle}>Nhập số điện thoại để đăng nhập</Text>
              <TextInput
                style={styles.input}
                placeholder="0983 987 986"
                keyboardType="phone-pad"
                value={phoneNumber}
                onChangeText={setPhoneNumber}
                maxLength={12}
              />
              <Button
                title={loading ? 'Đang gửi...' : 'Nhận mã OTP'}
                onPress={handleSendOtp}
                disabled={loading}
              />
            </>
          ) : (
            <>
              <Text style={styles.subtitle}>
                Nhập mã OTP đã gửi đến {phoneNumber}
              </Text>
              <TextInput
                style={styles.input}
                placeholder="123456"
                keyboardType="number-pad"
                value={otp}
                onChangeText={setOtp}
                maxLength={6}
                autoFocus
              />
              <Button
                title={loading ? 'Đang xác thực...' : 'Đăng nhập'}
                onPress={handleVerifyOtp}
                disabled={loading}
              />
              <Button
                title="← Đổi số điện thoại"
                variant="outline"
                onPress={() => { setStep('phone'); setOtp(''); }}
                style={{ marginTop: 12 }}
              />
            </>
          )}
        </Card>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 20, backgroundColor: '#f5f5f5' },
  card: { padding: 24 },
  logo: { fontSize: 32, fontWeight: 'bold', textAlign: 'center', color: Colors.primary, marginBottom: 8 },
  subtitle: { fontSize: 14, color: '#666', textAlign: 'center', marginBottom: 24 },
  input: {
    borderWidth: 1, borderColor: '#ddd', borderRadius: 12, padding: 16,
    fontSize: 18, marginBottom: 16, textAlign: 'center', letterSpacing: 4,
    backgroundColor: '#fff',
  },
});
```

## Bước 6: Build và test

**Quan trọng: Firebase Phone Auth KHÔNG hoạt động trên Expo Go.** Phải build dev client:

```bash
# Android
npx expo run:android

# iOS
npx expo run:ios
```

### Test với test phone number (không gửi SMS thật)
1. Firebase Console → Authentication → Phone → thêm test number:
   - Phone: `+84983987986`, Code: `123456`
2. Mở app → nhập `0983987986` → nhập OTP `123456`
3. App nhận JWT → navigate Home

### Test với số thật
1. Nhập số thật → Firebase gửi SMS
2. Nhập OTP từ SMS
3. App nhận JWT → navigate Home

## Lưu ý

| Item | Chi tiết |
|------|---------|
| **Expo Go** | Không hỗ trợ Firebase native → phải dùng `expo run:android/ios` |
| **SHA-1** | Android cần SHA-1 fingerprint trong Firebase Console |
| **APNs** | iOS cần APNs key upload trong Firebase Console |
| **Rate limit** | Firebase free tier: 10,000 SMS/tháng (đủ cho 500 users) |
| **Test numbers** | Dùng test numbers khi dev → không tốn SMS quota |
| **Không cần password** | User chỉ cần số điện thoại, không cần tạo password |
| **Auto-create user** | Backend tự tạo AppUser nếu phone chưa có trong hệ thống |
