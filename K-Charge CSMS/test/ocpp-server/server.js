/**
 * KLC CSMS - OCPP 1.6J WebSocket Server (Test Implementation)
 *
 * This is a functional test server that mirrors the C# backend architecture.
 * It implements the full OCPP 1.6J message flow for end-to-end testing.
 *
 * Architecture matches: OcppWebSocketMiddleware → OcppMessageRouter → Handlers
 */

const http = require('http');
const { WebSocketServer, WebSocket } = require('ws');
const crypto = require('crypto');

// ─── Configuration ───────────────────────────────────────────────────────────
const PORT = 8081;
const HEARTBEAT_INTERVAL = 30; // seconds (shorter for testing)
const METER_SAMPLE_INTERVAL = 10; // seconds (shorter for testing)

// ─── In-Memory Database ──────────────────────────────────────────────────────
const db = {
    stations: new Map(),       // chargePointId → station object
    sessions: new Map(),       // transactionId → session object
    connectors: new Map(),     // `${chargePointId}:${connectorId}` → connector
    idTags: new Map(),         // tagId → idTag object
    meterValues: [],           // all meter value records
    faults: [],                // all fault records
    nextTransactionId: 1,
};

// Pre-register test stations
db.stations.set('KLC-HCM-001', {
    chargePointId: 'KLC-HCM-001',
    name: 'Test Station HCM 001',
    vendor: null, model: null, serialNumber: null, firmwareVersion: null,
    status: 'Unavailable', isOnline: false,
    lastBootTime: null, lastHeartbeat: null,
    latitude: 10.7769, longitude: 106.7009,
    address: '123 Nguyen Hue, Q1, HCM',
});
db.stations.set('KLC-HCM-002', {
    chargePointId: 'KLC-HCM-002',
    name: 'Test Station HCM 002',
    vendor: null, model: null, serialNumber: null, firmwareVersion: null,
    status: 'Unavailable', isOnline: false,
    lastBootTime: null, lastHeartbeat: null,
    latitude: 10.7800, longitude: 106.6950,
    address: '456 Le Loi, Q1, HCM',
});

// Pre-register connectors
for (const cpId of ['KLC-HCM-001', 'KLC-HCM-002']) {
    db.connectors.set(`${cpId}:1`, {
        chargePointId: cpId, connectorId: 1, type: 'Type2',
        status: 'Unavailable', errorCode: 'NoError', maxPowerKw: 7.0,
    });
    db.connectors.set(`${cpId}:2`, {
        chargePointId: cpId, connectorId: 2, type: 'Type2',
        status: 'Unavailable', errorCode: 'NoError', maxPowerKw: 7.0,
    });
}

// Pre-register test ID tags
db.idTags.set('TAG001', { tagId: 'TAG001', isBlocked: false, expiryDate: null, parentIdTag: null });
db.idTags.set('TAG002', { tagId: 'TAG002', isBlocked: false, expiryDate: null, parentIdTag: null });
db.idTags.set('BLOCKED', { tagId: 'BLOCKED', isBlocked: true, expiryDate: null, parentIdTag: null });
db.idTags.set('EXPIRED', { tagId: 'EXPIRED', isBlocked: false, expiryDate: '2020-01-01T00:00:00Z', parentIdTag: null });

// ─── Connection Manager ──────────────────────────────────────────────────────
const connections = new Map(); // chargePointId → { ws, connectedAt, lastHeartbeat, pendingRequests }

function getConnection(chargePointId) {
    return connections.get(chargePointId);
}

// ─── OCPP Message Types ──────────────────────────────────────────────────────
const CALL = 2;
const CALLRESULT = 3;
const CALLERROR = 4;

// ─── Message Handlers ────────────────────────────────────────────────────────

