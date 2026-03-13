import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/link', () => ({
  default: ({ children, href, ...props }: { children: React.ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/lib/store', () => ({
  useAuthStore: () => ({
    permissions: [],
    user: null,
    token: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    setPermissions: vi.fn(),
    hasPermission: () => true,
  }),
}));

import { AccessDenied } from '../access-denied';

describe('AccessDenied', () => {
  it('renders access denied heading', () => {
    renderWithProviders(<AccessDenied />);
    expect(screen.getByText('Access Denied')).toBeInTheDocument();
  });

  it('renders description text', () => {
    renderWithProviders(<AccessDenied />);
    expect(screen.getByText(/do not have permission/)).toBeInTheDocument();
  });

  it('renders back to dashboard link', () => {
    renderWithProviders(<AccessDenied />);
    const link = screen.getByText('Back to Dashboard').closest('a');
    expect(link).toHaveAttribute('href', '/');
  });

  it('renders shield icon', () => {
    renderWithProviders(<AccessDenied />);
    // ShieldX renders as an SVG
    const svg = document.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });
});
