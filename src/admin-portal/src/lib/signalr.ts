import { useEffect, useRef, useState, useCallback } from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

const HUB_URL =
  (process.env.NEXT_PUBLIC_API_URL || "https://localhost:44305") +
  "/hubs/monitoring";

export type ConnectionStatus = "disconnected" | "connecting" | "connected";

export interface StationStatusUpdate {
  stationId: string;
  stationName: string;
  previousStatus: string;
  newStatus: string;
  timestamp: string;
}

export interface ConnectorStatusUpdate {
  stationId: string;
  connectorNumber: number;
  previousStatus: string;
  newStatus: string;
  timestamp: string;
}

export interface AlertNotification {
  alertId: string;
  stationId: string | null;
  stationName: string | null;
  alertType: string;
  message: string;
  timestamp: string;
}

export interface SessionUpdate {
  sessionId: string;
  stationId: string;
  connectorNumber: number;
  status: string;
  currentEnergyKwh: number;
  currentCost: number;
  timestamp: string;
}

export interface MeterValueUpdate {
  sessionId: string;
  energyKwh: number;
  powerKw: number | null;
  socPercent: number | null;
  timestamp: string;
}

export interface ConnectorAllocation {
  connectorId: string;
  stationId: string;
  stationCode: string;
  connectorNumber: number;
  allocatedPowerKw: number;
  maxPowerKw: number;
}

export interface PowerAllocationUpdate {
  groupId: string;
  groupName: string;
  totalCapacityKw: number;
  totalAllocatedKw: number;
  activeConnectors: number;
  profilesDispatched: number;
  allocations: ConnectorAllocation[];
  timestamp: string;
}

interface MonitoringCallbacks {
  onStationStatusChanged?: (update: StationStatusUpdate) => void;
  onConnectorStatusChanged?: (update: ConnectorStatusUpdate) => void;
  onAlertCreated?: (alert: AlertNotification) => void;
  onSessionUpdated?: (update: SessionUpdate) => void;
  onMeterValueReceived?: (update: MeterValueUpdate) => void;
  onPowerAllocationChanged?: (update: PowerAllocationUpdate) => void;
}

export function useMonitoringHub(callbacks: MonitoringCallbacks) {
  const [status, setStatus] = useState<ConnectionStatus>("disconnected");
  const connectionRef = useRef<HubConnection | null>(null);
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;

  useEffect(() => {
    const token = localStorage.getItem("access_token");
    if (!token) return;

    let cancelled = false;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () =>
          localStorage.getItem("access_token") || "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.None)
      .build();

    connection.on("OnStationStatusChanged", (update: StationStatusUpdate) => {
      callbacksRef.current.onStationStatusChanged?.(update);
    });

    connection.on(
      "OnConnectorStatusChanged",
      (update: ConnectorStatusUpdate) => {
        callbacksRef.current.onConnectorStatusChanged?.(update);
      }
    );

    connection.on("OnAlertCreated", (alert: AlertNotification) => {
      callbacksRef.current.onAlertCreated?.(alert);
    });

    connection.on("OnSessionUpdated", (update: SessionUpdate) => {
      callbacksRef.current.onSessionUpdated?.(update);
    });

    connection.on("OnMeterValueReceived", (update: MeterValueUpdate) => {
      callbacksRef.current.onMeterValueReceived?.(update);
    });

    connection.on("OnPowerAllocationChanged", (update: PowerAllocationUpdate) => {
      callbacksRef.current.onPowerAllocationChanged?.(update);
    });

    connection.onreconnecting(() => {
      if (!cancelled) setStatus("connecting");
    });
    connection.onreconnected(() => {
      if (!cancelled) setStatus("connected");
    });
    connection.onclose(() => {
      if (!cancelled) setStatus("disconnected");
    });

    // Delay start to survive React 18 strict mode double-mount and HMR.
    // On the first (discarded) mount, cleanup fires immediately and clears
    // the timeout before the connection starts. The second mount then
    // starts cleanly without the "stopped during negotiation" error.
    const startTimer = setTimeout(() => {
      if (cancelled) return;
      connectionRef.current = connection;
      setStatus("connecting");
      connection
        .start()
        .then(() => {
          if (!cancelled) setStatus("connected");
        })
        .catch(() => {
          if (!cancelled) setStatus("disconnected");
        });
    }, 200);

    return () => {
      cancelled = true;
      clearTimeout(startTimer);
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop();
      }
      connectionRef.current = null;
    };
  }, []);

  const subscribeToStation = useCallback(async (stationId: string) => {
    const conn = connectionRef.current;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("SubscribeToStation", stationId);
    }
  }, []);

  const unsubscribeFromStation = useCallback(async (stationId: string) => {
    const conn = connectionRef.current;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("UnsubscribeFromStation", stationId);
    }
  }, []);

  const subscribeToPowerSharingGroup = useCallback(async (groupId: string) => {
    const conn = connectionRef.current;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("SubscribeToPowerSharingGroup", groupId);
    }
  }, []);

  const unsubscribeFromPowerSharingGroup = useCallback(async (groupId: string) => {
    const conn = connectionRef.current;
    if (conn?.state === HubConnectionState.Connected) {
      await conn.invoke("UnsubscribeFromPowerSharingGroup", groupId);
    }
  }, []);

  return {
    status,
    subscribeToStation,
    unsubscribeFromStation,
    subscribeToPowerSharingGroup,
    unsubscribeFromPowerSharingGroup,
  };
}
