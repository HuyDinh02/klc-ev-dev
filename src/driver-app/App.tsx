import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { NavigationContainer, type LinkingOptions } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import './src/i18n';
import { RootNavigator } from './src/navigation';
import { Colors } from './src/constants/colors';
import { ErrorBoundary } from './src/components/common';
import type { RootStackParamList } from './src/navigation/types';

const linking: LinkingOptions<RootStackParamList> = {
  prefixes: ['klc://', 'https://ev.odcall.com'],
  config: {
    screens: {
      StationDetail: 'station/:stationId',
      Session: 'session',
      Notifications: 'notifications',
      PaymentMethods: 'payment-methods',
      Main: {
        screens: {
          Home: 'home',
          Favorites: 'favorites',
          History: 'history',
          Wallet: 'wallet',
          Profile: 'profile',
        },
      },
    },
  },
};

export default function App() {
  return (
    <SafeAreaProvider>
      <ErrorBoundary>
        <NavigationContainer
          linking={linking}
          theme={{
            dark: false,
            colors: {
              primary: Colors.primary,
              background: Colors.background,
              card: Colors.background,
              text: Colors.text,
              border: Colors.border,
              notification: Colors.secondary,
            },
          }}
        >
          <RootNavigator />
          <StatusBar style="dark" />
        </NavigationContainer>
      </ErrorBoundary>
    </SafeAreaProvider>
  );
}
