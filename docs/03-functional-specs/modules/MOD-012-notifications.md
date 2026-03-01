# MOD-012: Notifications & Alerts

> Status: APPROVED | Priority: Phase 2 | Last Updated: 2026-03-01

## 1. Overview
Push notifications to EV drivers for charging events, billing, and system alerts via Firebase Cloud Messaging (FCM). Phase 2 feature for mobile app.

## 2. Actors
| Actor | Role |
|-------|------|
| System | Send automated notifications based on events |
| EV Driver | Receive and view notifications |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-012-01 | Send push notification when charging session completes | Must |
| FR-012-02 | Send notification for payment/fee alerts | Must |
| FR-012-03 | Send notification when charging issues/faults occur during session | Must |
| FR-012-04 | Store notification history for in-app viewing | Should |
| FR-012-05 | User notification preferences (enable/disable types) | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-012-01 | Notifications sent via FCM to both iOS and Android |
| BR-012-02 | User must have FCM token registered |
| BR-012-03 | Notification delivery is best-effort (no guaranteed delivery) |
| BR-012-04 | Notification history retained for 90 days |

## 5. Data Model
### Notification (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| UserId | Guid | Yes | FK to AppUser |
| Type | NotificationType (enum) | Yes | ChargeComplete, FeeAlert, FaultAlert, System |
| Title | string(200) | Yes | Notification title |
| Body | string(1000) | Yes | Notification content |
| IsRead | bool | Yes | Read status |
| CreatedAt | DateTime | Yes | When sent |
| ReferenceId | Guid? | No | Related entity ID (session, payment) |

## 6. API Endpoints (Driver BFF)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/notifications | List user notifications | Driver |
| PUT | /api/v1/notifications/{id}/read | Mark as read | Driver |
| PUT | /api/v1/notifications/read-all | Mark all as read | Driver |
| POST | /api/v1/devices/register | Register FCM token | Driver |

## 7. Notification Triggers
| Event | Notification |
|-------|-------------|
| Session completed | "Charging complete! {kWh} kWh charged at {station}" |
| Payment processed | "Payment of {amount}đ processed successfully" |
| Payment failed | "Payment failed. Please retry." |
| Fault during session | "Issue detected at {station}. Please check your session." |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-012-01 | Session completes | Push notification sent, stored in history |
| TC-012-02 | Payment fails | Alert notification sent |
| TC-012-03 | View notification history | Correct list returned |
| TC-012-04 | Mark notification as read | IsRead updated |
