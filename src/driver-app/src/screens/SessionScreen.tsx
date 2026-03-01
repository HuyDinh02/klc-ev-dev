import React, { useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button } from '../components/common';
import { sessionsApi } from '../api/sessions';
import { useSessionStore } from '../stores';
import { useSignalR } from '../hooks/useSignalR';

export function SessionScreen() {
  const navigation = useNavigation();
  const { activeSession, latestMeterValue, clearSession } = useSessionStore();
  const { connect, subscribeToSession, unsubscribeFromSession } = useSignalR();

  useEffect(() => {
    if (activeSession?.id) {
      connect().then(() => {
        subscribeToSession(activeSession.id);
      });

      return () => {
        unsubscribeFromSession(activeSession.id);
      };
    }
  }, [activeSession?.id, connect, subscribeToSession, unsubscribeFromSession]);

  const handleStopCharging = async () => {
    if (!activeSession) return;

    Alert.alert(
      'Stop Charging',
      'Are you sure you want to stop the charging session?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Stop',
          style: 'destructive',
          onPress: async () => {
            try {
              await sessionsApi.stop(activeSession.id);
              clearSession();
              navigation.goBack();
            } catch (error) {
              console.error('Failed to stop session:', error);
              Alert.alert('Error', 'Failed to stop charging session');
            }
          },
        },
      ]
    );
  };

  const formatDuration = (minutes: number): string => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) {
      return `${hours}h ${mins}m`;
    }
    return `${mins}m`;
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  if (!activeSession) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.noSessionContainer}>
          <Text style={styles.noSessionText}>No active session</Text>
          <Button
            title="Go Back"
            onPress={() => navigation.goBack()}
            variant="outline"
            style={styles.goBackButton}
          />
        </View>
      </SafeAreaView>
    );
  }

  const powerKw = latestMeterValue?.powerKw ?? 0;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <View style={styles.header}>
          <View style={styles.statusIndicator}>
            <View style={styles.pulsingDot} />
            <Text style={styles.statusText}>Charging</Text>
          </View>
          <Text style={styles.stationName}>{activeSession.stationName}</Text>
        </View>

        <Card style={styles.mainCard}>
          <View style={styles.energyContainer}>
            <Text style={styles.energyValue}>
              {activeSession.energyKwh.toFixed(2)}
            </Text>
            <Text style={styles.energyUnit}>kWh</Text>
          </View>

          <View style={styles.statsRow}>
            <View style={styles.statItem}>
              <Text style={styles.statValue}>{powerKw.toFixed(1)}</Text>
              <Text style={styles.statLabel}>kW</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Text style={styles.statValue}>
                {formatDuration(activeSession.durationMinutes)}
              </Text>
              <Text style={styles.statLabel}>Duration</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Text style={styles.statValue}>
                {latestMeterValue?.soc ?? '--'}%
              </Text>
              <Text style={styles.statLabel}>SoC</Text>
            </View>
          </View>
        </Card>

        <Card style={styles.costCard}>
          <View style={styles.costRow}>
            <Text style={styles.costLabel}>Estimated Cost</Text>
            <Text style={styles.costValue}>
              {formatCurrency(activeSession.estimatedCost)}
            </Text>
          </View>
          <View style={styles.costNote}>
            <Text style={styles.costNoteText}>
              Final cost will be calculated when session ends
            </Text>
          </View>
        </Card>

        <Card style={styles.sessionInfoCard}>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Connector</Text>
            <Text style={styles.infoValue}>
              {activeSession.connectorType}
            </Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Started</Text>
            <Text style={styles.infoValue}>
              {new Date(activeSession.startTime).toLocaleTimeString('vi-VN')}
            </Text>
          </View>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>Meter Start</Text>
            <Text style={styles.infoValue}>
              {activeSession.meterStart.toFixed(3)} kWh
            </Text>
          </View>
        </Card>

        <Button
          title="Stop Charging"
          onPress={handleStopCharging}
          variant="danger"
          size="large"
          style={styles.stopButton}
        />
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  noSessionContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  noSessionText: {
    fontSize: 18,
    color: Colors.textSecondary,
    marginBottom: 24,
  },
  goBackButton: {
    minWidth: 150,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  header: {
    alignItems: 'center',
    marginBottom: 24,
  },
  statusIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  pulsingDot: {
    width: 12,
    height: 12,
    borderRadius: 6,
    backgroundColor: Colors.success,
    marginRight: 8,
  },
  statusText: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.success,
  },
  stationName: {
    fontSize: 20,
    fontWeight: '600',
    color: Colors.text,
  },
  mainCard: {
    alignItems: 'center',
    paddingVertical: 32,
    marginBottom: 16,
    ...Shadows.medium,
  },
  energyContainer: {
    flexDirection: 'row',
    alignItems: 'baseline',
    marginBottom: 24,
  },
  energyValue: {
    fontSize: 56,
    fontWeight: '700',
    color: Colors.primary,
  },
  energyUnit: {
    fontSize: 24,
    fontWeight: '500',
    color: Colors.textSecondary,
    marginLeft: 8,
  },
  statsRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statItem: {
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  statValue: {
    fontSize: 24,
    fontWeight: '600',
    color: Colors.text,
  },
  statLabel: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  statDivider: {
    width: 1,
    height: 40,
    backgroundColor: Colors.border,
  },
  costCard: {
    marginBottom: 16,
  },
  costRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  costLabel: {
    fontSize: 16,
    color: Colors.textSecondary,
  },
  costValue: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
  },
  costNote: {
    marginTop: 8,
    padding: 8,
    backgroundColor: Colors.surface,
    borderRadius: 8,
  },
  costNoteText: {
    fontSize: 12,
    color: Colors.textLight,
    textAlign: 'center',
  },
  sessionInfoCard: {
    marginBottom: 24,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  infoLabel: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  infoValue: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.text,
  },
  stopButton: {
    marginTop: 'auto',
  },
});
