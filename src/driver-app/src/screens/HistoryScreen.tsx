import React, { useState, useCallback } from 'react';
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
import { Colors } from '../constants/colors';
import { Card, Badge } from '../components/common';
import { sessionsApi } from '../api/sessions';
import type { ChargingSession, SessionStatus } from '../types';

export function HistoryScreen() {
  const [sessions, setSessions] = useState<ChargingSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [cursor, setCursor] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(true);

  const loadSessions = async (reset = false) => {
    if (!reset && !hasMore) return;

    try {
      const result = await sessionsApi.getHistory(reset ? undefined : cursor);
      if (reset) {
        setSessions(result.items);
      } else {
        setSessions((prev) => [...prev, ...result.items]);
      }
      setCursor(result.nextCursor);
      setHasMore(result.hasMore);
    } catch (error) {
      console.error('Failed to load sessions:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadSessions(true);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadSessions(true);
  };

  const onEndReached = () => {
    if (!loading && hasMore) {
      loadSessions();
    }
  };

  const getStatusVariant = (status: SessionStatus): 'success' | 'warning' | 'error' | 'info' => {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Active':
        return 'info';
      case 'Failed':
      case 'Cancelled':
        return 'error';
      default:
        return 'info';
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  };

  const formatTime = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDuration = (minutes: number): string => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) {
      return `${hours}h ${mins}m`;
    }
    return `${mins}m`;
  };

  const renderSession = ({ item: session }: { item: ChargingSession }) => (
    <Card style={styles.sessionCard}>
      <View style={styles.sessionHeader}>
        <View>
          <Text style={styles.stationName}>{session.stationName}</Text>
          <Text style={styles.dateTime}>
            {formatDate(session.startTime)} • {formatTime(session.startTime)}
          </Text>
        </View>
        <Badge label={session.status} variant={getStatusVariant(session.status)} />
      </View>

      <View style={styles.statsRow}>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{session.energyKwh.toFixed(2)}</Text>
          <Text style={styles.statLabel}>kWh</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{formatDuration(session.durationMinutes)}</Text>
          <Text style={styles.statLabel}>Duration</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>
            {formatCurrency(session.actualCost ?? session.estimatedCost)}
          </Text>
          <Text style={styles.statLabel}>Cost</Text>
        </View>
      </View>

      <View style={styles.connectorInfo}>
        <Text style={styles.connectorText}>
          {session.connectorType}
        </Text>
      </View>
    </Card>
  );

  if (loading && sessions.length === 0) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title}>Charging History</Text>
        <Text style={styles.subtitle}>{sessions.length} sessions</Text>
      </View>

      <FlatList
        data={sessions}
        renderItem={renderSession}
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
            <Text style={styles.emptyText}>No charging history</Text>
            <Text style={styles.emptySubtext}>
              Your charging sessions will appear here
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
  listContent: {
    padding: 16,
  },
  sessionCard: {
    marginBottom: 12,
  },
  sessionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 16,
  },
  stationName: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 4,
  },
  dateTime: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  statsRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingVertical: 16,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  statItem: {
    alignItems: 'center',
  },
  statValue: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  statLabel: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  connectorInfo: {
    marginTop: 12,
  },
  connectorText: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 48,
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
  },
  loadMore: {
    marginVertical: 16,
  },
});
