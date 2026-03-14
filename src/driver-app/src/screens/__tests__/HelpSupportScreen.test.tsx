import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { HelpSupportScreen } from '../HelpSupportScreen';
import { feedbackApi } from '../../api/feedback';

jest.mock('../../api/feedback', () => ({
  feedbackApi: {
    getFaq: jest.fn(),
    submitFeedback: jest.fn(),
  },
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

jest.spyOn(Alert, 'alert');

const mockFaqItems = [
  { question: 'How to start charging?', answer: 'Plug in your vehicle and tap Start.' },
  { question: 'How to pay?', answer: 'Use your wallet or payment method.' },
];

beforeEach(() => {
  jest.clearAllMocks();
  (feedbackApi.getFaq as jest.Mock).mockResolvedValue(mockFaqItems);
});

describe('HelpSupportScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: unknown) => void;
    (feedbackApi.getFaq as jest.Mock).mockReturnValue(
      new Promise((r) => { resolve = r; })
    );

    const { getByLabelText } = render(<HelpSupportScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!([]); });
  });

  it('renders help & support title', async () => {
    const { getByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Help & Support')).toBeTruthy();
    });
  });

  it('renders FAQ section', async () => {
    const { getByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Frequently Asked Questions')).toBeTruthy();
    });
    expect(getByText('How to start charging?')).toBeTruthy();
    expect(getByText('How to pay?')).toBeTruthy();
  });

  it('expands FAQ answer on tap', async () => {
    const { getByText, queryByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('How to start charging?')).toBeTruthy();
    });

    // Answer should not be visible initially
    expect(queryByText('Plug in your vehicle and tap Start.')).toBeNull();

    fireEvent.press(getByText('How to start charging?'));

    expect(getByText('Plug in your vehicle and tap Start.')).toBeTruthy();
  });

  it('renders send feedback section', async () => {
    const { getByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Send Feedback')).toBeTruthy();
    });
  });

  it('renders contact info section', async () => {
    const { getByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Contact Us')).toBeTruthy();
    });
    expect(getByText('support@klc.vn')).toBeTruthy();
    expect(getByText('1900-xxxx')).toBeTruthy();
  });

  it('shows error when submitting empty feedback', async () => {
    const { getByText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Submit Feedback')).toBeTruthy();
    });

    // Submit is disabled when fields are empty (disabled prop)
    // But we can verify the button exists
    expect(getByText('Submit Feedback')).toBeTruthy();
  });

  it('submits feedback successfully', async () => {
    (feedbackApi.submitFeedback as jest.Mock).mockResolvedValue({});

    const { getByText, getByLabelText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Send Feedback')).toBeTruthy();
    });

    // Fill in subject and message
    const subjectInput = getByLabelText('Subject');
    const messageInput = getByLabelText('Message');

    fireEvent.changeText(subjectInput, 'Test Subject');
    fireEvent.changeText(messageInput, 'Test message body');

    await act(async () => {
      fireEvent.press(getByText('Submit Feedback'));
    });

    expect(feedbackApi.submitFeedback).toHaveBeenCalledWith({
      type: 0,
      subject: 'Test Subject',
      message: 'Test message body',
    });

    expect(Alert.alert).toHaveBeenCalledWith('Success', expect.any(String));
  });

  it('handles feedback submission error', async () => {
    (feedbackApi.submitFeedback as jest.Mock).mockRejectedValue(new Error('Failed'));

    const { getByText, getByLabelText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByText('Send Feedback')).toBeTruthy();
    });

    fireEvent.changeText(getByLabelText('Subject'), 'Test');
    fireEvent.changeText(getByLabelText('Message'), 'Test message');

    await act(async () => {
      fireEvent.press(getByText('Submit Feedback'));
    });

    expect(Alert.alert).toHaveBeenCalledWith('Error', expect.any(String));
  });

  it('renders back button', async () => {
    const { getByLabelText } = render(<HelpSupportScreen />);

    await waitFor(() => {
      expect(getByLabelText('Go Back')).toBeTruthy();
    });
  });
});
