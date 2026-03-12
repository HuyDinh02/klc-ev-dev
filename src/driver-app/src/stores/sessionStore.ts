import { create } from 'zustand';
import type { ChargingSession, MeterValue } from '../types';
import { sessionsApi } from '../api/sessions';

interface SessionState {
  activeSession: ChargingSession | null;
  latestMeterValue: MeterValue | null;
  isConnecting: boolean;
  isCheckingActive: boolean;
  setActiveSession: (session: ChargingSession | null) => void;
  updateMeterValue: (meterValue: MeterValue) => void;
  updateSessionStatus: (status: ChargingSession['status']) => void;
  clearSession: () => void;
  setConnecting: (connecting: boolean) => void;
  checkActiveSession: () => Promise<boolean>;
}

export const useSessionStore = create<SessionState>((set) => ({
  activeSession: null,
  latestMeterValue: null,
  isConnecting: false,
  isCheckingActive: false,

  setActiveSession: (session) => set({ activeSession: session }),

  updateMeterValue: (meterValue) =>
    set((state) => ({
      latestMeterValue: meterValue,
      activeSession: state.activeSession
        ? {
            ...state.activeSession,
            energyKwh: meterValue.energyKwh,
          }
        : null,
    })),

  updateSessionStatus: (status) =>
    set((state) => ({
      activeSession: state.activeSession
        ? { ...state.activeSession, status }
        : null,
    })),

  clearSession: () => set({ activeSession: null, latestMeterValue: null }),

  setConnecting: (connecting) => set({ isConnecting: connecting }),

  checkActiveSession: async () => {
    set({ isCheckingActive: true });
    try {
      const session = await sessionsApi.getActive();
      if (session) {
        set({ activeSession: session });
        return true;
      }
      return false;
    } catch {
      return false;
    } finally {
      set({ isCheckingActive: false });
    }
  },
}));
