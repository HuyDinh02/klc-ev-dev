import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Configuration
const ADMIN_API_URL = __ENV.ADMIN_API_URL || 'https://api.ev.odcall.com';
const CLIENT_ID = __ENV.CLIENT_ID || 'KLC_Api';
const CLIENT_SECRET = __ENV.CLIENT_SECRET || '';
const ADMIN_USER = __ENV.ADMIN_USER || 'admin';
const ADMIN_PASS = __ENV.ADMIN_PASS || 'Admin@123';

// Custom metrics
const authSuccess = new Rate('auth_success');
const apiErrors = new Rate('api_errors');
const operatorLatency = new Trend('operator_api_duration');
const fleetLatency = new Trend('fleet_api_duration');
const powerSharingLatency = new Trend('power_sharing_api_duration');

export const options = {
  scenarios: {
    // Phase 2 API smoke test
    phase2_smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      exec: 'phase2SmokeTest',
      tags: { scenario: 'phase2_smoke' },
    },
    // Phase 2 API load test
    phase2_load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '2m', target: 20 },
        { duration: '1m', target: 0 },
      ],
      exec: 'phase2LoadTest',
      startTime: '35s',
      tags: { scenario: 'phase2_load' },
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
    auth_success: ['rate>0.95'],
    operator_api_duration: ['p(95)<1500'],
    fleet_api_duration: ['p(95)<1500'],
    power_sharing_api_duration: ['p(95)<1500'],
  },
};

function getAdminToken() {
  const res = http.post(`${ADMIN_API_URL}/connect/token`, {
    grant_type: 'password',
    client_id: CLIENT_ID,
    client_secret: CLIENT_SECRET,
    username: ADMIN_USER,
    password: ADMIN_PASS,
    scope: 'KLC',
  }, { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } });

  const success = res.status === 200;
  authSuccess.add(success);
  if (success) {
    return JSON.parse(res.body).access_token;
  }
  return null;
}

function authHeaders(token) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };
}

// Smoke test: verify all Phase 2 endpoints respond
export function phase2SmokeTest() {
  const token = getAdminToken();
  if (!token) {
    apiErrors.add(true);
    return;
  }
  const headers = authHeaders(token);

  group('Operators API', () => {
    const res = http.get(`${ADMIN_API_URL}/api/v1/operators?pageSize=5`, headers);
    check(res, { 'operators list 200': (r) => r.status === 200 });
    operatorLatency.add(res.timings.duration);
    apiErrors.add(res.status !== 200);
  });

  group('Fleets API', () => {
    const res = http.get(`${ADMIN_API_URL}/api/v1/fleets?pageSize=5`, headers);
    check(res, { 'fleets list 200': (r) => r.status === 200 });
    fleetLatency.add(res.timings.duration);
    apiErrors.add(res.status !== 200);
  });

  group('Power Sharing API', () => {
    const res = http.get(`${ADMIN_API_URL}/api/v1/power-sharing?pageSize=5`, headers);
    check(res, { 'power sharing list 200': (r) => r.status === 200 });
    powerSharingLatency.add(res.timings.duration);
    apiErrors.add(res.status !== 200);
  });

  sleep(1);
}

// Load test: simulate concurrent admin users browsing Phase 2 pages
export function phase2LoadTest() {
  const token = getAdminToken();
  if (!token) {
    apiErrors.add(true);
    return;
  }
  const headers = authHeaders(token);

  group('Operators CRUD flow', () => {
    // List operators
    const listRes = http.get(`${ADMIN_API_URL}/api/v1/operators?pageSize=20`, headers);
    check(listRes, { 'operators list OK': (r) => r.status === 200 });
    operatorLatency.add(listRes.timings.duration);

    // Search operators
    const searchRes = http.get(`${ADMIN_API_URL}/api/v1/operators?search=test&pageSize=10`, headers);
    check(searchRes, { 'operators search OK': (r) => r.status === 200 });
    operatorLatency.add(searchRes.timings.duration);
  });

  group('Fleets CRUD flow', () => {
    // List fleets
    const listRes = http.get(`${ADMIN_API_URL}/api/v1/fleets?pageSize=20`, headers);
    check(listRes, { 'fleets list OK': (r) => r.status === 200 });
    fleetLatency.add(listRes.timings.duration);

    // Search fleets
    const searchRes = http.get(`${ADMIN_API_URL}/api/v1/fleets?search=test&pageSize=10`, headers);
    check(searchRes, { 'fleets search OK': (r) => r.status === 200 });
    fleetLatency.add(searchRes.timings.duration);

    // Parse fleet list for detail request
    if (listRes.status === 200) {
      try {
        const body = JSON.parse(listRes.body);
        const items = body.items || body.data?.items || [];
        if (items.length > 0) {
          const detailRes = http.get(`${ADMIN_API_URL}/api/v1/fleets/${items[0].id}`, headers);
          check(detailRes, { 'fleet detail OK': (r) => r.status === 200 });
          fleetLatency.add(detailRes.timings.duration);

          // Get analytics
          const analyticsRes = http.get(
            `${ADMIN_API_URL}/api/v1/fleets/${items[0].id}/analytics`,
            headers
          );
          check(analyticsRes, { 'fleet analytics OK': (r) => r.status === 200 });
          fleetLatency.add(analyticsRes.timings.duration);
        }
      } catch (_) { /* ignore parse errors */ }
    }
  });

  group('Power Sharing flow', () => {
    // List groups
    const listRes = http.get(`${ADMIN_API_URL}/api/v1/power-sharing?pageSize=20`, headers);
    check(listRes, { 'power sharing list OK': (r) => r.status === 200 });
    powerSharingLatency.add(listRes.timings.duration);

    // Filter by mode
    const filterRes = http.get(
      `${ADMIN_API_URL}/api/v1/power-sharing?mode=0&pageSize=10`,
      headers
    );
    check(filterRes, { 'power sharing filter OK': (r) => r.status === 200 });
    powerSharingLatency.add(filterRes.timings.duration);
  });

  sleep(Math.random() * 2 + 1); // 1-3s think time
}
