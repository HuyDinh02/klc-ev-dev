# MOD-015: E-Invoice Integration

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Integrates with Vietnamese e-invoice providers (MISA, Viettel, VNPT) to automatically generate legal e-invoices after successful payment. Ensures compliance with Vietnamese tax regulations.

## 2. Actors
| Actor | Role |
|-------|------|
| System | Auto-generate e-invoice after payment |
| EV Driver | View and download invoices |
| Finance | Monitor invoice generation, resolve issues |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-015-01 | Auto-generate e-invoice after successful payment via configured provider | Must |
| FR-015-02 | Include session details: station, energy, duration, tariff, tax breakdown | Must |
| FR-015-03 | Send invoice to user via email and in-app | Must |
| FR-015-04 | Support multiple e-invoice providers: MISA, Viettel, VNPT | Must |
| FR-015-05 | Store invoice records with external provider reference | Must |
| FR-015-06 | Finance team can view and manage invoice statuses | Should |
| FR-015-07 | Handle invoice generation failure with retry mechanism | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-015-01 | E-invoice generated only after successful payment confirmation |
| BR-015-02 | Invoice must comply with Vietnamese tax regulation format |
| BR-015-03 | Failed invoice generation retried up to 3 times before alerting Finance |
| BR-015-04 | One invoice per payment transaction (no duplicates) |
| BR-015-05 | Invoice data: buyer info, seller info, items, tax, total amount |

## 5. Data Model
### EInvoice (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| InvoiceId | Guid | Yes | FK to Invoice (MOD-008) |
| Provider | EInvoiceProvider (enum) | Yes | MISA, Viettel, VNPT |
| ExternalInvoiceId | string | No | Provider's invoice ID |
| Status | EInvoiceStatus (enum) | Yes | Pending, Issued, Failed |
| IssuedAt | DateTime? | No | When provider issued |
| RetryCount | int | Yes | Number of retry attempts |
| ErrorMessage | string? | No | Last error if failed |

### EInvoiceProvider Enum
`MISA = 1, Viettel = 2, VNPT = 3`

### EInvoiceStatus Enum
`Pending = 0, Issued = 1, Failed = 2`

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/invoices/{id}/e-invoice | Get e-invoice details | Driver, Finance |
| POST | /api/v1/e-invoices/{id}/retry | Retry failed e-invoice | Finance |
| GET | /api/v1/e-invoices | List e-invoices (admin view) | Finance |

## 7. E-Invoice Flow
```
1. Payment successful (MOD-008)
2. System creates EInvoice record (Pending)
3. System calls e-invoice provider API (MISA/Viettel/VNPT)
4. Success → status = Issued, store external ID
5. Failure → retry up to 3 times
6. Still failing → alert Finance team, status = Failed
7. Invoice accessible in app and via email
```

## 8. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_015_001 | Invoice not found | 404 |
| MOD_015_002 | E-invoice provider error | 502 |
| MOD_015_003 | Max retry attempts exceeded | 500 (alert Finance) |

## 9. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-015-01 | Payment success → e-invoice generated | Invoice issued via provider |
| TC-015-02 | Provider API fails | Retry up to 3 times |
| TC-015-03 | Max retries exceeded | Finance alerted, status = Failed |
| TC-015-04 | View e-invoice in app | Correct invoice data displayed |
