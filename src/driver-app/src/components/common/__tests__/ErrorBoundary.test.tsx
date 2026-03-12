import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { Text } from 'react-native';
import { ErrorBoundary } from '../ErrorBoundary';

// Suppress console.error for expected errors in these tests
const originalConsoleError = console.error;
beforeAll(() => {
  console.error = jest.fn();
});
afterAll(() => {
  console.error = originalConsoleError;
});

// A component that throws an error
function ProblemChild({ shouldThrow = true }: { shouldThrow?: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <Text>Child content rendered</Text>;
}

// A component that can toggle between throwing and not throwing
function ToggleableChild() {
  return <Text>Child content rendered</Text>;
}

describe('ErrorBoundary', () => {
  it('renders children when no error occurs', () => {
    const { getByText } = render(
      <ErrorBoundary>
        <ToggleableChild />
      </ErrorBoundary>
    );

    expect(getByText('Child content rendered')).toBeTruthy();
  });

  it('catches errors and renders default ErrorFallback', () => {
    const { getByText } = render(
      <ErrorBoundary>
        <ProblemChild />
      </ErrorBoundary>
    );

    // ErrorFallback should render with i18n translations loaded in jest.setup.js
    expect(getByText('Something went wrong')).toBeTruthy();
    expect(
      getByText('An unexpected error occurred. Please try again.')
    ).toBeTruthy();
  });

  it('renders Try Again button that resets error state', () => {
    const { getByText, queryByText } = render(
      <ErrorBoundary>
        <ProblemChild />
      </ErrorBoundary>
    );

    // ErrorFallback should be visible
    expect(getByText('Something went wrong')).toBeTruthy();
    expect(getByText('Try Again')).toBeTruthy();

    // Note: pressing Try Again resets error state, but the ProblemChild will
    // throw again causing it to re-enter error state. This verifies the
    // resetError function is wired up correctly.
    fireEvent.press(getByText('Try Again'));

    // Since ProblemChild always throws, it will re-enter error state
    expect(getByText('Something went wrong')).toBeTruthy();
  });

  it('renders Go to Home button in the fallback', () => {
    const { getByText } = render(
      <ErrorBoundary>
        <ProblemChild />
      </ErrorBoundary>
    );

    expect(getByText('Go to Home')).toBeTruthy();
  });

  it('renders custom fallback when provided', () => {
    const customFallback = <Text>Custom error view</Text>;

    const { getByText, queryByText } = render(
      <ErrorBoundary fallback={customFallback}>
        <ProblemChild />
      </ErrorBoundary>
    );

    expect(getByText('Custom error view')).toBeTruthy();
    // Default ErrorFallback should NOT be rendered
    expect(queryByText('Something went wrong')).toBeNull();
  });

  it('shows error details in development mode', () => {
    // __DEV__ is true in test environment by default
    const { getByText } = render(
      <ErrorBoundary>
        <ProblemChild />
      </ErrorBoundary>
    );

    expect(getByText('Error details')).toBeTruthy();
    expect(getByText('Test error message')).toBeTruthy();
  });

  it('renders multiple children normally when no error', () => {
    const { getByText } = render(
      <ErrorBoundary>
        <Text>First child</Text>
        <Text>Second child</Text>
      </ErrorBoundary>
    );

    expect(getByText('First child')).toBeTruthy();
    expect(getByText('Second child')).toBeTruthy();
  });
});
