# API Contracts

## Conventions
- RESTful, versioned (/api/v1/)
- Error format: { "code": "MOD_NNN_NNN", "message": "...", "details": {} }
- Pagination: cursor-based { "cursor": "abc", "limit": 20 }
- Auth: Bearer JWT token
- Date: ISO 8601 (stored UTC, displayed UTC+7)
- Currency: VNĐ (dấu chấm phân cách: 9.900đ)

## Admin API (port 5000)
- /api/v1/stations — Station CRUD
- /api/v1/stations/{id}/connectors — Connector management
- /api/v1/monitoring/dashboard — Real-time status
- /api/v1/alerts — Alert management
- /api/v1/faults — Fault tracking
- /api/v1/tariffs — Pricing configuration
- /api/v1/admin/sessions — Session management
- /api/v1/audit-logs — Audit log queries
- /api/v1/station-groups — Station grouping
- /api/v1/e-invoices — E-invoice management

## Driver BFF API (port 5001)
- /api/v1/auth/* — Register, login, token refresh
- /api/v1/profile — User profile
- /api/v1/vehicles — Vehicle CRUD + set-active
- /api/v1/sessions/* — Start, stop, active, history
- /api/v1/payments/* — Process, history, callbacks
- /api/v1/payment-methods — Payment method management
- /api/v1/invoices/* — Invoice access
- /api/v1/notifications — Notification list + read
- /api/v1/stations/nearby — Find nearby stations (map)
