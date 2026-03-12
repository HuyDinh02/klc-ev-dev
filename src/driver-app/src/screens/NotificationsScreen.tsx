import React, { useState, useCallback, useEffect, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { formatDistanceToNow } from 'date-fns';
import { vi } from 'date-fns/locale';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { notificationsApi } from '../api/notifications';
import { useSignalR } from '../hooks/useSignalR';
import type { NotificationMessage } from '../hooks/useSignalR';
import type { Notification, NotificationType } from '../types';

interface NotificationIconConfig {
  symbol: string;
  color: string;
  backgroundColor: string;
}

const NOTIFICATION_ICON_MAP: Record<NotificationType, NotificationIconConfig> = {
  SessionComplete: {
    symbol: '\u2713',
    color: '#FFFFFF',
    backgroundColor: Colors.success,
  },
  PaymentSuccess: {
    symbol: '$',
    color: '#FFFFFF',
    backgroundColor: Colors.success,
  },
  PaymentFailed: {
    symbol: '\u2717',
    color: '#FFFFFF',
    backgroundColor: Colors.error,
  },
  Promotion: {
    symbol: '\u266F',
    color: '#FFFFFF',
    backgroundColor: Colors.secondary,
  },
  System: {
    symbol: 'i',
    color: '#FFFFFF',
    backgroundColor: Colors.primary,
  },
};

const UNREAD_BACKGROUND = '#EBF4FF';

function formatTimeAgo(dateString: string): string {
  try {
    return formatDistanceToNow(new Date(dateString), { addSuffix: true, locale: vi });
  } catch {
    return '';
  }
}

export function NotificationsScreen() {
  const { t } = useTranslation();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [cursor, setCursor] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(true);
  const [markingAllRead, setMarkingAllRead] = useState(false);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  // SignalR: listen for new notifications in real-time
  const signalRCallbacks = useMemo(() => ({
    onNotification: (message: NotificationMessage) => {
      const newNotification: Notification = {
        id: message.notificationId,
        title: message.title,
        message: message.body,
        type: (message.type as NotificationType) || 'System',
        isRead: false,
        createdAt: message.timestamp,
      };
      setNotifications((prev) => [newNotification, ...prev]);
    },
  }), []);

  const { connect } = useSignalR(signalRCallbacks);

  useEffect(() => {
    connect();
  }, [connect]);

  const loadNotifications = async (reset = false) => {
    if (!reset && !hasMore) return;

    try {
      const result = await notificationsApi.getAll(reset ? undefined : cursor);
      if (reset) {
        setNotifications(result.items);
      } else {
        setNotifications((prev) => [...prev, ...result.items]);
      }
      setCursor(result.nextCursor);
      setHasMore(result.hasMore);
    } catch (error) {
      console.error('Failed to load notifications:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadNotifications(true);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadNotifications(true);
  };

  const onEndReached = () => {
    if (!loading && hasMore) {
      loadNotifications();
    }
  };

  const handleMarkAsRead = async (notificationId: string) => {
    try {
      await notificationsApi.markAsRead(notificationId);
      setNotifications((prev) =>
        prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
      );
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  };

  const handleMarkAllAsRead = async () => {
    if (unreadCount === 0 || markingAllRead) return;

    setMarkingAllRead(true);
    try {
      await notificationsApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    } catch (error) {
      console.error('Failed to mark all as read:', error);
    } finally {
      setMarkingAllRead(false);
    }
  };

  const handleNotificationPress = (notification: Notification) => {
    if (!notification.isRead) {
      handleMarkAsRead(notification.id);
    }
  };

  const renderNotification = ({ item }: { item: Notification }) => {
    const iconConfig = NOTIFICATION_ICON_MAP[item.type] ?? NOTIFICATION_ICON_MAP.System;

    return (
      <TouchableOpacity
        style={[
          styles.notificationItem,
          !item.isRead && styles.notificationItemUnread,
        ]}
        onPress={() => handleNotificationPress(item)}
        activeOpacity={0.7}
        accessible={true}
        accessibilityRole="button"
        accessibilityLabel={`${item.title}, ${item.message}, ${formatTimeAgo(item.createdAt)}${!item.isRead ? ', unread' : ''}`}
        accessibilityState={{ selected: !item.isRead }}
        accessibilityHint={!item.isRead ? 'Double tap to mark as read' : undefined}
      >
        <View style={[styles.iconContainer, { backgroundColor: iconConfig.backgroundColor }]}>
          <Text style={[styles.iconText, { color: iconConfig.color }]}>
            {iconConfig.symbol}
          </Text>
        </View>

        <View style={styles.contentContainer}>
          <View style={styles.titleRow}>
            <Text style={styles.notificationTitle} numberOfLines={1}>
              {item.title}
            </Text>
            {!item.isRead && <View style={styles.unreadDot} />}
          </View>
          <Text style={styles.notificationMessage} numberOfLines={1}>
            {item.message}
          </Text>
          <Text style={styles.notificationTime}>
            {formatTimeAgo(item.createdAt)}
          </Text>
        </View>
      </TouchableOpacity>
    );
  };

  if (loading && notifications.length === 0) {
    return (
      <View
        style={styles.loadingContainer}
        accessible={true}
        accessibilityLabel="Loading notifications"
        accessibilityState={{ busy: true }}
      >
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <View style={styles.headerTop}>
          <Text style={styles.title} accessibilityRole="header">{t('notifications.title')}</Text>
          {unreadCount > 0 && (
            <TouchableOpacity
              style={styles.markAllButton}
              onPress={handleMarkAllAsRead}
              disabled={markingAllRead}
              activeOpacity={0.7}
              accessible={true}
              accessibilityRole="button"
              accessibilityLabel={`Mark all ${unreadCount} notifications as read`}
              accessibilityState={{ disabled: markingAllRead, busy: markingAllRead }}
            >
              {markingAllRead ? (
                <ActivityIndicator size="small" color={Colors.primary} />
              ) : (
                <Text style={styles.markAllText}>{t('notifications.markAllAsRead')}</Text>
              )}
            </TouchableOpacity>
          )}
        </View>
        <Text style={styles.subtitle}>
          {unreadCount > 0 ? t('notifications.unreadCount', { count: unreadCount }) : t('notifications.allCaughtUp')}
        </Text>
      </View>

      <FlatList
        data={notifications}
        renderItem={renderNotification}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
        onEndReached={onEndReached}
        onEndReachedThreshold={0.5}
        ListFooterComponent={
          hasMore && !refreshing ? (
            <ActivityIndicator size="small" color={Colors.primary} style={styles.loadMore} />
          ) : null
        }
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <View style={styles.emptyIconContainer}>
              <Text style={styles.emptyIcon}>{'\uD83D\uDD14'}</Text>
            </View>
            <Text style={styles.emptyText}>{t('notifications.noNotifications')}</Text>
            <Text style={styles.emptySubtext}>
              {t('notifications.notificationsDescription')}
            </Text>
          </View>
        }
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
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 12,
    backgroundColor: Colors.background,
  },
  headerTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    color: Colors.text,
  },
  subtitle: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  markAllButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    backgroundColor: Colors.surface,
  },
  markAllText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.primary,
  },
  listContent: {
    padding: 16,
  },
  notificationItem: {
    flexDirection: 'row',
    padding: 16,
    marginBottom: 8,
    borderRadius: 12,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  notificationItemUnread: {
    backgroundColor: UNREAD_BACKGROUND,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  iconText: {
    fontSize: 18,
    fontWeight: '700',
  },
  contentContainer: {
    flex: 1,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  notificationTitle: {
    flex: 1,
    fontSize: 15,
    fontWeight: '600',
    color: Colors.text,
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: Colors.primary,
    marginLeft: 8,
  },
  notificationMessage: {
    fontSize: 13,
    color: Colors.textSecondary,
    marginBottom: 4,
    lineHeight: 18,
  },
  notificationTime: {
    fontSize: 12,
    color: Colors.textLight,
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
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  emptySubtext: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginTop: 8,
    textAlign: 'center',
    lineHeight: 20,
  },
  loadMore: {
    marginVertical: 16,
  },
});
