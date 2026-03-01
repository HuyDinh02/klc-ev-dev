# Security Architecture

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. Authentication & Authorization

- **Identity:** ABP Identity + OpenIddict (OAuth2/OpenID Connect)
- **Admin Portal:** RBAC with 4 roles: Admin, Operator, Finance, Technical Support
- **Mobile App:** JWT bearer tokens, secure password policies
- **API:** Bearer token authentication on all endpoints

## 2. Data Protection

- All data encrypted in transit via HTTPS/TLS
- Personal and payment data encrypted at rest
- WSS (WebSocket Secure) for OCPP charger communication
- PCI-DSS compliance for payment data handling

## 3. Access Control

| Role | Access Level |
|------|-------------|
| Admin | Full system access, configuration, pricing, user management |
| Operator | Station monitoring, charger management, maintenance |
| Finance | Revenue dashboards, transaction tracking, reporting |
| Technical Support | Fault management, maintenance tickets, diagnostics |
| EV Driver | Own profile, vehicles, sessions, payments (mobile app only) |

## 4. Compliance

- Vietnamese data protection law compliance
- Financial transaction logs retained for audit
- Tax calculation compliance with local regulations
- E-invoice regulatory compliance (MISA, Viettel, VNPT)
