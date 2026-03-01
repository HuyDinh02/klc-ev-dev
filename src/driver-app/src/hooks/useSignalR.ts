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

    // Handle meter value updates
    connection.on('MeterValueUpdate', (data: MeterValue) => {
      updateMeterValue(data);
    });

    // Handle session status changes
    connection.on('SessionStatusChanged', (data: { sessionId: string; status: ChargingSession['status'] }) => {
      updateSessionStatus(data.status);
    });

    // Handle session started
    connection.on('SessionStarted', (session: ChargingSession) => {
      setActiveSession(session);
    });

    // Handle session completed
    connection.on('SessionCompleted', (session: ChargingSession) => {
      setActiveSession(session);
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
