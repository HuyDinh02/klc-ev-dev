# MOD-008: Payment & Billing

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-23

## 1. Overview
Handles payment processing for charging sessions and wallet top-ups via Vietnamese payment gateways (VNPay, MoMo, ZaloPay). Manages payment methods, transaction lifecycle, automatic invoice generation, and e-wallet operations (SBV Circular 41/2025 compliant).

## 2. Actors
| Actor | Role |
|-------|------|
| EV Driver | Pay for sessions, manage payment methods |
| Finance | View transactions, validate revenue |
| System | Auto-calculate cost, process payment, generate invoice |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-008-01 | Integrate with VNPay, MoMo, ZaloPay payment gateways | Must |
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
| Gateway | PaymentGateway (enum) | Yes | VnPay, MoMo, ZaloPay, Wallet, Voucher |
| Amount | decimal | Yes | Total amount (VNĐ) |
| Status | PaymentStatus (enum) | Yes | Pending, Processing, Completed, Failed, Refunded, Cancelled |
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
| Gateway | PaymentGateway (enum) | Yes | VnPay, MoMo, ZaloPay |
| DisplayName | string | Yes | User-friendly name |
| IsDefault | bool | Yes | Default method flag |
| TokenReference | string | Yes | Secure token (no raw card data) |

## 6. API Endpoints

### Admin API (port 44305)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/payments/process | Process payment for session | Driver |
| GET | /api/v1/payments/history | User payment history | Driver, Finance |
| GET | /api/v1/payments/{id} | Payment detail | Driver, Finance |
| POST | /api/v1/payments/callback/{gateway} | Payment gateway callback (POST) | Anonymous (HMAC) |
| GET | /api/v1/payments/vnpay-ipn | VNPay IPN callback (GET) | Anonymous (HMAC) |
| GET | /api/v1/payments/{id}/query-vnpay | Query VNPay transaction status | Admin |
| POST | /api/v1/payments/{id}/refund | Refund payment (calls VNPay refund API) | Admin |
| POST | /api/v1/payment-methods | Add payment method | Driver |
| GET | /api/v1/payment-methods | List user's payment methods | Driver |
| DELETE | /api/v1/payment-methods/{id} | Remove payment method | Driver |
| GET | /api/v1/invoices/{id} | Get invoice | Driver, Finance |

### Driver BFF (port 5001)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/payments/process | Process session payment (with voucher) | Driver |
| GET | /api/v1/payments/history | Payment history | Driver |
| GET | /api/v1/payments/{id} | Payment detail | Driver |
| POST | /api/v1/wallet/topup | Initiate wallet top-up via VNPay/MoMo | Driver |
| GET | /api/v1/wallet/topup/vnpay-ipn | VNPay IPN for wallet top-ups | Anonymous (HMAC) |
| GET | /api/v1/wallet/balance | Get wallet balance | Driver |
| GET | /api/v1/wallet/transactions | Transaction history | Driver |

## 7. Payment Flow

### Session Payment (via Admin API)
```
1. Session ends → System calculates cost (MOD-007 tariff)
2. Driver selects payment method (VNPay/MoMo/Wallet/Voucher)
3. System creates PaymentTransaction (Pending)
4. VNPay: Build signed URL → redirect to VNPay payment page
5. Driver completes payment on VNPay
6. VNPay sends IPN (GET) to /api/v1/payments/vnpay-ipn
7. System verifies signature + amount → updates status → generates Invoice
8. VNPay redirects browser to ReturnUrl (display result only)
```

### Wallet Top-Up (via Driver BFF)
```
1. Driver requests top-up (amount + gateway)
2. System validates limits (SBV Circular 41/2025: 100M VND/month)
3. Creates WalletTransaction (Pending) → calls VNPay CreateTopUp
4. Returns RedirectUrl to mobile app
5. Driver completes payment → VNPay IPN → /api/v1/wallet/topup/vnpay-ipn
6. System credits wallet → SignalR notification → cache invalidation
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
| ID | Scenario | Test Card | Expected Result |
|----|----------|-----------|----------------|
| TC-008-01 | Successful payment via VNPay | NCB `9704198526191432198` OTP `123456` | Transaction Completed, Invoice generated |
| TC-008-02 | Insufficient funds | NCB `9704195798459170488` | Transaction Failed, vnp_ResponseCode=51 |
| TC-008-03 | Customer cancels payment | (click cancel on VNPay page) | vnp_ResponseCode=24, retry available |
| TC-008-04 | Card locked | NCB `9704193370791314` | vnp_ResponseCode=12, user notified |
| TC-008-05 | Duplicate payment for same session | — | Existing pending/completed returned, no double charge |
| TC-008-06 | IPN idempotency | Send same IPN twice | First: RspCode=00, Second: RspCode=02 |
| TC-008-07 | IPN amount mismatch | Tamper vnp_Amount | RspCode=04, payment not updated |
| TC-008-08 | IPN invalid signature | Tamper hash | RspCode=97, payment not updated |
| TC-008-09 | Payment via international card | VISA `4456530000001005` CVV `123` | Transaction Completed |
| TC-008-10 | Wallet top-up via VNPay | NCB success card | Wallet credited, SignalR notification |
| TC-008-11 | Query VNPay transaction status | — | Correct status returned via querydr API |
| TC-008-12 | Refund completed VNPay payment | — | Wallet credited + VNPay refund API called |

> See `docs/08-guides/vnpay-integration.md` for full sandbox test cards and integration guide.
