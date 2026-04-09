import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation, useRoute } from '@react-navigation/native';
import type { NativeStackNavigationProp, NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Button, Card } from '../components/common';
import { authApi } from '../api';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'OtpVerification'>;
type Nav = NativeStackNavigationProp<RootStackParamList>;

const RESEND_COOLDOWN = 60;

export function OtpVerificationScreen({ route }: Props) {
  const { t } = useTranslation();
  const navigation = useNavigation<Nav>();
  const { phoneNumber } = route.params;

  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [countdown, setCountdown] = useState(RESEND_COOLDOWN);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    startCountdown();
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const startCountdown = () => {
    setCountdown(RESEND_COOLDOWN);
    if (timerRef.current) clearInterval(timerRef.current);
    timerRef.current = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  };

  const handleVerify = async () => {
    if (otp.length !== 6) {
      Alert.alert(t('common.error'), t('otp.errorInvalid'));
      return;
    }

    setLoading(true);
    try {
      const result = await authApi.verifyOtp({ phoneNumber, otp });
      if (result.success) {
        Alert.alert(t('otp.successTitle'), t('otp.successMessage'), [
          {
            text: t('login.signIn'),
            onPress: () => {
              navigation.navigate('Login');
            },
          },
        ]);
      } else {
        const msg = result.error?.message ?? '';
        if (msg.includes('OtpExpired')) {
          Alert.alert(t('common.error'), t('otp.errorExpired'));
        } else {
          Alert.alert(t('common.error'), t('otp.errorInvalid'));
        }
      }
    } catch {
      Alert.alert(t('common.error'), t('otp.errorFailed'));
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    setResendLoading(true);
    try {
      await authApi.resendOtp({ phoneNumber });
      startCountdown();
    } catch {
      Alert.alert(t('common.error'), t('otp.errorFailed'));
    } finally {
      setResendLoading(false);
    }
  };

  const handleOtpChange = (text: string) => {
    const digits = text.replace(/\D/g, '').slice(0, 6);
    setOtp(digits);
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <View style={styles.header}>
          <View style={styles.iconContainer} accessible={false}>
            <Text style={styles.iconText}>📱</Text>
          </View>
          <Text style={styles.title} accessibilityRole="header">{t('otp.title')}</Text>
          <Text style={styles.subtitle}>
            {t('otp.subtitle', { phone: phoneNumber })}
          </Text>
        </View>

        <Card style={styles.card}>
          <View style={styles.inputContainer}>
            <Text style={styles.label}>{t('otp.otpCode')}</Text>
            <TextInput
              style={styles.otpInput}
              value={otp}
              onChangeText={handleOtpChange}
              placeholder={t('otp.otpPlaceholder')}
              placeholderTextColor={Colors.textLight}
              keyboardType="number-pad"
              maxLength={6}
              autoFocus
              textAlign="center"
              accessibilityLabel="OTP Code"
              testID="otp-input"
            />
          </View>

          <Button
            title={loading ? t('otp.verifying') : t('otp.verify')}
            onPress={handleVerify}
            loading={loading}
            size="large"
            style={styles.verifyButton}
            testID="otp-verify-button"
          />

          <View style={styles.resendContainer}>
            {countdown > 0 ? (
              <Text style={styles.resendCountdown}>
                {t('otp.resendIn', { seconds: countdown })}
              </Text>
            ) : (
              <TouchableOpacity
                onPress={handleResend}
                disabled={resendLoading}
                accessibilityRole="button"
                accessibilityLabel="Resend OTP"
                testID="otp-resend-button"
              >
                <Text style={styles.resendLink}>
                  {resendLoading ? '...' : t('otp.resend')}
                </Text>
              </TouchableOpacity>
            )}
          </View>
        </Card>

        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          accessibilityRole="button"
        >
          <Text style={styles.backText}>← {t('common.goBack')}</Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.primary,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  header: {
    alignItems: 'center',
    marginBottom: 32,
  },
  iconContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: 'rgba(255,255,255,0.2)',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  iconText: {
    fontSize: 40,
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    color: Colors.background,
    marginBottom: 8,
    textAlign: 'center',
  },
  subtitle: {
    fontSize: 15,
    color: Colors.background,
    opacity: 0.85,
    textAlign: 'center',
    lineHeight: 22,
  },
  card: {
    ...Shadows.medium,
  },
  inputContainer: {
    marginBottom: 20,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    color: Colors.text,
    marginBottom: 8,
  },
  otpInput: {
    backgroundColor: Colors.surface,
    borderRadius: 12,
    padding: 20,
    fontSize: 28,
    fontWeight: '700',
    color: Colors.text,
    borderWidth: 2,
    borderColor: Colors.primary,
    letterSpacing: 12,
  },
  verifyButton: {
    marginBottom: 16,
  },
  resendContainer: {
    alignItems: 'center',
    paddingVertical: 4,
  },
  resendCountdown: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  resendLink: {
    fontSize: 14,
    color: Colors.primary,
    fontWeight: '600',
  },
  backButton: {
    marginTop: 24,
    alignItems: 'center',
  },
  backText: {
    fontSize: 15,
    color: Colors.background,
    opacity: 0.8,
  },
});
