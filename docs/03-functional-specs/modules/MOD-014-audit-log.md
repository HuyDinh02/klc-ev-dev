# MOD-014: Audit Logging

> Status: APPROVED | Priority: Phase 2 (Basic in Phase 1) | Last Updated: 2026-03-01

## 1. Overview
Records and stores all operational actions related to charging stations: configuration changes, session start/stop, manual overrides. Built on ABP's built-in audit logging, extended for CSMS operations.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | View audit logs, configure retention |
| System | Auto-log all operations |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-014-01 | Log all station-related operations (create, update, decommission) | Must |
| FR-014-02 | Log all session operations (start, stop, manual interventions) | Must |
| FR-014-03 | Log all configuration changes (tariff, connector settings) | Must |
| FR-014-04 | Query audit logs with filtering by entity, user, date range, action type | Must |
| FR-014-05 | Retain audit logs for minimum 12 months | Must |
| FR-014-06 | Export audit logs (CSV/Excel) | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-014-01 | All admin/operator actions logged automatically |
| BR-014-02 | Logs are immutable (cannot be edited or deleted by users) |
| BR-014-03 | Logs include: who (user), what (action), when (timestamp), where (entity) |
| BR-014-04 | ABP's built-in AuditLog entity extended with CSMS-specific fields |

## 5. Data Model
Uses ABP's built-in `AuditLog` entity, extended with:
| Field | Type | Description |
|-------|------|-------------|
| EntityType | string | Station, Connector, Session, Tariff, etc. |
| EntityId | Guid | ID of affected entity |
| ActionType | string | Create, Update, Delete, Start, Stop, Override |
| Details | string(JSON) | Additional context |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/audit-logs | Query audit logs (filtered) | Admin |
| GET | /api/v1/audit-logs/export | Export logs as CSV | Admin |

## 7. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-014-01 | Create station | Audit log entry created |
| TC-014-02 | Update tariff | Change logged with before/after values |
| TC-014-03 | Query by date range | Correct filtered results |
| TC-014-04 | Attempt to delete audit log | Operation denied |
