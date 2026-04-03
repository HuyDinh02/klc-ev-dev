# Hướng dẫn tích hợp Firebase Phone Auth cho Mobile App

## Tổng quan

Thay thế login bằng phone/password hiện tại bằng Firebase Phone Auth:
- Firebase gửi SMS OTP trực tiếp → User nhập OTP → Firebase verify → Gửi ID token lên BFF
- BFF verify Firebase token → tạo/tìm user → trả JWT cho app

```
Mobile App                Firebase Cloud           Backend (BFF)
─────────                ──────────────           ─────────────
1. signInWithPhoneNumber()
         → Firebase gửi SMS ──→ User nhận OTP
2. User nhập OTP
3. confirm(otp)
         → Firebase verify ──→ OK
4. getIdToken()
5. POST /api/v1/auth/firebase-phone ──────────→ 6. Verify Firebase token
   { idToken: "eyJ..." }                        7. Tìm/tạo AppUser
                                                 8. Trả JWT
   ← { accessToken, user } ←────────────────── 9. App lưu JWT
```

## Bước 1: Cài đặt dependencies

```bash
cd src/driver-app
npx expo install @react-native-firebase/app @react-native-firebase/auth
```

## Bước 2: Cấu hình Firebase

### Android
1. Tải `google-services.json` từ Firebase Console → Project Settings → Your apps → Android
   - Package name: `vn.klc.driver`
2. Đặt file vào: `src/driver-app/android/app/google-services.json`

### iOS
1. Tải `GoogleService-Info.plist` từ Firebase Console → Project Settings → Your apps → iOS
   - Bundle ID: `vn.klc.driver`
2. Đặt file vào: `src/driver-app/ios/KlcDriver/GoogleService-Info.plist`

### app.json — Thêm plugin
```json
{
  "expo": {
    "plugins": [
      "@react-native-firebase/app",
      "@react-native-firebase/auth",
      "expo-location",
      "expo-secure-store",
      ...
    ],
    "android": {
      "googleServicesFile": "./google-services.json",
      ...
    },
    "ios": {
      "googleServicesFile": "./GoogleService-Info.plist",
      ...
    }
  }
}
```

## Bước 3: Tạo Firebase Auth service

Tạo file `src/driver-app/src/services/firebaseAuth.ts`:

```typescript
import auth, { FirebaseAuthTypes } from '@react-native-firebase/auth';

export interface PhoneAuthResult {
  success: boolean;
  verificationId?: string;
  error?: string;
}

export interface OtpVerifyResult {
  success: boolean;
  idToken?: string;
  phoneNumber?: string;
  error?: string;
}

/**
 * Bước 1: Gửi OTP đến số điện thoại
 * Firebase tự động gửi SMS
 */
export async function sendOtp(phoneNumber: string): Promise<PhoneAuthResult> {
  try {
    // Chuẩn hóa số điện thoại VN: 0983987986 → +84983987986
    const formatted = phoneNumber.startsWith('0')
      ? '+84' + phoneNumber.slice(1)
      : phoneNumber.startsWith('+') ? phoneNumber : '+84' + phoneNumber;

    const confirmation = await auth().signInWithPhoneNumber(formatted);

    // Lưu confirmation để verify OTP sau
    return {
      success: true,
      verificationId: confirmation.verificationId,
    };
  } catch (error: any) {
    console.error('Firebase sendOtp error:', error);
    return {
      success: false,
      error: error.message || 'Không gửi được OTP',
    };
  }
}

/**
 * Bước 2: Verify OTP và lấy Firebase ID token
 */
export async function verifyOtp(
  verificationId: string,
  otp: string
): Promise<OtpVerifyResult> {
  try {
    const credential = auth.PhoneAuthProvider.credential(verificationId, otp);
    const userCredential = await auth().signInWithCredential(credential);

    // Lấy ID token để gửi lên BFF
    const idToken = await userCredential.user.getIdToken();
    const phoneNumber = userCredential.user.phoneNumber;

    return {
      success: true,
      idToken,
      phoneNumber: phoneNumber || undefined,
    };
  } catch (error: any) {
    console.error('Firebase verifyOtp error:', error);
    return {
      success: false,
      error: error.code === 'auth/invalid-verification-code'
        ? 'OTP không đúng'
        : error.message || 'Xác thực thất bại',
    };
  }
}

/**
 * Đăng xuất Firebase (optional — app dùng JWT riêng)
 */
export async function signOutFirebase(): Promise<void> {
  try {
    await auth().signOut();
  } catch { /* ignore */ }
}
```

## Bước 4: Thêm API endpoint

Thêm vào `src/driver-app/src/api/auth.ts`:

```typescript
export interface FirebasePhoneLoginRequest {
  idToken: string;
}

export const authApi = {
  // ... existing login, refreshToken, logout ...

  /**
   * Login/Register bằng Firebase Phone Auth
   * BFF verify Firebase token, tạo user nếu chưa có, trả JWT
   */
  firebasePhoneLogin: async (request: FirebasePhoneLoginRequest): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/firebase-phone', request);
    return data;
  },
};
```

## Bước 5: Cập nhật LoginScreen

Thay thế `src/driver-app/src/screens/LoginScreen.tsx`:

