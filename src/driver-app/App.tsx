import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { NavigationContainer } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import './src/i18n';
import { RootNavigator } from './src/navigation';
import { Colors } from './src/constants/colors';
import { ErrorBoundary } from './src/components/common';

export default function App() {
  return (
    <SafeAreaProvider>
      <ErrorBoundary>
        <NavigationContainer
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
