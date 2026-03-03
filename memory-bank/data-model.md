# Data Model

## Key Entities

### Aggregate Roots
- **ChargingStation**: StationCode, Name, Location (lat/lng), Address, Status, FirmwareVersion, TariffPlanId
- **ChargingSession**: UserId, VehicleId, ConnectorId, StationId, OcppTransactionId, Status, StartTime, EndTime, MeterStart, MeterEnd, TotalEnergyKwh, TotalCost, TariffPlanId
- **TariffPlan**: Name, BaseRatePerKwh, TaxRatePercent, EffectiveFrom, EffectiveTo
- **StationGroup**: Name, Description, Region

### Entities
- **Connector**: StationId, ConnectorNumber, ConnectorType (Type2/CCS/CHAdeMO), MaxPowerKw, Status, IsEnabled
- **Vehicle**: UserId, Make, Model, LicensePlate, BatteryCapacityKwh, PreferredConnectorType, IsActive
- **MeterValue**: SessionId, ConnectorId, Timestamp, EnergyKwh, CurrentAmps, VoltageVolts, PowerKw, SocPercent
- **Fault**: StationId, ConnectorId, ErrorCode, Status (Open/Investigating/Resolved), DetectedAt
- **PaymentTransaction**: SessionId, UserId, Gateway, Amount, Status, GatewayTransactionId
- **Invoice**: PaymentTransactionId, InvoiceNumber, EnergyKwh, BaseAmount, TaxAmount, TotalAmount
- **EInvoice**: InvoiceId, Provider (MISA/Viettel/VNPT), ExternalInvoiceId, Status
- **Notification**: UserId, Type, Title, Body, IsRead
- **Alert**: StationId, AlertType, Message, Status (New/Acknowledged/Resolved)
- **StatusChangeLog**: StationId, ConnectorId, PreviousStatus, NewStatus, Timestamp, Source
- **AppUser**: extends ABP IdentityUser + PhoneNumber, FullName, IsVerified
- **UserIdTag**: UserId, IdTag (unique), TagType (Rfid/Mobile/Virtual), FriendlyName, IsActive, ExpiryDate

## Key Relationships
```
Station 1──N Connector
Station N──1 StationGroup
Session N──1 Connector, User, Vehicle
Session 1──N MeterValue
Session 1──1 PaymentTransaction
Payment 1──1 Invoice 1──1 EInvoice
User 1──N Vehicle
User 1──N UserIdTag
Fault N──1 Station
```

## Base Classes
- Aggregate roots: `FullAuditedAggregateRoot<Guid>`
- Entities: `FullAuditedEntity<Guid>` or `Entity<Guid>`
- Soft delete: `ISoftDelete` where appropriate
