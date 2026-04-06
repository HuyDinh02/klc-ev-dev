import React, { useEffect, useRef } from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { ActivityIndicator, View, StyleSheet } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { MainNavigator } from './MainNavigator';
import { LoginScreen, RegisterScreen, OtpVerificationScreen, StationDetailScreen, SessionScreen, VehiclesScreen, NotificationsScreen, SettingsScreen, QRScannerScreen, PaymentMethodsScreen, HelpSupportScreen, PromotionsScreen } from '../screens';
import { useAuthStore, useSessionStore } from '../stores';
import { Colors } from '../constants/colors';
import type { RootStackParamList } from './types';
import {
  addNotificationResponseReceivedListener,
  getLastNotificationResponse,
} from '../services/notifications';

const Stack = createNativeStackNavigator<RootStackParamList>();

function SessionResumeHandler() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { checkActiveSession } = useSessionStore();
  const hasChecked = useRef(false);

  useEffect(() => {
    if (hasChecked.current) return;
    hasChecked.current = true;

    checkActiveSession().then((hasActive) => {
      if (hasActive) {
        navigation.navigate('Session');
      }
    });
  }, [checkActiveSession, navigation]);

  return null;
}

function NotificationHandler() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const hasHandledInitial = useRef(false);

  useEffect(() => {
    // Handle notification taps when the app is running
    const subscription = addNotificationResponseReceivedListener((response) => {
      const data = response.notification.request.content.data;
      navigateFromNotification(navigation, data);
    });

    // Handle the case where the app was opened from a killed state via notification
    if (!hasHandledInitial.current) {
      hasHandledInitial.current = true;
      getLastNotificationResponse().then((response) => {
        if (response) {
          const data = response.notification.request.content.data;
          navigateFromNotification(navigation, data);
        }
      });
    }

    return () => subscription.remove();
  }, [navigation]);

  return null;
}

function navigateFromNotification(
  navigation: NativeStackNavigationProp<RootStackParamList>,
  data: Record<string, unknown> | undefined
) {
  if (!data) {
    navigation.navigate('Notifications');
    return;
  }

  if (data.sessionId && typeof data.sessionId === 'string') {
    navigation.navigate('Session');
  } else if (data.stationId && typeof data.stationId === 'string') {
    navigation.navigate('StationDetail', { stationId: data.stationId as string });
  } else {
    navigation.navigate('Notifications');
  }
}

export function RootNavigator() {
  const { t } = useTranslation();
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
            component={MainNavigatorWithSessionResume}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="StationDetail"
            component={StationDetailScreen}
            options={{
              title: t('stations.title'),
            }}
          />
          <Stack.Screen
            name="Session"
            component={SessionScreen}
            options={{
              title: t('session.title'),
              headerBackVisible: false,
            }}
          />
          <Stack.Screen
            name="Vehicles"
            component={VehiclesScreen}
            options={{
              title: t('vehicles.title'),
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="Notifications"
            component={NotificationsScreen}
            options={{
              title: t('notifications.title'),
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="Settings"
            component={SettingsScreen}
            options={{
              title: t('settings.title'),
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="PaymentMethods"
            component={PaymentMethodsScreen}
            options={{
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="HelpSupport"
            component={HelpSupportScreen}
            options={{
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="Promotions"
            component={PromotionsScreen}
            options={{
              headerShown: false,
            }}
          />
          <Stack.Screen
            name="QRScanner"
            component={QRScannerScreen}
            options={{
              headerShown: false,
              presentation: 'fullScreenModal',
            }}
          />
        </>
      ) : (
        <>
          <Stack.Screen
            name="Login"
            component={LoginScreen}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="Register"
            component={RegisterScreen}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="OtpVerification"
            component={OtpVerificationScreen}
            options={{ headerShown: false }}
          />
        </>
      )}
    </Stack.Navigator>
  );
}

function MainNavigatorWithSessionResume() {
  return (
    <>
      <SessionResumeHandler />
      <NotificationHandler />
      <MainNavigator />
    </>
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