const handlers = {
    BootNotification(chargePointId, payload) {
        const station = db.stations.get(chargePointId);

        if (!station) {
            console.log(`  ⚠ Unknown charger ${chargePointId} → Pending`);
            return {
                status: 'Pending',
                currentTime: new Date().toISOString(),
                interval: HEARTBEAT_INTERVAL,
            };
        }

        // Update station info
        station.vendor = payload.chargePointVendor;
        station.model = payload.chargePointModel;
        station.serialNumber = payload.chargePointSerialNumber || null;
        station.firmwareVersion = payload.firmwareVersion || null;
        station.isOnline = true;
        station.lastBootTime = new Date().toISOString();
        station.lastHeartbeat = new Date().toISOString();
        station.status = 'Available';

        console.log(`  ✅ Boot accepted: ${chargePointId} (${station.vendor} ${station.model}, FW: ${station.firmwareVersion})`);

        // Schedule auto-configuration
        setTimeout(() => autoConfigureCharger(chargePointId), 1000);

        return {
            status: 'Accepted',
            currentTime: new Date().toISOString(),
            interval: HEARTBEAT_INTERVAL,
        };
    },

    Heartbeat(chargePointId, _payload) {
        const conn = connections.get(chargePointId);
        if (conn) conn.lastHeartbeat = new Date();

        const station = db.stations.get(chargePointId);
        if (station) station.lastHeartbeat = new Date().toISOString();

        console.log(`  💓 Heartbeat from ${chargePointId}`);
        return { currentTime: new Date().toISOString() };
    },

    StatusNotification(chargePointId, payload) {
        const { connectorId, status, errorCode, timestamp, info } = payload;

        if (connectorId === 0) {
            // Overall station status
            const station = db.stations.get(chargePointId);
            if (station) station.status = status;
            console.log(`  📊 Station ${chargePointId} status: ${status} (error: ${errorCode})`);
        } else {
            // Connector status
            const key = `${chargePointId}:${connectorId}`;
            const connector = db.connectors.get(key);
            if (connector) {
                connector.status = status;
                connector.errorCode = errorCode;
            }
            console.log(`  📊 Connector ${chargePointId}/${connectorId}: ${status} (error: ${errorCode})`);
        }

        // Log fault if error
        if (errorCode && errorCode !== 'NoError') {
            db.faults.push({
                chargePointId, connectorId, errorCode,
                info: info || null,
                timestamp: timestamp || new Date().toISOString(),
            });
            console.log(`  🔴 FAULT detected: ${chargePointId}/${connectorId} → ${errorCode}: ${info || 'N/A'}`);
        }

        return {}; // Empty response per OCPP spec
    },

    Authorize(chargePointId, payload) {
        const { idTag } = payload;
        const tag = db.idTags.get(idTag);

        let status;
        if (!tag) {
            status = 'Invalid';
            console.log(`  🔒 Authorize ${idTag}: Invalid (unknown tag)`);
        } else if (tag.isBlocked) {
            status = 'Blocked';
            console.log(`  🔒 Authorize ${idTag}: Blocked`);
        } else if (tag.expiryDate && new Date(tag.expiryDate) < new Date()) {
            status = 'Expired';
            console.log(`  🔒 Authorize ${idTag}: Expired`);
        } else {
            status = 'Accepted';
            console.log(`  🔓 Authorize ${idTag}: Accepted`);
        }

        const idTagInfo = { status };
        if (tag?.expiryDate) idTagInfo.expiryDate = tag.expiryDate;
        if (tag?.parentIdTag) idTagInfo.parentIdTag = tag.parentIdTag;

        return { idTagInfo };
    },

    StartTransaction(chargePointId, payload) {
        const { connectorId, idTag, meterStart, timestamp, reservationId } = payload;

        // Validate idTag
        const tag = db.idTags.get(idTag);
        if (!tag || tag.isBlocked) {
            console.log(`  ❌ StartTransaction rejected: invalid idTag ${idTag}`);
            return {
                transactionId: 0,
                idTagInfo: { status: tag?.isBlocked ? 'Blocked' : 'Invalid' },
            };
        }

        const transactionId = db.nextTransactionId++;
        const session = {
            id: crypto.randomUUID(),
            transactionId,
            chargePointId,
            connectorId,
            idTag,
            meterStartWh: meterStart,
            meterStopWh: null,
            startTimestamp: timestamp || new Date().toISOString(),
            stopTimestamp: null,
            status: 'Active',
            stopReason: null,
            energyConsumedWh: 0,
        };

        db.sessions.set(transactionId, session);

        // Update connector status
        const key = `${chargePointId}:${connectorId}`;
        const connector = db.connectors.get(key);
        if (connector) connector.status = 'Charging';

        console.log(`  ⚡ Transaction #${transactionId} started: ${chargePointId}/${connectorId}, tag=${idTag}, meter=${meterStart}Wh`);

        return {
            transactionId,
            idTagInfo: { status: 'Accepted' },
        };
    },

    StopTransaction(chargePointId, payload) {
        const { transactionId, meterStop, timestamp, reason, transactionData } = payload;

        const session = db.sessions.get(transactionId);
        if (!session) {
            console.log(`  ⚠ StopTransaction for unknown txn #${transactionId}`);
            return {};
        }

        session.meterStopWh = meterStop;
        session.stopTimestamp = timestamp || new Date().toISOString();
        session.status = 'Completed';
        session.stopReason = reason || 'Local';
        session.energyConsumedWh = meterStop - session.meterStartWh;

        const energyKwh = (session.energyConsumedWh / 1000).toFixed(3);
        const durationMs = new Date(session.stopTimestamp) - new Date(session.startTimestamp);
        const durationMin = (durationMs / 60000).toFixed(1);

        // Process transactionData meter values
        if (transactionData && Array.isArray(transactionData)) {
            for (const mv of transactionData) {
                if (mv.sampledValue) {
                    for (const sv of mv.sampledValue) {
                        db.meterValues.push({
                            transactionId,
                            chargePointId,
                            timestamp: mv.timestamp,
                            ...sv,
                        });
                    }
                }
            }
        }

        // Update connector status
        const key = `${chargePointId}:${session.connectorId}`;
        const connector = db.connectors.get(key);
        if (connector) connector.status = 'Available';

        console.log(`  🔋 Transaction #${transactionId} completed: ${energyKwh} kWh, ${durationMin} min, reason=${session.stopReason}`);

        return { idTagInfo: { status: 'Accepted' } };
    },

    MeterValues(chargePointId, payload) {
        const { connectorId, transactionId, meterValue } = payload;

        if (!meterValue || !Array.isArray(meterValue)) return {};

        for (const mv of meterValue) {
            const ts = mv.timestamp;
            const values = mv.sampledValue || [];

            for (const sv of values) {
                db.meterValues.push({
                    transactionId,
                    chargePointId,
                    connectorId,
                    timestamp: ts,
                    value: sv.value,
                    measurand: sv.measurand || 'Energy.Active.Import.Register',
                    unit: sv.unit || 'Wh',
                    context: sv.context || 'Sample.Periodic',
                });
            }

            // Pretty print
            const summary = values.map(v => {
                const m = (v.measurand || 'Energy').split('.').pop();
                return `${m}=${v.value}${v.unit || 'Wh'}`;
            }).join(', ');

            console.log(`  📊 MeterValues txn#${transactionId} @${connectorId}: ${summary}`);
        }

        return {};
    },

    DataTransfer(chargePointId, payload) {
        console.log(`  📦 DataTransfer from ${chargePointId}: vendor=${payload.vendorId}, msgId=${payload.messageId}`);
        return { status: 'Accepted' };
    },

    DiagnosticsStatusNotification(chargePointId, payload) {
        console.log(`  🔧 DiagnosticsStatus from ${chargePointId}: ${payload.status}`);
        return {};
    },

    FirmwareStatusNotification(chargePointId, payload) {
        console.log(`  📦 FirmwareStatus from ${chargePointId}: ${payload.status}`);
        return {};
    },
};

