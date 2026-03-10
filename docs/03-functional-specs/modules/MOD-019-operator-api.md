# MOD-019: Operator API (Cloud-to-Cloud)

> Status: DRAFT | Priority: Phase 2 | Last Updated: 2026-03-11

## 1. Overview
External REST API enabling third-party charging network operators to integrate with the CSMS via a cloud-to-cloud interface. Provides OAuth2-authenticated endpoints for station management, session monitoring, analytics retrieval, and webhook-based event delivery. Ensures strict data isolation so each operator only accesses their own assigned stations.

## 2. Actors
| Actor | Role |
|-------|------|
| External Operator (API Consumer) | Authenticate via OAuth2, manage stations, query sessions and analytics, receive webhooks |
| Admin | Approve/suspend operator accounts, assign stations to operators, configure API access |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-019-01 | OAuth2 client credentials flow for operator authentication (client_id + client_secret → access token) | Must |
| FR-019-02 | Station endpoints: list, get detail, update status for operator-assigned stations only | Must |
| FR-019-03 | Session endpoints: list active/historical sessions, get session detail, remote start/stop | Must |
| FR-019-04 | Analytics endpoints: energy summary, revenue summary, station utilization, per-period aggregation | Must |
| FR-019-05 | Webhook registration and delivery for events: session.started, session.completed, station.status_changed, fault.created | Must |
| FR-019-06 | Operator management: CRUD operator accounts, assign/unassign stations, manage API keys | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-019-01 | API rate limit: 1000 requests per minute per operator (HTTP 429 on exceeded) |
| BR-019-02 | Webhook delivery retries: 3 attempts with exponential backoff (10s, 60s, 300s) |
| BR-019-03 | Strict data isolation: operator can only access stations explicitly assigned to them |
| BR-019-04 | Access tokens expire after 1 hour; refresh via client credentials re-authentication |
| BR-019-05 | All API requests are audit-logged with operator ID, endpoint, timestamp, and response status |
| BR-019-06 | Webhook endpoints must respond with HTTP 2xx within 30 seconds or the delivery is marked failed |
| BR-019-07 | Operator API keys can be rotated without downtime (support two active keys during rotation) |
| BR-019-08 | Suspended operators receive HTTP 403 on all API calls; active webhooks are paused |

## 5. Data Model
### OperatorAccount (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| Name | string(200) | Operator organization name |
| ClientId | string(50) | OAuth2 client_id (unique) |
| ClientSecretHash | string(256) | Hashed client_secret |
| SecondaryClientSecretHash | string(256)? | For key rotation (optional) |
| ContactEmail | string(100) | Primary contact email |
| WebhookUrl | string(500)? | Webhook delivery endpoint |
| WebhookSecret | string(100)? | HMAC-SHA256 signing secret for webhook payloads |
| RateLimitPerMinute | int | Requests per minute (default 1000) |
| Status | OperatorStatus | Active, Suspended, Pending |

### OperatorStationAssignment (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| OperatorAccountId | Guid | FK to OperatorAccount |
| StationId | Guid | FK to ChargingStation |
| AssignedAt | DateTime | When station was assigned |

### WebhookDelivery (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| OperatorAccountId | Guid | FK to OperatorAccount |
| EventType | string(50) | session.started, session.completed, station.status_changed, fault.created |
| PayloadJson | string(JSON) | Event payload |
| AttemptCount | int | Number of delivery attempts |
| LastAttemptAt | DateTime? | Last attempt timestamp |
| LastResponseStatus | int? | HTTP status from webhook endpoint |
| Status | WebhookDeliveryStatus | Pending, Delivered, Failed |

### OperatorApiLog (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| OperatorAccountId | Guid | FK to OperatorAccount |
| Method | string(10) | HTTP method (GET, POST, etc.) |
| Endpoint | string(200) | API endpoint path |
| ResponseStatus | int | HTTP response status code |
| Timestamp | DateTime | Request timestamp |

## 6. Authentication Flow
```
1. Operator sends POST /api/v1/operator/token with client_id + client_secret
2. System validates credentials against OperatorAccount (primary or secondary secret)
3. If valid and status = Active → issue JWT access token (1 hour expiry, contains operator_id claim)
4. If suspended → return 403 with explanation
5. If invalid → return 401
6. Operator includes access token in Authorization: Bearer header on subsequent requests
7. All endpoints check operator_id claim → filter data to assigned stations only
```

### Webhook Delivery Flow
```
1. Event occurs (session starts, station status changes, etc.)
2. System checks if any operator is assigned to the affected station
3. For each matched operator with a configured webhook URL:
   a. Build event payload JSON
   b. Sign payload with HMAC-SHA256 using operator webhook secret
   c. POST to operator webhook URL with X-Webhook-Signature header
4. If delivery fails (non-2xx or timeout):
   a. Retry with exponential backoff: 10s → 60s → 300s
   b. After 3 failures → mark as Failed, log warning
```

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_019_001 | Invalid client credentials | 401 |
| MOD_019_002 | Operator account suspended | 403 |
| MOD_019_003 | Rate limit exceeded | 429 |
| MOD_019_004 | Station not assigned to operator | 404 |
| MOD_019_005 | Webhook delivery failed after all retries | Warning (logged) |
| MOD_019_006 | Invalid webhook URL format | 400 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-019-01 | Operator authenticates with valid client credentials | Access token issued with 1-hour expiry |
| TC-019-02 | Operator queries stations — only sees assigned stations | Response contains only operator-assigned stations; others excluded |
| TC-019-03 | Operator exceeds 1000 requests per minute | HTTP 429 returned; subsequent requests allowed after window resets |
| TC-019-04 | Session completes on operator-assigned station | Webhook delivered with session.completed event and valid HMAC signature |
| TC-019-05 | Webhook endpoint returns HTTP 500 | System retries 3 times with exponential backoff; marked Failed after exhaustion |
| TC-019-06 | Suspended operator attempts API call | HTTP 403 returned; request audit-logged |
