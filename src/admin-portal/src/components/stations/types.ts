export interface StationListItem {
  id: string;
  stationCode: string;
  name: string;
  address: string;
  status: number;
  isEnabled: boolean;
  connectorCount?: number;
  lastHeartbeat?: string;
}
