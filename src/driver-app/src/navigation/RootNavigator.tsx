import React, { useEffect } from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { ActivityIndicator, View, StyleSheet } from 'react-native';
import { MainNavigator } from './MainNavigator';
import { LoginScreen, StationDetailScreen, SessionScreen, VehiclesScreen, NotificationsScreen } from '../screens';
import { useAuthStore } from '../stores';
import { Colors } from '../constants/colors';
import type { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

export function RootNavigator() {
  const { isAuthenticated, isLoading, checkAuth } = useAuthStore();

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color={Colors.primary} />
      </View>
    );
  }

  return (
    <Stack.Navigator
      screenOptions={{
        headerBackTitleVisible: false,
        headerTintColor: Colors.primary,
        headerStyle: {
          backgroundColor: Colors.background,
        },
        headerShadowVisible: false,
      }}
    >
      {isAuthenticated ? (
        <>
          <Stack.Screen
            name="Main"
            component={MainNavigator}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="StationDetail"
            component={StationDetailScreen}
            options={{
              title: 'Station Details',
            }}
          />
          <Stack.Screen
            name="Session"
            component={SessionScreen}
            options={{
              title: 'Charging Session',
              headerBackVisible: false,
            }}
          />
          <Stack.Screen
            name="Vehicles"
            component={VehiclesScreen}
            options={{
              title: 'My Vehicles',
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="Notifications"
            component={NotificationsScreen}
            options={{
              title: 'Notifications',
              headerShown: false,
            }}
          />
        </>
      ) : (
        <Stack.Screen
          name="Login"
          component={LoginScreen}
          options={{ headerShown: false }}
        />
      )}
    </Stack.Navigator>
  );
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: Colors.background,
  },
});
