import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
  ScrollView,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { AxiosError } from 'axios';
import { Colors, Shadows } from '../constants/colors';
import { Button, Card } from '../components/common';
import { authApi } from '../api';
import type { RootStackParamList } from '../navigation/types';

type Nav = NativeStackNavigationProp<RootStackParamList>;

export function RegisterScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<Nav>();

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const validate = (): string | null => {
    if (!fullName.trim()) return t('register.errorNameRequired');
    if (!phone.trim()) return t('register.errorPhoneRequired');
    if (!/^0\d{9}$/.test(phone.trim())) return t('register.errorPhoneInvalid');
    if (!password) return t('register.errorPasswordRequired');
    if (password.length < 8) return t('register.errorPasswordTooShort');
    if (password !== confirmPassword) return t('register.errorPasswordMismatch');
    return null;
  };

  const handleRegister = async () => {
    const error = validate();
    if (error) {
      Alert.alert(t('common.error'), error);
      return;
    }

    setLoading(true);
    try {
      const result = await authApi.register({
        phoneNumber: phone.trim(),
        fullName: fullName.trim(),
        password,
      });

      if (result.success) {
        navigation.navigate('OtpVerification', { phoneNumber: phone.trim() });
      } else {
        const msg = result.error?.message ?? '';
        if (msg.includes('PhoneAlreadyRegistered')) {
          Alert.alert(t('common.error'), t('register.errorPhoneTaken'));
        } else {
          Alert.alert(t('common.error'), t('register.errorFailed'));
        }
      }
    } catch (err) {
      const axiosError = err as AxiosError;
      if (axiosError.response?.status === 400) {
        Alert.alert(t('common.error'), t('register.errorPhoneTaken'));
      } else {
        Alert.alert(t('common.error'), t('register.errorFailed'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={styles.keyboardView}
      >
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.header}>
            <View style={styles.logoContainer} accessible={false}>
              <Text style={styles.logoText}>K</Text>
            </View>
            <Text style={styles.title} accessibilityRole="header">{t('register.title')}</Text>
            <Text style={styles.subtitle}>{t('register.subtitle')}</Text>
          </View>

          <Card style={styles.formCard}>
            <View style={styles.inputContainer}>
              <Text style={styles.label}>{t('register.fullName')}</Text>
              <TextInput
                style={styles.input}
                value={fullName}
                onChangeText={setFullName}
                placeholder={t('register.fullNamePlaceholder')}
                placeholderTextColor={Colors.textLight}
                autoCapitalize="words"
                autoCorrect={false}
                accessibilityLabel="Full Name"
                testID="register-name-input"
              />
            </View>

            <View style={styles.inputContainer}>
              <Text style={styles.label}>{t('register.phone')}</Text>
              <TextInput
                style={styles.input}
                value={phone}
                onChangeText={setPhone}
                placeholder={t('register.phonePlaceholder')}
                placeholderTextColor={Colors.textLight}
                keyboardType="phone-pad"
                autoCapitalize="none"
                autoCorrect={false}
                accessibilityLabel="Phone"
                testID="register-phone-input"
              />
            </View>

            <View style={styles.inputContainer}>
              <Text style={styles.label}>{t('register.password')}</Text>
              <TextInput
                style={styles.input}
                value={password}
                onChangeText={setPassword}
                placeholder={t('register.passwordPlaceholder')}
                placeholderTextColor={Colors.textLight}
                secureTextEntry
                accessibilityLabel="Password"
                testID="register-password-input"
              />
            </View>

            <View style={styles.inputContainer}>
              <Text style={styles.label}>{t('register.confirmPassword')}</Text>
              <TextInput
                style={styles.input}
                value={confirmPassword}
                onChangeText={setConfirmPassword}
                placeholder={t('register.confirmPasswordPlaceholder')}
                placeholderTextColor={Colors.textLight}
                secureTextEntry
                accessibilityLabel="Confirm Password"
                testID="register-confirm-password-input"
              />
            </View>

            <Button
              title={loading ? t('register.signingUp') : t('register.signUp')}
              onPress={handleRegister}
              loading={loading}
              size="large"
              style={styles.submitButton}
              testID="register-submit-button"
            />

            <View style={styles.loginContainer}>
              <Text style={styles.loginText}>{t('register.haveAccount')}</Text>
              <TouchableOpacity
                accessibilityRole="button"
                accessibilityLabel="Sign In"
                onPress={() => navigation.goBack()}
              >
                <Text style={styles.loginLink}>{t('register.signIn')}</Text>
              </TouchableOpacity>
            </View>
          </Card>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.primary,
  },
  keyboardView: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 24,
    paddingBottom: 40,
  },
  header: {
    alignItems: 'center',
    marginBottom: 32,
  },
  logoContainer: {
    width: 70,
    height: 70,
    borderRadius: 18,
    backgroundColor: Colors.background,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 12,
    ...Shadows.medium,
  },
  logoText: {
    fontSize: 36,
    fontWeight: '700',
    color: Colors.primary,
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    color: Colors.background,
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 15,
    color: Colors.background,
    opacity: 0.8,
  },
  formCard: {
    ...Shadows.medium,
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
  submitButton: {
    marginTop: 8,
    marginBottom: 16,
  },
  loginContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
  },
  loginText: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  loginLink: {
    fontSize: 14,
    color: Colors.primary,
    fontWeight: '600',
  },
});
