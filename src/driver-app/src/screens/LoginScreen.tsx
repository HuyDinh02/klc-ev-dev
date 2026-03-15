import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { AxiosError } from 'axios';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Button, Card } from '../components/common';
import { useAuthStore } from '../stores';
import { authApi, mapAuthUserToProfile } from '../api';
import type { ApiError } from '../types';

export function LoginScreen() {
  const { t } = useTranslation();
  const { login } = useAuthStore();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert(t('common.error'), t('login.errorEmpty'));
      return;
    }

    setLoading(true);
    try {
      const result = await authApi.login({ phoneNumber: email, password });

      if (result.success && result.accessToken && result.user) {
        const userProfile = mapAuthUserToProfile(result.user);
        if (userProfile) {
          await login(result.accessToken, result.refreshToken ?? '', userProfile);
        }
      } else {
        Alert.alert(t('common.error'), result.error ?? t('login.errorInvalidCredentials'));
      }
    } catch (error) {
      const axiosError = error as AxiosError<ApiError>;
      if (axiosError.response) {
        const status = axiosError.response.status;
        if (status === 401) {
          Alert.alert(t('common.error'), t('login.errorInvalidCredentials'));
        } else {
          const message = axiosError.response.data?.message ?? t('login.errorLoginFailed');
          Alert.alert(t('common.error'), message);
        }
      } else if (axiosError.request) {
        Alert.alert(t('common.error'), t('login.errorNetwork'));
      } else {
        Alert.alert(t('common.error'), t('login.errorLoginFailed'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={styles.content}
      >
        <View style={styles.header}>
          <View style={styles.logoContainer} accessible={false}>
            <Text style={styles.logoText}>K</Text>
          </View>
          <Text style={styles.title} accessibilityRole="header">{t('login.title')}</Text>
          <Text style={styles.subtitle}>{t('login.subtitle')}</Text>
        </View>

        <Card style={styles.formCard}>
          <Text style={styles.formTitle} accessibilityRole="header">{t('login.formTitle')}</Text>

          <View style={styles.inputContainer}>
            <Text style={styles.label}>{t('login.email')}</Text>
            <TextInput
              style={styles.input}
              value={email}
              onChangeText={setEmail}
              placeholder={t('login.emailPlaceholder')}
              placeholderTextColor={Colors.textLight}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              accessibilityLabel="Email"
              testID="login-email-input"
            />
          </View>

          <View style={styles.inputContainer}>
            <Text style={styles.label}>{t('login.password')}</Text>
            <TextInput
              style={styles.input}
              value={password}
              onChangeText={setPassword}
              placeholder={t('login.passwordPlaceholder')}
              placeholderTextColor={Colors.textLight}
              secureTextEntry
              accessibilityLabel="Password"
              testID="login-password-input"
            />
          </View>

          <TouchableOpacity style={styles.forgotPassword} accessibilityRole="button" accessibilityLabel="Forgot Password">
            <Text style={styles.forgotPasswordText}>{t('login.forgotPassword')}</Text>
          </TouchableOpacity>

          <Button
            title={loading ? t('login.signingIn') : t('login.signIn')}
            onPress={handleLogin}
            loading={loading}
            size="large"
            style={styles.loginButton}
            testID="login-submit-button"
          />

          <View style={styles.registerContainer}>
            <Text style={styles.registerText}>{t('login.noAccount')}</Text>
            <TouchableOpacity accessibilityRole="button" accessibilityLabel="Sign Up">
              <Text style={styles.registerLink}>{t('login.signUp')}</Text>
            </TouchableOpacity>
          </View>
        </Card>

        <View style={styles.demoCredentials}>
          <Text style={styles.demoTitle}>{t('login.demoTitle')}</Text>
          <Text style={styles.demoText}>{t('login.demoEmail')}</Text>
          <Text style={styles.demoText}>{t('login.demoPassword')}</Text>
        </View>
      </KeyboardAvoidingView>
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
  logoContainer: {
    width: 80,
    height: 80,
    borderRadius: 20,
    backgroundColor: Colors.background,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
    ...Shadows.medium,
  },
  logoText: {
    fontSize: 40,
    fontWeight: '700',
    color: Colors.primary,
  },
  title: {
    fontSize: 32,
    fontWeight: '700',
    color: Colors.background,
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: Colors.background,
    opacity: 0.8,
  },
  formCard: {
    ...Shadows.medium,
  },
  formTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 24,
    textAlign: 'center',
  },
  inputContainer: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    color: Colors.text,
    marginBottom: 8,
  },
  input: {
    backgroundColor: Colors.surface,
    borderRadius: 12,
    padding: 16,
    fontSize: 16,
    color: Colors.text,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  forgotPassword: {
    alignSelf: 'flex-end',
    marginBottom: 24,
  },
  forgotPasswordText: {
    fontSize: 14,
    color: Colors.primary,
    fontWeight: '500',
  },
  loginButton: {
    marginBottom: 16,
  },
  registerContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
  },
  registerText: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  registerLink: {
    fontSize: 14,
    color: Colors.primary,
    fontWeight: '600',
  },
  demoCredentials: {
    marginTop: 32,
    padding: 16,
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 12,
  },
  demoTitle: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.background,
    marginBottom: 8,
    textAlign: 'center',
  },
  demoText: {
    fontSize: 12,
    color: Colors.background,
    opacity: 0.8,
    textAlign: 'center',
  },
});
