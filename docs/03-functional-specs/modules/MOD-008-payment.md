# MOD-008: Payment & Billing

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Handles payment processing for charging sessions via Vietnamese payment gateways (ZaloPay, MoMo, OnePay). Manages payment methods, transaction lifecycle, and automatic invoice generation.

## 2. Actors
| Actor | Role |
|-------|------|
| EV Driver | Pay for sessions, manage payment methods |
| Finance | View transactions, validate revenue |
| System | Auto-calculate cost, process payment, generate invoice |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-008-01 | Integrate with ZaloPay, MoMo, OnePay payment gateways | Must |
| FR-008-02 | Process payments securely after session completion | Must |
| FR-008-03 | Handle payment success, failure, and timeout scenarios | Must |
| FR-008-04 | Allow users to add, update, and manage payment methods | Must |
| FR-008-05 | Auto-generate digital invoices with session details and tax breakdown | Must |
| FR-008-06 | Send invoice to user via email/in-app | Must |
| FR-008-07 | Payment transaction history with detailed cost breakdown | Must |
| FR-008-08 | Async payment callback handling (webhook from gateways) | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-008-01 | Payment triggered automatically after session ends |
| BR-008-02 | PCI-DSS compliance required for all payment handling |
| BR-008-03 | No duplicate payments for same session |
| BR-008-04 | Failed payment → user prompted to retry, session data preserved |
| BR-008-05 | Invoice includes: session details, energy consumed, tariff, discount, tax, total |
| BR-008-06 | All payment transactions logged for audit |

## 5. Data Model
### PaymentTransaction (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| SessionId | Guid | Yes | FK to ChargingSession |
| UserId | Guid | Yes | FK to AppUser |
| Gateway | PaymentGateway (enum) | Yes | ZaloPay, MoMo, OnePay |
| Amount | decimal | Yes | Total amount (VNĐ) |
| Status | PaymentStatus (enum) | Yes | Pending, Success, Failed, Refunded |
| GatewayTransactionId | string | No | External reference |
| CreatedAt | DateTime | Yes | When initiated |
| CompletedAt | DateTime? | No | When completed |

### Invoice (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| PaymentTransactionId | Guid | Yes | FK |
| InvoiceNumber | string | Yes | Unique invoice number |
| EnergyKwh | decimal | Yes | Total energy |
| BaseAmount | decimal | Yes | Before tax |
| TaxAmount | decimal | Yes | Tax amount |
| DiscountAmount | decimal | Yes | Discount applied |
| TotalAmount | decimal | Yes | Final amount |
| EInvoiceId | string? | No | External e-invoice reference |

### UserPaymentMethod (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| UserId | Guid | Yes | FK |
| Gateway | PaymentGateway (enum) | Yes | ZaloPay, MoMo, OnePay |
| DisplayName | string | Yes | User-friendly name |
| IsDefault | bool | Yes | Default method flag |
| TokenReference | string | Yes | Secure token (no raw card data) |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/payments/process | Process payment for session | Driver |
| GET | /api/v1/payments/history | User payment history | Driver |
| GET | /api/v1/payments/{id} | Payment detail | Driver, Finance |
| POST | /api/v1/payment-methods | Add payment method | Driver |
| GET | /api/v1/payment-methods | List user's payment methods | Driver |
| DELETE | /api/v1/payment-methods/{id} | Remove payment method | Driver |
| GET | /api/v1/invoices/{id} | Get invoice | Driver, Finance |
| POST | /api/v1/payments/callback/{gateway} | Payment gateway callback | System (webhook) |

## 7. Payment Flow
```
1. Session ends → System calculates cost (MOD-007 tariff)
2. System creates PaymentTransaction (Pending)
3. App shows payment screen → user selects method
4. System calls gateway API (ZaloPay/MoMo/OnePay)
5. Gateway processes → callback to CSMS webhook
6. Success → update PaymentTransaction, generate Invoice
7. Failure → notify user, allow retry
```

## 8. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_008_001 | Session not found or already paid | 400 |
| MOD_008_002 | Payment gateway error | 502 |
| MOD_008_003 | Payment timeout | 504 |
| MOD_008_004 | Invalid payment method | 400 |
| MOD_008_005 | Duplicate payment attempt | 409 |

## 9. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-008-01 | Successful payment via ZaloPay | Transaction Success, Invoice generated |
| TC-008-02 | Payment fails | Transaction Failed, user can retry |
| TC-008-03 | Payment timeout | Transaction timeout handled, retry available |
| TC-008-04 | Duplicate payment for same session | 409 error, no double charge |
| TC-008-05 | Add/remove payment method | Methods managed correctly |
| TC-008-06 | View payment history | Correct transaction list |
