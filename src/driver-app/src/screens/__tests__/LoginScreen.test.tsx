import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { LoginScreen } from '../LoginScreen';
import { useAuthStore } from '../../stores';
import { authApi } from '../../api';
import type { LoginResponse } from '../../api';

// Mock the auth API module
jest.mock('../../api', () => ({
  authApi: {
    login: jest.fn(),
  },
  mapAuthUserToProfile: jest.requireActual('../../api/auth').mapAuthUserToProfile,
}));

// Spy on Alert.alert
jest.spyOn(Alert, 'alert');

// Reset auth store and mocks before each test
beforeEach(() => {
  useAuthStore.setState({
    isAuthenticated: false,
    isLoading: false,
    user: null,
    token: null,
  });
  jest.clearAllMocks();
});

// Helper to find the Sign In button (there are two "Sign In" texts: heading + button)
function getSignInButton(getAllByText: (text: string | RegExp) => any[]) {
  const elements = getAllByText('Sign In');
  // The button's text is the last one (inside the Button component)
  return elements[elements.length - 1];
}

const mockLoginResponse: LoginResponse = {
  success: true,
  accessToken: 'real-access-token',
  refreshToken: 'real-refresh-token',
  expiresIn: 3600,
  user: {
    userId: 'user-123',
    fullName: 'Test Driver',
    phoneNumber: '0901234567',
    email: 'driver@klc.vn',
    avatarUrl: undefined,
    isPhoneVerified: true,
    membershipTier: 0,
    walletBalance: 0,
  },
};

describe('LoginScreen', () => {
  it('renders the login form with brand and inputs', () => {
    const { getByText, getByLabelText, getAllByText } = render(
      <LoginScreen />
    );

    expect(getByText('K-Charge')).toBeTruthy();
    expect(getByText('EV Charging Made Simple')).toBeTruthy();
    // "Sign In" appears as both heading and button text
    expect(getAllByText('Sign In').length).toBeGreaterThanOrEqual(2);
    expect(getByLabelText('Email')).toBeTruthy();
    expect(getByLabelText('Password')).toBeTruthy();
  });

  it('renders email and password inputs', () => {
    const { getByLabelText } = render(<LoginScreen />);

    const emailInput = getByLabelText('Email');
    const passwordInput = getByLabelText('Password');

    expect(emailInput).toBeTruthy();
    expect(passwordInput).toBeTruthy();
  });

  it('renders the Sign In button text', () => {
    const { getAllByText } = render(<LoginScreen />);
    // Should have at least 2 elements: the form heading and the button label
    const signInElements = getAllByText('Sign In');
    expect(signInElements.length).toBeGreaterThanOrEqual(2);
  });

  it('renders demo credentials section', () => {
    const { getByText } = render(<LoginScreen />);
    expect(getByText('Demo Credentials')).toBeTruthy();
    expect(getByText('Email: driver@klc.vn')).toBeTruthy();
    expect(getByText('Password: driver123')).toBeTruthy();
  });

  it('renders Forgot Password and Sign Up links', () => {
    const { getByText } = render(<LoginScreen />);
    expect(getByText('Forgot Password?')).toBeTruthy();
    expect(getByText('Sign Up')).toBeTruthy();
  });

  it('shows error alert when submitting empty form', () => {
    const { getAllByText } = render(<LoginScreen />);

    fireEvent.press(getSignInButton(getAllByText));

    expect(Alert.alert).toHaveBeenCalledWith(
      'Error',
      'Please enter email and password'
    );
  });

  it('allows typing in email and password fields', () => {
    const { getByLabelText } = render(<LoginScreen />);

    const emailInput = getByLabelText('Email');
    const passwordInput = getByLabelText('Password');

    fireEvent.changeText(emailInput, 'driver@klc.vn');
    fireEvent.changeText(passwordInput, 'driver123');

    expect(emailInput.props.value).toBe('driver@klc.vn');
    expect(passwordInput.props.value).toBe('driver123');
  });

  it('calls login on successful credential submission', async () => {
    const loginMock = jest.fn().mockResolvedValue(undefined);
    useAuthStore.setState({ login: loginMock } as any);
    (authApi.login as jest.Mock).mockResolvedValue(mockLoginResponse);

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), '0901234567');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(authApi.login).toHaveBeenCalledWith({
        phoneNumber: '0901234567',
        password: 'driver123',
      });
      expect(loginMock).toHaveBeenCalledWith('real-access-token', {
        id: 'user-123',
        email: 'driver@klc.vn',
        phoneNumber: '0901234567',
        fullName: 'Test Driver',
        avatarUrl: undefined,
        isPhoneVerified: true,
        isEmailVerified: true,
      });
    });
  });

  it('shows error alert when API returns success=false', async () => {
    (authApi.login as jest.Mock).mockResolvedValue({
      success: false,
      error: 'AUTH:INVALID_CREDENTIALS',
    });

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'wrong@email.com');
    fireEvent.changeText(getByLabelText('Password'), 'wrongpass');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'AUTH:INVALID_CREDENTIALS'
      );
    });
  });

  it('shows error alert on 401 unauthorized response', async () => {
    const axiosError = {
      response: { status: 401, data: {} },
      request: {},
      isAxiosError: true,
    };
    (authApi.login as jest.Mock).mockRejectedValue(axiosError);

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'wrong@email.com');
    fireEvent.changeText(getByLabelText('Password'), 'wrongpass');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Invalid email or password'
      );
    });
  });

  it('shows server error message on non-401 API error', async () => {
    const axiosError = {
      response: { status: 500, data: { message: 'Internal server error' } },
      request: {},
      isAxiosError: true,
    };
    (authApi.login as jest.Mock).mockRejectedValue(axiosError);

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Internal server error'
      );
    });
  });

  it('shows network error alert when no response received', async () => {
    const networkError = {
      request: {},
      isAxiosError: true,
    };
    (authApi.login as jest.Mock).mockRejectedValue(networkError);

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Network error. Please check your connection and try again.'
      );
    });
  });

  it('shows generic error alert on unexpected error', async () => {
    (authApi.login as jest.Mock).mockRejectedValue(new Error('Unexpected'));

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');
    fireEvent.press(getSignInButton(getAllByText));

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Login failed. Please try again.'
      );
    });
  });

  it('disables button during login submission', async () => {
    // Make login hang to keep loading state active
    let resolveLogin: (value: LoginResponse) => void;
    (authApi.login as jest.Mock).mockReturnValue(
      new Promise<LoginResponse>((resolve) => {
        resolveLogin = resolve;
      })
    );

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');

    const signInButton = getSignInButton(getAllByText);
    fireEvent.press(signInButton);

    // During the loading state, the button title changes to "Signing in..."
    await waitFor(() => {
      const allSignIn = getAllByText('Sign In');
      // When loading, one "Sign In" (heading) remains, the button text changes to "Signing in..."
      expect(allSignIn.length).toBeLessThanOrEqual(1);
    });

    // Resolve the login to clean up
    resolveLogin!(mockLoginResponse);
  });
});
