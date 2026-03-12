import React, { useEffect, useState, useCallback, useMemo } from 'react';
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
import { useTranslation } from 'react-i18next';
import MapView, { Marker, Callout, type Region } from 'react-native-maps';
import { Colors, Shadows } from '../constants/colors';
import { Config } from '../constants/config';
import { Card, Badge } from '../components/common';
import { stationsApi } from '../api/stations';
import { useLocationStore, useSessionStore } from '../stores';
import type { Station, ConnectorStatus } from '../types';

type RootStackParamList = {
  StationDetail: { stationId: string };
  Session: undefined;
  QRScanner: undefined;
};

type ViewMode = 'list' | 'map';

export function HomeScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { latitude, longitude, hasPermission, requestPermission } = useLocationStore();
  const { activeSession } = useSessionStore();

  const [stations, setStations] = useState<Station[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>('list');

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

  const getMarkerColor = (station: Station): string => {
    if (!station.isOnline) return Colors.offline;
    const availableCount = getAvailableCount(station);
    return availableCount > 0 ? Colors.available : Colors.secondary;
  };

  const mapRegion: Region = useMemo(() => {
    if (stations.length === 0) {
      return {
        latitude,
        longitude,
        latitudeDelta: Config.DEFAULT_REGION.latitudeDelta,
        longitudeDelta: Config.DEFAULT_REGION.longitudeDelta,
      };
    }

    // Calculate bounds to fit all stations plus user location
    const lats = [latitude, ...stations.map((s) => s.latitude)];
    const lngs = [longitude, ...stations.map((s) => s.longitude)];
    const minLat = Math.min(...lats);
    const maxLat = Math.max(...lats);
    const minLng = Math.min(...lngs);
    const maxLng = Math.max(...lngs);

    const latDelta = Math.max((maxLat - minLat) * 1.4, 0.02);
    const lngDelta = Math.max((maxLng - minLng) * 1.4, 0.02);

    return {
      latitude: (minLat + maxLat) / 2,
      longitude: (minLng + maxLng) / 2,
      latitudeDelta: latDelta,
      longitudeDelta: lngDelta,
    };
  }, [latitude, longitude, stations]);

  const toggleViewMode = () => {
    setViewMode((prev) => (prev === 'list' ? 'map' : 'list'));
  };

  const renderStationCard = ({ item: station }: { item: Station }) => {
    const availableCount = getAvailableCount(station);
    const totalCount = station.connectors.length;

    return (
      <TouchableOpacity
        onPress={() => navigation.navigate('StationDetail', { stationId: station.id })}
        activeOpacity={0.8}
        accessible={true}
        accessibilityRole="button"
        accessibilityLabel={`${station.name}, ${availableCount} of ${totalCount} connectors available${station.distance ? `, ${station.distance.toFixed(1)} kilometers away` : ''}`}
        accessibilityHint="Double tap to view station details"
      >
        <Card style={styles.stationCard}>
          <View style={styles.stationHeader}>
            <View style={styles.stationInfo}>
              <Text style={styles.stationName}>{station.name}</Text>
              <Text style={styles.stationAddress}>{station.address}</Text>
              {station.distance && (
                <Text style={styles.distance}>{t('home.kmAway', { distance: station.distance.toFixed(1) })}</Text>
              )}
            </View>
            <View style={styles.availabilityContainer}>
              <Text style={[
                styles.availabilityCount,
                availableCount > 0 ? styles.availableText : styles.unavailableText
              ]}>
                {availableCount}/{totalCount}
              </Text>
              <Text style={styles.availabilityLabel}>{t('common.available')}</Text>
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
      <View
        style={styles.loadingContainer}
        accessible={true}
        accessibilityLabel="Loading nearby stations"
        accessibilityState={{ busy: true }}
      >
        <ActivityIndicator size="large" color={Colors.primary} />
        <Text style={styles.loadingText}>{t('home.loadingStations')}</Text>
      </View>
    );
  }

  const renderMapView = () => (
    <View style={styles.mapContainer}>
      <MapView
        style={styles.map}
        initialRegion={mapRegion}
        showsUserLocation={hasPermission}
        showsMyLocationButton={true}
        showsCompass={true}
      >
        {stations.map((station) => {
          const availableCount = getAvailableCount(station);
          const totalCount = station.connectors.length;
          return (
            <Marker
              key={station.id}
              coordinate={{
                latitude: station.latitude,
                longitude: station.longitude,
              }}
              pinColor={getMarkerColor(station)}
              title={station.name}
              description={`${availableCount}/${totalCount} ${t('common.available')}`}
              onCalloutPress={() =>
                navigation.navigate('StationDetail', { stationId: station.id })
              }
            >
              <Callout tooltip={false}>
                <TouchableOpacity
                  style={styles.calloutContainer}
                  onPress={() =>
                    navigation.navigate('StationDetail', { stationId: station.id })
                  }
                  accessible={true}
                  accessibilityRole="button"
                  accessibilityLabel={`${station.name}, ${availableCount} of ${totalCount} connectors available`}
                  accessibilityHint="Tap to view station details"
                >
                  <Text style={styles.calloutTitle}>{station.name}</Text>
                  <Text style={styles.calloutAddress} numberOfLines={1}>
                    {station.address}
                  </Text>
                  <View style={styles.calloutFooter}>
                    <Text
                      style={[
                        styles.calloutAvailability,
                        availableCount > 0
                          ? styles.calloutAvailable
                          : styles.calloutUnavailable,
                      ]}
                    >
                      {availableCount}/{totalCount} {t('common.available')}
                    </Text>
                    {station.distance != null && (
                      <Text style={styles.calloutDistance}>
                        {t('home.kmAway', { distance: station.distance.toFixed(1) })}
                      </Text>
                    )}
                  </View>
                </TouchableOpacity>
              </Callout>
            </Marker>
          );
        })}
      </MapView>

      {stations.length > 0 && (
        <View style={styles.mapStationCount}>
          <Text style={styles.mapStationCountText}>
            {stations.length} {t('home.stationsNearby')}
          </Text>
        </View>
      )}
    </View>
  );

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <View style={styles.headerRow}>
          <View style={styles.headerTitleArea}>
            <Text style={styles.title} accessibilityRole="header">{t('home.title')}</Text>
            <Text style={styles.subtitle}>
              {t('home.subtitle', { count: stations.length })}
            </Text>
          </View>
          <TouchableOpacity
            style={styles.viewToggle}
            onPress={toggleViewMode}
            activeOpacity={0.7}
            accessible={true}
            accessibilityRole="button"
            accessibilityLabel={
              viewMode === 'list'
                ? t('home.mapView')
                : t('home.listView')
            }
            accessibilityHint="Double tap to switch view"
          >
            <Text style={styles.viewToggleIcon}>
              {viewMode === 'list' ? '\uD83D\uDCCD' : '\uD83D\uDCCB'}
            </Text>
            <Text style={styles.viewToggleText}>
              {viewMode === 'list' ? t('home.mapView') : t('home.listView')}
            </Text>
          </TouchableOpacity>
        </View>
      </View>

      {activeSession && (
        <TouchableOpacity
          style={styles.activeSessionBanner}
          onPress={() => navigation.navigate('Session')}
          accessible={true}
          accessibilityRole="button"
          accessibilityLabel={`Active charging session, ${activeSession.energyKwh.toFixed(2)} kilowatt hours delivered`}
          accessibilityHint="Double tap to view session details"
        >
          <View style={styles.sessionInfo}>
            <Text style={styles.sessionLabel}>{t('home.activeSession')}</Text>
            <Text style={styles.sessionEnergy}>
              {activeSession.energyKwh.toFixed(2)} kWh
            </Text>
          </View>
          <Text style={styles.viewSession}>{t('common.view')}</Text>
        </TouchableOpacity>
      )}

      {viewMode === 'list' ? (
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
              <Text style={styles.emptyText}>{t('home.noStations')}</Text>
              <Text style={styles.emptySubtext}>
                {t('home.expandSearch')}
              </Text>
            </View>
          }
        />
      ) : (
        renderMapView()
      )}

      <TouchableOpacity
        style={styles.fab}
        onPress={() => navigation.navigate('QRScanner')}
        activeOpacity={0.85}
        accessible={true}
        accessibilityRole="button"
        accessibilityLabel={t('qrScanner.title')}
        accessibilityHint="Double tap to open QR code scanner"
      >
        <Text style={styles.fabIcon}>{'\u229E'}</Text>
        <Text style={styles.fabLabel}>{t('stations.scanQrCode')}</Text>
      </TouchableOpacity>
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
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerTitleArea: {
    flex: 1,
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
  viewToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.surface,
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: Colors.border,
    marginLeft: 12,
  },
  viewToggleIcon: {
    fontSize: 16,
    marginRight: 6,
  },
  viewToggleText: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.primary,
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
  fab: {
    position: 'absolute',
    bottom: 24,
    right: 20,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.primary,
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderRadius: 28,
    ...Shadows.medium,
  },
  fabIcon: {
    fontSize: 20,
    color: Colors.background,
    marginRight: 8,
  },
  fabLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.background,
  },
  mapContainer: {
    flex: 1,
  },
  map: {
    flex: 1,
  },
  mapStationCount: {
    position: 'absolute',
    top: 12,
    alignSelf: 'center',
    backgroundColor: Colors.background,
    paddingVertical: 6,
    paddingHorizontal: 14,
    borderRadius: 16,
    ...Shadows.small,
  },
  mapStationCountText: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.text,
  },
  calloutContainer: {
    width: 220,
    padding: 10,
  },
  calloutTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 2,
  },
  calloutAddress: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginBottom: 6,
  },
  calloutFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  calloutAvailability: {
    fontSize: 13,
    fontWeight: '600',
  },
  calloutAvailable: {
    color: Colors.success,
  },
  calloutUnavailable: {
    color: Colors.textSecondary,
  },
  calloutDistance: {
    fontSize: 12,
    color: Colors.primary,
    fontWeight: '500',
  },
});
