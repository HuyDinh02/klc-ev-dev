# Integration Requirements

> Status: APPROVED | Last Updated: 2026-03-01 | Source: BRD, Kickoff Document

---

## 1. OCPP Protocol Integration

| ID | Requirement | Details |
|----|-------------|---------|
| INT-OCPP-01 | Protocol Version | OCPP 1.6J (JSON over WebSocket) |
| INT-OCPP-02 | Connection | WebSocket persistent connection per charger |
| INT-OCPP-03 | CP → CSMS Messages | BootNotification, Heartbeat, StatusNotification, Authorize, StartTransaction, StopTransaction, MeterValues, DiagnosticsStatusNotification, FirmwareStatusNotification |
| INT-OCPP-04 | CSMS → CP Messages | RemoteStartTransaction, RemoteStopTransaction, Reset, ChangeConfiguration, GetConfiguration, UnlockConnector, UpdateFirmware, SetChargingProfile, ClearChargingProfile, TriggerMessage |
| INT-OCPP-05 | Reliability | Handle reconnection, offline queuing, message retry, idempotent handling |
| INT-OCPP-06 | Data Persistence | Transaction data persisted immediately for billing accuracy |

## 2. Payment Gateway Integration

| ID | Provider | Details |
|----|----------|---------|
| INT-PAY-01 | ZaloPay | Leading mobile payment by VNG Corporation. REST API integration. |
| INT-PAY-02 | MoMo | Vietnam's largest e-wallet. REST API integration. |
| INT-PAY-03 | OnePay | Multi-channel payment gateway. REST API integration. |
| INT-PAY-04 | Security | PCI-DSS compliance required. Secure token handling. |
| INT-PAY-05 | Scenarios | Handle success, failure, timeout, and retry. Callback URL for async notifications. |

## 3. E-Invoice Integration

| ID | Provider | Details |
|----|----------|---------|
| INT-INV-01 | MISA | Vietnamese e-invoice provider. API integration for auto-generation. |
| INT-INV-02 | Viettel eInvoice | Viettel's e-invoicing service. |
| INT-INV-03 | VNPT eInvoice | VNPT's e-invoicing service. |
| INT-INV-04 | Requirements | Auto-generate after successful payment. Include session details and tax breakdown. |

## 4. Maps & Location

| ID | Requirement | Details |
|----|-------------|---------|
| INT-MAP-01 | Google Maps API | Station locations display on map |
| INT-MAP-02 | GPS Integration | Find nearby stations based on user location |
| INT-MAP-03 | Navigation | Directions to selected station |

## 5. Push Notifications

| ID | Requirement | Details |
|----|-------------|---------|
| INT-NOTIF-01 | Firebase Cloud Messaging (FCM) | Push notifications to iOS and Android |
| INT-NOTIF-02 | Notification Types | Charge complete, billing alerts, fault alerts, promotional |
