import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { View, StyleSheet, Text } from 'react-native';
import { HomeScreen, HistoryScreen, ProfileScreen } from '../screens';
import { Colors } from '../constants/colors';
import type { MainTabParamList } from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

// Simple icon components
function HomeIcon({ focused }: { focused: boolean }) {
  return (
    <View style={[styles.iconContainer, focused && styles.iconFocused]}>
      <Text style={[styles.iconText, focused && styles.iconTextFocused]}>🏠</Text>
    </View>
  );
}

function HistoryIcon({ focused }: { focused: boolean }) {
  return (
    <View style={[styles.iconContainer, focused && styles.iconFocused]}>
      <Text style={[styles.iconText, focused && styles.iconTextFocused]}>📋</Text>
    </View>
  );
}

function ProfileIcon({ focused }: { focused: boolean }) {
  return (
    <View style={[styles.iconContainer, focused && styles.iconFocused]}>
      <Text style={[styles.iconText, focused && styles.iconTextFocused]}>👤</Text>
    </View>
  );
}

export function MainNavigator() {
  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarStyle: styles.tabBar,
        tabBarActiveTintColor: Colors.primary,
        tabBarInactiveTintColor: Colors.textSecondary,
        tabBarLabelStyle: styles.tabBarLabel,
      }}
    >
      <Tab.Screen
        name="Home"
        component={HomeScreen}
        options={{
          tabBarLabel: 'Stations',
          tabBarIcon: HomeIcon,
        }}
      />
      <Tab.Screen
        name="History"
        component={HistoryScreen}
        options={{
          tabBarLabel: 'History',
          tabBarIcon: HistoryIcon,
        }}
      />
      <Tab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{
          tabBarLabel: 'Profile',
          tabBarIcon: ProfileIcon,
        }}
      />
    </Tab.Navigator>
  );
}

const styles = StyleSheet.create({
  tabBar: {
    backgroundColor: Colors.background,
    borderTopWidth: 1,
    borderTopColor: Colors.border,
    paddingTop: 8,
    paddingBottom: 8,
    height: 60,
  },
  tabBarLabel: {
    fontSize: 12,
    fontWeight: '500',
  },
  iconContainer: {
    width: 32,
    height: 32,
    justifyContent: 'center',
    alignItems: 'center',
  },
  iconFocused: {
    // Add any focus styles if needed
  },
  iconText: {
    fontSize: 20,
  },
  iconTextFocused: {
    // Add any focus styles if needed
  },
});
