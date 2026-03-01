# OCPP 1.6J Test Scenarios

> Status: APPROVED | Last Updated: 2026-03-01 | Protocol: OCPP 1.6J (JSON over WebSocket)

## Executive Summary
Comprehensive test scenarios for OCPP 1.6J protocol integration. Covers connection lifecycle, charging transactions, real-time messaging, error handling, and smart charging profiles. Each scenario includes step-by-step message flow, expected results, and error conditions.

---

## 1. Boot & Connection Scenarios

### TC-OCPP-01: BootNotification - Accepted Response

**Priority**: Critical
**Modules**: MOD-001 (Station), MOD-006 (OCPP)

**Preconditions**:
- Station ST-001 registered in database (Status = Offline)
- WebSocket server ready at ws://localhost:5000/ocpp

**Test Steps**:
1. Charger initiates WebSocket connection: `ws://localhost:5000/ocpp/ST-001`
2. Charger sends BootNotification:
```json
[2, "msg-001", "BootNotification", {
  "chargePointModel": "Tesla-Wallbox",
  "chargePointSerialNumber": "SN-12345",
  "chargePointVendor": "Tesla",
  "firmwareVersion": "2.1.0",
  "iccid": "89001012812000074500",
  "imsi": "310150123456789",
  "meterSerialNumber": "METER-001"
}]
```
3. CSMS processes BootNotification
4. CSMS responds:
```json
[3, "msg-001", {
  "currentTime": "2026-03-01T10:30:00Z",
  "heartbeatInterval": 300,
  "status": "Accepted"
}]
```
5. Station status changes to Available (if no errors)

**Expected Results**:
- WebSocket connection remains open
- OcppConnection record created in Redis: `{"ChargePointId": "ST-001", "Status": "Connected", "LastHeartbeat": <timestamp>}`
- ChargingStation.Status = Available
- FirmwareVersion updated to "2.1.0"
- OCPP message logged in OcppMessageLog table

**Error Handling**:
- If charger not registered: Connection rejected with error MOD_006_001
- If malformed JSON: OCPP CALL_ERROR response with code "FormationViolation"

**Metrics**:
- Connection setup time: < 100ms
- Message processing latency: < 50ms

---

### TC-OCPP-02: BootNotification - Rejected/Pending Responses

**Priority**: High
**Related**: TC-OCPP-01

#### Scenario A: Rejected
**Test Steps**:
1. Unknown charger (ST-UNKNOWN) attempts connection
2. CSMS identifies charger not in database
3. CSMS responds:
```json
[3, "msg-002", {
  "currentTime": "2026-03-01T10:30:00Z",
  "heartbeatInterval": 300,
  "status": "Rejected"
}]
```

**Expected Results**:
- Station status remains Offline (not updated in DB)
- Log entry: "Boot rejected for unknown charger ST-UNKNOWN"
- No OcppConnection record created
- Error code MOD_006_001 logged

#### Scenario B: Pending (Charger waiting for approval)
**Test Steps**:
1. Charger with pending status attempts boot
2. CSMS responds:
```json
[3, "msg-003", {
  "currentTime": "2026-03-01T10:30:00Z",
  "heartbeatInterval": 600,
  "status": "Pending"
}]
```
3. Charger waits and retries after heartbeat interval

**Expected Results**:
- Station status = Offline (pending approval)
- Charger enters wait-and-retry loop
- Admin can approve/reject in portal

---

### TC-OCPP-03: Heartbeat Cycle

**Priority**: Critical
**Modules**: MOD-006 (OCPP), MOD-003 (Real-time Monitoring)

**Preconditions**:
- Station booted and connected (see TC-OCPP-01)
- Heartbeat interval = 300 seconds (from BootNotification)

**Test Steps**:
1. CSMS records LastHeartbeat = <boot_time>
2. After 300 seconds, charger sends Heartbeat:
```json
[2, "msg-004", "Heartbeat", {}]
```
3. CSMS processes and responds:
```json
[3, "msg-004", {
  "currentTime": "2026-03-01T10:35:00Z"
}]
```
4. CSMS updates OcppConnection.LastHeartbeat = <current_time>
5. Repeat every 300 seconds

**Expected Results**:
- Heartbeat received within ±5 seconds of interval
- LastHeartbeat timestamp updated in Redis
- Station remains Available (no timeout)
- No alerts generated

**Timeout Scenario**:
- No heartbeat for > 600 seconds (2× interval)
- CSMS marks station Offline
- Alert auto-generated: "Station ST-001 offline (no heartbeat)"
- Status notification sent to TechSupport

**Metrics**:
- Heartbeat latency: < 100ms
- Heartbeat loss detection: within 2 seconds of timeout

---

### TC-OCPP-04: Connection Lost & Reconnection with State Recovery

**Priority**: Critical
**Modules**: MOD-006 (OCPP), MOD-010 (Charging Session), MOD-003 (Monitoring)

