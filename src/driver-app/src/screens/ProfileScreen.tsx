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
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { Colors, Shadows } from '../constants/colors';
import { Card, Button } from '../components/common';
import { profileApi, vehiclesApi } from '../api/profile';
import { useAuthStore } from '../stores';
import type { UserProfile, UserStatistics, Vehicle } from '../types';

export function ProfileScreen() {
  const { logout } = useAuthStore();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [statistics, setStatistics] = useState<UserStatistics | null>(null);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadData = async () => {
    try {
      const [profileData, statsData, vehiclesData] = await Promise.all([
        profileApi.get(),
        profileApi.getStatistics(),
        vehiclesApi.getAll(),
      ]);
      setProfile(profileData);
      setStatistics(statsData);
      setVehicles(vehiclesData);
    } catch (error) {
      console.error('Failed to load profile:', error);
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

  const handleLogout = () => {
    Alert.alert(
      'Logout',
      'Are you sure you want to logout?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Logout',
          style: 'destructive',
          onPress: () => logout(),
        },
      ]
    );
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <ScrollView
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={Colors.primary}
          />
        }
      >
        <View style={styles.header}>
          <View style={styles.avatarContainer}>
            <Text style={styles.avatarText}>
              {profile?.fullName?.charAt(0) ?? 'U'}
            </Text>
          </View>
          <Text style={styles.name}>{profile?.fullName ?? 'User'}</Text>
          <Text style={styles.email}>{profile?.email}</Text>
        </View>

        <View style={styles.statsSection}>
          <Card style={styles.statsCard}>
            <View style={styles.statsGrid}>
              <View style={styles.statItem}>
                <Text style={styles.statValue}>{statistics?.totalSessions ?? 0}</Text>
                <Text style={styles.statLabel}>Sessions</Text>
              </View>
              <View style={styles.statItem}>
                <Text style={styles.statValue}>
                  {(statistics?.totalEnergyKwh ?? 0).toFixed(1)}
                </Text>
                <Text style={styles.statLabel}>kWh</Text>
              </View>
              <View style={styles.statItem}>
                <Text style={styles.statValue}>
                  {(statistics?.co2Saved ?? 0).toFixed(0)}
                </Text>
                <Text style={styles.statLabel}>kg CO₂</Text>
              </View>
            </View>
            <View style={styles.totalSpent}>
              <Text style={styles.totalSpentLabel}>Total Spent</Text>
              <Text style={styles.totalSpentValue}>
                {formatCurrency(statistics?.totalSpent ?? 0)}
              </Text>
            </View>
          </Card>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>My Vehicles</Text>
          {vehicles.length === 0 ? (
            <Card style={styles.emptyCard}>
              <Text style={styles.emptyText}>No vehicles added</Text>
              <Button
                title="Add Vehicle"
                variant="outline"
                size="small"
                onPress={() => {}}
                style={styles.addButton}
              />
            </Card>
          ) : (
            vehicles.map((vehicle) => (
              <Card key={vehicle.id} style={styles.vehicleCard}>
                <View style={styles.vehicleHeader}>
                  <View>
                    <Text style={styles.vehicleName}>
                      {vehicle.make} {vehicle.model}
                    </Text>
                    <Text style={styles.vehiclePlate}>{vehicle.licensePlate}</Text>
                  </View>
                  {vehicle.isDefault && (
                    <View style={styles.defaultBadge}>
                      <Text style={styles.defaultBadgeText}>Default</Text>
                    </View>
                  )}
                </View>
                <View style={styles.vehicleInfo}>
                  <Text style={styles.vehicleInfoText}>
                    {vehicle.batteryCapacityKwh} kWh • {vehicle.connectorType}
                  </Text>
                </View>
              </Card>
            ))
          )}
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Account</Text>
          <Card padding="none">
            <MenuItem title="Payment Methods" onPress={() => {}} />
            <MenuItem title="Notifications" onPress={() => {}} />
            <MenuItem title="E-Invoices" onPress={() => {}} />
            <MenuItem title="Settings" onPress={() => {}} />
            <MenuItem title="Help & Support" onPress={() => {}} />
          </Card>
        </View>

        <View style={styles.logoutSection}>
          <Button
            title="Logout"
            variant="outline"
            onPress={handleLogout}
            style={styles.logoutButton}
          />
        </View>

        <Text style={styles.version}>Version 1.0.0</Text>
      </ScrollView>
    </SafeAreaView>
  );
}

function MenuItem({ title, onPress }: { title: string; onPress: () => void }) {
  return (
    <TouchableOpacity style={styles.menuItem} onPress={onPress}>
      <Text style={styles.menuItemText}>{title}</Text>
      <Text style={styles.menuItemArrow}>›</Text>
    </TouchableOpacity>
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
    alignItems: 'center',
    paddingVertical: 32,
    backgroundColor: Colors.background,
    ...Shadows.small,
  },
  avatarContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  avatarText: {
    fontSize: 32,
    fontWeight: '700',
    color: Colors.background,
  },
  name: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
    marginBottom: 4,
  },
  email: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  statsSection: {
    padding: 16,
  },
  statsCard: {
    ...Shadows.medium,
  },
  statsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingBottom: 16,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  statItem: {
    alignItems: 'center',
  },
  statValue: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.primary,
  },
  statLabel: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 4,
  },
  totalSpent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: 16,
  },
  totalSpentLabel: {
    fontSize: 14,
    color: Colors.textSecondary,
  },
  totalSpentValue: {
    fontSize: 20,
    fontWeight: '700',
    color: Colors.text,
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
  emptyCard: {
    alignItems: 'center',
    paddingVertical: 24,
  },
  emptyText: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginBottom: 16,
  },
  addButton: {
    minWidth: 120,
  },
  vehicleCard: {
    marginBottom: 8,
  },
  vehicleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  vehicleName: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.text,
  },
  vehiclePlate: {
    fontSize: 14,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  defaultBadge: {
    backgroundColor: Colors.primary + '20',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  defaultBadgeText: {
    fontSize: 10,
    fontWeight: '600',
    color: Colors.primary,
  },
  vehicleInfo: {
    marginTop: 8,
  },
  vehicleInfoText: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  menuItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 16,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  menuItemText: {
    fontSize: 16,
    color: Colors.text,
  },
  menuItemArrow: {
    fontSize: 20,
    color: Colors.textSecondary,
  },
  logoutSection: {
    padding: 16,
  },
  logoutButton: {
    borderColor: Colors.error,
  },
  version: {
    textAlign: 'center',
    fontSize: 12,
    color: Colors.textLight,
    paddingBottom: 32,
  },
});
