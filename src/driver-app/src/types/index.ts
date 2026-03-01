// Station types
export interface Station {
  id: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  status: StationStatus;
  connectors: Connector[];
  distance?: number;
  isOnline: boolean;
}

export type StationStatus = 'Online' | 'Offline' | 'Maintenance';

export interface Connector {
  id: string;
  connectorId: number;
  type: ConnectorType;
  status: ConnectorStatus;
  powerKw: number;
  currentSessionId?: string;
}

export type ConnectorType = 'Type2' | 'CCS2' | 'CHAdeMO' | 'GBT';
export type ConnectorStatus = 'Available' | 'Preparing' | 'Charging' | 'Finishing' | 'Faulted' | 'Unavailable';

// Session types
export interface ChargingSession {
  id: string;
  stationId: string;
  stationName: string;
  connectorId: string;
  connectorType: ConnectorType;
  status: SessionStatus;
  startTime: string;
  endTime?: string;
  energyKwh: number;
  durationMinutes: number;
  estimatedCost: number;
  actualCost?: number;
  meterStart: number;
  meterStop?: number;
}

export type SessionStatus = 'Active' | 'Completed' | 'Failed' | 'Cancelled';

export interface MeterValue {
  timestamp: string;
  energyKwh: number;
  powerKw: number;
  soc?: number;
}

// Payment types
export interface Payment {
  id: string;
  sessionId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method: PaymentMethod;
  createdAt: string;
  paidAt?: string;
}

export type PaymentStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Refunded';
export type PaymentMethod = 'ZaloPay' | 'MoMo' | 'OnePay' | 'VNPay' | 'Card';

export interface PaymentMethodInfo {
  id: string;
  type: PaymentMethod;
  displayName: string;
  isDefault: boolean;
  lastFourDigits?: string;
}

// Vehicle types
export interface Vehicle {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  batteryCapacityKwh: number;
  connectorType: ConnectorType;
  isDefault: boolean;
}

// User types
export interface UserProfile {
  id: string;
  email: string;
  phoneNumber?: string;
  fullName: string;
  avatarUrl?: string;
  isPhoneVerified: boolean;
  isEmailVerified: boolean;
}

export interface UserStatistics {
  totalSessions: number;
  totalEnergyKwh: number;
  totalSpent: number;
  totalChargingMinutes: number;
  co2Saved: number;
}

// Notification types
export interface Notification {
  id: string;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  data?: Record<string, unknown>;
}

export type NotificationType = 'SessionComplete' | 'PaymentSuccess' | 'PaymentFailed' | 'Promotion' | 'System';

// API Response types
export interface PaginatedResponse<T> {
  items: T[];
  nextCursor?: string;
  hasMore: boolean;
}

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}
