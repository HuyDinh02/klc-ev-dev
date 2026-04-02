import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { QRScannerScreen } from '../QRScannerScreen';
import { Camera } from 'expo-camera';

const mockNavigate = jest.fn();
const mockGoBack = jest.fn();
const mockReplace = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: mockNavigate,
    goBack: mockGoBack,
    replace: mockReplace,
  }),
}));

// Mock Camera.useCameraPermissions
const mockRequestPermission = jest.fn();
jest.mock('expo-camera', () => {
  const React = require('react');
  const { View } = require('react-native');
  const MockCamera = React.forwardRef((props: Record<string, unknown>, ref: unknown) =>
    React.createElement(View, { ...props, ref, testID: 'mock-camera' })
  );
  (MockCamera as unknown as Record<string, unknown>).useCameraPermissions = jest.fn();
  return {
    Camera: MockCamera,
    CameraType: { back: 'back', front: 'front' },
    FlashMode: { off: 'off', torch: 'torch' },
    BarCodeScanningResult: {},
  };
});

jest.spyOn(Alert, 'alert');

beforeEach(() => {
  jest.clearAllMocks();
});

/** Helper: render with camera granted and extract onBarCodeScanned prop */
function renderWithPermission() {
  (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
    { granted: true },
    mockRequestPermission,
  ]);
  const utils = render(<QRScannerScreen />);
  return utils;
}

