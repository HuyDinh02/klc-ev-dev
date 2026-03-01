# Admin Portal

## Stack
React.js + Next.js + TailwindCSS

## Roles (RBAC)
| Role | Access |
|------|--------|
| Admin | Full: stations, connectors, tariffs, users, groups, audit |
| Operator | Monitoring, station management, maintenance |
| Finance | Revenue dashboards, transactions, invoices, reporting |
| Technical Support | Faults, alerts, maintenance tickets |

## Key Pages
1. **Dashboard** — Real-time station map, KPIs (revenue, utilization, sessions)
2. **Station Management** — CRUD stations, connector management, status control
3. **Monitoring** — Live status updates (SignalR), alert panel, status timeline
4. **Charging Sessions** — Active + historical sessions, details
5. **Tariff Management** — Pricing plans CRUD, assignment to stations
6. **Payment & Transactions** — Transaction list, revenue tracking
7. **Fault Management** — Fault list, status tracking, link to maintenance
8. **Maintenance** — Ticket management (create, assign, track, resolve)
9. **Station Groups** — Group CRUD, station assignment
10. **Audit Logs** — Query, filter, export
11. **E-Invoice Management** — Invoice list, retry failed, status monitoring

## API Integration
All calls to Admin API (port 5000), full ABP controllers.
