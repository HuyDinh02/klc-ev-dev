import { useSessionStore } from '../sessionStore';
import { sessionsApi } from '../../api/sessions';
import type { ChargingSession, MeterValue } from '../../types';

// Mock the sessions API module
jest.mock('../../api/sessions', () => ({
  sessionsApi: {
    getActive: jest.fn(),
  },
}));

const mockSession: ChargingSession = {
  id: 'session-1',
  stationId: 'station-1',
  stationName: 'KLC Station A',
  connectorId: 'connector-1',
  connectorType: 'CCS2',
  status: 'Active',
  startTime: '2026-03-12T10:00:00Z',
  energyKwh: 5.5,
  durationMinutes: 30,
  estimatedCost: 45000,
  meterStart: 1000,
};

const mockMeterValue: MeterValue = {
  timestamp: '2026-03-12T10:30:00Z',
  energyKwh: 12.3,
  powerKw: 50,
  soc: 65,
};

// Reset store and mocks before each test
beforeEach(() => {
  useSessionStore.setState({
    activeSession: null,
    latestMeterValue: null,
    isConnecting: false,
    isCheckingActive: false,
  });
  jest.clearAllMocks();
});

describe('useSessionStore', () => {
  describe('initial state', () => {
    it('should have correct default values', () => {
      const state = useSessionStore.getState();
      expect(state.activeSession).toBeNull();
      expect(state.latestMeterValue).toBeNull();
      expect(state.isConnecting).toBe(false);
      expect(state.isCheckingActive).toBe(false);
    });
  });

  describe('setActiveSession', () => {
    it('should set the active session', () => {
      useSessionStore.getState().setActiveSession(mockSession);
      expect(useSessionStore.getState().activeSession).toEqual(mockSession);
    });

    it('should allow setting session to null', () => {
      useSessionStore.getState().setActiveSession(mockSession);
      useSessionStore.getState().setActiveSession(null);
      expect(useSessionStore.getState().activeSession).toBeNull();
    });
  });

  describe('clearSession', () => {
    it('should clear session and meter value', () => {
      // Set up some state first
      useSessionStore.setState({
        activeSession: mockSession,
        latestMeterValue: mockMeterValue,
      });

      useSessionStore.getState().clearSession();

      const state = useSessionStore.getState();
      expect(state.activeSession).toBeNull();
      expect(state.latestMeterValue).toBeNull();
    });
  });

  describe('updateMeterValue', () => {
    it('should update meter value and session energy when session exists', () => {
      useSessionStore.setState({ activeSession: mockSession });

      useSessionStore.getState().updateMeterValue(mockMeterValue);

      const state = useSessionStore.getState();
      expect(state.latestMeterValue).toEqual(mockMeterValue);
      expect(state.activeSession?.energyKwh).toBe(mockMeterValue.energyKwh);
    });

    it('should update meter value but keep session null when no active session', () => {
      useSessionStore.getState().updateMeterValue(mockMeterValue);

      const state = useSessionStore.getState();
      expect(state.latestMeterValue).toEqual(mockMeterValue);
      expect(state.activeSession).toBeNull();
    });

    it('should preserve other session fields when updating energy', () => {
      useSessionStore.setState({ activeSession: mockSession });

      useSessionStore.getState().updateMeterValue(mockMeterValue);

      const session = useSessionStore.getState().activeSession;
      expect(session?.id).toBe(mockSession.id);
      expect(session?.stationName).toBe(mockSession.stationName);
      expect(session?.status).toBe(mockSession.status);
      expect(session?.energyKwh).toBe(mockMeterValue.energyKwh);
    });
  });

  describe('updateSessionStatus', () => {
    it('should update session status when session exists', () => {
      useSessionStore.setState({ activeSession: mockSession });

      useSessionStore.getState().updateSessionStatus('Completed');

      expect(useSessionStore.getState().activeSession?.status).toBe(
        'Completed'
      );
    });

    it('should keep session null when no active session', () => {
      useSessionStore.getState().updateSessionStatus('Completed');

      expect(useSessionStore.getState().activeSession).toBeNull();
    });

    it('should preserve other session fields when updating status', () => {
      useSessionStore.setState({ activeSession: mockSession });

      useSessionStore.getState().updateSessionStatus('Completed');

      const session = useSessionStore.getState().activeSession;
      expect(session?.id).toBe(mockSession.id);
      expect(session?.energyKwh).toBe(mockSession.energyKwh);
      expect(session?.stationName).toBe(mockSession.stationName);
    });
  });

  describe('setConnecting', () => {
    it('should set connecting to true', () => {
      useSessionStore.getState().setConnecting(true);
      expect(useSessionStore.getState().isConnecting).toBe(true);
    });

    it('should set connecting to false', () => {
      useSessionStore.getState().setConnecting(true);
      useSessionStore.getState().setConnecting(false);
      expect(useSessionStore.getState().isConnecting).toBe(false);
    });
  });

  describe('checkActiveSession', () => {
    it('should return true when active session exists', async () => {
      (sessionsApi.getActive as jest.Mock).mockResolvedValue(mockSession);

      const result = await useSessionStore.getState().checkActiveSession();

      expect(result).toBe(true);
      expect(useSessionStore.getState().activeSession).toEqual(mockSession);
      expect(useSessionStore.getState().isCheckingActive).toBe(false);
    });

    it('should return false when no active session', async () => {
      (sessionsApi.getActive as jest.Mock).mockResolvedValue(null);

      const result = await useSessionStore.getState().checkActiveSession();

      expect(result).toBe(false);
      expect(useSessionStore.getState().activeSession).toBeNull();
      expect(useSessionStore.getState().isCheckingActive).toBe(false);
    });

    it('should handle API errors gracefully and return false', async () => {
      (sessionsApi.getActive as jest.Mock).mockRejectedValue(
        new Error('Network error')
      );

      const result = await useSessionStore.getState().checkActiveSession();

      expect(result).toBe(false);
      expect(useSessionStore.getState().isCheckingActive).toBe(false);
    });

    it('should set isCheckingActive during the check', async () => {
      let resolveGetActive: (value: ChargingSession | null) => void;
      (sessionsApi.getActive as jest.Mock).mockReturnValue(
        new Promise<ChargingSession | null>((resolve) => {
          resolveGetActive = resolve;
        })
      );

      const promise = useSessionStore.getState().checkActiveSession();

      // While checking, isCheckingActive should be true
      expect(useSessionStore.getState().isCheckingActive).toBe(true);

      // Resolve and verify it's set back to false
      resolveGetActive!(null);
      await promise;

      expect(useSessionStore.getState().isCheckingActive).toBe(false);
    });

    it('should call sessionsApi.getActive', async () => {
      (sessionsApi.getActive as jest.Mock).mockResolvedValue(null);

      await useSessionStore.getState().checkActiveSession();

      expect(sessionsApi.getActive).toHaveBeenCalledTimes(1);
    });
  });
});
