import React, { useState, useCallback, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  Dimensions,
  Platform,
  Linking,
} from 'react-native';
import { Camera, FlashMode, CameraType, BarCodeScanningResult } from 'expo-camera';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { Colors } from '../constants/colors';
import { Button } from '../components/common';
import type { RootStackParamList } from '../navigation/types';

const SCREEN_WIDTH = Dimensions.get('window').width;
const VIEWFINDER_SIZE = SCREEN_WIDTH * 0.65;
const DEBOUNCE_MS = 3000;

type ParsedQR = {
  stationId?: string;
  stationCode?: string;
  connectorNumber?: number;
};

function parseQrData(data: string): ParsedQR | null {
  // Format 1: JSON — {"stationId":"uuid","connectorNumber":1}
  try {
    const parsed = JSON.parse(data);
    if (parsed && typeof parsed.stationId === 'string' && parsed.stationId.length > 0) {
      return {
        stationId: parsed.stationId,
        connectorNumber: typeof parsed.connectorNumber === 'number' ? parsed.connectorNumber : undefined,
      };
    }
  } catch {
    // Not JSON
  }

  // Format 2: KLC URL — klc://station/{stationId}/connector/{number}
  const klcMatch = data.match(/^klc:\/\/station\/([^/]+)(?:\/connector\/(\d+))?$/);
  if (klcMatch && klcMatch[1]) {
    return {
      stationId: klcMatch[1],
      connectorNumber: klcMatch[2] ? parseInt(klcMatch[2], 10) : undefined,
    };
  }

  // Format 3: Charger vendor QR — "SN:{serialNumber}:{connectorId}" or just serial number
  // Examples: "SN:244902000001:1", "SN:244902000001:2", "244902000001:1"
  const vendorMatch = data.match(/^(?:SN:)?(\d{6,}):(\d+)$/);
  if (vendorMatch) {
    return {
      stationCode: vendorMatch[1],
      connectorNumber: parseInt(vendorMatch[2], 10),
    };
  }

  // Format 4: Plain serial number (no connector) — "244902000001"
  const serialMatch = data.match(/^(\d{8,})$/);
  if (serialMatch) {
    return {
      stationCode: serialMatch[1],
    };
  }

  // Format 5: stationCode-connector — "KLC-HCM-ANPHU-001-01" or "KC-HN-001-01"
  // Station code followed by dash and 2-digit connector number
  const codeConnMatch = data.match(/^(.+)-(\d{2})$/);
  if (codeConnMatch && codeConnMatch[1].length >= 5) {
    return {
      stationCode: codeConnMatch[1],
      connectorNumber: parseInt(codeConnMatch[2], 10),
    };
  }

  // Format 6: HTTPS URL — https://klc.vn/s/{stationId}/c/{number} (future web app)
  const httpsMatch = data.match(/^https?:\/\/[^/]+\/s\/([^/]+)(?:\/c\/(\d+))?/);
  if (httpsMatch && httpsMatch[1]) {
    return {
      stationId: httpsMatch[1],
      connectorNumber: httpsMatch[2] ? parseInt(httpsMatch[2], 10) : undefined,
    };
  }

  return null;
}

export function QRScannerScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const [permission, requestPermission] = Camera.useCameraPermissions();
  const [torchOn, setTorchOn] = useState(false);
  const lastScannedRef = useRef<string | null>(null);
  const lastScannedTimeRef = useRef<number>(0);

  const handleBarcodeScanned = useCallback(
    (result: BarCodeScanningResult) => {
      const now = Date.now();

      // Debounce: don't scan same code within 3 seconds
      if (
        lastScannedRef.current === result.data &&
        now - lastScannedTimeRef.current < DEBOUNCE_MS
      ) {
        return;
      }

      lastScannedRef.current = result.data;
      lastScannedTimeRef.current = now;

      const parsed = parseQrData(result.data);

      if (!parsed) {
        Alert.alert(t('common.error'), t('qrScanner.invalidQr'));
        return;
      }

      // Navigate with stationId or stationCode + optional connectorNumber
      if (parsed.stationId) {
        navigation.replace('StationDetail', {
          stationId: parsed.stationId,
          connectorNumber: parsed.connectorNumber,
        });
      } else if (parsed.stationCode) {
        // Vendor QR: lookup station by code, then navigate
        navigation.replace('StationDetail', {
          stationCode: parsed.stationCode,
          connectorNumber: parsed.connectorNumber,
        });
      } else {
        Alert.alert(t('common.error'), t('qrScanner.invalidQr'));
        return;
      }
    },
    [navigation, t],
  );

  const handleClose = useCallback(() => {
    navigation.goBack();
  }, [navigation]);

  const handleToggleTorch = useCallback(() => {
    setTorchOn((prev) => !prev);
  }, []);

  const handleOpenSettings = useCallback(() => {
    if (Platform.OS === 'ios') {
      Linking.openURL('app-settings:');
    } else {
      Linking.openSettings();
    }
  }, []);

  // Permission not yet determined
  if (!permission) {
    return (
      <View style={styles.container}>
        <Text style={styles.permissionText}>{t('common.loading')}</Text>
      </View>
    );
  }

  // Permission denied
  if (!permission.granted) {
    return (
      <View style={styles.container}>
        <View style={styles.permissionContainer}>
          <Text style={styles.permissionTitle}>
            {t('qrScanner.permissionRequired')}
          </Text>
          <Text style={styles.permissionMessage}>
            {t('qrScanner.permissionMessage')}
          </Text>
          {permission.canAskAgain ? (
            <Button
              title={t('qrScanner.permissionRequired')}
              onPress={requestPermission}
              style={styles.permissionButton}
            />
          ) : (
            <Button
              title={t('qrScanner.openSettings')}
              onPress={handleOpenSettings}
              style={styles.permissionButton}
            />
          )}
          <TouchableOpacity
            onPress={handleClose}
            style={styles.permissionCloseButton}
            accessibilityRole="button"
            accessibilityLabel={t('common.goBack')}
          >
            <Text style={styles.permissionCloseText}>{t('common.goBack')}</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Camera
        style={StyleSheet.absoluteFillObject}
        type={CameraType.back}
        flashMode={torchOn ? FlashMode.torch : FlashMode.off}
        barCodeScannerSettings={{
          barCodeTypes: ['qr'],
        }}
        onBarCodeScanned={handleBarcodeScanned}
      />

      {/* Overlay */}
      <View style={styles.overlay}>
        {/* Top overlay */}
        <View style={styles.overlayTop} />

        {/* Middle row: left + viewfinder + right */}
        <View style={styles.overlayMiddle}>
          <View style={styles.overlaySide} />
          <View style={styles.viewfinder}>
            {/* Corner brackets */}
            <View style={[styles.corner, styles.cornerTopLeft]} />
            <View style={[styles.corner, styles.cornerTopRight]} />
            <View style={[styles.corner, styles.cornerBottomLeft]} />
            <View style={[styles.corner, styles.cornerBottomRight]} />
          </View>
          <View style={styles.overlaySide} />
        </View>

        {/* Bottom overlay with instruction */}
        <View style={styles.overlayBottom}>
          <Text style={styles.instructionText}>
            {t('qrScanner.instruction')}
          </Text>
        </View>
      </View>

      {/* Close button (top-right) */}
      <TouchableOpacity
        style={styles.closeButton}
        onPress={handleClose}
        accessibilityRole="button"
        accessibilityLabel={t('common.goBack')}
        hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
      >
        <Text style={styles.closeButtonText}>✕</Text>
      </TouchableOpacity>

      {/* Flash toggle button */}
      <TouchableOpacity
        style={[styles.flashButton, torchOn && styles.flashButtonActive]}
        onPress={handleToggleTorch}
        accessibilityRole="button"
        accessibilityLabel={t('qrScanner.flash')}
        accessibilityState={{ selected: torchOn }}
        hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
      >
        <Text style={styles.flashButtonText}>
          {torchOn ? '⚡' : '💡'}
        </Text>
        <Text style={[styles.flashLabel, torchOn && styles.flashLabelActive]}>
          {t('qrScanner.flash')}
        </Text>
      </TouchableOpacity>
    </View>
  );
}

