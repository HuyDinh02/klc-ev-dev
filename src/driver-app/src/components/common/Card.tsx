import React, { ReactNode } from 'react';
import { View, StyleSheet, ViewStyle, ViewProps } from 'react-native';
import { Colors, Shadows } from '../../constants/colors';

interface CardProps extends Pick<ViewProps, 'accessibilityLabel' | 'accessibilityRole' | 'accessible'> {
  children: ReactNode;
  style?: ViewStyle;
  padding?: 'none' | 'small' | 'medium' | 'large';
}

export function Card({ children, style, padding = 'medium', ...accessibilityProps }: CardProps) {
  return (
    <View style={[styles.card, styles[padding], Shadows.small, style]} {...accessibilityProps}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: Colors.background,
    borderRadius: 16,
  },
  none: {
    padding: 0,
  },
  small: {
    padding: 12,
  },
  medium: {
    padding: 16,
  },
  large: {
    padding: 24,
  },
});
