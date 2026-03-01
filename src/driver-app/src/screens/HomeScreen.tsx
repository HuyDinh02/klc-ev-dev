import React, { useEffect, useState, useCallback } from 'react';
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
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Colors, Shadows } from '../constants/colors';
import { Card, Badge } from '../components/common';
import { stationsApi } from '../api/stations';
import { useLocationStore, useSessionStore } from '../stores';
import type { Station, ConnectorStatus } from '../types';

type RootStackParamList = {
  StationDetail: { stationId: string };
  Session: undefined;
};

export function HomeScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { latitude, longitude, requestPermission } = useLocationStore();
  const { activeSession } = useSessionStore();

  const [stations, setStations] = useState<Station[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadStations = useCallback(async () => {
    try {
      const result = await stationsApi.getNearby({
        latitude,
        longitude,
        radiusKm: 10,
        limit: 20,
      });
      setStations(result.items);
    } catch (error) {
      console.error('Failed to load stations:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [latitude, longitude]);

  useEffect(() => {
    requestPermission().then(() => loadStations());
  }, [requestPermission, loadStations]);

  const onRefresh = () => {
    setRefreshing(true);
    loadStations();
  };

  const getAvailableCount = (station: Station) => {
    return station.connectors.filter((c) => c.status === 'Available').length;
  };

  const getStatusVariant = (status: ConnectorStatus): 'success' | 'warning' | 'error' | 'neutral' => {
    switch (status) {
      case 'Available':
        return 'success';
      case 'Charging':
      case 'Preparing':
        return 'info' as 'success';
      case 'Faulted':
        return 'error';
      default:
        return 'neutral';
    }
  };

  const renderStationCard = ({ item: station }: { item: Station }) => {
    const availableCount = getAvailableCount(station);
    const totalCount = station.connectors.length;

    return (
      <TouchableOpacity
        onPress={() => navigation.navigate('StationDetail', { stationId: station.id })}
        activeOpacity={0.8}
      >
        <Card style={styles.stationCard}>
          <View style={styles.stationHeader}>
            <View style={styles.stationInfo}>
              <Text style={styles.stationName}>{station.name}</Text>
              <Text style={styles.stationAddress}>{station.address}</Text>
              {station.distance && (
                <Text style={styles.distance}>{station.distance.toFixed(1)} km</Text>
              )}
            </View>
            <View style={styles.availabilityContainer}>
              <Text style={[
                styles.availabilityCount,
                availableCount > 0 ? styles.availableText : styles.unavailableText
              ]}>
                {availableCount}/{totalCount}
              </Text>
              <Text style={styles.availabilityLabel}>Available</Text>
            </View>
          </View>

          <View style={styles.connectorTypes}>
            {station.connectors.map((connector) => (
              <Badge
                key={connector.id}
                label={`${connector.type} ${connector.powerKw}kW`}
                variant={getStatusVariant(connector.status)}
                size="small"
                style={styles.connectorBadge}
              />
            ))}
          </View>
        </Card>
      </TouchableOpacity>
    );
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
        <Text style={styles.loadingText}>Finding nearby stations...</Text>
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title}>Nearby Stations</Text>
        <Text style={styles.subtitle}>
          {stations.length} stations within 10km
        </Text>
      </View>

      {activeSession && (
        <TouchableOpacity
          style={styles.activeSessionBanner}
          onPress={() => navigation.navigate('Session')}
        >
          <View style={styles.sessionInfo}>
            <Text style={styles.sessionLabel}>Active Session</Text>
            <Text style={styles.sessionEnergy}>
              {activeSession.energyKwh.toFixed(2)} kWh
            </Text>
          </View>
          <Text style={styles.viewSession}>View</Text>
        </TouchableOpacity>
      )}

      <FlatList
        data={stations}
        renderItem={renderStationCard}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>No stations found nearby</Text>
            <Text style={styles.emptySubtext}>
              Try expanding your search radius
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
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: Colors.textSecondary,
  },
  header: {
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 12,
    backgroundColor: Colors.background,
    ...Shadows.small,
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
  activeSessionBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: Colors.primary,
    marginHorizontal: 16,
    marginTop: 16,
    padding: 16,
    borderRadius: 12,
  },
  sessionInfo: {
    flex: 1,
  },
  sessionLabel: {
    fontSize: 12,
    color: Colors.background,
    opacity: 0.8,
  },
  sessionEnergy: {
    fontSize: 20,
    fontWeight: '700',
    color: Colors.background,
  },
  viewSession: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.background,
  },
  listContent: {
    padding: 16,
  },
  stationCard: {
    marginBottom: 12,
  },
  stationHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  stationInfo: {
    flex: 1,
    marginRight: 16,
  },
  stationName: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    marginBottom: 4,
  },
  stationAddress: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginBottom: 4,
  },
  distance: {
    fontSize: 12,
    color: Colors.primary,
    fontWeight: '500',
  },
  availabilityContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: 12,
    backgroundColor: Colors.surface,
    borderRadius: 12,
    minWidth: 70,
  },
  availabilityCount: {
    fontSize: 24,
    fontWeight: '700',
  },
  availableText: {
    color: Colors.success,
  },
  unavailableText: {
    color: Colors.textSecondary,
  },
  availabilityLabel: {
    fontSize: 10,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  connectorTypes: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 12,
    gap: 8,
  },
  connectorBadge: {
    marginRight: 0,
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
});