// ─── OCPP Command Dispatcher (CSMS → CP) ────────────────────────────────────

function sendCommand(chargePointId, action, payload, timeout = 30000) {
    return new Promise((resolve, reject) => {
        const conn = connections.get(chargePointId);
        if (!conn || conn.ws.readyState !== WebSocket.OPEN) {
            return reject(new Error(`Charge point ${chargePointId} not connected`));
        }

        const uniqueId = crypto.randomUUID().replace(/-/g, '').slice(0, 36);
        const message = JSON.stringify([CALL, uniqueId, action, payload]);

        const timer = setTimeout(() => {
            conn.pendingRequests.delete(uniqueId);
            reject(new Error(`Command ${action} to ${chargePointId} timed out`));
        }, timeout);

        conn.pendingRequests.set(uniqueId, { resolve, reject, timer });
        conn.ws.send(message);
        console.log(`  📤 Sent ${action} to ${chargePointId} (id=${uniqueId.slice(0, 8)}...)`);
    });
}

// ─── Auto-Configuration After Boot ──────────────────────────────────────────

async function autoConfigureCharger(chargePointId) {
    const configs = {
        HeartbeatInterval: String(HEARTBEAT_INTERVAL),
        MeterValueSampleInterval: String(METER_SAMPLE_INTERVAL),
        MeterValuesSampledData: 'Energy.Active.Import.Register,Power.Active.Import,Current.Import,Voltage,SoC',
        StopTxnSampledData: 'Energy.Active.Import.Register',
        ConnectionTimeOut: '60',
        StopTransactionOnEVSideDisconnect: 'true',
    };

    console.log(`\n  ⚙️  Auto-configuring ${chargePointId}...`);
    for (const [key, value] of Object.entries(configs)) {
        try {
            const result = await sendCommand(chargePointId, 'ChangeConfiguration', { key, value });
            const status = result?.status || 'Unknown';
            console.log(`    Config ${key}=${value} → ${status}`);
        } catch (err) {
            console.log(`    Config ${key} failed: ${err.message}`);
        }
        await new Promise(r => setTimeout(r, 100));
    }
    console.log(`  ⚙️  Auto-configuration complete for ${chargePointId}\n`);
}

