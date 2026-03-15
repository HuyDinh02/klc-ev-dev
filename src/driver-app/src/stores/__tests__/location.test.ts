import { useLocationStore } from '../locationStore';
import * as Location from 'expo-location';
import { Config } from '../../constants/config';

// expo-location is mocked in jest.setup.js

describe('useLocationStore', () => {
  beforeEach(() => {
    // Reset store state
    useLocationStore.setState({
      latitude: Config.DEFAULT_REGION.latitude,
      longitude: Config.DEFAULT_REGION.longitude,
      hasPermission: false,
      isLoading: false,
      error: null,
    });
    jest.clearAllMocks();
  });

  it('has correct initial state', () => {
    const state = useLocationStore.getState();
    expect(state.latitude).toBe(Config.DEFAULT_REGION.latitude);
    expect(state.longitude).toBe(Config.DEFAULT_REGION.longitude);
    expect(state.hasPermission).toBe(false);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  it('requestPermission grants permission when status is granted', async () => {
    (Location.requestForegroundPermissionsAsync as jest.Mock).mockResolvedValue({
      status: 'granted',
    });

    const result = await useLocationStore.getState().requestPermission();

    expect(result).toBe(true);
    expect(useLocationStore.getState().hasPermission).toBe(true);
    expect(useLocationStore.getState().isLoading).toBe(false);
  });

  it('requestPermission denies when status is denied', async () => {
    (Location.requestForegroundPermissionsAsync as jest.Mock).mockResolvedValue({
      status: 'denied',
    });

    const result = await useLocationStore.getState().requestPermission();

    expect(result).toBe(false);
    expect(useLocationStore.getState().hasPermission).toBe(false);
  });

  it('requestPermission updates location after granting permission', async () => {
    (Location.requestForegroundPermissionsAsync as jest.Mock).mockResolvedValue({
      status: 'granted',
    });
    (Location.getCurrentPositionAsync as jest.Mock).mockResolvedValue({
      coords: { latitude: 10.762, longitude: 106.660 },
    });

    await useLocationStore.getState().requestPermission();

    expect(useLocationStore.getState().latitude).toBe(10.762);
    expect(useLocationStore.getState().longitude).toBe(106.660);
  });

  it('requestPermission handles error', async () => {
    (Location.requestForegroundPermissionsAsync as jest.Mock).mockRejectedValue(
      new Error('Permission error')
    );

    const result = await useLocationStore.getState().requestPermission();

    expect(result).toBe(false);
    expect(useLocationStore.getState().error).toBe('Failed to get location permission');
    expect(useLocationStore.getState().isLoading).toBe(false);
  });

  it('updateLocation updates coordinates', async () => {
    (Location.getCurrentPositionAsync as jest.Mock).mockResolvedValue({
      coords: { latitude: 21.0285, longitude: 105.8542 },
    });

    await useLocationStore.getState().updateLocation();

    expect(useLocationStore.getState().latitude).toBe(21.0285);
    expect(useLocationStore.getState().longitude).toBe(105.8542);
  });

  it('updateLocation handles error', async () => {
    (Location.getCurrentPositionAsync as jest.Mock).mockRejectedValue(
      new Error('Location unavailable')
    );

    await useLocationStore.getState().updateLocation();

    expect(useLocationStore.getState().error).toBe('Failed to get current location');
  });

  it('setLocation updates coordinates directly', () => {
    useLocationStore.getState().setLocation(10.0, 106.0);

    expect(useLocationStore.getState().latitude).toBe(10.0);
    expect(useLocationStore.getState().longitude).toBe(106.0);
  });

  it('sets isLoading during requestPermission', async () => {
    let resolvePermission: (v: { status: string }) => void;
    (Location.requestForegroundPermissionsAsync as jest.Mock).mockReturnValue(
      new Promise((r) => { resolvePermission = r; })
    );

    const promise = useLocationStore.getState().requestPermission();

    expect(useLocationStore.getState().isLoading).toBe(true);
    expect(useLocationStore.getState().error).toBeNull();

    resolvePermission!({ status: 'granted' });
    await promise;

    expect(useLocationStore.getState().isLoading).toBe(false);
  });
});
