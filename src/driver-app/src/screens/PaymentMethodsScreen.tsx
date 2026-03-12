import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  RefreshControl,
  ActivityIndicator,
  Modal,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button } from '../components/common';
import { paymentsApi } from '../api/payments';
import type { PaymentMethodInfo, PaymentMethod } from '../types';

interface AvailableMethod {
  type: PaymentMethod;
  icon: string;
  nameKey: string;
}

const AVAILABLE_METHODS: AvailableMethod[] = [
  { type: 'MoMo', icon: '\uD83D\uDCF1', nameKey: 'paymentMethods.momo' },
  { type: 'ZaloPay', icon: '\uD83D\uDCF1', nameKey: 'paymentMethods.zalopay' },
  { type: 'VNPay', icon: '\uD83D\uDCF1', nameKey: 'paymentMethods.vnpay' },
  { type: 'OnePay', icon: '\uD83D\uDCF1', nameKey: 'paymentMethods.onepay' },
  { type: 'Card', icon: '\uD83D\uDCB3', nameKey: 'paymentMethods.card' },
];

function getMethodIcon(type: PaymentMethod): string {
  return type === 'Card' ? '\uD83D\uDCB3' : '\uD83D\uDCF1';
}

function getMethodNameKey(type: PaymentMethod): string {
  const map: Record<PaymentMethod, string> = {
    MoMo: 'paymentMethods.momo',
    ZaloPay: 'paymentMethods.zalopay',
    VNPay: 'paymentMethods.vnpay',
    OnePay: 'paymentMethods.onepay',
    Card: 'paymentMethods.card',
  };
  return map[type];
}

