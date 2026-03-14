import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/analytics',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock recharts — ResponsiveContainer needs width/height to render children
vi.mock('recharts', () => ({
  AreaChart: ({ children }: { children: React.ReactNode }) => <div data-testid="area-chart">{children}</div>,
  Area: () => <div />,
  BarChart: ({ children }: { children: React.ReactNode }) => <div data-testid="bar-chart">{children}</div>,
  Bar: () => <div />,
  XAxis: () => <div />,
  YAxis: () => <div />,
  CartesianGrid: () => <div />,
  Tooltip: () => <div />,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

// Mock API modules
const mockGetAnalytics = vi.fn();

vi.mock('@/lib/api', () => ({
  monitoringApi: {
    getAnalytics: (params: unknown) => mockGetAnalytics(params),
  },
}));

import AnalyticsPage from '../page';

const mockAnalyticsData = {
  dailyStats: [
    { date: '2026-03-01', sessions: 10, energyKwh: 250, revenue: 1250000 },
    { date: '2026-03-02', sessions: 15, energyKwh: 350, revenue: 1750000 },
    { date: '2026-03-03', sessions: 12, energyKwh: 300, revenue: 1500000 },
  ],
  stationUtilization: [
    {
      stationId: 'station-1',
      stationName: 'Station Alpha',
      totalSessions: 50,
      totalEnergyKwh: 1200,
      totalRevenue: 6000000,
      utilizationPercent: 65.5,
      onlinePercent: 98.2,
    },
    {
      stationId: 'station-2',
      stationName: 'Station Beta',
      totalSessions: 30,
      totalEnergyKwh: 800,
      totalRevenue: 4000000,
      utilizationPercent: 35.0,
      onlinePercent: 88.5,
    },
  ],
  totalRevenue: 4500000,
  totalEnergyKwh: 900,
  totalSessions: 37,
  averageSessionDurationMinutes: 45,
  uptimePercent: 96.5,
  mtbfHours: 72,
  peakHourUtc: 3, // 3 UTC = 10:00 VN
  peakHourSessionCount: 8,
};

describe('AnalyticsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAnalytics.mockResolvedValue({
      data: mockAnalyticsData,
    });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Analytics')).toBeInTheDocument();
    });
    expect(screen.getByText('Revenue trends, utilization rates, and performance KPIs')).toBeInTheDocument();
  });

  it('renders date range filter buttons', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Last 7 days')).toBeInTheDocument();
    });
    expect(screen.getByText('Last 30 days')).toBeInTheDocument();
    expect(screen.getByText('Last 90 days')).toBeInTheDocument();
  });

  it('renders KPI stat cards', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Total Revenue')).toBeInTheDocument();
    });
    expect(screen.getByText('Energy Delivered')).toBeInTheDocument();
    expect(screen.getByText('Avg Session Duration')).toBeInTheDocument();
    expect(screen.getByText('Network Uptime')).toBeInTheDocument();
    expect(screen.getByText('Avg Revenue/kWh')).toBeInTheDocument();
  });

  it('renders revenue value', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('4.500.000đ')).toBeInTheDocument();
    });
  });

  it('renders energy value with sessions count', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('900.00 kWh')).toBeInTheDocument();
    });
    expect(screen.getByText('37 sessions')).toBeInTheDocument();
  });

  it('renders average session duration', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('45 min')).toBeInTheDocument();
    });
  });

  it('renders network uptime with healthy status', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('96.5%')).toBeInTheDocument();
    });
    expect(screen.getByText('Healthy availability')).toBeInTheDocument();
  });

  it('renders degraded uptime status', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, uptimePercent: 92.0 },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('92.0%')).toBeInTheDocument();
    });
    expect(screen.getByText('Degraded availability')).toBeInTheDocument();
  });

  it('renders critical uptime status', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, uptimePercent: 85.0 },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('85.0%')).toBeInTheDocument();
    });
    expect(screen.getByText('Critical availability')).toBeInTheDocument();
  });

  it('renders operational metrics (MTBF and Peak Hour)', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Mean Time Between Faults (MTBF)')).toBeInTheDocument();
    });
    expect(screen.getByText('Peak Charging Hour')).toBeInTheDocument();
    // 72 hours = 3.0 days
    expect(screen.getByText('3.0 days')).toBeInTheDocument();
    // Peak hour: UTC 3 + 7 = 10:00-11:00
    expect(screen.getByText('10:00\u201311:00')).toBeInTheDocument();
    expect(screen.getByText('8 sessions started during this hour (UTC+7)')).toBeInTheDocument();
  });

  it('renders MTBF in hours when less than 24', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, mtbfHours: 18.5 },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('18.5 hrs')).toBeInTheDocument();
    });
  });

  it('renders "No faults" when mtbfHours is 0', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, mtbfHours: 0 },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('No faults')).toBeInTheDocument();
    });
    expect(screen.getByText('No faults recorded in this period')).toBeInTheDocument();
  });

  it('renders chart sections', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Revenue Trend')).toBeInTheDocument();
    });
    expect(screen.getByText('Energy Delivered (kWh)')).toBeInTheDocument();
    expect(screen.getByText('Daily Sessions')).toBeInTheDocument();
  });

  it('renders station utilization section with table', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Utilization')).toBeInTheDocument();
    });
    // Table column headers
    expect(screen.getByText('Station')).toBeInTheDocument();
    expect(screen.getByText('Sessions')).toBeInTheDocument();
    expect(screen.getByText('Energy')).toBeInTheDocument();
    expect(screen.getByText('Revenue')).toBeInTheDocument();
    expect(screen.getByText('Utilization')).toBeInTheDocument();
    expect(screen.getByText('Online')).toBeInTheDocument();
    // Station names in table
    expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
  });

  it('renders station utilization percentages', async () => {
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('65.5%')).toBeInTheDocument();
    });
    expect(screen.getByText('98.2%')).toBeInTheDocument();
    expect(screen.getByText('35.0%')).toBeInTheDocument();
    expect(screen.getByText('88.5%')).toBeInTheDocument();
  });

  it('renders empty state for charts when no daily stats', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, dailyStats: [] },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('No revenue data')).toBeInTheDocument();
    });
    expect(screen.getByText('No energy data')).toBeInTheDocument();
    expect(screen.getByText('No session data')).toBeInTheDocument();
    // "No data available for the selected period" appears multiple times
    expect(screen.getAllByText('No data available for the selected period').length).toBeGreaterThanOrEqual(1);
  });

  it('renders empty state for station utilization when no stations', async () => {
    mockGetAnalytics.mockResolvedValue({
      data: { ...mockAnalyticsData, stationUtilization: [] },
    });
    renderWithProviders(<AnalyticsPage />);
    await waitFor(() => {
      expect(screen.getByText('No station data')).toBeInTheDocument();
    });
    expect(screen.getByText('No utilization data available for the selected period')).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    mockGetAnalytics.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<AnalyticsPage />);
    // Title always renders even in loading state
    expect(screen.getByText('Analytics')).toBeInTheDocument();
    // KPI cards should not be visible while loading
    expect(screen.queryByText('Total Revenue')).not.toBeInTheDocument();
  });

  it('switching date range triggers new query', async () => {
    renderWithProviders(<AnalyticsPage />);
    // Wait for initial data to load
    await waitFor(() => {
      expect(screen.getByText('Total Revenue')).toBeInTheDocument();
    });

    const initialCallCount = mockGetAnalytics.mock.calls.length;

    const btn7d = screen.getByText('Last 7 days');
    fireEvent.click(btn7d);

    // Should re-fetch with new params after switching
    await waitFor(() => {
      expect(mockGetAnalytics.mock.calls.length).toBeGreaterThan(initialCallCount);
    });
  });
});
