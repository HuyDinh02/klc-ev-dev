import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

const OCPP_URL = __ENV.OCPP_URL || 'wss://api.ev.odcall.com/ocpp';

const wsConnections = new Counter('ws_connections');
const wsMessages = new Counter('ws_messages_sent');
const wsResponseTime = new Trend('ws_response_time');

export const options = {
  scenarios: {
    chargers: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 10 },   // 10 chargers
        { duration: '2m', target: 50 },   // 50 chargers
        { duration: '3m', target: 50 },   // Hold
        { duration: '1m', target: 100 },  // 100 chargers
        { duration: '2m', target: 100 },  // Hold
        { duration: '1m', target: 0 },    // Ramp down
      ],
    },
  },
  thresholds: {
    ws_response_time: ['p(95)<5000'],
    ws_connections: ['count>0'],
  },
};

function generateUniqueId() {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

function ocppCall(messageId, action, payload) {
  return JSON.stringify([2, messageId, action, payload]);
}

export default function () {
  const chargerId = `LOAD-CP-${__VU}-${__ITER}`;
  const url = `${OCPP_URL}/${chargerId}`;

  const res = ws.connect(url, { headers: { 'Sec-WebSocket-Protocol': 'ocpp1.6' } }, function (socket) {
    wsConnections.add(1);

    socket.on('open', function () {
      // Send BootNotification
      const bootId = generateUniqueId();
      const bootStart = Date.now();
      socket.send(ocppCall(bootId, 'BootNotification', {
        chargePointVendor: 'LoadTest',
        chargePointModel: 'K6-Sim',
        firmwareVersion: '1.0.0',
        chargePointSerialNumber: chargerId,
      }));
      wsMessages.add(1);

      socket.on('message', function (msg) {
        const elapsed = Date.now() - bootStart;
        wsResponseTime.add(elapsed);

        try {
          const parsed = JSON.parse(msg);
          if (parsed[0] === 3) {
            // CallResult -- acknowledged
          }
        } catch (e) {
          // ignore parse errors
        }
      });

      // Send heartbeats every 30s
      const heartbeatInterval = setInterval(() => {
        const hbId = generateUniqueId();
        socket.send(ocppCall(hbId, 'Heartbeat', {}));
        wsMessages.add(1);
      }, 30000);

      // Send StatusNotification every 60s
      const statusInterval = setInterval(() => {
        const stId = generateUniqueId();
        socket.send(ocppCall(stId, 'StatusNotification', {
          connectorId: 1,
          errorCode: 'NoError',
          status: 'Available',
          timestamp: new Date().toISOString(),
        }));
        wsMessages.add(1);
      }, 60000);

      // Stay connected for 2-5 minutes
      const connectionDuration = (Math.random() * 180 + 120) * 1000;
      sleep(connectionDuration / 1000);

      clearInterval(heartbeatInterval);
      clearInterval(statusInterval);
    });

    socket.on('error', function (e) {
      console.error(`WebSocket error for ${chargerId}: ${e.error()}`);
    });

    socket.setTimeout(function () {
      socket.close();
    }, 300000); // 5min max
  });

  check(res, { 'ws connected': (r) => r && r.status === 101 });
}
