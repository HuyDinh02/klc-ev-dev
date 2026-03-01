import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { NavigationContainer } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { RootNavigator } from './src/navigation';
import { Colors } from './src/constants/colors';

export default function App() {
  return (
    <SafeAreaProvider>
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
          fonts: {
            regular: { fontFamily: 'System', fontWeight: '400' },
            medium: { fontFamily: 'System', fontWeight: '500' },
            bold: { fontFamily: 'System', fontWeight: '700' },
            heavy: { fontFamily: 'System', fontWeight: '900' },
          },
        }}
      >
        <RootNavigator />
        <StatusBar style="dark" />
      </NavigationContainer>
    </SafeAreaProvider>
  );
}
