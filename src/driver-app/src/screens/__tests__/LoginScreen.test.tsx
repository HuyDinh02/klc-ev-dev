import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { LoginScreen } from '../LoginScreen';
import { useAuthStore } from '../../stores';

// Spy on Alert.alert
jest.spyOn(Alert, 'alert');

// Reset auth store before each test
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

describe('LoginScreen', () => {
  it('renders the login form with brand and inputs', () => {
    const { getByText, getByLabelText, getAllByText } = render(
      <LoginScreen />
    );

    expect(getByText('KLC')).toBeTruthy();
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
    // Use fake timers to skip the 1-second delay inside handleLogin
    jest.useFakeTimers();
    const loginMock = jest.fn().mockResolvedValue(undefined);
    useAuthStore.setState({ login: loginMock } as any);

    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');
    fireEvent.press(getSignInButton(getAllByText));

    // Advance past the 1000ms setTimeout in handleLogin
    jest.advanceTimersByTime(1100);

    await waitFor(() => {
      expect(loginMock).toHaveBeenCalledWith('mock-token', {
        id: '1',
        email: 'driver@klc.vn',
        fullName: 'Test Driver',
        isPhoneVerified: true,
        isEmailVerified: true,
      });
    });

    jest.useRealTimers();
  });

  it('shows error alert on invalid credentials', async () => {
    jest.useFakeTimers();
    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'wrong@email.com');
    fireEvent.changeText(getByLabelText('Password'), 'wrongpass');
    fireEvent.press(getSignInButton(getAllByText));

    // Advance past the 1000ms setTimeout in handleLogin
    jest.advanceTimersByTime(1100);

    await waitFor(() => {
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Invalid email or password'
      );
    });

    jest.useRealTimers();
  });

  it('disables button during login submission', async () => {
    jest.useFakeTimers();
    const { getByLabelText, getAllByText } = render(<LoginScreen />);

    fireEvent.changeText(getByLabelText('Email'), 'driver@klc.vn');
    fireEvent.changeText(getByLabelText('Password'), 'driver123');

    const signInButton = getSignInButton(getAllByText);
    fireEvent.press(signInButton);

    // During the loading state (1s setTimeout), the button title changes to "Signing in..."
    // The Button component renders ActivityIndicator when loading=true
    // Verify the state changed by checking the button text is no longer "Sign In" x2
    // (one is the heading, the other should now be "Signing in...")
    await waitFor(() => {
      // The loading flag is set, button should now show "Signing in..." as its title prop
      // Even though Button renders ActivityIndicator, the title prop text is still in the tree
      const allSignIn = getAllByText('Sign In');
      // When loading, one "Sign In" (heading) remains, the button text changes to "Signing in..."
      expect(allSignIn.length).toBeLessThanOrEqual(1);
    });

    // Advance timers to finish the login flow
    jest.advanceTimersByTime(1100);
    jest.useRealTimers();
  });
});