```typescript
import React, { useState } from 'react';
import {
  View, Text, StyleSheet, TextInput, KeyboardAvoidingView,
  Platform, Alert, ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Button, Card } from '../components/common';
import { useAuthStore } from '../stores';
import { authApi, mapAuthUserToProfile } from '../api';
import { sendOtp, verifyOtp } from '../services/firebaseAuth';

type AuthStep = 'phone' | 'otp';

export function LoginScreen() {
  const { t } = useTranslation();
  const { login } = useAuthStore();
  const [step, setStep] = useState<AuthStep>('phone');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otp, setOtp] = useState('');
  const [verificationId, setVerificationId] = useState('');
  const [loading, setLoading] = useState(false);

  // Bước 1: Gửi OTP
  const handleSendOtp = async () => {
    if (!phoneNumber || phoneNumber.length < 9) {
      Alert.alert(t('common.error'), 'Nhập số điện thoại hợp lệ');
      return;
    }

    setLoading(true);
    try {
      const result = await sendOtp(phoneNumber);
      if (result.success && result.verificationId) {
        setVerificationId(result.verificationId);
        setStep('otp');
      } else {
        Alert.alert(t('common.error'), result.error || 'Không gửi được OTP');
      }
    } catch (error) {
      Alert.alert(t('common.error'), 'Lỗi kết nối');
    } finally {
      setLoading(false);
    }
  };

  // Bước 2: Verify OTP → Login BFF
  const handleVerifyOtp = async () => {
    if (!otp || otp.length !== 6) {
      Alert.alert(t('common.error'), 'Nhập OTP 6 số');
      return;
    }

    setLoading(true);
    try {
      // Verify OTP với Firebase
      const firebaseResult = await verifyOtp(verificationId, otp);
      if (!firebaseResult.success || !firebaseResult.idToken) {
        Alert.alert(t('common.error'), firebaseResult.error || 'OTP không đúng');
        return;
      }

      // Gửi Firebase ID token lên BFF
      const bffResult = await authApi.firebasePhoneLogin({
        idToken: firebaseResult.idToken,
      });

      if (bffResult.success && bffResult.accessToken && bffResult.user) {
        const userProfile = mapAuthUserToProfile(bffResult.user);
        if (userProfile) {
          await login(bffResult.accessToken, bffResult.refreshToken || '', userProfile);
        }
      } else {
        Alert.alert(t('common.error'), bffResult.error || 'Đăng nhập thất bại');
      }
    } catch (error) {
      Alert.alert(t('common.error'), 'Lỗi kết nối');
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
        <Card style={styles.card}>
          <Text style={styles.title}>K-Charge</Text>

          {step === 'phone' ? (
            <>
              <Text style={styles.subtitle}>Nhập số điện thoại</Text>
              <TextInput
                style={styles.input}
                placeholder="0983987986"
                keyboardType="phone-pad"
                value={phoneNumber}
                onChangeText={setPhoneNumber}
                maxLength={12}
              />
              <Button
                title={loading ? 'Đang gửi...' : 'Gửi OTP'}
                onPress={handleSendOtp}
                disabled={loading}
              />
            </>
          ) : (
            <>
              <Text style={styles.subtitle}>Nhập mã OTP đã gửi đến {phoneNumber}</Text>
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
                title={loading ? 'Đang xác thực...' : 'Xác nhận'}
                onPress={handleVerifyOtp}
                disabled={loading}
              />
              <Button
                title="Gửi lại OTP"
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
  container: { flex: 1, justifyContent: 'center', padding: 20 },
  card: { padding: 24 },
  title: { fontSize: 28, fontWeight: 'bold', textAlign: 'center', color: Colors.primary, marginBottom: 8 },
  subtitle: { fontSize: 14, color: Colors.textSecondary, textAlign: 'center', marginBottom: 20 },
  input: { borderWidth: 1, borderColor: '#ddd', borderRadius: 8, padding: 14, fontSize: 16, marginBottom: 16, textAlign: 'center', letterSpacing: 2 },
});
```

## Bước 6: Test

### Test trên Expo Go (development)
```bash
cd src/driver-app
npx expo start
```

Firebase Phone Auth **không hoạt động trên Expo Go**. Phải build dev client:
```bash
npx expo run:android
# hoặc
npx expo run:ios
```

### Test cards cho Firebase (không cần SIM thật)
Trong Firebase Console → Authentication → Sign-in method → Phone:
1. Bật "Phone" provider
2. Thêm test phone numbers:
   - `+84983987986` → OTP: `123456`
   - `+84901234001` → OTP: `123456`

Các test numbers này không gửi SMS thật — chỉ cần nhập OTP đã set.

## Backend — Đã sẵn sàng ✅

| Component | Status |
|-----------|--------|
| BFF endpoint `POST /api/v1/auth/firebase-phone` | ✅ Deployed |
| Firebase Admin SDK trên Cloud Run | ✅ Initialized |
| Auto-create user nếu chưa có | ✅ Implemented |
| Trả JWT + user profile | ✅ Implemented |

## Lưu ý quan trọng

1. **Firebase project**: `klc-ev-charging` (GCP project đã có)
2. **SHA-1 fingerprint**: Phải thêm debug/release SHA-1 vào Firebase Console cho Android
3. **APNs**: Phải upload APNs key/cert cho iOS
4. **Expo**: Firebase Phone Auth cần native code → dùng `expo-dev-client`, không dùng Expo Go
5. **Rate limit**: Firebase giới hạn SMS free tier — dùng test phone numbers khi dev
