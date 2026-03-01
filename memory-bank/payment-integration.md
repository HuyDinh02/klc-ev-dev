# Payment Integration

## Gateways
| Provider | Type | Notes |
|----------|------|-------|
| ZaloPay | Mobile payment (VNG) | REST API, popular in Vietnam |
| MoMo | E-wallet | REST API, largest Vietnam e-wallet |
| OnePay | Payment gateway | Multi-channel, card + bank transfer |

## Flow
```
Session ends → Calculate cost (tariff)
→ Create PaymentTransaction (Pending)
→ App shows payment screen → user selects method
→ Call gateway API → redirect/SDK flow
→ Gateway processes → callback webhook to CSMS
→ Success → update transaction, generate Invoice → e-invoice (MISA/Viettel/VNPT)
→ Failure → notify user, allow retry
```

## E-Invoice Providers
| Provider | Notes |
|----------|-------|
| MISA | Vietnamese accounting/e-invoice |
| Viettel eInvoice | Telecom provider's e-invoice service |
| VNPT eInvoice | State telecom e-invoice service |

## Rules
- PCI-DSS compliance required
- No raw card data stored (token-based)
- No duplicate payments per session
- All transactions logged for audit
- Currency: VNĐ (dấu chấm phân cách)
- Async callback handling (webhooks)