**Preconditions**:
- Station connected with active charging session
- Session in Charging state with TransactionId = 42

**Test Steps**:

#### Part A: Connection Loss
1. Active session: StartTransaction accepted (txnId=42)
2. Network interruption: WebSocket disconnected abruptly
3. CSMS detects: Connection timeout (no heartbeat for 30 seconds)
4. Station status → Offline, Session status → Pending (temporary)
5. Alert generated: "Station ST-001 disconnected during active session"

#### Part B: Reconnection
1. Charger regains network, initiates new WebSocket connection
2. Sends BootNotification again (same charger ID)
3. CSMS responds with Accepted
4. Charger sends StatusNotification for connectors:
```json
[2, "msg-005", "StatusNotification", {
  "connectorId": 1,
  "status": "Charging",
  "timestamp": "2026-03-01T10:32:00Z"
}]
```
5. CSMS recovers session state from database:
   - Session ID still exists
   - OcppTransactionId = 42
   - Status transitions from Pending → Charging
6. Charger continues: sends MeterValues for transaction 42
7. User stops session normally

**Expected Results**:
- Session survives disconnection-reconnection cycle
- No duplicate session created
- MeterValues persisted with correct transaction ID
- Session finalized successfully
- Final cost calculated correctly

**Failure Mode** (if reconnection takes > 30 min):
- Session auto-finalized with "Interrupted" status
- Last known meter value used for billing
- User notified via push notification

**Metrics**:
- State recovery time: < 5 seconds
- Data consistency: No orphaned sessions
- Cost calculation accuracy: ±0.1 VNĐ

---

## 2. Authorization & RFID Scenarios

### TC-OCPP-05: Authorization - Valid RFID Token

**Priority**: High
**Modules**: MOD-006 (OCPP), MOD-011 (User Account)

**Preconditions**:
- Station available and connected
- User registered with RFID token "RFID-12345"

**Test Steps**:
1. User taps RFID card
2. Charger sends Authorize request:
```json
[2, "msg-006", "Authorize", {
  "idTag": "RFID-12345"
}]
```
3. CSMS looks up RFID in database (Linked to User via Vehicle)
4. CSMS responds:
```json
[3, "msg-006", {
  "idTagInfo": {
    "status": "Accepted",
    "expiryDate": "2026-12-31T23:59:59Z"
  }
}]
```

**Expected Results**:
- Authorization status = Accepted
- Charger proceeds to StartTransaction
- Session initiated for associated user

---

### TC-OCPP-06: Authorization - Invalid/Expired RFID

**Priority**: High
**Related**: TC-OCPP-05

#### Scenario A: Unknown RFID
**Test Steps**:
1. User taps unknown RFID: "RFID-UNKNOWN"
2. CSMS checks database → not found
3. CSMS responds with status: "Blocked"

**Expected Results**:
- Authorization rejected
- Error code displayed on charger screen
- Session not initiated

#### Scenario B: Expired RFID
**Test Steps**:
1. User taps RFID with expiryDate = "2025-12-31"
2. Current date = "2026-03-01" (after expiry)
3. CSMS responds: status = "Expired"

**Expected Results**:
- Authorization rejected
- Session not initiated
- User can renew subscription (MOD-011)

---

### TC-OCPP-07: Concurrent Authorization Requests

**Priority**: Medium
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Two users tap RFID simultaneously: RFID-USER1, RFID-USER2
2. Both connectors (1 and 2) send Authorize in same second
3. Both CSMS processes:
   - Msg-007: Authorize RFID-USER1 → Accepted
   - Msg-008: Authorize RFID-USER2 → Accepted (different connector)

**Expected Results**:
- Both authorizations processed independently
- No race conditions in database
- Both sessions can proceed

**Failure Mode** (same connector):
- Only first authorization accepted
- Second denied with "Blocked" (connector occupied)

---

## 3. Charging Session - Complete Flow

### TC-OCPP-08: Normal Charging Session (QR Scan to Stop)

**Priority**: Critical
**Modules**: MOD-010 (Session), MOD-006 (OCPP), MOD-004 (Metering), MOD-008 (Payment)

**Duration**: Simulates 30-minute charging session

**Test Steps**:

#### Step 1: RemoteStartTransaction (from mobile app)
```
User scans QR code → BFF validates → CSMS sends RemoteStartTransaction
```
1. User initiates via app: POST /api/v1/sessions/start
   - ConnectorId, VehicleId, UserId provided
2. BFF creates ChargingSession (Status=Initiated)
3. CSMS sends RemoteStartTransaction to charger:
```json
[2, "msg-009", "RemoteStartTransaction", {
  "connectorId": 1,
  "idTag": "RFID-USER1"
}]
```
4. Charger responds:
```json
[3, "msg-009", {
  "status": "Accepted"
}]
```

