import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/groups',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockGetAll = vi.fn();
const mockGetById = vi.fn();
const mockCreate = vi.fn();
const mockUpdate = vi.fn();
const mockDelete = vi.fn();
const mockAssignStation = vi.fn();
const mockUnassignStation = vi.fn();
const mockApiGet = vi.fn();

vi.mock('@/lib/api', () => ({
  stationGroupsApi: {
    getAll: (params: unknown) => mockGetAll(params),
    getById: (id: string) => mockGetById(id),
    create: (data: unknown) => mockCreate(data),
    update: (id: string, data: unknown) => mockUpdate(id, data),
    delete: (id: string) => mockDelete(id),
    assignStation: (groupId: string, stationId: string) => mockAssignStation(groupId, stationId),
    unassignStation: (groupId: string, stationId: string) => mockUnassignStation(groupId, stationId),
  },
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
  },
}));

import StationGroupsPage from '../page';

const mockGroups = [
  {
    id: 'group-1',
    name: 'Hanoi Central',
    description: 'Central Hanoi stations',
    region: 'Hanoi',
    groupType: 0,
    parentGroupId: null,
    parentGroupName: null,
    isActive: true,
    stationCount: 5,
    childGroupCount: 2,
  },
  {
    id: 'group-2',
    name: 'HCMC Operations',
    description: 'Operational group for HCMC',
    region: 'HCMC',
    groupType: 1,
    parentGroupId: null,
    parentGroupName: null,
    isActive: true,
    stationCount: 3,
    childGroupCount: 0,
  },
  {
    id: 'group-3',
    name: 'Decommissioned',
    description: 'Inactive group',
    region: 'Da Nang',
    groupType: 3,
    parentGroupId: null,
    parentGroupName: null,
    isActive: false,
    stationCount: 0,
    childGroupCount: 0,
  },
];

describe('StationGroupsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAll.mockResolvedValue({
      data: { items: mockGroups, totalCount: 3 },
    });
    mockCreate.mockResolvedValue({ data: mockGroups[0] });
    mockDelete.mockResolvedValue({ data: {} });
  });

  it('renders groups page title', async () => {
    renderWithProviders(<StationGroupsPage />);
    expect(screen.getByText('Station Groups')).toBeInTheDocument();
  });

  it('renders group cards with data', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Central')).toBeInTheDocument();
    });
    expect(screen.getByText('HCMC Operations')).toBeInTheDocument();
  });

  it('renders station counts', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Central')).toBeInTheDocument();
    });
    // Station count badges
    expect(screen.getByText(/5 stations/i)).toBeInTheDocument();
    expect(screen.getByText(/3 stations/i)).toBeInTheDocument();
  });

  it('renders group type badges', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Central')).toBeInTheDocument();
    });
    // "Geographic" and "Operational" appear both in stat cards and group badges
    expect(screen.getAllByText('Geographic').length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText('Operational').length).toBeGreaterThanOrEqual(1);
  });

  it('renders region badges', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi')).toBeInTheDocument();
    });
    expect(screen.getByText('HCMC')).toBeInTheDocument();
  });

  it('shows empty state when no groups', async () => {
    mockGetAll.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('No groups found')).toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(mockGetAll).toHaveBeenCalled();
    });
  });

  it('renders stat cards for group types', async () => {
    renderWithProviders(<StationGroupsPage />);
    await waitFor(() => {
      expect(screen.getByText('All Groups')).toBeInTheDocument();
    });
  });
});