describe('QRScannerScreen', () => {
  it('shows loading when permission is not determined', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([null, mockRequestPermission]);

    const { getByText } = render(<QRScannerScreen />);
    expect(getByText('Loading')).toBeTruthy();
  });

  it('shows permission request when not granted and can ask again', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: false, canAskAgain: true },
      mockRequestPermission,
    ]);

    const { getAllByText } = render(<QRScannerScreen />);
    expect(getAllByText('Camera permission required').length).toBeGreaterThan(0);
  });

  it('shows open settings when permission denied and cannot ask again', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: false, canAskAgain: false },
      mockRequestPermission,
    ]);

    const { getByText } = render(<QRScannerScreen />);
    expect(getByText('Open Settings')).toBeTruthy();
  });

  it('shows go back button in permission screen', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: false, canAskAgain: true },
      mockRequestPermission,
    ]);

    const { getByText } = render(<QRScannerScreen />);
    fireEvent.press(getByText('Go Back'));
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('renders camera view when permission granted', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: true },
      mockRequestPermission,
    ]);

    const { getByText } = render(<QRScannerScreen />);
    expect(getByText(/QR/i)).toBeTruthy();
  });

  it('renders close button', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: true },
      mockRequestPermission,
    ]);

    const { getByLabelText } = render(<QRScannerScreen />);
    expect(getByLabelText('Go Back')).toBeTruthy();
  });

  it('renders flash toggle button', () => {
    (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
      { granted: true },
      mockRequestPermission,
    ]);

    const { getByLabelText } = render(<QRScannerScreen />);
    expect(getByLabelText('Flash')).toBeTruthy();
  });

  // --- Camera Permission Requests ---

  describe('Camera Permission', () => {
    it('calls requestPermission when permission button is pressed', () => {
      (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
        { granted: false, canAskAgain: true },
        mockRequestPermission,
      ]);

      const { getAllByText } = render(<QRScannerScreen />);

      // The button text is "Camera permission required"
      const permissionButtons = getAllByText('Camera permission required');
      fireEvent.press(permissionButtons[permissionButtons.length - 1]);

      expect(mockRequestPermission).toHaveBeenCalled();
    });

    it('shows permission message text', () => {
      (Camera.useCameraPermissions as jest.Mock).mockReturnValue([
        { granted: false, canAskAgain: true },
        mockRequestPermission,
      ]);

      const { getByText } = render(<QRScannerScreen />);
      expect(getByText('Allow camera access to scan station QR codes')).toBeTruthy();
    });
  });

  // --- QR Code Parsing & Navigation ---

  describe('QR Code Scanning', () => {
    it('navigates to StationDetail on valid JSON QR code', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: '{"stationId":"station-abc-123"}',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(mockReplace).toHaveBeenCalledWith('StationDetail', { stationId: 'station-abc-123' });
    });

    it('navigates to StationDetail on JSON QR with connectorId', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: '{"stationId":"station-xyz","connectorId":"conn-1"}',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(mockReplace).toHaveBeenCalledWith('StationDetail', { stationId: 'station-xyz' });
    });

    it('navigates to StationDetail on URL format klc://station/{id}', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: 'klc://station/my-station-456',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(mockReplace).toHaveBeenCalledWith('StationDetail', { stationId: 'my-station-456' });
    });

    it('navigates to StationDetail on URL format with connectorId', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: 'klc://station/station-789/connector/2',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(mockReplace).toHaveBeenCalledWith('StationDetail', { stationId: 'station-789', connectorNumber: 2 });
    });

    it('shows error alert for invalid QR code (random string)', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: 'some-random-garbage-data',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(Alert.alert).toHaveBeenCalledWith('Error', 'Invalid QR code');
      expect(mockReplace).not.toHaveBeenCalled();
    });

    it('shows error alert for invalid JSON without stationId', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: '{"foo":"bar"}',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(Alert.alert).toHaveBeenCalledWith('Error', 'Invalid QR code');
      expect(mockReplace).not.toHaveBeenCalled();
    });

    it('shows error alert for empty stationId in JSON', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: '{"stationId":""}',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(Alert.alert).toHaveBeenCalledWith('Error', 'Invalid QR code');
      expect(mockReplace).not.toHaveBeenCalled();
    });

    it('shows error alert for invalid URL format', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      onBarCodeScanned({
        type: 'qr',
        data: 'https://example.com/station/123',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      });

      expect(Alert.alert).toHaveBeenCalledWith('Error', 'Invalid QR code');
      expect(mockReplace).not.toHaveBeenCalled();
    });

    it('debounces scanning the same QR code within 3 seconds', () => {
      const { getByTestId } = renderWithPermission();

      const cameraView = getByTestId('mock-camera');
      const onBarCodeScanned = cameraView.props.onBarCodeScanned;

      const qrResult = {
        type: 'qr',
        data: '{"stationId":"station-debounce"}',
        bounds: { origin: { x: 0, y: 0 }, size: { width: 100, height: 100 } },
      };

      // First scan should navigate
      onBarCodeScanned(qrResult);
      expect(mockReplace).toHaveBeenCalledTimes(1);

      // Second scan with same data should be debounced
      onBarCodeScanned(qrResult);
      expect(mockReplace).toHaveBeenCalledTimes(1);
    });
  });

  // --- Torch / Flash Toggle ---

  describe('Torch Toggle', () => {
    it('toggles flash state when flash button is pressed', () => {
      const { getByLabelText } = renderWithPermission();

      const flashButton = getByLabelText('Flash');

      // Initially torch is off — accessibilityState.selected should be false
      expect(flashButton.props.accessibilityState).toEqual({ selected: false });

      // Press to turn on
      fireEvent.press(flashButton);

      // After press, state should update to selected: true
      expect(getByLabelText('Flash').props.accessibilityState).toEqual({ selected: true });
    });

    it('toggles torch back off on second press', () => {
      const { getByLabelText } = renderWithPermission();

      const flashButton = getByLabelText('Flash');

      // Press to turn on
      fireEvent.press(flashButton);
      expect(getByLabelText('Flash').props.accessibilityState).toEqual({ selected: true });

      // Press to turn off
      fireEvent.press(getByLabelText('Flash'));
      expect(getByLabelText('Flash').props.accessibilityState).toEqual({ selected: false });
    });
  });

  // --- Close Button ---

  describe('Close Button', () => {
    it('calls goBack when close button is pressed', () => {
      const { getByLabelText } = renderWithPermission();

      fireEvent.press(getByLabelText('Go Back'));

      expect(mockGoBack).toHaveBeenCalled();
    });
  });

  // --- Instruction Text ---

  describe('Instruction Text', () => {
    it('shows scan instruction when camera is active', () => {
      const { getByText } = renderWithPermission();

      expect(getByText('Point camera at a station QR code')).toBeTruthy();
    });
  });
});
