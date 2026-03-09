import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { Config } from '../constants/config';
import { useSessionStore } from '../stores/sessionStore';
import { getAuthToken } from '../api/client';
import type { MeterValue, ChargingSession } from '../types';

// SignalR message types matching the backend DriverHub records
export interface NotificationMessage {
  notificationId: string;
  type: string;
  title: string;
  body: string;
  actionUrl?: string;
  timestamp: string;
}

export interface WalletBalanceChangedMessage {
  userId: string;
  newBalance: number;
  changeAmount: number;
  reason: string;
  timestamp: string;
}

export interface ConnectorStatusMessage {
  stationId: string;
  connectorNumber: number;
  status: string;
  timestamp: string;
}

export interface StationStatusChangedMessage {
  stationId: string;
  stationName: string;
  previousStatus: string;
  newStatus: string;
  timestamp: string;
}

// Optional callbacks that screens can provide to react to specific events
export interface SignalRCallbacks {
  onNotification?: (message: NotificationMessage) => void;
  onWalletBalanceChanged?: (message: WalletBalanceChangedMessage) => void;
  onConnectorStatusChanged?: (message: ConnectorStatusMessage) => void;
  onStationStatusChanged?: (message: StationStatusChangedMessage) => void;
}

export function useSignalR(callbacks?: SignalRCallbacks) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const callbacksRef = useRef<SignalRCallbacks | undefined>(callbacks);
  const { updateMeterValue, updateSessionStatus, setActiveSession } = useSessionStore();

  // Keep callbacks ref in sync without triggering reconnect
  useEffect(() => {
    callbacksRef.current = callbacks;
  }, [callbacks]);

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

    // Handle new notification for user
    connection.on('OnNotification', (data: NotificationMessage) => {
      callbacksRef.current?.onNotification?.(data);
    });

    // Handle wallet balance changed
    connection.on('OnWalletBalanceChanged', (data: WalletBalanceChangedMessage) => {
      callbacksRef.current?.onWalletBalanceChanged?.(data);
    });

    // Handle connector status changed (individual connector)
    connection.on('OnConnectorStatusChanged', (data: ConnectorStatusMessage) => {
      callbacksRef.current?.onConnectorStatusChanged?.(data);
    });

    // Handle station status changed (station-level online/offline)
    connection.on('OnStationStatusChanged', (data: StationStatusChangedMessage) => {
      callbacksRef.current?.onStationStatusChanged?.(data);
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

  const subscribeToStation = useCallback(async (stationId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToStation', stationId);
    }
  }, []);

  const unsubscribeFromStation = useCallback(async (stationId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromStation', stationId);
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
    subscribeToStation,
    unsubscribeFromStation,
    isConnected: connectionRef.current?.state === signalR.HubConnectionState.Connected,
  };
}