// ─── Message Router ──────────────────────────────────────────────────────────

function routeMessage(chargePointId, message) {
    const messageType = message[0];

    switch (messageType) {
        case CALL: {
            const [, uniqueId, action, payload] = message;
            console.log(`\n← CALL from ${chargePointId}: ${action} (id=${uniqueId?.slice(0, 8)}...)`);

            const handler = handlers[action];
            if (!handler) {
                console.log(`  ❌ No handler for action: ${action}`);
                return [CALLERROR, uniqueId, 'NotImplemented', `Action '${action}' not supported`, {}];
            }

            try {
                const result = handler(chargePointId, payload || {});
                return [CALLRESULT, uniqueId, result];
            } catch (err) {
                console.error(`  ❌ Handler error for ${action}: ${err.message}`);
                return [CALLERROR, uniqueId, 'InternalError', err.message, {}];
            }
        }

        case CALLRESULT: {
            const [, uniqueId, payload] = message;
            const conn = connections.get(chargePointId);
            if (conn) {
                const pending = conn.pendingRequests.get(uniqueId);
                if (pending) {
                    clearTimeout(pending.timer);
                    conn.pendingRequests.delete(uniqueId);
                    pending.resolve(payload);
                    console.log(`\n← CALLRESULT from ${chargePointId} (id=${uniqueId?.slice(0, 8)}...)`);
                }
            }
            return null; // No response needed
        }

        case CALLERROR: {
            const [, uniqueId, errorCode, errorDesc] = message;
            const conn = connections.get(chargePointId);
            if (conn) {
                const pending = conn.pendingRequests.get(uniqueId);
                if (pending) {
                    clearTimeout(pending.timer);
                    conn.pendingRequests.delete(uniqueId);
                    pending.reject(new Error(`CALLERROR: ${errorCode} - ${errorDesc}`));
                    console.log(`\n← CALLERROR from ${chargePointId}: ${errorCode} - ${errorDesc}`);
                }
            }
            return null;
        }

        default:
            console.log(`  ⚠ Unknown message type: ${messageType}`);
            return null;
    }
}

// ─── REST API (basic, for admin commands) ────────────────────────────────────

function handleHttpRequest(req, res) {
    const url = new URL(req.url, `http://${req.headers.host}`);
    res.setHeader('Content-Type', 'application/json');

    // Health check
    if (url.pathname === '/health') {
        res.writeHead(200);
        return res.end(JSON.stringify({ status: 'healthy', connections: connections.size }));
    }

    // GET /api/stations - list all stations
    if (url.pathname === '/api/stations' && req.method === 'GET') {
        const stations = [...db.stations.values()].map(s => ({
            ...s,
            connectors: [...db.connectors.values()].filter(c => c.chargePointId === s.chargePointId),
            isConnected: connections.has(s.chargePointId),
        }));
        res.writeHead(200);
        return res.end(JSON.stringify(stations, null, 2));
    }

    // GET /api/sessions - list all sessions
    if (url.pathname === '/api/sessions' && req.method === 'GET') {
        res.writeHead(200);
        return res.end(JSON.stringify([...db.sessions.values()], null, 2));
    }

    // GET /api/meter-values - list recent meter values
    if (url.pathname === '/api/meter-values' && req.method === 'GET') {
        res.writeHead(200);
        return res.end(JSON.stringify(db.meterValues.slice(-50), null, 2));
    }

    // POST /api/commands/remote-start
    if (url.pathname === '/api/commands/remote-start' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', async () => {
            try {
                const { chargePointId, connectorId, idTag } = JSON.parse(body);
                const result = await sendCommand(chargePointId, 'RemoteStartTransaction', {
                    connectorId, idTag
                });
                res.writeHead(200);
                res.end(JSON.stringify({ status: result?.status || 'Unknown' }));
            } catch (err) {
                res.writeHead(500);
                res.end(JSON.stringify({ error: err.message }));
            }
        });
        return;
    }

    // POST /api/commands/remote-stop
    if (url.pathname === '/api/commands/remote-stop' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', async () => {
            try {
                const { transactionId } = JSON.parse(body);
                const result = await sendCommand(
                    // Find chargePointId from session
                    db.sessions.get(transactionId)?.chargePointId,
                    'RemoteStopTransaction', { transactionId }
                );
                res.writeHead(200);
                res.end(JSON.stringify({ status: result?.status || 'Unknown' }));
            } catch (err) {
                res.writeHead(500);
                res.end(JSON.stringify({ error: err.message }));
            }
        });
        return;
    }

    // POST /api/commands/reset
    if (url.pathname === '/api/commands/reset' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', async () => {
            try {
                const { chargePointId, type } = JSON.parse(body);
                const result = await sendCommand(chargePointId, 'Reset', { type: type || 'Soft' });
                res.writeHead(200);
                res.end(JSON.stringify({ status: result?.status || 'Unknown' }));
            } catch (err) {
                res.writeHead(500);
                res.end(JSON.stringify({ error: err.message }));
            }
        });
        return;
    }

    res.writeHead(404);
    res.end(JSON.stringify({ error: 'Not found' }));
}