#### Step 2: Charger Starts Physical Charging
1. Charger connector plugged in → plug detection
2. Charger sends StartTransaction:
```json
[2, "msg-010", "StartTransaction", {
  "connectorId": 1,
  "idTag": "RFID-USER1",
  "meterStart": 15230,  // kWh
  "timestamp": "2026-03-01T10:35:00Z",
  "reservationId": null,
  "transactionId": 42
}]
```
3. CSMS responds:
```json
[3, "msg-010", {
  "idTagInfo": {
    "status": "Accepted"
  },
  "transactionId": 42
}]
```
4. ChargingSession.Status = Charging, OcppTransactionId = 42, MeterStart = 15230

#### Step 3: Periodic MeterValues (10-30 second intervals)
1. Charger sends MeterValues (3 measurements):
```json
[2, "msg-011", "MeterValues", {
  "connectorId": 1,
  "transactionId": 42,
  "meterValue": [
    {
      "timestamp": "2026-03-01T10:35:10Z",
      "sampledValue": [
        {
          "value": "15231.5",
          "measurand": "Energy.Active.Import.Register",
          "unit": "kWh"
        },
        {
          "value": "230",
          "measurand": "Voltage",
          "unit": "V"
        },
        {
          "value": "16",
          "measurand": "Current.Import",
          "unit": "A"
        },
        {
          "value": "3.68",
          "measurand": "Power.Active.Import",
          "unit": "kW"
        }
      ]
    }
  ]
}]
```
2. CSMS processes and stores MeterValue record
3. CSMS responds: status OK
4. Repeat every 20 seconds for 30 minutes total

#### Step 4: Final MeterValues & StopTransaction
1. User stops from app: POST /api/v1/sessions/{id}/stop
2. CSMS sends RemoteStopTransaction:
```json
[2, "msg-012", "RemoteStopTransaction", {
  "transactionId": 42
}]
```
3. Charger responds: Accepted, stops power delivery
4. After ~5 seconds, charger sends StopTransaction:
```json
[2, "msg-013", "StopTransaction", {
  "transactionId": 42,
  "idTag": "RFID-USER1",
  "meterStop": 15245,  // kWh
  "timestamp": "2026-03-01T11:05:00Z",
  "reason": "Remote",
  "transactionData": [
    {
      "timestamp": "2026-03-01T11:05:00Z",
      "sampledValue": [
        {
          "value": "15245",
          "measurand": "Energy.Active.Import.Register",
          "unit": "kWh"
        }
      ]
    }
  ]
}]
```
5. CSMS calculates:
   - TotalEnergyKwh = 15245 - 15230 = 15 kWh
   - Applies tariff (MOD-007): 15 × 3500 + tax = final cost
   - Creates PaymentTransaction (Pending)
6. CSMS responds: status OK
7. ChargingSession.Status = Completed, MeterEnd = 15245, TotalCost calculated

#### Step 5: Payment Processing
1. BFF triggers payment via gateway (MOD-008)
2. Payment succeeds → Invoice generated (MOD-015)
3. User receives push notification

**Expected Results**:
- Session duration: ~30 minutes
- Energy consumed: 15 kWh (correctly calculated)
- Cost: 52.500 VNĐ (15 × 3500 = 52.500, with tax)
- Session record complete with all meter values
- Charger connector status → Available
- Invoice sent to user

**Database Verification**:
```sql
SELECT * FROM ChargingSessions WHERE Id = '<sessionId>';
-- Status = Completed, TotalEnergyKwh = 15, TotalCost = 52500

SELECT COUNT(*) FROM MeterValues WHERE SessionId = '<sessionId>';
-- Count = ~90 (30 min × 1 value per 20 sec)

SELECT * FROM PaymentTransactions WHERE SessionId = '<sessionId>';
-- Status = Success, Amount = 52500
```

---

### TC-OCPP-09: Remote Start Transaction (Direct from Portal)

**Priority**: High
**Modules**: MOD-006 (OCPP), MOD-010 (Session)

**Preconditions**:
- Station connected and available
- Station pre-registered with RFID "ADMIN-CARD"

**Test Steps**:
1. Admin clicks "Start Charging" for station ST-001, connector 1
2. CSMS sends RemoteStartTransaction (without user context):
```json
[2, "msg-014", "RemoteStartTransaction", {
  "connectorId": 1,
  "idTag": "ADMIN-CARD"
}]
```
3. Charger responds: Accepted
4. No actual charging occurs (no physical connection)
5. Charger reports StatusNotification: Available (still no vehicle)

**Expected Results**:
- RemoteStartTransaction accepted
- Session created in Initiated state
- Can be used for testing/diagnostics
- If vehicle plugged in later: transitions to Charging

---

### TC-OCPP-10: Remote Stop Transaction

**Priority**: High
**Related**: TC-OCPP-08

