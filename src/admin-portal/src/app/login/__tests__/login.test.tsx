import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
const mockPush = vi.fn();
const mockReplace = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, replace: mockReplace, back: vi.fn() }),
  usePathname: () => '/login',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock auth API
const mockLogin = vi.fn();
vi.mock('@/lib/api', () => ({
  authApi: {
    login: (username: string, password: string) => mockLogin(username, password),
    parseToken: (token: string) => {
      if (token === 'valid-token') {
        return {
          sub: 'user-1',
          preferred_username: 'admin',
          email: 'admin@test.com',
          role: 'admin',
          given_name: 'Admin',
          family_name: 'User',
        };
      }
      return null;
    },
  },
}));

// Mock auth store — make isAuthenticated always false for login page tests
// unless we test redirect after login
const mockStoreLogin = vi.fn();
vi.mock('@/lib/store', () => ({
  useAuthStore: () => ({
    user: null,
    token: null,
    isAuthenticated: false,
    login: mockStoreLogin,
    logout: vi.fn(),
  }),
}));

import LoginPage from '../page';

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLogin.mockResolvedValue({ access_token: 'valid-token' });
  });

  it('renders login form with username and password fields', async () => {
    renderWithProviders(<LoginPage />);
    await waitFor(() => {
      expect(screen.getByLabelText('Username')).toBeInTheDocument();
    });
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });

  it('renders brand logo', async () => {
    renderWithProviders(<LoginPage />);
    await waitFor(() => {
      expect(screen.getByAltText('K-Charge')).toBeInTheDocument();
    });
  });

  it('renders sign in button', async () => {
    renderWithProviders(<LoginPage />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Sign In' })).toBeInTheDocument();
    });
  });

  it('submit calls authApi.login with credentials', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Username')).toBeInTheDocument();
    });

    const usernameInput = screen.getByLabelText('Username');
    const passwordInput = screen.getByLabelText('Password');
    const submitButton = screen.getByRole('button', { name: 'Sign In' });

    await user.type(usernameInput, 'admin');
    await user.type(passwordInput, 'Admin@123');
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith('admin', 'Admin@123');
    });
  });

  it('calls store login and redirects on successful login', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Username')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText('Username'), 'admin');
    await user.type(screen.getByLabelText('Password'), 'Admin@123');
    await user.click(screen.getByRole('button', { name: 'Sign In' }));

    await waitFor(() => {
      expect(mockStoreLogin).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'user-1',
          username: 'admin',
          email: 'admin@test.com',
          role: 'admin',
        }),
        'valid-token'
      );
    });

    expect(mockPush).toHaveBeenCalledWith('/');
  });

  it('shows error message on login failure', async () => {
    mockLogin.mockRejectedValue({
      response: { status: 400, data: { error_description: 'Invalid username or password' } },
    });

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Username')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText('Username'), 'wrong');
    await user.type(screen.getByLabelText('Password'), 'bad');
    await user.click(screen.getByRole('button', { name: 'Sign In' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText('Invalid username or password')).toBeInTheDocument();
  });

  it('shows connection error on network failure', async () => {
    mockLogin.mockRejectedValue(new Error('Network Error'));

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Username')).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText('Username'), 'admin');
    await user.type(screen.getByLabelText('Password'), 'test');
    await user.click(screen.getByRole('button', { name: 'Sign In' }));

    await waitFor(() => {
      expect(screen.getByText('Unable to connect to server. Please check your connection.')).toBeInTheDocument();
    });
  });

  it('password toggle visibility works', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Password')).toBeInTheDocument();
    });

    const passwordInput = screen.getByLabelText('Password');
    expect(passwordInput).toHaveAttribute('type', 'password');

    const toggleButton = screen.getByLabelText('Show password');
    await user.click(toggleButton);
    expect(passwordInput).toHaveAttribute('type', 'text');

    const hideButton = screen.getByLabelText('Hide password');
    await user.click(hideButton);
    expect(passwordInput).toHaveAttribute('type', 'password');
  });
});