// ─── WebSocket Server ────────────────────────────────────────────────────────

const httpServer = http.createServer(handleHttpRequest);

// Use noServer mode for full control over the upgrade handshake
const wss = new WebSocketServer({ noServer: true });

httpServer.on('upgrade', (req, socket, head) => {
    if (!req.url.startsWith('/ocpp/')) {
        socket.destroy();
        return;
    }
    wss.handleUpgrade(req, socket, head, (ws) => {
        wss.emit('connection', ws, req);
    });
});

wss.on('connection', (ws, req) => {
    // Extract chargePointId from URL: /ocpp/{chargePointId}
    const match = req.url.match(/\/ocpp\/([^/?]+)/);
    if (!match) {
        console.log('Connection rejected: no chargePointId in URL');
        ws.close(1008, 'Missing chargePointId');
        return;
    }

    const chargePointId = decodeURIComponent(match[1]);

    // Close existing connection if reconnecting
    if (connections.has(chargePointId)) {
        const old = connections.get(chargePointId);
        console.log(`\n🔄 ${chargePointId} reconnecting - closing old connection`);
        try { old.ws.close(); } catch {}
        connections.delete(chargePointId);
    }

    const connection = {
        ws,
        chargePointId,
        connectedAt: new Date(),
        lastHeartbeat: new Date(),
        pendingRequests: new Map(),
    };

    connections.set(chargePointId, connection);
    console.log(`\n🔌 ${chargePointId} connected (total: ${connections.size})`);

    ws.on('message', (data) => {
        let message;
        try {
            message = JSON.parse(data.toString());
        } catch (err) {
            console.log(`  ⚠ Malformed JSON from ${chargePointId}`);
            return;
        }

        const response = routeMessage(chargePointId, message);
        if (response) {
            ws.send(JSON.stringify(response));
        }
    });

    ws.on('close', (code, reason) => {
        connections.delete(chargePointId);
        const station = db.stations.get(chargePointId);
        if (station) {
            station.isOnline = false;
            station.status = 'Unavailable';
        }
        console.log(`\n🔌 ${chargePointId} disconnected (code=${code}, total: ${connections.size})`);
    });

    ws.on('error', (err) => {
        console.error(`  ⚠ WebSocket error for ${chargePointId}: ${err.message}`);
    });
});

// ─── Start Server ────────────────────────────────────────────────────────────

httpServer.listen(PORT, () => {
    console.log('');
    console.log('╔═══════════════════════════════════════════════════════════╗');
    console.log('║           KLC CSMS - OCPP 1.6J Test Server               ║');
    console.log('╠═══════════════════════════════════════════════════════════╣');
    console.log(`║  WebSocket: ws://localhost:${PORT}/ocpp/{chargePointId}      ║`);
    console.log(`║  REST API:  http://localhost:${PORT}/api/stations            ║`);
    console.log(`║  Health:    http://localhost:${PORT}/health                  ║`);
    console.log('╠═══════════════════════════════════════════════════════════╣');
    console.log('║  Pre-registered stations: KLC-HCM-001, KLC-HCM-002      ║');
    console.log('║  Test ID tags: TAG001, TAG002, BLOCKED, EXPIRED          ║');
    console.log('║  Sub-protocol: ocpp1.6                                   ║');
    console.log('╚═══════════════════════════════════════════════════════════╝');
    console.log('');
    console.log('Waiting for charge point connections...\n');
});