**Test Steps**:
1. Active charging session (txnId=42)
2. Admin sends RemoteStopTransaction:
```json
[2, "msg-015", "RemoteStopTransaction", {
  "transactionId": 42
}]
```
3. Charger responds: Accepted
4. Charger stops power delivery within 10 seconds
5. Charger sends StopTransaction with reason="Remote"

**Expected Results**:
- Session stops cleanly
- Final meter values captured
- Cost calculated and payment triggered
- Charger available for next user

---

### TC-OCPP-11: Session Interrupted (Power Loss / Network Disconnect)

**Priority**: High
**Modules**: MOD-010 (Session), MOD-006 (OCPP)

**Test Steps**:
1. Active charging session (txnId=42, 10 kWh charged so far)
2. Power loss at charger → WebSocket disconnects
3. CSMS detects: No heartbeat for 60 seconds
4. Station marked Offline
5. Session auto-finalized (Status=Completed):
   - MeterEnd = last known meter value (10 kWh)
   - StopReason = "InterruptedByPowerLoss"
   - Cost calculated from available data
6. User notified: "Charging interrupted. Session saved."

**Expected Results**:
- Session finalized with last known meter data
- Cost calculated and payment triggered (10 kWh)
- No data loss
- Session recoverable if charger reconnects within 30 min

**Reconnection Scenario** (within 30 min):
- Charger reconnects and sends BootNotification
- CSMS detects: Active session with matching connector
- Charger can resume StopTransaction with updated meter value
- Session cost updated with actual final reading

---

## 4. Status Notifications & Transitions

### TC-OCPP-12: StatusNotification Full Transition Flow

**Priority**: High
**Modules**: MOD-002 (Connector), MOD-003 (Monitoring), MOD-005 (Fault)

**Preconditions**:
- Connector Status = Available

**Test Steps** (State Machine):

```
Available → Preparing → Charging → Finishing → Available
```

1. **Available → Preparing**
   ```json
   [2, "msg-016", "StatusNotification", {
     "connectorId": 1,
     "status": "Preparing",
     "timestamp": "2026-03-01T10:35:00Z"
   }]
   ```
   - User initiates charging (vehicle plugged in, authorization approved)
   - Connector state: Preparing (waiting for power)

2. **Preparing → Charging**
   ```json
   [2, "msg-017", "StatusNotification", {
     "connectorId": 1,
     "status": "Charging",
     "timestamp": "2026-03-01T10:35:05Z"
   }]
   ```
   - Power delivery started
   - Connector state: Charging

3. **Charging → Finishing**
   ```json
   [2, "msg-018", "StatusNotification", {
     "connectorId": 1,
     "status": "Finishing",
     "timestamp": "2026-03-01T11:05:00Z"
   }]
   ```
   - User stopped session / RemoteStop command received
   - Charger powering down

4. **Finishing → Available**
   ```json
   [2, "msg-019", "StatusNotification", {
     "connectorId": 1,
     "status": "Available",
     "timestamp": "2026-03-01T11:05:10Z"
   }]
   ```
   - Charging complete, connector ready for next user
   - StopTransaction already received

**Expected Results**:
- Each status transition logged in StatusChangeLog table
- Connector.Status updated in real-time
- Real-time dashboard updated via SignalR
- Timeline visible in station detail page

**Error Transitions**:
- Any state → Faulted (if error detected)
- Faulted → Available (after manual reset/fix)

---

### TC-OCPP-13: Status with Fault Code

**Priority**: High
**Modules**: MOD-005 (Fault), MOD-003 (Monitoring)

**Test Steps**:
1. Charger detects hardware fault during charging
2. Charger sends StatusNotification with error:
```json
[2, "msg-020", "StatusNotification", {
  "connectorId": 1,
  "status": "Faulted",
  "timestamp": "2026-03-01T11:05:30Z",
  "errorCode": "TemperatureMonitoring",
  "vendorId": "Tesla",
  "vendorErrorCode": "TEMP_HIGH_102"
}]
```
3. CSMS processes:
   - Connector.Status → Faulted
   - Creates Fault record: errorCode="TemperatureMonitoring"
   - Generates Alert: Type=Faulted, severity=High
   - Notifies TechSupport via push

**Expected Results**:
- Fault logged in database with error code
- Alert visible in monitoring dashboard
- Session finalized with "Faulted" reason
- User notified: "Charger fault detected. Session saved."

**Recovery**:
- TechSupport investigates via logs
- Charger manually reset or replaced
- StatusNotification: Available
- Fault status → Resolved

---

### TC-OCPP-14: Connector Availability Change

**Priority**: Medium
**Modules**: MOD-002 (Connector), MOD-003 (Monitoring)

