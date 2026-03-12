import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Badge } from '../components/common';
import { promotionsApi } from '../api/promotions';
import type { Promotion } from '../types';

function formatCurrency(amount: number): string {
  return (
    new Intl.NumberFormat('vi-VN', {
      style: 'decimal',
      maximumFractionDigits: 0,
    }).format(amount) + '\u0111'
  );
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();
  return `${day}/${month}/${year}`;
}

function isExpired(endDate: string): boolean {
  return new Date(endDate) < new Date();
}

export function PromotionsScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(false);

  const loadPromotions = async (reset = false) => {
    try {
      const result = await promotionsApi.getPromotions(
        reset ? undefined : nextCursor,
        20
      );
      if (reset) {
        setPromotions(result.items);
      } else {
        setPromotions((prev) => [...prev, ...result.items]);
      }
      setNextCursor(result.nextCursor);
      setHasMore(result.hasMore);
    } catch (error) {
      console.error('Failed to load promotions:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      setLoading(true);
      loadPromotions(true);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadPromotions(true);
  };

  const loadMore = () => {
    if (!hasMore || loadingMore || !nextCursor) return;
    setLoadingMore(true);
    loadPromotions(false);
  };

  const renderDiscountBadge = (promotion: Promotion) => {
    if (promotion.discountType === 0) {
      return (
        <View style={styles.discountBadge}>
          <Text style={styles.discountBadgeText}>
            {t('promotions.percentOff', { value: promotion.discountValue })}
          </Text>
        </View>
      );
    }
    return (
      <View style={styles.discountBadge}>
        <Text style={styles.discountBadgeText}>
          {t('promotions.amountOff', {
            value: formatCurrency(promotion.discountValue),
          })}
        </Text>
      </View>
    );
  };

  const renderPromotionCard = ({ item }: { item: Promotion }) => {
    const expired = isExpired(item.endDate);
    const active = item.isActive && !expired;

    return (
      <View
        style={[styles.promotionCard, expired && styles.promotionCardExpired]}
        accessible={true}
        accessibilityLabel={`${item.name}, ${
          item.discountType === 0
            ? `${item.discountValue}% off`
            : `${formatCurrency(item.discountValue)} off`
        }, ${active ? 'active' : 'expired'}`}
      >
        <View style={styles.cardAccent} />
        <View style={styles.cardContent}>
          <View style={styles.cardHeader}>
            <View style={styles.cardTitleRow}>
              <Text style={styles.promotionName} numberOfLines={1}>
                {item.name}
              </Text>
              <Badge
                label={active ? t('promotions.active') : t('promotions.expired')}
                variant={active ? 'success' : 'error'}
                size="small"
              />
            </View>
            {renderDiscountBadge(item)}
          </View>

          {item.description ? (
            <Text style={styles.promotionDescription} numberOfLines={2}>
              {item.description}
            </Text>
          ) : null}

          <View style={styles.cardMeta}>
            <Text style={styles.metaText}>
              {t('promotions.validUntil', { date: formatDate(item.endDate) })}
            </Text>

            {item.minimumChargeAmount != null && item.minimumChargeAmount > 0 ? (
              <Text style={styles.metaText}>
                {t('promotions.minCharge', {
                  amount: formatCurrency(item.minimumChargeAmount),
                })}
              </Text>
            ) : null}

            {item.maxUsageCount != null && item.maxUsageCount > 0 ? (
              <Text style={styles.metaText}>
                {t('promotions.usage', {
                  current: item.currentUsageCount ?? 0,
                  max: item.maxUsageCount,
                })}
              </Text>
            ) : null}
          </View>
        </View>
      </View>
    );
  };

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <View style={styles.emptyIconContainer}>
        <Text style={styles.emptyIcon}>{'\uD83C\uDF81'}</Text>
      </View>
      <Text style={styles.emptyTitle}>{t('promotions.noPromotions')}</Text>
      <Text style={styles.emptyText}>{t('promotions.noPromotionsDesc')}</Text>
    </View>
  );

  const renderFooter = () => {
    if (!loadingMore) return null;
    return (
      <View style={styles.footerLoader}>
        <ActivityIndicator size="small" color={Colors.primary} />
      </View>
    );
  };

  if (loading && promotions.length === 0) {
    return (
      <View
        style={styles.loadingContainer}
        accessible={true}
        accessibilityLabel="Loading promotions"
        accessibilityState={{ busy: true }}
      >
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          accessible={true}
          accessibilityRole="button"
          accessibilityLabel={t('common.goBack')}
        >
          <Text style={styles.backButtonText}>{'\u2039'}</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle} accessibilityRole="header">
          {t('promotions.title')}
        </Text>
        <View style={styles.headerSpacer} />
      </View>

      <FlatList
        data={promotions}
        keyExtractor={(item) => item.id}
        renderItem={renderPromotionCard}
        ListEmptyComponent={renderEmpty}
        ListFooterComponent={renderFooter}
        onEndReached={loadMore}
        onEndReachedThreshold={0.3}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
        contentContainerStyle={styles.listContent}
      />
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
    paddingVertical: 12,
    backgroundColor: Colors.background,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  backButton: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: Colors.surface,
    justifyContent: 'center',
    alignItems: 'center',
  },
  backButtonText: {
    fontSize: 24,
    fontWeight: '600',
    color: Colors.text,
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
    width: 36,
  },
  listContent: {
    flexGrow: 1,
    padding: 16,
  },
  promotionCard: {
    backgroundColor: Colors.background,
    borderRadius: 16,
    marginBottom: 12,
    overflow: 'hidden',
    ...Shadows.small,
  },
  promotionCardExpired: {
    opacity: 0.65,
  },
  cardAccent: {
    height: 4,
    backgroundColor: Colors.secondary,
  },
  cardContent: {
    padding: 16,
  },
  cardHeader: {
    marginBottom: 8,
  },
  cardTitleRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  promotionName: {
    flex: 1,
    fontSize: 16,
    fontWeight: '700',
    color: Colors.text,
    marginRight: 8,
  },
  discountBadge: {
    backgroundColor: Colors.secondary,
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
    alignSelf: 'flex-start',
  },
  discountBadgeText: {
    fontSize: 14,
    fontWeight: '700',
    color: Colors.background,
  },
  promotionDescription: {
    fontSize: 14,
    color: Colors.textSecondary,
    lineHeight: 20,
    marginBottom: 12,
  },
  cardMeta: {
    borderTopWidth: 1,
    borderTopColor: Colors.border,
    paddingTop: 12,
    gap: 4,
  },
  metaText: {
    fontSize: 13,
    color: Colors.textSecondary,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 64,
    paddingHorizontal: 32,
  },
  emptyIconContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: Colors.border,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  emptyIcon: {
    fontSize: 36,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    lineHeight: 20,
  },
  footerLoader: {
    paddingVertical: 16,
    alignItems: 'center',
  },
});
