import React, { useState, useCallback, useEffect, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Card } from '../components/common';
import { walletApi } from '../api/wallet';
import { useSignalR } from '../hooks/useSignalR';
import type { WalletBalanceChangedMessage } from '../hooks/useSignalR';
import type { WalletTransaction } from '../types';

const QUICK_AMOUNTS = [50_000, 100_000, 200_000, 500_000];

const TRANSACTION_ICONS: Record<WalletTransaction['type'], string> = {
  TopUp: '\u2B06',
  Payment: '\u2B07',
  Refund: '\u21A9',
  Bonus: '\u2B50',
};

const TRANSACTION_COLORS: Record<WalletTransaction['type'], string> = {
  TopUp: Colors.success,
  Payment: Colors.error,
  Refund: Colors.primary,
  Bonus: Colors.warning,
};

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'decimal',
    maximumFractionDigits: 0,
  }).format(amount) + '\u0111';
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();
  const hours = date.getHours().toString().padStart(2, '0');
  const minutes = date.getMinutes().toString().padStart(2, '0');
  return `${day}/${month}/${year} ${hours}:${minutes}`;
}

export function WalletScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const [balance, setBalance] = useState<number>(0);
  const [transactions, setTransactions] = useState<WalletTransaction[]>([]);
  const [nextCursor, setNextCursor] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [topUpLoading, setTopUpLoading] = useState(false);

  // SignalR: listen for wallet balance changes in real-time
  const signalRCallbacks = useMemo(() => ({
    onWalletBalanceChanged: (message: WalletBalanceChangedMessage) => {
      setBalance(message.newBalance);

      // Prepend a synthetic transaction entry so the user sees the change immediately
      const isCredit = message.changeAmount > 0;
      const newTransaction: WalletTransaction = {
        id: `rt-${message.timestamp}`,
        type: isCredit ? 'TopUp' : 'Payment',
        amount: Math.abs(message.changeAmount),
        balance: message.newBalance,
        description: message.reason,
        createdAt: message.timestamp,
      };
      setTransactions((prev) => [newTransaction, ...prev]);
    },
  }), []);

  const { connect } = useSignalR(signalRCallbacks);

  useEffect(() => {
    connect();
  }, [connect]);

  const loadData = async () => {
    try {
      const [balanceData, txData] = await Promise.all([
        walletApi.getBalance(),
        walletApi.getTransactions(),
      ]);
      setBalance(balanceData.balance);
      setTransactions(txData.items);
      setNextCursor(txData.nextCursor);
      setHasMore(txData.hasMore);
    } catch (error) {
      console.error('Failed to load wallet data:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadData();
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadData();
  };

  const loadMore = async () => {
    if (!hasMore || loadingMore || !nextCursor) return;
    setLoadingMore(true);
    try {
      const txData = await walletApi.getTransactions(nextCursor);
      setTransactions((prev) => [...prev, ...txData.items]);
      setNextCursor(txData.nextCursor);
      setHasMore(txData.hasMore);
    } catch (error) {
      console.error('Failed to load more transactions:', error);
    } finally {
      setLoadingMore(false);
    }
  };

  const handleTopUp = (amount: number) => {
    Alert.alert(
      t('wallet.topUpTitle'),
      t('wallet.topUpMessage', { amount: formatCurrency(amount) }),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('common.confirm'),
          onPress: () => processTopUp(amount),
        },
      ]
    );
  };

  const processTopUp = async (amount: number) => {
    setTopUpLoading(true);
    try {
      await walletApi.topUp(amount, 'MoMo');
      await loadData();
      Alert.alert(t('common.success'), t('wallet.topUpSuccess', { amount: formatCurrency(amount) }));
    } catch (error) {
      console.error('Top up failed:', error);
      Alert.alert(t('common.error'), t('wallet.topUpError'));
    } finally {
      setTopUpLoading(false);
    }
  };

  const renderTransactionItem = ({ item }: { item: WalletTransaction }) => {
    const isCredit = item.type === 'TopUp' || item.type === 'Refund' || item.type === 'Bonus';
    const amountPrefix = isCredit ? '+' : '-';
    const amountColor = isCredit ? Colors.success : Colors.error;
    const iconColor = TRANSACTION_COLORS[item.type];

    return (
      <View
        style={styles.transactionItem}
        accessible={true}
        accessibilityLabel={`${item.description}, ${amountPrefix}${formatCurrency(Math.abs(item.amount))}, ${formatDate(item.createdAt)}`}
      >
        <View style={[styles.transactionIcon, { backgroundColor: iconColor + '20' }]}>
          <Text style={[styles.transactionIconText, { color: iconColor }]}>
            {TRANSACTION_ICONS[item.type]}
          </Text>
        </View>
        <View style={styles.transactionDetails}>
          <Text style={styles.transactionDescription} numberOfLines={1}>
            {item.description}
          </Text>
          <Text style={styles.transactionDate}>{formatDate(item.createdAt)}</Text>
        </View>
        <View style={styles.transactionAmountContainer}>
          <Text style={[styles.transactionAmount, { color: amountColor }]}>
            {amountPrefix}{formatCurrency(Math.abs(item.amount))}
          </Text>
          <Text style={styles.transactionBalance}>
            {formatCurrency(item.balance)}
          </Text>
        </View>
      </View>
    );
  };

  const renderHeader = () => (
    <View>
      <Card style={styles.balanceCard}>
        <Text style={styles.balanceLabel}>{t('wallet.walletBalance')}</Text>
        <Text
          style={styles.balanceAmount}
          accessible={true}
          accessibilityLabel={`Wallet balance: ${formatCurrency(balance)}`}
          accessibilityRole="text"
        >
          {formatCurrency(balance)}
        </Text>
        <TouchableOpacity
          style={[styles.topUpButton, topUpLoading && styles.topUpButtonDisabled]}
          onPress={() => handleTopUp(100_000)}
          disabled={topUpLoading}
          accessible={true}
          accessibilityRole="button"
          accessibilityLabel={`Top up ${formatCurrency(100_000)}`}
          accessibilityState={{ disabled: topUpLoading, busy: topUpLoading }}
        >
          {topUpLoading ? (
            <ActivityIndicator size="small" color={Colors.background} />
          ) : (
            <Text style={styles.topUpButtonText}>{t('wallet.topUp')}</Text>
          )}
        </TouchableOpacity>
      </Card>

      <View style={styles.quickAmountsSection}>
        <Text style={styles.sectionTitle}>{t('wallet.quickTopUp')}</Text>
        <View style={styles.quickAmountsGrid}>
          {QUICK_AMOUNTS.map((amount) => (
            <TouchableOpacity
              key={amount}
              style={styles.quickAmountButton}
              onPress={() => handleTopUp(amount)}
              disabled={topUpLoading}
              accessible={true}
              accessibilityRole="button"
              accessibilityLabel={`Top up ${formatCurrency(amount)}`}
              accessibilityState={{ disabled: topUpLoading }}
            >
              <Text style={styles.quickAmountText}>{formatCurrency(amount)}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <TouchableOpacity
        style={styles.promotionsCard}
        onPress={() => navigation.navigate('Promotions')}
        activeOpacity={0.7}
        accessible={true}
        accessibilityRole="button"
        accessibilityLabel={t('promotions.viewAll')}
      >
        <View style={styles.promotionsCardIcon}>
          <Text style={styles.promotionsCardIconText}>{'\uD83C\uDF81'}</Text>
        </View>
        <View style={styles.promotionsCardContent}>
          <Text style={styles.promotionsCardTitle}>{t('promotions.title')}</Text>
          <Text style={styles.promotionsCardSubtitle}>{t('promotions.viewAll')}</Text>
        </View>
        <Text style={styles.promotionsCardArrow}>{'\u203A'}</Text>
      </TouchableOpacity>

      <View style={styles.historyHeader}>
        <Text style={styles.sectionTitle}>{t('wallet.transactionHistory')}</Text>
      </View>
    </View>
  );

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <Text style={styles.emptyIcon}>{'\uD83D\uDCB3'}</Text>
      <Text style={styles.emptyTitle}>{t('wallet.noTransactions')}</Text>
      <Text style={styles.emptyText}>
        {t('wallet.noTransactionsDescription')}
      </Text>
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

  if (loading) {
    return (
      <View
        style={styles.loadingContainer}
        accessible={true}
        accessibilityLabel="Loading wallet"
        accessibilityState={{ busy: true }}
      >
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.screenHeader}>
        <Text style={styles.screenTitle} accessibilityRole="header">{t('wallet.title')}</Text>
      </View>
      <FlatList
        data={transactions}
        keyExtractor={(item) => item.id}
        renderItem={renderTransactionItem}
        ListHeaderComponent={renderHeader}
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
        ItemSeparatorComponent={() => <View style={styles.separator} />}
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
  screenHeader: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: Colors.background,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  screenTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
  },
  listContent: {
    flexGrow: 1,
  },
  balanceCard: {
    margin: 16,
    alignItems: 'center',
    paddingVertical: 24,
    backgroundColor: Colors.primary,
    borderRadius: 16,
    ...Shadows.medium,
  },
  balanceLabel: {
    fontSize: 14,
    fontWeight: '500',
    color: Colors.background + 'CC',
    marginBottom: 8,
  },
  balanceAmount: {
    fontSize: 36,
    fontWeight: '700',
    color: Colors.background,
    marginBottom: 20,
  },
  topUpButton: {
    backgroundColor: Colors.background,
    paddingHorizontal: 32,
    paddingVertical: 12,
    borderRadius: 24,
    minWidth: 120,
    alignItems: 'center',
  },
  topUpButtonDisabled: {
    opacity: 0.7,
  },
  topUpButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.primary,
  },
  quickAmountsSection: {
    paddingHorizontal: 16,
    marginBottom: 8,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 12,
  },
  quickAmountsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  quickAmountButton: {
    flex: 1,
    marginHorizontal: 4,
    backgroundColor: Colors.background,
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: Colors.border,
    ...Shadows.small,
  },
  quickAmountText: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.primary,
  },
  promotionsCard: {
    flexDirection: 'row',
    alignItems: 'center',
    marginHorizontal: 16,
    marginTop: 12,
    padding: 14,
    backgroundColor: Colors.background,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Colors.secondary + '40',
    ...Shadows.small,
  },
  promotionsCardIcon: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: Colors.secondary + '20',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  promotionsCardIconText: {
    fontSize: 20,
  },
  promotionsCardContent: {
    flex: 1,
  },
  promotionsCardTitle: {
    fontSize: 15,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 2,
  },
  promotionsCardSubtitle: {
    fontSize: 13,
    color: Colors.secondary,
    fontWeight: '500',
  },
  promotionsCardArrow: {
    fontSize: 22,
    color: Colors.textSecondary,
    marginLeft: 8,
  },
  historyHeader: {
    paddingHorizontal: 16,
    paddingTop: 16,
  },
  transactionItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: Colors.background,
  },
  transactionIcon: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  transactionIconText: {
    fontSize: 18,
  },
  transactionDetails: {
    flex: 1,
    marginRight: 12,
  },
  transactionDescription: {
    fontSize: 14,
    fontWeight: '500',
    color: Colors.text,
    marginBottom: 4,
  },
  transactionDate: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  transactionAmountContainer: {
    alignItems: 'flex-end',
  },
  transactionAmount: {
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 2,
  },
  transactionBalance: {
    fontSize: 11,
    color: Colors.textLight,
  },
  separator: {
    height: 1,
    backgroundColor: Colors.border,
    marginLeft: 68,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 48,
    paddingHorizontal: 32,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 16,
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