**Test Steps**:
1. Connector 1 initially Available
2. Admin disables via portal (ChangeAvailability command)
3. CSMS sends:
```json
[2, "msg-021", "ChangeAvailability", {
  "connectorId": 1,
  "type": "Inoperative"
}]
```
4. Charger responds: Accepted
5. Charger sends StatusNotification (Unavailable):
```json
[2, "msg-022", "StatusNotification", {
  "connectorId": 1,
  "status": "Unavailable",
  "timestamp": "2026-03-01T11:06:00Z"
}]
```

**Expected Results**:
- Connector.IsEnabled = false
- Connector.Status = Unavailable
- Mobile app shows connector as disabled
- New sessions cannot start on this connector
- Dashboard updated

---

## 5. Energy Metering & Measurands

### TC-OCPP-15: MeterValues - Complete Measurands

**Priority**: High
**Modules**: MOD-004 (Energy Metering), MOD-006 (OCPP)

**Test Steps**:
1. During active charging session, charger sends comprehensive MeterValues:
```json
[2, "msg-023", "MeterValues", {
  "connectorId": 1,
  "transactionId": 42,
  "meterValue": [
    {
      "timestamp": "2026-03-01T10:35:10Z",
      "sampledValue": [
        {
          "value": "15231.5",
          "measurand": "Energy.Active.Import.Register",
          "unit": "kWh",
          "context": "Sample.Periodic"
        },
        {
          "value": "230.5",
          "measurand": "Voltage",
          "unit": "V",
          "context": "Sample.Periodic"
        },
        {
          "value": "16.2",
          "measurand": "Current.Import",
          "unit": "A",
          "context": "Sample.Periodic"
        },
        {
          "value": "3.75",
          "measurand": "Power.Active.Import",
          "unit": "kW",
          "context": "Sample.Periodic"
        },
        {
          "value": "85",
          "measurand": "SoC",
          "unit": "%",
          "context": "Sample.Periodic"
        }
      ]
    }
  ]
}]
```
2. CSMS parses all measurands
3. CSMS stores in MeterValue table with normalized units:
   - Energy.Active.Import.Register → EnergyKwh = 15231.5
   - Voltage → VoltageVolts = 230.5
   - Current.Import → CurrentAmps = 16.2
   - Power.Active.Import → PowerKw = 3.75
   - SoC → SocPercent = 85

**Expected Results**:
- All measurands persisted in database
- Energy value used for billing calculation
- Voltage/Current used for diagnostics
- SoC displayed to user in real-time

---

### TC-OCPP-16: MeterValues - Invalid Data Handling

**Priority**: High
**Related**: TC-OCPP-15

**Test Steps** (Various Invalid Scenarios):

#### Scenario A: Negative Energy Value
```json
{
  "value": "-100",  // Invalid: negative kWh
  "measurand": "Energy.Active.Import.Register"
}
```
- CSMS validates: Energy must be >= previous reading
- Logs: "Invalid meter value: negative energy"
- Stores: IsValid = false
- Excludes from billing

#### Scenario B: Out of Range Voltage
```json
{
  "value": "500",  // Invalid: should be ~220-240V
  "measurand": "Voltage"
}
```
- CSMS validates: Voltage 200-260V acceptable
- Logs: "Out of range voltage"
- Stores: IsValid = false
- Uses previous valid value for power calculation

#### Scenario C: Duplicate Timestamp
```json
// Same meter value received twice
"timestamp": "2026-03-01T10:35:10Z"  // Exact duplicate from prior message
```
- CSMS detects: Timestamp already exists for this connector+transaction
- Logs: "Duplicate meter value, skipping"
- Does not store duplicate

**Expected Results**:
- Invalid values logged but not used for billing
- Audit trail preserved
- Session not affected by bad readings
- Alert generated for repeated invalid data

---

### TC-OCPP-17: MeterValues with Transaction Correlation

**Priority**: High
**Modules**: MOD-004 (Metering), MOD-010 (Session)

**Test Steps**:
1. StartTransaction received: txnId=42
2. CSMS creates ChargingSession, stores OcppTransactionId=42
3. Charger sends multiple MeterValues, each with `"transactionId": 42`
4. At StopTransaction: txnId=42
5. CSMS queries: SELECT * FROM MeterValues WHERE SessionId = X ORDER BY Timestamp
6. Calculates: TotalEnergyKwh = LastReading - FirstReading

**Calculation Example**:
- StartTransaction: meterStart = 15230 kWh
- StopTransaction: meterStop = 15245 kWh
- Energy consumed = 15245 - 15230 = 15 kWh

**Expected Results**:
- All meter values linked to correct session
- Energy calculation accurate to 0.1 kWh
- No data loss or orphaned meter values

---

## 6. Remote Operations

### TC-OCPP-18: Reset - Soft Reset

**Priority**: Medium
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. No active charging session
2. Admin sends Reset (Soft):
```json
[2, "msg-024", "Reset", {
  "type": "Soft"
}]
```
3. Charger responds: Accepted
4. Charger restarts software (stays in same location, network may drop briefly)
5. After 10-30 seconds, charger sends BootNotification again

