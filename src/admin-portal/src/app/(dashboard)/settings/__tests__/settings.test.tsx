import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/settings',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module — settings page uses `settingsApi` which internally calls `api`
const mockSettingsGet = vi.fn();
const mockSettingsPut = vi.fn();

vi.mock('@/lib/api', () => ({
  settingsApi: {
    get: () => mockSettingsGet(),
    update: (data: unknown) => mockSettingsPut(data),
  },
}));

import SettingsPage from '../page';

const mockSettings = {
  siteName: 'KLC EV Charging',
  timezone: 'Asia/Ho_Chi_Minh',
  currency: 'VND',
  language: 'en',
  emailNotifications: true,
  smsNotifications: false,
  pushNotifications: true,
  alertEmail: 'admin@klc.vn',
  ocppWebSocketPort: 44305,
  ocppHeartbeatInterval: 60,
  ocppMeterValueInterval: 30,
  defaultPaymentGateway: 'VNPay',
  autoInvoiceGeneration: true,
  eInvoiceProvider: 'MISA',
  sessionTimeout: 30,
  requireMfa: false,
  passwordMinLength: 8,
};

describe('SettingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockSettingsGet.mockResolvedValue({ data: mockSettings });
    mockSettingsPut.mockResolvedValue({ data: mockSettings });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('Configure system settings and preferences')).toBeInTheDocument();
  });

  it('renders settings navigation tabs', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General')).toBeInTheDocument();
    });
    expect(screen.getByText('Notifications')).toBeInTheDocument();
    expect(screen.getByText('OCPP')).toBeInTheDocument();
    expect(screen.getByText('Payments')).toBeInTheDocument();
    expect(screen.getByText('Security')).toBeInTheDocument();
  });

  it('renders General Settings content by default', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('Site Name')).toBeInTheDocument();
    expect(screen.getByText('Timezone')).toBeInTheDocument();
    expect(screen.getByText('Currency')).toBeInTheDocument();
    expect(screen.getByText('Language')).toBeInTheDocument();
  });

  it('renders Save Changes button', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('Save Changes')).toBeInTheDocument();
    });
  });

  it('switches to Notifications tab', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General Settings')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Notifications'));
    await waitFor(() => {
      expect(screen.getByText('Notification Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('Email Notifications')).toBeInTheDocument();
    expect(screen.getByText('SMS Notifications')).toBeInTheDocument();
    expect(screen.getByText('Push Notifications')).toBeInTheDocument();
  });

  it('switches to OCPP tab', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General Settings')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('OCPP'));
    await waitFor(() => {
      expect(screen.getByText('OCPP Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('WebSocket Port')).toBeInTheDocument();
    expect(screen.getByText('Heartbeat Interval (seconds)')).toBeInTheDocument();
    expect(screen.getByText('Meter Value Interval (seconds)')).toBeInTheDocument();
  });

  it('switches to Payments tab', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General Settings')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Payments'));
    await waitFor(() => {
      expect(screen.getByText('Payment Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('Default Payment Gateway')).toBeInTheDocument();
    expect(screen.getByText('E-Invoice Provider')).toBeInTheDocument();
    expect(screen.getByText('Auto Invoice Generation')).toBeInTheDocument();
  });

  it('switches to Security tab', async () => {
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('General Settings')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Security'));
    await waitFor(() => {
      expect(screen.getByText('Security Settings')).toBeInTheDocument();
    });
    expect(screen.getByText('Session Timeout (minutes)')).toBeInTheDocument();
    expect(screen.getByText('Minimum Password Length')).toBeInTheDocument();
    expect(screen.getByText('Require MFA')).toBeInTheDocument();
  });

  it('shows error state when settings fail to load', async () => {
    mockSettingsGet.mockRejectedValue(new Error('Network error'));
    renderWithProviders(<SettingsPage />);
    await waitFor(() => {
      expect(screen.getByText('Failed to load settings')).toBeInTheDocument();
    });
  });

  it('renders loading state initially', () => {
    mockSettingsGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<SettingsPage />);
    expect(screen.getByText('Settings')).toBeInTheDocument();
    // General Settings card should not appear while loading
    expect(screen.queryByText('General Settings')).not.toBeInTheDocument();
  });
});
