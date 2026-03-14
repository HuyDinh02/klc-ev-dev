import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
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

beforeEach(() => {
  jest.clearAllMocks();
});

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
});
