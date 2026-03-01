import React from 'react';
import { View, Text, StyleSheet, ViewStyle } from 'react-native';
import { Colors } from '../../constants/colors';

interface BadgeProps {
  label: string;
  variant?: 'success' | 'warning' | 'error' | 'info' | 'neutral';
  size?: 'small' | 'medium';
  style?: ViewStyle;
}

export function Badge({ label, variant = 'neutral', size = 'medium', style }: BadgeProps) {
  return (
    <View style={[styles.badge, styles[variant], styles[size], style]}>
      <Text style={[styles.text, styles[`${variant}Text` as keyof typeof styles], styles[`${size}Text` as keyof typeof styles]]}>
        {label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    borderRadius: 100,
    alignSelf: 'flex-start',
  },
  small: {
    paddingVertical: 2,
    paddingHorizontal: 8,
  },
  medium: {
    paddingVertical: 4,
    paddingHorizontal: 12,
  },
  success: {
    backgroundColor: Colors.success + '20',
  },
  warning: {
    backgroundColor: Colors.warning + '20',
  },
  error: {
    backgroundColor: Colors.error + '20',
  },
  info: {
    backgroundColor: Colors.primary + '20',
  },
  neutral: {
    backgroundColor: Colors.textSecondary + '20',
  },
  text: {
    fontWeight: '600',
  },
  successText: {
    color: Colors.success,
  },
  warningText: {
    color: Colors.warning,
  },
  errorText: {
    color: Colors.error,
  },
  infoText: {
    color: Colors.primary,
  },
  neutralText: {
    color: Colors.textSecondary,
  },
  smallText: {
    fontSize: 10,
  },
  mediumText: {
    fontSize: 12,
  },
});