**Expected Results**:
- Charger remains registered
- Session state preserved in CSMS
- Connection re-established quickly
- No data loss

---

### TC-OCPP-19: Reset - Hard Reset

**Priority**: Medium
**Related**: TC-OCPP-18

**Test Steps**:
1. Admin sends Reset (Hard):
```json
[2, "msg-025", "Reset", {
  "type": "Hard"
}]
```
2. Charger performs factory reset (longer downtime expected)
3. After 30-60 seconds, charger reconnects and boots normally

**Expected Results**:
- Charger re-initializes
- Configuration may be reset (CSMS updates via ChangeConfiguration if needed)
- Sessions continue normally

---

### TC-OCPP-20: UnlockConnector

**Priority**: High
**Modules**: MOD-006 (OCPP), MOD-002 (Connector)

**Preconditions**:
- Vehicle physically stuck (unable to unlock mechanically)

**Test Steps**:
1. User/Admin requests unlock via portal: POST /api/v1/connectors/{id}/unlock
2. CSMS sends UnlockConnector:
```json
[2, "msg-026", "UnlockConnector", {
  "connectorId": 1
}]
```
3. Charger responds: Accepted
4. Charger unlocks connector (solenoid activates)

**Expected Results**:
- Vehicle can be disconnected
- Connector.Status updated to Available
- Session finalized if active
- Alert cleared

---

### TC-OCPP-21: ChangeConfiguration

**Priority**: Medium
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Admin changes charger config: Set heartbeat interval to 600 seconds
2. CSMS sends ChangeConfiguration:
```json
[2, "msg-027", "ChangeConfiguration", {
  "key": "HeartbeatInterval",
  "value": "600"
}]
```
3. Charger responds:
```json
[3, "msg-027", {
  "status": "Accepted"
}]
```
4. Configuration takes effect after charger restarts (or immediately for some configs)

**Expected Results**:
- Configuration updated on charger
- Subsequent heartbeats follow new interval
- CSMS adjusts timeout expectations

---

### TC-OCPP-22: GetConfiguration

**Priority**: Medium
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Admin requests charger config from portal
2. CSMS sends GetConfiguration:
```json
[2, "msg-028", "GetConfiguration", {
  "key": ["HeartbeatInterval", "NumberOfConnectors"]
}]
```
3. Charger responds:
```json
[3, "msg-028", {
  "configurationKey": [
    {
      "key": "HeartbeatInterval",
      "readonly": false,
      "value": "300"
    },
    {
      "key": "NumberOfConnectors",
      "readonly": true,
      "value": "2"
    }
  ],
  "unknownKey": []
}]
```

**Expected Results**:
- Configuration values retrieved
- Displayed in admin portal
- Readonly flags respected in UI

---

## 7. Smart Charging Profiles

### TC-OCPP-23: SetChargingProfile

**Priority**: Medium
**Modules**: MOD-006 (OCPP), MOD-007 (Tariff)

**Preconditions**:
- Vehicle supports charging profiles (contactless communication)

**Test Steps**:
1. Admin sets charging profile: Limit to 7 kW (for off-peak hours)
2. CSMS sends SetChargingProfile:
```json
[2, "msg-029", "SetChargingProfile", {
  "connectorId": 1,
  "csChargingProfiles": {
    "chargingProfileId": 101,
    "stackLevel": 0,
    "chargingProfilePurpose": "TxProfile",
    "chargingProfileKind": "Absolute",
    "validFrom": "2026-03-01T18:00:00Z",
    "validTo": "2026-03-02T06:00:00Z",
    "chargingSchedule": [
      {
        "duration": 12 * 3600,  // 12 hours
        "startSchedule": "2026-03-01T18:00:00Z",
        "chargingRateUnit": "A",
        "chargingSchedulePeriod": [
          {
            "startPeriod": 0,
            "limit": 30.4,  // 7 kW / 230V = 30.4A (approximately)
            "numberPhases": 1
          }
        ]
      }
    ]
  }
}]
```
3. Charger acknowledges and applies profile
4. Charger limits power to 7 kW during specified time window

**Expected Results**:
- Charging power limited as per profile
- Session continues with reduced power
- Energy still metered accurately
- Cost calculated based on actual consumption

---

### TC-OCPP-24: ClearChargingProfile

**Priority**: Medium
**Related**: TC-OCPP-23

**Test Steps**:
1. Admin clears charging profile:
```json
[2, "msg-030", "ClearChargingProfile", {
  "id": 101,
  "connectorId": 1
}]
```
2. Charger responds: Accepted
3. Profile removed, charger returns to normal power

**Expected Results**:
- Profile cleared
- Power limit removed
- Charger operates at full capacity

---

## 8. Diagnostics & Firmware

### TC-OCPP-25: DiagnosticsStatusNotification

