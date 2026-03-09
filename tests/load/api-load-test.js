import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Configuration
const ADMIN_API_URL = __ENV.ADMIN_API_URL || 'https://api.ev.odcall.com';
const BFF_URL = __ENV.BFF_URL || 'https://bff.ev.odcall.com';
const CLIENT_ID = __ENV.CLIENT_ID || 'KLC_Api';
const CLIENT_SECRET = __ENV.CLIENT_SECRET || '';
const ADMIN_USER = __ENV.ADMIN_USER || 'admin';
const ADMIN_PASS = __ENV.ADMIN_PASS || 'Admin@123';

// Custom metrics
const authSuccess = new Rate('auth_success');
const apiErrors = new Rate('api_errors');

// Test scenarios
export const options = {
  scenarios: {
    // Smoke test: 1 user for 30s
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      exec: 'smokeTest',
      tags: { scenario: 'smoke' },
    },
    // Load test: ramp to 50 users over 2min, hold 3min, ramp down
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 50 },
        { duration: '3m', target: 50 },
        { duration: '1m', target: 0 },
      ],
      exec: 'loadTest',
      startTime: '35s',
      tags: { scenario: 'load' },
    },
    // Stress test: ramp to 200 users
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 100 },
        { duration: '3m', target: 200 },
        { duration: '2m', target: 0 },
      ],
      exec: 'loadTest',
      startTime: '7m',
      tags: { scenario: 'stress' },
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests under 2s
    http_req_failed: ['rate<0.05'],     // Less than 5% errors
    auth_success: ['rate>0.95'],         // 95% auth success
  },
};

// Get auth token
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

// Smoke test -- basic health checks
export function smokeTest() {
  group('Health Checks', () => {
    const adminHealth = http.get(`${ADMIN_API_URL}/health`);
    check(adminHealth, { 'Admin API healthy': (r) => r.status === 200 });

    const bffHealth = http.get(`${BFF_URL}/health`);
    check(bffHealth, { 'BFF healthy': (r) => r.status === 200 });
  });
  sleep(1);
}

// Load/stress test -- authenticated API operations
export function loadTest() {
  group('Health', () => {
    const res = http.get(`${ADMIN_API_URL}/health`);
    check(res, { 'health 200': (r) => r.status === 200 });
    apiErrors.add(res.status >= 400);
  });

  group('BFF Stations', () => {
    const res = http.get(`${BFF_URL}/api/v1/stations/nearby?latitude=21.0285&longitude=105.8542&radiusKm=10`);
    // May return 400/401 without auth, that's expected
    check(res, { 'stations responded': (r) => r.status < 500 });
    apiErrors.add(res.status >= 500);
  });

  group('Auth Flow', () => {
    const token = getAdminToken();
    if (token) {
      const headers = { Authorization: `Bearer ${token}`, Accept: 'application/json' };

      // Fetch stations
      const stations = http.get(`${ADMIN_API_URL}/api/app/charging-station?MaxResultCount=10`, { headers });
      check(stations, { 'stations 200': (r) => r.status === 200 });
      apiErrors.add(stations.status >= 400);

      // Fetch dashboard stats
      const dashboard = http.get(`${ADMIN_API_URL}/api/app/dashboard`, { headers });
      check(dashboard, { 'dashboard responded': (r) => r.status < 500 });
    }
  });

  sleep(Math.random() * 2 + 1); // Random 1-3s think time
}
