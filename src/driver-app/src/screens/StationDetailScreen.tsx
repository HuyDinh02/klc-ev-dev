import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button, Badge } from '../components/common';
import { stationsApi } from '../api/stations';
import { sessionsApi } from '../api/sessions';
import { useSessionStore } from '../stores';
import type { Station, Connector, ConnectorStatus } from '../types';

type RootStackParamList = {
  StationDetail: { stationId: string };
  Session: undefined;
  QRScanner: undefined;
};

type StationDetailRouteProp = RouteProp<RootStackParamList, 'StationDetail'>;

export function StationDetailScreen() {
  const { t } = useTranslation();
  const route = useRoute<StationDetailRouteProp>();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { setActiveSession, setConnecting } = useSessionStore();
  const { stationId } = route.params;

  const [station, setStation] = useState<Station | null>(null);
  const [loading, setLoading] = useState(true);
  const [startingSession, setStartingSession] = useState<string | null>(null);

  useEffect(() => {
    loadStation();
  }, [stationId]);

  const loadStation = async () => {
    try {
      const data = await stationsApi.getById(stationId);
      setStation(data);
    } catch (error) {
      console.error('Failed to load station:', error);
      Alert.alert(t('common.error'), t('stations.errorLoadStation'));
    } finally {
      setLoading(false);
    }
  };

  const handleStartCharging = async (connector: Connector) => {
    if (connector.status !== 'Available') {
      Alert.alert(t('stations.unavailableTitle'), t('stations.unavailableMessage'));
      return;
    }

    setStartingSession(connector.id);
    setConnecting(true);

    try {
      const session = await sessionsApi.start({
        connectorId: connector.id,
      });
      setActiveSession(session);
      navigation.navigate('Session');
    } catch (error) {
      console.error('Failed to start session:', error);
      Alert.alert(t('common.error'), t('stations.errorStartSession'));
    } finally {
      setStartingSession(null);
      setConnecting(false);
    }
  };

  const getStatusColor = (status: ConnectorStatus) => {
    switch (status) {
      case 'Available':
        return Colors.available;
      case 'Charging':
      case 'Preparing':
      case 'Finishing':
        return Colors.charging;
      case 'Faulted':
        return Colors.faulted;
      default:
        return Colors.offline;
    }
  };

  const getStatusVariant = (status: ConnectorStatus): 'success' | 'info' | 'warning' | 'error' | 'neutral' => {
    switch (status) {
      case 'Available':
        return 'success';
      case 'Charging':
      case 'Preparing':
      case 'Finishing':
        return 'info';
      case 'Faulted':
        return 'error';
      default:
        return 'neutral';
    }
  };

  const renderConnector = (connector: Connector) => (
    <Card key={connector.id} style={styles.connectorCard} accessibilityLabel={`Connector ${connector.connectorId}, ${connector.type}, ${connector.powerKw} kilowatts, ${connector.status}`}>
      <View style={styles.connectorHeader}>
        <View style={styles.connectorIdContainer}>
          <View style={[styles.statusDot, { backgroundColor: getStatusColor(connector.status) }]} accessible={false} />
          <Text style={styles.connectorId}>#{connector.connectorId}</Text>
        </View>
        <Badge label={connector.status} variant={getStatusVariant(connector.status)} />
      </View>

      <View style={styles.connectorInfo}>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>{t('stations.type')}</Text>
          <Text style={styles.infoValue}>{connector.type}</Text>
        </View>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>{t('stations.power')}</Text>
          <Text style={styles.infoValue}>{connector.powerKw} kW</Text>
        </View>
      </View>

      {connector.status === 'Available' && (
        <Button
          title={startingSession === connector.id ? t('stations.starting') : t('stations.startCharging')}
          onPress={() => handleStartCharging(connector)}
          loading={startingSession === connector.id}
          style={styles.startButton}
        />
      )}

      {connector.status === 'Charging' && connector.currentSessionId && (
        <View style={styles.inUseContainer}>
          <Text style={styles.inUseText} accessibilityRole="text">{t('stations.inUse')}</Text>
        </View>
      )}
    </Card>
  );

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} accessibilityLabel="Loading" />
      </View>
    );
  }

  if (!station) {
    return (
      <View style={styles.errorContainer}>
        <Text style={styles.errorText}>{t('stations.notFound')}</Text>
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['bottom']}>
      <ScrollView>
        <View style={styles.header}>
          <Text style={styles.stationName} accessibilityRole="header">{station.name}</Text>
          <Text style={styles.stationAddress} accessibilityRole="text">{station.address}</Text>
          <Badge
            label={station.isOnline ? t('common.online') : t('common.offline')}
            variant={station.isOnline ? 'success' : 'neutral'}
            style={styles.statusBadge}
          />
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle} accessibilityRole="header">{t('stations.connectors')}</Text>
          {station.connectors.map(renderConnector)}
        </View>

        <TouchableOpacity
          style={styles.qrButton}
          onPress={() => navigation.navigate('QRScanner')}
          accessibilityRole="button"
          accessibilityLabel="Scan QR code to start charging"
        >
          <Text style={styles.qrButtonText}>{t('stations.scanQrCode')}</Text>
        </TouchableOpacity>
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
    backgroundColor: Colors.surface,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: Colors.surface,
  },
  errorText: {
    fontSize: 18,
    color: Colors.textSecondary,
  },
  header: {
    backgroundColor: Colors.background,
    padding: 20,
    ...Shadows.small,
  },
  stationName: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 8,
  },
  stationAddress: {
    fontSize: 16,
    color: Colors.textSecondary,
    marginBottom: 12,
  },
  statusBadge: {
    alignSelf: 'flex-start',
  },
  section: {
    padding: 16,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 12,
  },
  connectorCard: {
    marginBottom: 12,
  },
  connectorHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  connectorIdContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusDot: {
    width: 12,
    height: 12,
    borderRadius: 6,
    marginRight: 8,
  },
  connectorId: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
  },
  connectorInfo: {
    marginBottom: 16,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
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
  startButton: {
    marginTop: 8,
  },
  inUseContainer: {
    backgroundColor: Colors.charging + '20',
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 8,
  },
  inUseText: {
    color: Colors.charging,
    fontWeight: '600',
  },
  qrButton: {
    backgroundColor: Colors.secondary,
    margin: 16,
    padding: 16,
    borderRadius: 12,
    alignItems: 'center',
  },
  qrButtonText: {
    color: Colors.background,
    fontSize: 16,
    fontWeight: '600',
  },
});
