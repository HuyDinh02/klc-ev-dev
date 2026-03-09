import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Colors, Shadows } from '../constants/colors';
import { Card, Badge } from '../components/common';
import { favoritesApi } from '../api/favorites';
import type { Station, ConnectorStatus } from '../types';

type RootStackParamList = {
  StationDetail: { stationId: string };
};

export function FavoritesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const [favorites, setFavorites] = useState<Station[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadFavorites = async () => {
    try {
      const result = await favoritesApi.getAll();
      setFavorites(result);
    } catch (error) {
      console.error('Failed to load favorites:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      loadFavorites();
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadFavorites();
  };

  const handleRemoveFavorite = (station: Station) => {
    Alert.alert(
      'Remove Favorite',
      `Remove "${station.name}" from your favorites?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: async () => {
            try {
              await favoritesApi.remove(station.id);
              setFavorites((prev) => prev.filter((s) => s.id !== station.id));
            } catch (error) {
              console.error('Failed to remove favorite:', error);
              Alert.alert('Error', 'Failed to remove station from favorites.');
            }
          },
        },
      ]
    );
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
        onLongPress={() => handleRemoveFavorite(station)}
        activeOpacity={0.8}
      >
        <Card style={styles.stationCard}>
          <View style={styles.stationHeader}>
            <View style={styles.stationInfo}>
              <View style={styles.nameRow}>
                <Text style={styles.stationName} numberOfLines={1}>
                  {station.name}
                </Text>
                <Badge
                  label={station.isOnline ? 'Online' : 'Offline'}
                  variant={station.isOnline ? 'success' : 'neutral'}
                  size="small"
                />
              </View>
              <Text style={styles.stationAddress} numberOfLines={2}>
                {station.address}
              </Text>
              {station.distance != null && (
                <Text style={styles.distance}>{station.distance.toFixed(1)} km</Text>
              )}
            </View>
            <View style={styles.availabilityContainer}>
              <Text
                style={[
                  styles.availabilityCount,
                  availableCount > 0 ? styles.availableText : styles.unavailableText,
                ]}
              >
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

          <TouchableOpacity
            style={styles.removeButton}
            onPress={() => handleRemoveFavorite(station)}
            hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          >
            <Text style={styles.removeButtonText}>Remove</Text>
          </TouchableOpacity>
        </Card>
      </TouchableOpacity>
    );
  };

  if (loading && favorites.length === 0) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
        <Text style={styles.loadingText}>Loading favorites...</Text>
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title}>Favorites</Text>
        <Text style={styles.subtitle}>
          {favorites.length} {favorites.length === 1 ? 'station' : 'stations'}
        </Text>
      </View>

      <FlatList
        data={favorites}
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
            <Text style={styles.heartIcon}>&#x2661;</Text>
            <Text style={styles.emptyText}>No favorite stations</Text>
            <Text style={styles.emptySubtext}>
              Browse stations to add favorites
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
  listContent: {
    padding: 16,
    flexGrow: 1,
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
  nameRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 4,
  },
  stationName: {
    fontSize: 18,
    fontWeight: '600',
    color: Colors.text,
    flexShrink: 1,
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
  removeButton: {
    alignSelf: 'flex-end',
    marginTop: 12,
    paddingVertical: 4,
    paddingHorizontal: 12,
  },
  removeButtonText: {
    fontSize: 13,
    color: Colors.error,
    fontWeight: '500',
  },
  emptyContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 80,
  },
  heartIcon: {
    fontSize: 64,
    color: Colors.textLight,
    marginBottom: 16,
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