export function PaymentMethodsScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const [methods, setMethods] = useState<PaymentMethodInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [addModalVisible, setAddModalVisible] = useState(false);
  const [adding, setAdding] = useState(false);

  const loadMethods = async () => {
    try {
      setError(null);
      const data = await paymentsApi.getMethods();
      setMethods(data);
    } catch (err) {
      console.error('Failed to load payment methods:', err);
      setError(t('paymentMethods.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadMethods();
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadMethods();
  };

  const handleSetDefault = async (method: PaymentMethodInfo) => {
    if (method.isDefault) return;

    try {
      await paymentsApi.setDefaultMethod(method.id);
      setMethods((prev) =>
        prev.map((m) => ({
          ...m,
          isDefault: m.id === method.id,
        }))
      );
    } catch (err) {
      console.error('Failed to set default method:', err);
      Alert.alert(t('common.error'), t('paymentMethods.errorLoad'));
    }
  };

  const handleDelete = (method: PaymentMethodInfo) => {
    Alert.alert(
      t('paymentMethods.deleteConfirmTitle'),
      t('paymentMethods.deleteConfirmMessage'),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('common.remove'),
          style: 'destructive',
          onPress: async () => {
            try {
              await paymentsApi.deleteMethod(method.id);
              setMethods((prev) => prev.filter((m) => m.id !== method.id));
            } catch (err) {
              console.error('Failed to delete payment method:', err);
              Alert.alert(t('common.error'), t('paymentMethods.errorDelete'));
            }
          },
        },
      ]
    );
  };

  const handleAddMethod = async (type: PaymentMethod) => {
    setAdding(true);
    try {
      const newMethod = await paymentsApi.addMethod({ type });
      setMethods((prev) => [...prev, newMethod]);
      setAddModalVisible(false);
    } catch (err) {
      console.error('Failed to add payment method:', err);
      Alert.alert(t('common.error'), t('paymentMethods.errorAdd'));
    } finally {
      setAdding(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} accessibilityLabel="Loading" />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
          accessibilityRole="button"
          accessibilityLabel={t('common.goBack')}
        >
          <Text style={styles.backArrow}>{'\u2039'}</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle} accessibilityRole="header">
          {t('paymentMethods.title')}
        </Text>
        <View style={styles.headerSpacer} />
      </View>

      <ScrollView
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
      >
        {error ? (
          <View style={styles.errorContainer}>
            <Text style={styles.errorIcon}>!</Text>
            <Text style={styles.errorText}>{error}</Text>
            <Button
              title={t('common.retry')}
              variant="outline"
              size="small"
              onPress={() => {
                setLoading(true);
                loadMethods();
              }}
              style={styles.retryButton}
            />
          </View>
        ) : methods.length === 0 ? (
          <View style={styles.emptyContainer}>
            <View style={styles.emptyIconContainer}>
              <Text style={styles.emptyIcon}>{'\uD83D\uDCB3'}</Text>
            </View>
            <Text style={styles.emptyTitle}>{t('paymentMethods.noMethods')}</Text>
            <Text style={styles.emptySubtitle}>
              {t('paymentMethods.noMethodsSubtitle')}
            </Text>
            <Button
              title={t('paymentMethods.addMethod')}
              onPress={() => setAddModalVisible(true)}
              style={styles.emptyAddButton}
            />
          </View>
        ) : (
          <>
            {methods.map((method) => (
              <Card key={method.id} style={styles.methodCard}>
                <View style={styles.methodCardHeader}>
                  <View style={styles.methodIconContainer}>
                    <Text style={styles.methodIcon}>{getMethodIcon(method.type)}</Text>
                  </View>
                  <View style={styles.methodInfo}>
                    <Text style={styles.methodName}>{method.displayName}</Text>
                    <Text style={styles.methodType}>
                      {t(getMethodNameKey(method.type))}
                      {method.lastFourDigits
                        ? ` \u2022 ${t('paymentMethods.lastFour', { digits: method.lastFourDigits })}`
                        : ''}
                    </Text>
                  </View>
                  {method.isDefault && (
                    <View style={styles.defaultBadge}>
                      <Text style={styles.defaultStar}>{'\u2605'}</Text>
                      <Text style={styles.defaultBadgeText}>
                        {t('paymentMethods.default')}
                      </Text>
                    </View>
                  )}
                </View>

                <View style={styles.methodActions}>
                  {!method.isDefault && (
                    <TouchableOpacity
                      style={styles.actionButton}
                      onPress={() => handleSetDefault(method)}
                      accessibilityRole="button"
                      accessibilityLabel={`${t('paymentMethods.setDefault')} ${method.displayName}`}
                    >
                      <Text style={styles.actionButtonText}>
                        {t('paymentMethods.setDefault')}
                      </Text>
                    </TouchableOpacity>
                  )}
                  <TouchableOpacity
                    style={[styles.actionButton, styles.deleteAction]}
                    onPress={() => handleDelete(method)}
                    accessibilityRole="button"
                    accessibilityLabel={`${t('paymentMethods.deleteMethod')} ${method.displayName}`}
                  >
                    <Text style={styles.deleteActionText}>
                      {t('common.remove')}
                    </Text>
                  </TouchableOpacity>
                </View>
              </Card>
            ))}
          </>
        )}
      </ScrollView>

      {methods.length > 0 && (
        <View style={styles.bottomButtonContainer}>
          <Button
            title={t('paymentMethods.addMethod')}
            onPress={() => setAddModalVisible(true)}
            style={styles.addMethodButton}
          />
        </View>
      )}

      <Modal
        visible={addModalVisible}
        animationType="slide"
        presentationStyle="pageSheet"
        onRequestClose={() => setAddModalVisible(false)}
      >
        <SafeAreaView style={styles.modalContainer} edges={['top', 'bottom']}>
          <View style={styles.modalHeader}>
            <TouchableOpacity
              onPress={() => setAddModalVisible(false)}
              accessibilityRole="button"
              accessibilityLabel={t('common.cancel')}
            >
              <Text style={styles.modalCancel}>{t('common.cancel')}</Text>
            </TouchableOpacity>
            <Text style={styles.modalTitle} accessibilityRole="header">
              {t('paymentMethods.addTitle')}
            </Text>
            <View style={styles.modalHeaderSpacer} />
          </View>

          {adding ? (
            <View style={styles.addingContainer}>
              <ActivityIndicator size="large" color={Colors.primary} accessibilityLabel="Loading" />
            </View>
          ) : (
            <ScrollView style={styles.modalScrollView}>
              {AVAILABLE_METHODS.map((availableMethod) => (
                <TouchableOpacity
                  key={availableMethod.type}
                  style={styles.addMethodRow}
                  onPress={() => handleAddMethod(availableMethod.type)}
                  activeOpacity={0.7}
                  accessibilityRole="button"
                  accessibilityLabel={t(availableMethod.nameKey)}
                >
                  <View style={styles.addMethodIconContainer}>
                    <Text style={styles.addMethodIcon}>{availableMethod.icon}</Text>
                  </View>
                  <Text style={styles.addMethodName}>{t(availableMethod.nameKey)}</Text>
                  <Text style={styles.addMethodArrow}>{'\u203A'}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>
          )}
        </SafeAreaView>
      </Modal>
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
    backgroundColor: Colors.surface,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  backButton: {
    width: 32,
    height: 32,
    justifyContent: 'center',
    alignItems: 'center',
  },
  backArrow: {
    fontSize: 28,
    color: Colors.primary,
    fontWeight: '300',
    marginTop: -2,
  },
  headerTitle: {
    flex: 1,
    fontSize: 20,
    fontWeight: '700',
    color: Colors.text,
    textAlign: 'center',
  },
  headerSpacer: {
    width: 32,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 100,
  },

  // Error state
  errorContainer: {
    alignItems: 'center',
    paddingVertical: 48,
  },
  errorIcon: {
    fontSize: 32,
    fontWeight: '700',
    color: Colors.error,
    width: 56,
    height: 56,
    lineHeight: 56,
    textAlign: 'center',
    borderRadius: 28,
    backgroundColor: Colors.error + '15',
    overflow: 'hidden',
    marginBottom: 16,
  },
  errorText: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    marginBottom: 16,
  },
  retryButton: {
    minWidth: 100,
  },

  // Empty state
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 64,
  },
  emptyIconContainer: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: Colors.primary + '10',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 24,
  },
  emptyIcon: {
    fontSize: 48,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 8,
  },
  emptySubtitle: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    paddingHorizontal: 32,
    marginBottom: 24,
    lineHeight: 20,
  },
  emptyAddButton: {
    minWidth: 200,
  },

  // Method card
  methodCard: {
    marginBottom: 12,
  },
  methodCardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  methodIconContainer: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: Colors.primary + '10',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  methodIcon: {
    fontSize: 22,
  },
  methodInfo: {
    flex: 1,
  },
  methodName: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.text,
  },
  methodType: {
    fontSize: 13,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  defaultBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.primary + '20',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    gap: 4,
  },
  defaultStar: {
    fontSize: 12,
    color: Colors.primary,
  },
  defaultBadgeText: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.primary,
  },

  // Method actions
  methodActions: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
    gap: 12,
  },
  actionButton: {
    paddingVertical: 6,
    paddingHorizontal: 14,
    borderRadius: 8,
    backgroundColor: Colors.primary + '15',
  },
  actionButtonText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.primary,
  },
  deleteAction: {
    backgroundColor: Colors.error + '15',
  },
  deleteActionText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.error,
  },

  // Bottom add button
  bottomButtonContainer: {
    padding: 16,
    paddingBottom: 24,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  addMethodButton: {},

  // Modal
  modalContainer: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  modalCancel: {
    fontSize: 16,
    color: Colors.primary,
    fontWeight: '500',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  modalHeaderSpacer: {
    width: 50,
  },
  modalScrollView: {
    flex: 1,
  },
  addingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },

  // Add method rows
  addMethodRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 16,
    paddingHorizontal: 16,
    backgroundColor: Colors.background,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  addMethodIconContainer: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: Colors.primary + '10',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  addMethodIcon: {
    fontSize: 22,
  },
  addMethodName: {
    flex: 1,
    fontSize: 16,
    fontWeight: '500',
    color: Colors.text,
  },
  addMethodArrow: {
    fontSize: 20,
    color: Colors.textSecondary,
  },
});
