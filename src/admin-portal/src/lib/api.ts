import axios from "axios";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "https://localhost:44305";

export const api = axios.create({
  baseURL: `${API_BASE_URL}/api/v1`,
  headers: {
    "Content-Type": "application/json",
  },
});

// Auth API - uses different content type and endpoint
export const authApi = {
  login: async (username: string, password: string) => {
    const response = await axios.post(
      `${API_BASE_URL}/connect/token`,
      new URLSearchParams({
        grant_type: "password",
        username,
        password,
        client_id: "KLC_Api",
        client_secret: "1q2w3e*",
        scope: "KLC",
      }),
      {
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
      }
    );
    return response.data;
  },

  // Parse JWT token to extract user info
  parseToken: (token: string): { sub: string; preferred_username: string; email: string; role: string | string[]; given_name: string; family_name: string } | null => {
    try {
      const base64Payload = token.split(".")[1];
      const payload = JSON.parse(atob(base64Payload));
      return payload;
    } catch {
      return null;
    }
  },
};

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
        localStorage.removeItem("auth-storage");
        window.location.href = `/login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
      }
    }
    return Promise.reject(error);
  }
);

// API functions
export const stationsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number; status?: number; search?: string }) =>
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
  getAll: (params?: { skipCount?: number; maxResultCount?: number; stationId?: string; status?: number }) =>
    api.get("/admin/sessions", { params }),
  getById: (id: string) => api.get(`/sessions/${id}`),
  getMeterValues: (id: string) => api.get(`/sessions/${id}/meter-values`),
};

export const tariffsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number }) =>
    api.get("/tariffs", { params }),
  getById: (id: string) => api.get(`/tariffs/${id}`),
  create: (data: CreateTariffDto) => api.post("/tariffs", data),
  update: (id: string, data: UpdateTariffDto) => api.put(`/tariffs/${id}`, data),
  activate: (id: string) => api.post(`/tariffs/${id}/activate`),
  deactivate: (id: string) => api.post(`/tariffs/${id}/deactivate`),
  setDefault: (id: string) => api.post(`/tariffs/${id}/set-default`),
};

export const faultsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number; status?: number }) =>
    api.get("/faults", { params }),
  getById: (id: string) => api.get(`/faults/${id}`),
  updateStatus: (id: string, status: number, resolutionNotes?: string) =>
    api.put(`/faults/${id}/status`, { status, resolutionNotes }),
  getByStation: (stationId: string) => api.get(`/stations/${stationId}/faults`),
};

export const stationGroupsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number }) =>
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
  getHistory: (params?: { skipCount?: number; maxResultCount?: number; status?: number }) =>
    api.get("/payments/history", { params }),
  getById: (id: string) => api.get(`/payments/${id}`),
};

export const alertsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number }) =>
    api.get("/alerts", { params }),
  acknowledge: (id: string) => api.post(`/alerts/${id}/acknowledge`),
};

export const auditLogsApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number; entityType?: string }) =>
    api.get("/audit-logs", { params }),
  getById: (id: string) => api.get(`/audit-logs/${id}`),
  getEntityChanges: (params?: { skipCount?: number; maxResultCount?: number }) =>
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
  getAll: (params?: { skipCount?: number; maxResultCount?: number; status?: number }) =>
    api.get("/e-invoices", { params }),
  getById: (id: string) => api.get(`/e-invoices/${id}`),
  generate: (invoiceId: string) => api.post("/e-invoices", { invoiceId }),
  retry: (id: string) => api.post(`/e-invoices/${id}/retry`),
  cancel: (id: string) => api.post(`/e-invoices/${id}/cancel`),
  getPdfUrl: (id: string) => api.get(`/e-invoices/${id}/pdf-url`),
};

export const usersApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number; filter?: string }) =>
    api.get("/users", { params }),
  getById: (id: string) => api.get(`/users/${id}`),
  create: (data: CreateUserDto) => api.post("/users", data),
  update: (id: string, data: UpdateUserDto) => api.put(`/users/${id}`, data),
  delete: (id: string) => api.delete(`/users/${id}`),
  getRoles: (id: string) => api.get(`/users/${id}/roles`),
  updateRoles: (id: string, roleNames: string[]) =>
    api.put(`/users/${id}/roles`, { roleNames }),
  lock: (id: string) => api.post(`/users/${id}/lock`, { lockDurationSeconds: 0 }),
  unlock: (id: string) => api.post(`/users/${id}/unlock`),
  resetPassword: (id: string, newPassword: string) =>
    api.post(`/users/${id}/reset-password`, { newPassword }),
};

export const rolesApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number }) =>
    api.get("/roles", { params }),
  getById: (id: string) => api.get(`/roles/${id}`),
  create: (data: { name: string; isDefault?: boolean; isPublic?: boolean }) =>
    api.post("/roles", data),
  update: (id: string, data: { name: string; isDefault?: boolean; isPublic?: boolean; concurrencyStamp?: string }) =>
    api.put(`/roles/${id}`, data),
  delete: (id: string) => api.delete(`/roles/${id}`),
  getPermissions: (roleId: string) =>
    api.get(`/roles/${roleId}/permissions`),
  updatePermissions: (roleId: string, grantedPermissions: string[]) =>
    api.put(`/roles/${roleId}/permissions`, { grantedPermissions }),
};

export const vehiclesApi = {
  getAll: (params?: { skipCount?: number; maxResultCount?: number }) =>
    api.get("/vehicles", { params }),
  getById: (id: string) => api.get(`/vehicles/${id}`),
  delete: (id: string) => api.delete(`/vehicles/${id}`),
};

// Types
export interface CreateUserDto {
  userName: string;
  email: string;
  password: string;
  name?: string;
  surname?: string;
  phoneNumber?: string;
  isActive?: boolean;
  lockoutEnabled?: boolean;
  roleNames?: string[];
}

export interface UpdateUserDto {
  userName: string;
  email: string;
  name?: string;
  surname?: string;
  phoneNumber?: string;
  isActive?: boolean;
  lockoutEnabled?: boolean;
}

export interface CreateStationDto {
  stationCode: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  stationGroupId?: string;
  tariffPlanId?: string;
}

export interface UpdateStationDto {
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  stationGroupId?: string;
  tariffPlanId?: string;
}

export interface CreateConnectorDto {
  connectorNumber: number;
  connectorType: number;
  maxPowerKw: number;
}

export interface UpdateConnectorDto {
  maxPowerKw?: number;
}

export interface CreateTariffDto {
  name: string;
  description?: string;
  baseRatePerKwh: number;
  taxRatePercent: number;
  effectiveFrom: string;
  effectiveTo?: string;
}

export interface UpdateTariffDto {
  name?: string;
  description?: string;
  baseRatePerKwh?: number;
  taxRatePercent?: number;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface CreateStationGroupDto {
  name: string;
  description?: string;
}

export interface UpdateStationGroupDto {
  name?: string;
  description?: string;
}