const CORNER_SIZE = 24;
const CORNER_THICKNESS = 3;

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000000',
  },
  overlay: {
    ...StyleSheet.absoluteFillObject,
  },
  overlayTop: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
  },
  overlayMiddle: {
    flexDirection: 'row',
    height: VIEWFINDER_SIZE,
  },
  overlaySide: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
  },
  viewfinder: {
    width: VIEWFINDER_SIZE,
    height: VIEWFINDER_SIZE,
  },
  corner: {
    position: 'absolute',
    width: CORNER_SIZE,
    height: CORNER_SIZE,
  },
  cornerTopLeft: {
    top: 0,
    left: 0,
    borderTopWidth: CORNER_THICKNESS,
    borderLeftWidth: CORNER_THICKNESS,
    borderColor: Colors.primary,
  },
  cornerTopRight: {
    top: 0,
    right: 0,
    borderTopWidth: CORNER_THICKNESS,
    borderRightWidth: CORNER_THICKNESS,
    borderColor: Colors.primary,
  },
  cornerBottomLeft: {
    bottom: 0,
    left: 0,
    borderBottomWidth: CORNER_THICKNESS,
    borderLeftWidth: CORNER_THICKNESS,
    borderColor: Colors.primary,
  },
  cornerBottomRight: {
    bottom: 0,
    right: 0,
    borderBottomWidth: CORNER_THICKNESS,
    borderRightWidth: CORNER_THICKNESS,
    borderColor: Colors.primary,
  },
  overlayBottom: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    alignItems: 'center',
    paddingTop: 32,
  },
  instructionText: {
    color: Colors.background,
    fontSize: 16,
    fontWeight: '500',
    textAlign: 'center',
    paddingHorizontal: 40,
  },
  closeButton: {
    position: 'absolute',
    top: Platform.OS === 'ios' ? 60 : 40,
    right: 20,
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  closeButtonText: {
    color: Colors.background,
    fontSize: 20,
    fontWeight: '600',
  },
  flashButton: {
    position: 'absolute',
    bottom: Platform.OS === 'ios' ? 100 : 80,
    alignSelf: 'center',
    alignItems: 'center',
    justifyContent: 'center',
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
  },
  flashButtonActive: {
    backgroundColor: Colors.secondary,
  },
  flashButtonText: {
    fontSize: 22,
  },
  flashLabel: {
    color: Colors.background,
    fontSize: 10,
    marginTop: 2,
  },
  flashLabelActive: {
    color: Colors.text,
  },
  permissionContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 40,
  },
  permissionTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: Colors.background,
    textAlign: 'center',
    marginBottom: 12,
  },
  permissionMessage: {
    fontSize: 16,
    color: 'rgba(255, 255, 255, 0.7)',
    textAlign: 'center',
    marginBottom: 32,
    lineHeight: 24,
  },
  permissionText: {
    fontSize: 16,
    color: Colors.background,
    textAlign: 'center',
  },
  permissionButton: {
    width: '100%',
    marginBottom: 16,
  },
  permissionCloseButton: {
    paddingVertical: 12,
  },
  permissionCloseText: {
    color: 'rgba(255, 255, 255, 0.7)',
    fontSize: 16,
  },
});
