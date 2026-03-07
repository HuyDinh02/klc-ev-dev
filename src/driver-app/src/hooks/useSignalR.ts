import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { Config } from '../constants/config';
import { useSessionStore } from '../stores/sessionStore';
import { getAuthToken } from '../api/client';
import type { MeterValue, ChargingSession } from '../types';

export function useSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const { updateMeterValue, updateSessionStatus, setActiveSession } = useSessionStore();

  const connect = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const token = await getAuthToken();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(Config.SIGNALR_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Handle real-time session updates (energy, cost, duration) — sent every 10-30s during charging
    connection.on('OnSessionUpdate', (data: {
      sessionId: string;
      energyKwh: number;
      currentCost: number;
      durationMinutes: number;
      powerKw?: number;
      socPercent?: number;
      timestamp: string;
    }) => {
      updateMeterValue({
        timestamp: data.timestamp,
        energyKwh: data.energyKwh,
        powerKw: data.powerKw ?? 0,
        soc: data.socPercent,
      });
    });

    // Handle session status changes (started, stopped, completed, failed)
    connection.on('OnSessionStatusChanged', (data: { sessionId: string; status: string; message?: string }) => {
      updateSessionStatus(data.status as ChargingSession['status']);
    });

    // Handle session completed with summary
    connection.on('OnSessionCompleted', (data: {
      sessionId: string;
      totalEnergyKwh: number;
      totalCost: number;
      durationMinutes: number;
      completedAt: string;
    }) => {
      updateSessionStatus('Completed');
    });

    // Handle charging errors
    connection.on('OnChargingError', (data: { sessionId: string; errorCode: string; message: string }) => {
      updateSessionStatus('Failed');
    });

    try {
      await connection.start();
      connectionRef.current = connection;
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  }, [updateMeterValue, updateSessionStatus, setActiveSession]);

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      await connectionRef.current.stop();
      connectionRef.current = null;
    }
  }, []);

  const subscribeToSession = useCallback(async (sessionId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToSession', sessionId);
    }
  }, []);

  const unsubscribeFromSession = useCallback(async (sessionId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromSession', sessionId);
    }
  }, []);

  useEffect(() => {
    return () => {
      disconnect();
    };
  }, [disconnect]);

  return {
    connect,
    disconnect,
    subscribeToSession,
    unsubscribeFromSession,
    isConnected: connectionRef.current?.state === signalR.HubConnectionState.Connected,
  };
}