**Priority**: Low
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Charger initiates diagnostics upload:
```json
[2, "msg-031", "DiagnosticsStatusNotification", {
  "status": "Uploading",
  "timestamp": "2026-03-01T11:10:00Z"
}]
```
2. CSMS logs: Diagnostics upload in progress
3. Charger completes upload:
```json
[2, "msg-032", "DiagnosticsStatusNotification", {
  "status": "Uploaded",
  "timestamp": "2026-03-01T11:15:00Z"
}]
```

**Expected Results**:
- Diagnostics status logged
- TechSupport notified
- Upload URL accessible for analysis

---

### TC-OCPP-26: UpdateFirmware & FirmwareStatusNotification

**Priority**: Low
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Admin initiates firmware update:
```json
[2, "msg-033", "UpdateFirmware", {
  "location": "https://example.com/firmware/v2.2.0.bin",
  "retrieveDate": "2026-03-01T12:00:00Z",
  "installDate": "2026-03-01T13:00:00Z"
}]
```
2. Charger acknowledges, schedules download
3. Charger downloads firmware
4. Charger sends FirmwareStatusNotification:
```json
[2, "msg-034", "FirmwareStatusNotification", {
  "status": "Downloaded",
  "timestamp": "2026-03-01T12:30:00Z"
}]
```
5. Charger installs at installDate:
```json
[2, "msg-035", "FirmwareStatusNotification", {
  "status": "InstallationSucceeded",
  "timestamp": "2026-03-01T13:05:00Z"
}]
```
6. Charger reboots and sends BootNotification with new firmware version

**Expected Results**:
- Firmware version updated in database
- FirmwareVersion = "2.2.0"
- Charger remains operational after update

---

## 9. Error Scenarios & Edge Cases

### TC-OCPP-27: Malformed JSON Message

**Priority**: High
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Charger sends malformed JSON:
```json
[2, "msg-036", "StartTransaction" {
  "connectorId": 1,  // Missing closing brace
  ...
}]
```
2. CSMS parser fails

**Expected Results**:
- CSMS responds with CALL_ERROR:
```json
[4, "msg-036", "FormationViolation", "Invalid JSON format"]
```
- Error logged
- Connection remains open
- Session not created

---

### TC-OCPP-28: Duplicate Message (Charger Retry)

**Priority**: High
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Charger sends StartTransaction:
```json
[2, "msg-010", "StartTransaction", { "transactionId": 42, ... }]
```
2. CSMS responds: Accepted
3. Charger doesn't receive response (network hiccup), retries with same messageId:
```json
[2, "msg-010", "StartTransaction", { "transactionId": 42, ... }]
```

**Expected Results**:
- CSMS recognizes duplicate (same messageId)
- Idempotent handler: Returns cached response without creating duplicate session
- OcppTransactionId = 42 used (not 43)
- No double-charging

---

### TC-OCPP-29: Out-of-Order Messages

**Priority**: Medium
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. Charger sends messages in order: StartTransaction (txnId=42), MeterValues, StopTransaction
2. Network reordering causes StopTransaction to arrive before MeterValues
3. CSMS receives:
   - StartTransaction (txnId=42)
   - StopTransaction (txnId=42) ← arrives first!
   - MeterValues (txnId=42) ← arrives second

**Expected Results**:
- CSMS buffers out-of-order messages
- Session waits for StartTransaction before processing MeterValues
- Session finalized with available meter data
- No data loss (critical)

---

### TC-OCPP-30: Message Timeout (Charger Not Responding)

**Priority**: High
**Modules**: MOD-006 (OCPP)

**Test Steps**:
1. CSMS sends RemoteStartTransaction with timeout = 30 seconds
2. Charger is offline / not responding
3. After 30 seconds, CSMS times out

**Expected Results**:
- Error logged: MOD_006_004 "Command timeout"
- Session status: Failed
- User notified: "Charger not responding"
- Charger marked for troubleshooting

---

### TC-OCPP-31: WebSocket Connection Lost During Transaction

**Priority**: Critical
**Related**: TC-OCPP-04

**Test Steps**:
1. Active session (txnId=42)
2. WebSocket abruptly closed (network interface down)
3. No graceful close message
4. CSMS detects: No heartbeat for 60 seconds

**Expected Results**:
- Session auto-finalized with last meter value
- Cost calculated and payment triggered
- Charger marked Offline
- User can retry if charger reconnects within 30 min

---

## 10. Protocol Compliance & Edge Cases

### TC-OCPP-32: Multiple Connectors - Simultaneous Sessions

**Priority**: High
**Modules**: MOD-002 (Connector), MOD-010 (Session)

**Preconditions**:
- Station with 2 connectors

**Test Steps**:
1. User 1 starts session on Connector 1: RemoteStartTransaction (connectorId=1)
2. User 2 starts session on Connector 2: RemoteStartTransaction (connectorId=2)
3. Both receive Accepted responses
4. Both chargers send StartTransaction (different transactionIds)

