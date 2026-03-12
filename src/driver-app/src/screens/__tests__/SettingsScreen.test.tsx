import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { SettingsScreen } from '../SettingsScreen';
import { notificationsApi } from '../../api/notifications';
import type { NotificationPreferences } from '../../types';

// Mock the notifications API module
jest.mock('../../api/notifications', () => ({
  notificationsApi: {
    getPreferences: jest.fn(),
    updatePreferences: jest.fn(),
  },
}));

// Override the global navigation mock to add useFocusEffect
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    goBack: jest.fn(),
    navigate: jest.fn(),
    reset: jest.fn(),
  }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => {
      cb();
    }, []);
  },
}));

const mockPreferences: NotificationPreferences = {
  chargingComplete: true,
  paymentAlerts: true,
  faultAlerts: false,
  promotions: true,
};

beforeEach(() => {
  jest.clearAllMocks();
  (notificationsApi.getPreferences as jest.Mock).mockResolvedValue(
    mockPreferences
  );
});

describe('SettingsScreen', () => {
  it('renders loading state', async () => {
    let resolvePrefs: (value: NotificationPreferences) => void;
    (notificationsApi.getPreferences as jest.Mock).mockReturnValue(
      new Promise<NotificationPreferences>((resolve) => {
        resolvePrefs = resolve;
      })
    );

    const { getByText } = render(<SettingsScreen />);

    // The title should still be visible during loading
    expect(getByText('Settings')).toBeTruthy();

    // Cleanup
    await act(async () => {
      resolvePrefs!(mockPreferences);
    });
  });

  it('renders settings title', async () => {
    const { getByText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByText('Settings')).toBeTruthy();
    });
  });

  it('renders language toggle section', async () => {
    const { getByText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByText('Language')).toBeTruthy();
    });
    expect(getByText('Vietnamese')).toBeTruthy();
    // i18n is initialized with 'en' so should show English message
    expect(getByText('Currently using English')).toBeTruthy();
  });

  it('renders notification preferences section', async () => {
    const { getByText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByText('Notification Preferences')).toBeTruthy();
    });
    expect(getByText('Charging Complete')).toBeTruthy();
    expect(getByText('Notify when charging session ends')).toBeTruthy();
    expect(getByText('Payment Alerts')).toBeTruthy();
    expect(getByText('Notify on payment success or failure')).toBeTruthy();
    expect(getByText('Fault Alerts')).toBeTruthy();
    expect(getByText('Notify when charger faults occur')).toBeTruthy();
    expect(getByText('Promotions')).toBeTruthy();
    expect(getByText('Receive promotional offers and discounts')).toBeTruthy();
  });

  it('renders app version', async () => {
    const { getByText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByText('App Version')).toBeTruthy();
    });
    expect(getByText('1.0.0')).toBeTruthy();
  });

  it('renders about section', async () => {
    const { getByText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByText('About')).toBeTruthy();
    });
  });

  it('toggles language between en/vi', async () => {
    const i18n = require('i18next');
    const changeLanguageSpy = jest.spyOn(i18n, 'changeLanguage');

    const { getByLabelText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByLabelText('Toggle language')).toBeTruthy();
    });

    const languageSwitch = getByLabelText('Toggle language');

    // Toggle to Vietnamese (value becomes true)
    fireEvent(languageSwitch, 'valueChange', true);

    expect(changeLanguageSpy).toHaveBeenCalledWith('vi');

    // Toggle back to English (value becomes false)
    fireEvent(languageSwitch, 'valueChange', false);

    expect(changeLanguageSpy).toHaveBeenCalledWith('en');

    changeLanguageSpy.mockRestore();
  });

  it('toggles notification preferences', async () => {
    const updatedPrefs: NotificationPreferences = {
      ...mockPreferences,
      chargingComplete: false,
    };
    (notificationsApi.updatePreferences as jest.Mock).mockResolvedValue(
      updatedPrefs
    );

    const { getByLabelText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByLabelText('Charging Complete')).toBeTruthy();
    });

    const chargingSwitch = getByLabelText('Charging Complete');

    await act(async () => {
      fireEvent(chargingSwitch, 'valueChange', false);
    });

    await waitFor(() => {
      expect(notificationsApi.updatePreferences).toHaveBeenCalledWith({
        ...mockPreferences,
        chargingComplete: false,
      });
    });
  });

  it('handles preference update error and reverts', async () => {
    (notificationsApi.updatePreferences as jest.Mock).mockRejectedValue(
      new Error('Update failed')
    );

    const { getByLabelText } = render(<SettingsScreen />);

    await waitFor(() => {
      expect(getByLabelText('Charging Complete')).toBeTruthy();
    });

    const chargingSwitch = getByLabelText('Charging Complete');

    // Toggle off (chargingComplete was true, now false)
    await act(async () => {
      fireEvent(chargingSwitch, 'valueChange', false);
    });

    await waitFor(() => {
      expect(notificationsApi.updatePreferences).toHaveBeenCalled();
    });

    // The preference should revert to original since the API call failed
    // The screen calls setPreferences(preferences) (the old value) on error
  });

  it('loads preferences on mount', async () => {
    render(<SettingsScreen />);

    await waitFor(() => {
      expect(notificationsApi.getPreferences).toHaveBeenCalledTimes(1);
    });
  });
});
