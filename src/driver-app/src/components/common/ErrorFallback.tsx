import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Colors } from '../../constants/colors';
import { Button } from './Button';

interface ErrorFallbackProps {
  error: Error;
  resetError: () => void;
}

export function ErrorFallback({ error, resetError }: ErrorFallbackProps) {
  const { t } = useTranslation();
  const isDev = __DEV__;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <Text style={styles.icon}>{'\u26A0\uFE0F'}</Text>
        <Text style={styles.title}>{t('errorBoundary.title')}</Text>
        <Text style={styles.message}>{t('errorBoundary.message')}</Text>

        {isDev && error?.message ? (
          <View style={styles.detailsContainer}>
            <Text style={styles.detailsLabel}>
              {t('errorBoundary.errorDetails')}
            </Text>
            <Text style={styles.detailsText}>{error.message}</Text>
          </View>
        ) : null}

        <View style={styles.buttonContainer}>
          <Button
            title={t('errorBoundary.tryAgain')}
            onPress={resetError}
            variant="primary"
            size="large"
            style={styles.button}
          />
          <Button
            title={t('errorBoundary.goHome')}
            onPress={resetError}
            variant="outline"
            size="large"
            style={styles.button}
          />
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: Colors.surface,
  },
  content: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 32,
  },
  icon: {
    fontSize: 64,
    marginBottom: 24,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: Colors.text,
    textAlign: 'center',
    marginBottom: 12,
  },
  message: {
    fontSize: 16,
    color: Colors.textSecondary,
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 32,
  },
  detailsContainer: {
    width: '100%',
    backgroundColor: Colors.background,
    borderRadius: 12,
    padding: 16,
    marginBottom: 32,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  detailsLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.textSecondary,
    textTransform: 'uppercase',
    marginBottom: 8,
  },
  detailsText: {
    fontSize: 13,
    color: Colors.error,
    fontFamily: 'monospace',
  },
  buttonContainer: {
    width: '100%',
    gap: 12,
  },
  button: {
    width: '100%',
  },
});
