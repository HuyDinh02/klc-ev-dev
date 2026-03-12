import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Switch,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Card } from '../components/common';
import { notificationsApi } from '../api/notifications';
import type { NotificationPreferences } from '../types';

export function SettingsScreen() {
  const { t, i18n } = useTranslation();
  const [preferences, setPreferences] = useState<NotificationPreferences | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const isVietnamese = i18n.language === 'vi';

  const loadPreferences = async () => {
    try {
      const prefs = await notificationsApi.getPreferences();
      setPreferences(prefs);
    } catch (error) {
      console.error('Failed to load notification preferences:', error);
    } finally {
      setLoading(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadPreferences();
    }, [])
  );

  const handleLanguageToggle = (value: boolean) => {
    const newLang = value ? 'vi' : 'en';
    i18n.changeLanguage(newLang);
  };

  const handlePreferenceToggle = async (
    key: keyof NotificationPreferences,
    value: boolean,
  ) => {
    if (!preferences) return;

    const updated = { ...preferences, [key]: value };
    setPreferences(updated);
    setSaving(true);

    try {
      const result = await notificationsApi.updatePreferences(updated);
      setPreferences(result);
    } catch (error) {
      console.error('Failed to update notification preferences:', error);
      setPreferences(preferences);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <SafeAreaView style={styles.container} edges={['top']}>
        <View style={styles.header}>
          <Text style={styles.title} accessibilityRole="header">
            {t('settings.title')}
          </Text>
        </View>
        <View
          style={styles.loadingContainer}
          accessible={true}
          accessibilityLabel={t('common.loading')}
          accessibilityState={{ busy: true }}
        >
          <ActivityIndicator size="large" color={Colors.primary} />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title} accessibilityRole="header">
          {t('settings.title')}
        </Text>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('settings.language')}</Text>
          <Card style={styles.card}>
            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingLabel}>
                  {t('settings.vietnameseLanguage')}
                </Text>
                <Text style={styles.settingDescription}>
                  {isVietnamese
                    ? t('settings.currentLanguageVi')
                    : t('settings.currentLanguageEn')}
                </Text>
              </View>
              <Switch
                value={isVietnamese}
                onValueChange={handleLanguageToggle}
                trackColor={{ false: Colors.border, true: Colors.primary + '80' }}
                thumbColor={isVietnamese ? Colors.primary : Colors.textLight}
                accessibilityLabel={t('settings.toggleLanguage')}
                accessibilityRole="switch"
                accessibilityState={{ checked: isVietnamese }}
              />
            </View>
          </Card>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>
            {t('settings.notificationPreferences')}
          </Text>
          <Card style={styles.card} padding="none">
            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingLabel}>
                  {t('settings.chargingComplete')}
                </Text>
                <Text style={styles.settingDescription}>
                  {t('settings.chargingCompleteDesc')}
                </Text>
              </View>
              <Switch
                value={preferences?.chargingComplete ?? true}
                onValueChange={(value) =>
                  handlePreferenceToggle('chargingComplete', value)
                }
                trackColor={{ false: Colors.border, true: Colors.primary + '80' }}
                thumbColor={
                  preferences?.chargingComplete ? Colors.primary : Colors.textLight
                }
                disabled={saving}
                accessibilityLabel={t('settings.chargingComplete')}
                accessibilityRole="switch"
                accessibilityState={{
                  checked: preferences?.chargingComplete ?? true,
                  disabled: saving,
                }}
              />
            </View>

            <View style={styles.divider} />

            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingLabel}>
                  {t('settings.paymentAlerts')}
                </Text>
                <Text style={styles.settingDescription}>
                  {t('settings.paymentAlertsDesc')}
                </Text>
              </View>
              <Switch
                value={preferences?.paymentAlerts ?? true}
                onValueChange={(value) =>
                  handlePreferenceToggle('paymentAlerts', value)
                }
                trackColor={{ false: Colors.border, true: Colors.primary + '80' }}
                thumbColor={
                  preferences?.paymentAlerts ? Colors.primary : Colors.textLight
                }
                disabled={saving}
                accessibilityLabel={t('settings.paymentAlerts')}
                accessibilityRole="switch"
                accessibilityState={{
                  checked: preferences?.paymentAlerts ?? true,
                  disabled: saving,
                }}
              />
            </View>

            <View style={styles.divider} />

            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingLabel}>
                  {t('settings.faultAlerts')}
                </Text>
                <Text style={styles.settingDescription}>
                  {t('settings.faultAlertsDesc')}
                </Text>
              </View>
              <Switch
                value={preferences?.faultAlerts ?? true}
                onValueChange={(value) =>
                  handlePreferenceToggle('faultAlerts', value)
                }
                trackColor={{ false: Colors.border, true: Colors.primary + '80' }}
                thumbColor={
                  preferences?.faultAlerts ? Colors.primary : Colors.textLight
                }
                disabled={saving}
                accessibilityLabel={t('settings.faultAlerts')}
                accessibilityRole="switch"
                accessibilityState={{
                  checked: preferences?.faultAlerts ?? true,
                  disabled: saving,
                }}
              />
            </View>

            <View style={styles.divider} />

            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingLabel}>
                  {t('settings.promotions')}
                </Text>
                <Text style={styles.settingDescription}>
                  {t('settings.promotionsDesc')}
                </Text>
              </View>
              <Switch
                value={preferences?.promotions ?? true}
                onValueChange={(value) =>
                  handlePreferenceToggle('promotions', value)
                }
                trackColor={{ false: Colors.border, true: Colors.primary + '80' }}
                thumbColor={
                  preferences?.promotions ? Colors.primary : Colors.textLight
                }
                disabled={saving}
                accessibilityLabel={t('settings.promotions')}
                accessibilityRole="switch"
                accessibilityState={{
                  checked: preferences?.promotions ?? true,
                  disabled: saving,
                }}
              />
            </View>
          </Card>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('settings.about')}</Text>
          <Card style={styles.card}>
            <View style={styles.settingRow}>
              <Text style={styles.settingLabel}>{t('settings.appVersion')}</Text>
              <Text
                style={styles.settingValue}
                accessibilityLabel={t('common.version', { version: '1.0.0' })}
              >
                1.0.0
              </Text>
            </View>
          </Card>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 12,
    backgroundColor: Colors.background,
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    color: Colors.text,
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 32,
  },
  section: {
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.textSecondary,
    marginBottom: 8,
    marginLeft: 4,
  },
  card: {
    ...Shadows.small,
  },
  settingRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
  },
  settingInfo: {
    flex: 1,
    marginRight: 16,
  },
  settingLabel: {
    fontSize: 16,
    fontWeight: '500',
    color: Colors.text,
  },
  settingDescription: {
    fontSize: 13,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  settingValue: {
    fontSize: 16,
    color: Colors.textSecondary,
  },
  divider: {
    height: 1,
    backgroundColor: Colors.border,
    marginHorizontal: 16,
  },
});
