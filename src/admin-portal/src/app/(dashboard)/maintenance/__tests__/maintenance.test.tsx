import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/maintenance',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API modules
const mockGetAllTasks = vi.fn();
const mockGetStats = vi.fn();
const mockCreateTask = vi.fn();
const mockStartTask = vi.fn();
const mockCompleteTask = vi.fn();
const mockCancelTask = vi.fn();
const mockApiGet = vi.fn();

vi.mock('@/lib/api', () => ({
  maintenanceApi: {
    getAll: (params: unknown) => mockGetAllTasks(params),
    getStats: () => mockGetStats(),
    create: (data: unknown) => mockCreateTask(data),
    start: (id: string) => mockStartTask(id),
    complete: (id: string) => mockCompleteTask(id),
    cancel: (id: string) => mockCancelTask(id),
  },
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
  },
}));

import MaintenancePage from '../page';

const mockTasks = [
  {
    id: 'task-1',
    stationId: 'station-1',
    stationName: 'Station Alpha',
    connectorNumber: 1,
    type: 0, // Scheduled
    status: 0, // Planned
    title: 'Quarterly Inspection',
    description: 'Routine quarterly inspection of all connectors',
    assignedTo: 'Nguyen Van A',
    scheduledDate: '2026-03-15T00:00:00Z',
    creationTime: '2026-03-01T00:00:00Z',
  },
  {
    id: 'task-2',
    stationId: 'station-2',
    stationName: 'Station Beta',
    type: 2, // Emergency
    status: 1, // In Progress
    title: 'Connector Repair',
    description: 'Replace damaged connector cable',
    assignedTo: 'Tran Thi B',
    scheduledDate: '2026-03-10T00:00:00Z',
    startedAt: '2026-03-10T08:00:00Z',
    creationTime: '2026-03-09T00:00:00Z',
  },
  {
    id: 'task-3',
    stationId: 'station-1',
    stationName: 'Station Alpha',
    type: 1, // Inspection
    status: 2, // Completed
    title: 'Monthly Check',
    description: 'Regular monthly check',
    assignedTo: 'Le Van C',
    scheduledDate: '2026-02-15T00:00:00Z',
    completedAt: '2026-02-15T16:00:00Z',
    creationTime: '2026-02-01T00:00:00Z',
  },
];

const mockStatsData = {
  plannedCount: 5,
  inProgressCount: 2,
  completedCount: 10,
  overdueCount: 1,
};

describe('MaintenancePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAllTasks.mockResolvedValue({
      data: { items: mockTasks, totalCount: 3 },
    });
    mockGetStats.mockResolvedValue({
      data: mockStatsData,
    });
    mockCreateTask.mockResolvedValue({ data: {} });
    mockStartTask.mockResolvedValue({ data: {} });
    mockCompleteTask.mockResolvedValue({ data: {} });
    mockCancelTask.mockResolvedValue({ data: {} });
    mockApiGet.mockResolvedValue({ data: { items: [] } });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByText('Maintenance')).toBeInTheDocument();
    expect(screen.getByText('Schedule and track maintenance tasks')).toBeInTheDocument();
  });

  it('renders New Task button', async () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByText('New Task')).toBeInTheDocument();
  });

  it('renders stat cards with mock data', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      // Stat labels also appear in the filter dropdowns, use getAllByText
      expect(screen.getAllByText('Planned').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getAllByText('In Progress').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Completed').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Overdue')).toBeInTheDocument();
  });

  it('renders stat card values', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('5')).toBeInTheDocument(); // planned
    });
    expect(screen.getByText('10')).toBeInTheDocument(); // completed
    expect(screen.getByText('1')).toBeInTheDocument(); // overdue
  });

  it('renders task list with mock data', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Quarterly Inspection')).toBeInTheDocument();
    });
    expect(screen.getByText('Connector Repair')).toBeInTheDocument();
    expect(screen.getByText('Monthly Check')).toBeInTheDocument();
  });

  it('renders task descriptions', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Routine quarterly inspection of all connectors')).toBeInTheDocument();
    });
    expect(screen.getByText('Replace damaged connector cable')).toBeInTheDocument();
  });

  it('renders station names on tasks', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getAllByText('Station Alpha').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
  });

  it('renders assigned technician names', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    expect(screen.getByText('Tran Thi B')).toBeInTheDocument();
    expect(screen.getByText('Le Van C')).toBeInTheDocument();
  });

  it('renders type badges on tasks', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Quarterly Inspection')).toBeInTheDocument();
    });
    // Type labels appear both as badges on tasks and as filter dropdown options,
    // so use getAllByText to verify they are present
    expect(screen.getAllByText('Emergency').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Inspection').length).toBeGreaterThanOrEqual(1);
  });

  it('renders start button for planned tasks only', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Quarterly Inspection')).toBeInTheDocument();
    });
    // task-1 is planned (status 0) — should have Start button
    const startButtons = screen.getAllByLabelText('Start');
    expect(startButtons.length).toBe(1);
  });

  it('renders complete button for in-progress tasks only', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Connector Repair')).toBeInTheDocument();
    });
    // task-2 is in progress (status 1) — should have Complete button
    const completeButtons = screen.getAllByLabelText('Complete');
    expect(completeButtons.length).toBe(1);
  });

  it('renders cancel buttons for planned and in-progress tasks', async () => {
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('Quarterly Inspection')).toBeInTheDocument();
    });
    // task-1 (planned) and task-2 (in progress) should have cancel buttons
    const cancelButtons = screen.getAllByLabelText('Cancel');
    expect(cancelButtons.length).toBe(2);
  });

  it('renders status filter dropdown', async () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByLabelText('maintenance.filterByStatus')).toBeInTheDocument();
  });

  it('renders type filter dropdown', async () => {
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByLabelText('maintenance.filterByType')).toBeInTheDocument();
  });

  it('shows empty state when no tasks', async () => {
    mockGetAllTasks.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<MaintenancePage />);
    await waitFor(() => {
      expect(screen.getByText('No maintenance tasks found')).toBeInTheDocument();
    });
    expect(screen.getByText('Create a new task to schedule maintenance')).toBeInTheDocument();
  });

  it('opens create dialog when New Task is clicked', async () => {
    renderWithProviders(<MaintenancePage />);
    fireEvent.click(screen.getByText('New Task'));
    await waitFor(() => {
      expect(screen.getByText('New Maintenance Task')).toBeInTheDocument();
    });
    expect(screen.getByText('Create Task')).toBeInTheDocument();
    expect(screen.getByText('Cancel')).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    mockGetAllTasks.mockReturnValue(new Promise(() => {}));
    mockGetStats.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<MaintenancePage />);
    expect(screen.getByText('Maintenance')).toBeInTheDocument();
    expect(screen.queryByText('Quarterly Inspection')).not.toBeInTheDocument();
  });
});
