import axios from "axios";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export const api = axios.create({
  baseURL: `${API_BASE_URL}/api/v1`,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = typeof window !== "undefined"
      ? localStorage.getItem("access_token")
      : null;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      if (typeof window !== "undefined") {
        localStorage.removeItem("access_token");
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

// API functions
export const stationsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number; status?: string }) =>
    api.get("/stations", { params }),
  getById: (id: string) => api.get(`/stations/${id}`),
  create: (data: CreateStationDto) => api.post("/stations", data),
  update: (id: string, data: UpdateStationDto) => api.put(`/stations/${id}`, data),
  enable: (id: string) => api.post(`/stations/${id}/enable`),
  disable: (id: string) => api.post(`/stations/${id}/disable`),
  decommission: (id: string) => api.post(`/stations/${id}/decommission`),
};

export const connectorsApi = {
  getByStation: (stationId: string) => api.get(`/stations/${stationId}/connectors`),
  getById: (id: string) => api.get(`/connectors/${id}`),
  create: (stationId: string, data: CreateConnectorDto) =>
    api.post(`/stations/${stationId}/connectors`, data),
  update: (id: string, data: UpdateConnectorDto) => api.put(`/connectors/${id}`, data),
  enable: (id: string) => api.post(`/connectors/${id}/enable`),
  disable: (id: string) => api.post(`/connectors/${id}/disable`),
  delete: (id: string) => api.delete(`/connectors/${id}`),
};

export const sessionsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number; stationId?: string }) =>
    api.get("/admin/sessions", { params }),
  getById: (id: string) => api.get(`/sessions/${id}`),
  getMeterValues: (id: string) => api.get(`/sessions/${id}/meter-values`),
};

export const tariffsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number }) =>
    api.get("/tariffs", { params }),
  getById: (id: string) => api.get(`/tariffs/${id}`),
  create: (data: CreateTariffDto) => api.post("/tariffs", data),
  update: (id: string, data: UpdateTariffDto) => api.put(`/tariffs/${id}`, data),
  activate: (id: string) => api.post(`/tariffs/${id}/activate`),
  deactivate: (id: string) => api.post(`/tariffs/${id}/deactivate`),
  setDefault: (id: string) => api.post(`/tariffs/${id}/set-default`),
};

export const faultsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number; status?: string }) =>
    api.get("/faults", { params }),
  getById: (id: string) => api.get(`/faults/${id}`),
  updateStatus: (id: string, status: string) => api.put(`/faults/${id}/status`, { status }),
  getByStation: (stationId: string) => api.get(`/stations/${stationId}/faults`),
};

export const stationGroupsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number }) =>
    api.get("/station-groups", { params }),
  getById: (id: string) => api.get(`/station-groups/${id}`),
  create: (data: CreateStationGroupDto) => api.post("/station-groups", data),
  update: (id: string, data: UpdateStationGroupDto) => api.put(`/station-groups/${id}`, data),
  delete: (id: string) => api.delete(`/station-groups/${id}`),
  assignStation: (id: string, stationId: string) =>
    api.post(`/station-groups/${id}/assign`, { stationId }),
  unassignStation: (id: string, stationId: string) =>
    api.delete(`/station-groups/${id}/stations/${stationId}`),
};

export const paymentsApi = {
  getHistory: (params?: { skip?: number; maxResultCount?: number }) =>
    api.get("/payments/history", { params }),
  getById: (id: string) => api.get(`/payments/${id}`),
};

export const alertsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number }) =>
    api.get("/alerts", { params }),
  acknowledge: (id: string) => api.post(`/alerts/${id}/acknowledge`),
};

export const auditLogsApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number; entityType?: string }) =>
    api.get("/audit-logs", { params }),
  getById: (id: string) => api.get(`/audit-logs/${id}`),
  getEntityChanges: (params?: { skip?: number; maxResultCount?: number }) =>
    api.get("/audit-logs/entity-changes", { params }),
  export: (params?: { startDate?: string; endDate?: string }) =>
    api.get("/audit-logs/export", { params, responseType: "blob" }),
};

export const monitoringApi = {
  getDashboard: () => api.get("/monitoring/dashboard"),
  getStatusHistory: (stationId: string) =>
    api.get(`/monitoring/stations/${stationId}/status-history`),
  getEnergySummary: (stationId: string) =>
    api.get(`/monitoring/stations/${stationId}/energy-summary`),
};

export const eInvoicesApi = {
  getAll: (params?: { skip?: number; maxResultCount?: number; status?: string }) =>
    api.get("/e-invoices", { params }),
  getById: (id: string) => api.get(`/e-invoices/${id}`),
  generate: (invoiceId: string) => api.post("/e-invoices", { invoiceId }),
  retry: (id: string) => api.post(`/e-invoices/${id}/retry`),
  cancel: (id: string) => api.post(`/e-invoices/${id}/cancel`),
  getPdfUrl: (id: string) => api.get(`/e-invoices/${id}/pdf-url`),
};

// Types
export interface CreateStationDto {
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  groupId?: string;
}

export interface UpdateStationDto {
  name?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
}

export interface CreateConnectorDto {
  connectorNumber: number;
  connectorType: string;
  maxPowerKw: number;
}

export interface UpdateConnectorDto {
  maxPowerKw?: number;
}

export interface CreateTariffDto {
  name: string;
  description?: string;
  pricePerKwh: number;
  connectionFee?: number;
  idleFeePerMinute?: number;
}

export interface UpdateTariffDto {
  name?: string;
  description?: string;
  pricePerKwh?: number;
  connectionFee?: number;
  idleFeePerMinute?: number;
}

export interface CreateStationGroupDto {
  name: string;
  description?: string;
}

export interface UpdateStationGroupDto {
  name?: string;
  description?: string;
}
