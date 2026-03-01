import { create } from 'zustand';
import type { ChargingSession, MeterValue } from '../types';

interface SessionState {
  activeSession: ChargingSession | null;
  latestMeterValue: MeterValue | null;
  isConnecting: boolean;
  setActiveSession: (session: ChargingSession | null) => void;
  updateMeterValue: (meterValue: MeterValue) => void;
  updateSessionStatus: (status: ChargingSession['status']) => void;
  clearSession: () => void;
  setConnecting: (connecting: boolean) => void;
}

export const useSessionStore = create<SessionState>((set) => ({
  activeSession: null,
  latestMeterValue: null,
  isConnecting: false,

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
}));
