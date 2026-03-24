#!/usr/bin/env python3
"""
KLC CSMS - OCPP 1.6J Charge Point Simulator
Simulates a complete charger lifecycle: Boot → Heartbeat → Authorize → Charge → Stop
Tests the full end-to-end OCPP flow.
"""

import asyncio
import json
import uuid
import time
import sys

try:
    import websockets
except ImportError:
    print("Installing websockets...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "websockets", "--break-system-packages", "-q"])
    import websockets

# ─── Configuration ────────────────────────────────────────────────────────────
CSMS_URL = "ws://localhost:8081/ocpp/{cp_id}"
CHARGE_POINT_ID = "KLC-HCM-001"
VENDOR = "Chargecore"
MODEL = "AC003+"
FIRMWARE = "1.2.0"
SERIAL = "CC-AC003-20260301"
ID_TAG = "TAG001"

# ─── OCPP Message Helpers ─────────────────────────────────────────────────────

def make_call(action, payload):
    unique_id = uuid.uuid4().hex[:36]
    return unique_id, json.dumps([2, unique_id, action, payload])

def parse_response(data):
    msg = json.loads(data)
    if msg[0] == 3:  # CALLRESULT
        return "CALLRESULT", msg[1], msg[2]
    elif msg[0] == 4:  # CALLERROR
        return "CALLERROR", msg[1], {"errorCode": msg[2], "errorDescription": msg[3]}
    elif msg[0] == 2:  # CALL (server-initiated)
        return "CALL", msg[1], {"action": msg[2], "payload": msg[3]}
    return "UNKNOWN", None, None

# ─── Test Utilities ───────────────────────────────────────────────────────────

class TestResult:
    def __init__(self):
        self.passed = 0
        self.failed = 0
        self.tests = []

    def check(self, name, condition, detail=""):
        status = "✅ PASS" if condition else "❌ FAIL"
        self.tests.append((name, condition, detail))
        if condition:
            self.passed += 1
        else:
            self.failed += 1
        print(f"  {status}: {name}" + (f" ({detail})" if detail else ""))

    def summary(self):
        total = self.passed + self.failed
        print(f"\n{'='*60}")
        print(f"  Test Results: {self.passed}/{total} passed, {self.failed} failed")
        print(f"{'='*60}")
        return self.failed == 0

results = TestResult()

# ─── OCPP Message Exchange ────────────────────────────────────────────────────

async def send_and_receive(ws, action, payload, handle_server_calls=True):
    """Send CALL and wait for CALLRESULT, handling any server-initiated CALLs in between."""
    uid, msg = make_call(action, payload)
    await ws.send(msg)

    while True:
        data = await asyncio.wait_for(ws.recv(), timeout=10)
        msg_type, msg_id, content = parse_response(data)

        if msg_type == "CALL":
            # Server-initiated command - respond with Accepted
            server_action = content["action"]
            server_payload = content["payload"]
            print(f"    ← Server CALL: {server_action}")

            # Auto-respond to known commands
            response_payload = {}
            if server_action == "ChangeConfiguration":
                response_payload = {"status": "Accepted"}
            elif server_action == "GetConfiguration":
                response_payload = {"configurationKey": [], "unknownKey": []}
            elif server_action == "RemoteStartTransaction":
                response_payload = {"status": "Accepted"}
            elif server_action == "RemoteStopTransaction":
                response_payload = {"status": "Accepted"}
            elif server_action == "Reset":
                response_payload = {"status": "Accepted"}
            elif server_action == "TriggerMessage":
                response_payload = {"status": "Accepted"}
            elif server_action == "UnlockConnector":
                response_payload = {"status": "Unlocked"}
            elif server_action == "ChangeAvailability":
                response_payload = {"status": "Accepted"}
            else:
                response_payload = {"status": "Accepted"}

            response = json.dumps([3, msg_id, response_payload])
            await ws.send(response)
            continue

        if msg_type == "CALLRESULT" and msg_id == uid:
            return content
        elif msg_type == "CALLERROR" and msg_id == uid:
            return content
        else:
            print(f"    ⚠ Unexpected message: {msg_type} id={msg_id}")

# ─── Test Scenarios ───────────────────────────────────────────────────────────

async def test_full_charging_cycle():
    """Test the complete OCPP flow end-to-end."""
    url = CSMS_URL.format(cp_id=CHARGE_POINT_ID)
    print(f"\n{'='*60}")
    print(f"  KLC CSMS - OCPP 1.6J End-to-End Test")
    print(f"  Charger: {CHARGE_POINT_ID} ({VENDOR} {MODEL})")
    print(f"  Server:  {url}")
    print(f"{'='*60}\n")

    try:
        ws = await websockets.connect(url, subprotocols=["ocpp1.6"], open_timeout=5)
    except Exception as e:
        print(f"❌ Cannot connect to CSMS: {e}")
        print("   Make sure the server is running: node test/ocpp-server/server.js")
        return False

    print(f"🔌 Connected to CSMS (protocol: {ws.subprotocol})\n")
    results.check("WebSocket connected", ws.protocol is not None)
    results.check("Sub-protocol negotiated", ws.subprotocol == "ocpp1.6", ws.subprotocol)

    try:
        # ─── Test 1: BootNotification ─────────────────────────────────
        print("\n── Test 1: BootNotification ──")
        resp = await send_and_receive(ws, "BootNotification", {
            "chargePointVendor": VENDOR,
            "chargePointModel": MODEL,
            "chargePointSerialNumber": SERIAL,
            "firmwareVersion": FIRMWARE,
        })
        results.check("BootNotification accepted", resp.get("status") == "Accepted", resp.get("status"))
        results.check("Server returned currentTime", "currentTime" in resp)
        results.check("Server returned interval", "interval" in resp, f"interval={resp.get('interval')}")

        # Wait for auto-configuration commands
        print("\n  Waiting for auto-configuration commands...")
        await asyncio.sleep(3)

        # Drain any remaining server commands
        try:
            while True:
                data = await asyncio.wait_for(ws.recv(), timeout=1)
                msg_type, msg_id, content = parse_response(data)
                if msg_type == "CALL":
                    action = content["action"]
                    print(f"    ← Server CALL: {action}")
                    await ws.send(json.dumps([3, msg_id, {"status": "Accepted"}]))
        except asyncio.TimeoutError:
            pass

        # ─── Test 2: StatusNotification (connectors available) ────────
        print("\n── Test 2: StatusNotification ──")
        resp = await send_and_receive(ws, "StatusNotification", {
            "connectorId": 0, "status": "Available", "errorCode": "NoError",
            "timestamp": now_iso(),
        })
        results.check("Station status accepted", resp == {} or isinstance(resp, dict))

        resp = await send_and_receive(ws, "StatusNotification", {
            "connectorId": 1, "status": "Available", "errorCode": "NoError",
            "timestamp": now_iso(),
        })
        results.check("Connector 1 Available accepted", isinstance(resp, dict))

        resp = await send_and_receive(ws, "StatusNotification", {
            "connectorId": 2, "status": "Available", "errorCode": "NoError",
            "timestamp": now_iso(),
        })
        results.check("Connector 2 Available accepted", isinstance(resp, dict))

        # ─── Test 3: Heartbeat ────────────────────────────────────────
        print("\n── Test 3: Heartbeat ──")
        resp = await send_and_receive(ws, "Heartbeat", {})
        results.check("Heartbeat returned currentTime", "currentTime" in resp, resp.get("currentTime"))

        # ─── Test 4: Authorize (valid tag) ────────────────────────────
        print("\n── Test 4: Authorize ──")
        resp = await send_and_receive(ws, "Authorize", {"idTag": ID_TAG})
        results.check("Authorize TAG001 accepted",
                       resp.get("idTagInfo", {}).get("status") == "Accepted")

        # Test invalid tag
        resp = await send_and_receive(ws, "Authorize", {"idTag": "UNKNOWN_TAG"})
        results.check("Authorize unknown tag rejected",
                       resp.get("idTagInfo", {}).get("status") == "Invalid")

        # Test blocked tag
        resp = await send_and_receive(ws, "Authorize", {"idTag": "BLOCKED"})
        results.check("Authorize blocked tag rejected",
                       resp.get("idTagInfo", {}).get("status") == "Blocked")

        # Test expired tag
        resp = await send_and_receive(ws, "Authorize", {"idTag": "EXPIRED"})
        results.check("Authorize expired tag rejected",
                       resp.get("idTagInfo", {}).get("status") == "Expired")

        # ─── Test 5: StartTransaction ─────────────────────────────────
        print("\n── Test 5: StartTransaction ──")
        # Connector goes to Preparing
        await send_and_receive(ws, "StatusNotification", {
            "connectorId": 1, "status": "Preparing", "errorCode": "NoError",
        })

        meter_start = 10000  # 10 kWh already on meter
        start_time = now_iso()
        resp = await send_and_receive(ws, "StartTransaction", {
            "connectorId": 1,
            "idTag": ID_TAG,
            "meterStart": meter_start,
            "timestamp": start_time,
        })
        transaction_id = resp.get("transactionId", 0)
        results.check("StartTransaction returned transactionId", transaction_id > 0, f"txn#{transaction_id}")
        results.check("StartTransaction idTag accepted",
                       resp.get("idTagInfo", {}).get("status") == "Accepted")

        # Connector goes to Charging
        await send_and_receive(ws, "StatusNotification", {
            "connectorId": 1, "status": "Charging", "errorCode": "NoError",
        })

        # ─── Test 6: MeterValues (simulate charging) ─────────────────
        print("\n── Test 6: MeterValues (simulating 3 readings) ──")
        energy_wh = meter_start
        for i in range(3):
            energy_wh += 500  # 0.5 kWh per reading
            power_w = 6800 + (i * 100)
            current_a = 29.5 + (i * 0.5)
            voltage_v = 230 + i

            resp = await send_and_receive(ws, "MeterValues", {
                "connectorId": 1,
                "transactionId": transaction_id,
                "meterValue": [{
                    "timestamp": now_iso(),
                    "sampledValue": [
                        {"value": str(energy_wh), "measurand": "Energy.Active.Import.Register", "unit": "Wh"},
                        {"value": str(power_w), "measurand": "Power.Active.Import", "unit": "W"},
                        {"value": str(current_a), "measurand": "Current.Import", "unit": "A"},
                        {"value": str(voltage_v), "measurand": "Voltage", "unit": "V"},
                    ]
                }]
            })
            results.check(f"MeterValues reading #{i+1} accepted", isinstance(resp, dict),
                           f"energy={energy_wh}Wh, power={power_w}W")
            await asyncio.sleep(0.5)

        # ─── Test 7: StopTransaction ──────────────────────────────────
        print("\n── Test 7: StopTransaction ──")
        meter_stop = energy_wh + 200  # Final reading
        stop_time = now_iso()
        resp = await send_and_receive(ws, "StopTransaction", {
            "transactionId": transaction_id,
            "meterStop": meter_stop,
            "timestamp": stop_time,
            "reason": "Local",
            "transactionData": [{
                "timestamp": stop_time,
                "sampledValue": [
                    {"value": str(meter_stop), "measurand": "Energy.Active.Import.Register", "unit": "Wh"},
                ]
            }]
        })
        results.check("StopTransaction accepted",
                       resp.get("idTagInfo", {}).get("status") == "Accepted")
        energy_consumed = meter_stop - meter_start
        results.check(f"Energy consumed: {energy_consumed/1000:.3f} kWh", energy_consumed > 0,
                       f"{energy_consumed}Wh")

        # Connector back to Available
        await send_and_receive(ws, "StatusNotification", {
            "connectorId": 1, "status": "Finishing", "errorCode": "NoError",
        })
        await send_and_receive(ws, "StatusNotification", {
            "connectorId": 1, "status": "Available", "errorCode": "NoError",
        })

        # ─── Test 8: Fault Detection ─────────────────────────────────
        print("\n── Test 8: Fault Detection ──")
        resp = await send_and_receive(ws, "StatusNotification", {
            "connectorId": 2,
            "status": "Faulted",
            "errorCode": "GroundFailure",
            "info": "Earth leakage detected",
            "timestamp": now_iso(),
        })
        results.check("Fault StatusNotification accepted", isinstance(resp, dict))

        # Clear fault
        await send_and_receive(ws, "StatusNotification", {
            "connectorId": 2, "status": "Available", "errorCode": "NoError",
        })

        # ─── Test 9: DataTransfer ─────────────────────────────────────
        print("\n── Test 9: DataTransfer ──")
        resp = await send_and_receive(ws, "DataTransfer", {
            "vendorId": "Chargecore",
            "messageId": "GetDiagnostics",
            "data": json.dumps({"info": "test data"}),
        })
        results.check("DataTransfer accepted", resp.get("status") == "Accepted")

        # ─── Test 10: Invalid idTag StartTransaction ──────────────────
        print("\n── Test 10: StartTransaction with invalid tag ──")
        resp = await send_and_receive(ws, "StartTransaction", {
            "connectorId": 2,
            "idTag": "BLOCKED",
            "meterStart": 0,
            "timestamp": now_iso(),
        })
        results.check("StartTransaction blocked tag rejected",
                       resp.get("idTagInfo", {}).get("status") == "Blocked" or
                       resp.get("transactionId", 1) == 0)

        # ─── Test 11: Final Heartbeat ─────────────────────────────────
        print("\n── Test 11: Final Heartbeat ──")
        resp = await send_and_receive(ws, "Heartbeat", {})
        results.check("Final heartbeat OK", "currentTime" in resp)

    except asyncio.TimeoutError:
        print("❌ Timeout waiting for server response")
        results.check("No timeout", False, "Response timeout")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        results.check("No exceptions", False, str(e))
    finally:
        await ws.close()
        print("\n🔌 Disconnected from CSMS")

    return results.summary()


# ─── Test: Unknown Charger ────────────────────────────────────────────────────

async def test_unknown_charger():
    """Test that unknown charger gets Pending status."""
    print("\n── Test 12: Unknown Charger BootNotification ──")
    url = CSMS_URL.format(cp_id="UNKNOWN-CHARGER-999")

    try:
        ws = await websockets.connect(url, subprotocols=["ocpp1.6"], open_timeout=5)
        resp = await send_and_receive(ws, "BootNotification", {
            "chargePointVendor": "Unknown",
            "chargePointModel": "TestModel",
        })
        results.check("Unknown charger gets Pending", resp.get("status") == "Pending", resp.get("status"))
        await ws.close()
    except Exception as e:
        results.check("Unknown charger test", False, str(e))


# ─── Utilities ────────────────────────────────────────────────────────────────

def now_iso():
    from datetime import datetime, timezone
    return datetime.now(timezone.utc).isoformat()


# ─── Main ─────────────────────────────────────────────────────────────────────

async def main():
    all_passed = await test_full_charging_cycle()
    await test_unknown_charger()

    print("\n")
    success = results.summary()
    print("\n")

    if success:
        print("🎉 All tests passed! OCPP 1.6J flow is working end-to-end.")
    else:
        print("⚠️  Some tests failed. Check the output above.")

    return success

if __name__ == "__main__":
    success = asyncio.run(main())
    sys.exit(0 if success else 1)