**Expected Results**:
- Two independent sessions created
- Two meter value streams (one per connector)
- Independent stop/cost calculation
- No cross-contamination

---

### TC-OCPP-33: Very Long Session (> 24 hours)

**Priority**: Low
**Modules**: MOD-010 (Session), MOD-004 (Metering)

**Test Steps**:
1. Charger starts charging at 10:00 on Mar 1
2. Charging continues > 24 hours (until 10:00 on Mar 2)
3. Charger sends MeterValues every 20 seconds throughout
4. Session stops normally

**Expected Results**:
- Session duration correctly calculated (> 24 hours)
- All meter values persisted (> 4,320 meter entries)
- Energy calculation accurate
- No database truncation or data loss
- Cost calculation correct (no date boundary issues)

---

### TC-OCPP-34: Rapid Session Start-Stop

**Priority**: Medium
**Modules**: MOD-010 (Session)

**Test Steps**:
1. User starts session at 10:00:00
2. User immediately stops at 10:00:05 (5 seconds)
3. No meaningful MeterValues sent

**Expected Results**:
- Session completed (Status=Completed)
- Energy = 0 kWh (or minimal meter increment)
- Cost = minimal (proportional to 5 seconds)
- Session record preserved for audit

---

## 11. Performance & Load Testing (k6 Scenarios)

### TC-OCPP-35: Concurrent Charger Connections (1000 chargers)

**Tool**: k6 WebSocket load testing

```javascript
import ws from 'k6/ws';
import { check, group } from 'k6';

export let options = {
    vus: 1000,          // 1000 virtual users (chargers)
    duration: '5m',
    thresholds: {
        'ws_connecting': ['p(95)<100'],      // Connection setup
        'ws_messages_sent': ['rate>100'],
        'ws_messages_received': ['rate>100'],
        'ws_session_duration': ['avg<1m'],
    }
};

export default function() {
    let stationCode = `station-${__VU}`;
    let ws = ws.connect(`ws://localhost:5000/ocpp/${stationCode}`, {});

    // Send BootNotification
    ws.send(JSON.stringify([2, "boot-" + __VU, "BootNotification", {...}]));

    // Send Heartbeats every 30 seconds
    ws.on('open', () => {
        ws.setInterval(() => {
            ws.send(JSON.stringify([2, "hb-" + __ITER, "Heartbeat", {}]));
        }, 30000);
    });

    ws.close();
}
```

**Expected Results**:
- 1000 concurrent connections established
- Message send rate > 100 msg/sec
- Message receive rate > 100 msg/sec
- Connection setup time p95 < 100ms
- Error rate < 0.1%

---

### TC-OCPP-36: MeterValues Throughput (10,000 msg/sec)

**Tool**: k6 with simulated meter data

```javascript
export let options = {
    vus: 500,
    rps: 10000,  // 10,000 requests per second
    duration: '2m',
    thresholds: {
        http_req_duration: ['p(95)<100'],
        http_req_failed: ['rate<0.001'],
    }
};

export default function() {
    // POST /api/v1/ocpp/meter-values (simulated)
    let payload = {
        transactionId: __VU,
        energy: Math.random() * 100,
        timestamp: new Date().toISOString()
    };
    http.post('http://localhost:5000/api/v1/ocpp/meter-values', JSON.stringify(payload));
}
```

**Expected Results**:
- 10,000 MeterValues/sec throughput
- p95 latency < 100ms
- DB insert latency acceptable
- Cache hit rate > 80%

---

## Test Execution Commands

```bash
# Run specific OCPP test scenario
dotnet test --filter "Category=OCPP&Name=TC-OCPP-08"

# Run all OCPP tests
dotnet test --filter "Category=OCPP"

# Run k6 load test
k6 run tests/performance/ocpp-concurrent-stations.js --vus 1000 --duration 5m

# Generate OCPP test report
dotnet test /p:CollectCoverage=true --logger "trx" --collect:"XPlat Code Coverage" -l "console;verbosity=detailed"
```

---

## Test Data & Fixtures

All OCPP test scenarios use predefined test data:

| Entity | Test Data | Notes |
|--------|-----------|-------|
| Station Code | ST-001, ST-002, ST-UNKNOWN | Predefined in DB |
| Charger ID | CHARGER-001, CHARGER-002 | Maps to station code |
| RFID Token | RFID-USER1, RFID-USER2, RFID-UNKNOWN | Predefined users |
| Transaction ID | 42, 43, 44, ... | Sequential for each session |
| Connector ID | 1, 2 | Physical connector numbers |

---

## Sign-Off

| Role | Sign-Off | Date |
|------|----------|------|
| OCPP Specialist | [ ] | _____ |
| Backend Lead | [ ] | _____ |
| QA Lead | [ ] | _____ |


