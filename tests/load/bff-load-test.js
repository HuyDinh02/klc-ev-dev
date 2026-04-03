/**
 * K6 Load Test — KLC EV Charging BFF API
 * Target: 500 concurrent users, 10 stations
 *
 * Run: k6 run tests/load/bff-load-test.js
 * With options: k6 run --vus 100 --duration 5m tests/load/bff-load-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// --- Configuration ---
const BFF_URL = __ENV.BFF_URL || 'https://bff.ev.odcall.com/api/v1';
const TEST_PHONE_PREFIX = '090123400'; // Seed users: 0901234001-0901234010
const TEST_PASSWORD = 'Admin@123';

// --- Custom metrics ---
const loginSuccess = new Rate('login_success');
const stationLookupDuration = new Trend('station_lookup_duration');
const sessionStartDuration = new Trend('session_start_duration');
const walletBalanceDuration = new Trend('wallet_balance_duration');

// --- Test stages ---
export const options = {
  stages: [
    { duration: '30s', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 200 },    // Ramp to 200
    { duration: '3m', target: 500 },    // Peak: 500 concurrent
    { duration: '2m', target: 500 },    // Hold at 500
    { duration: '1m', target: 0 },      // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],   // 95% under 2s
    http_req_failed: ['rate<0.05'],      // <5% error rate
    login_success: ['rate>0.95'],        // >95% login success
    station_lookup_duration: ['p(95)<500'], // Station lookup <500ms
  },
};

// --- Pre-authenticate all test users in setup() ---
export function setup() {
  const tokens = {};
  for (let i = 1; i <= 10; i++) {
    const phone = `${TEST_PHONE_PREFIX}${i}`;
    const res = http.post(`${BFF_URL}/auth/login`, JSON.stringify({
      phoneNumber: phone,
      password: TEST_PASSWORD,
    }), { headers: { 'Content-Type': 'application/json' } });

    try {
      const body = res.json();
      if (res.status === 200 && body && body.accessToken) {
        tokens[i] = body.accessToken;
        loginSuccess.add(true);
      } else {
        loginSuccess.add(false);
        console.error(`Login failed for ${phone}: ${res.status}`);
      }
    } catch (e) {
      loginSuccess.add(false);
      console.error(`Login parse error for ${phone}`);
    }
    sleep(0.5); // Stagger logins
  }
  return { tokens };
}

// --- Main test scenario ---
export default function (data) {
  const userNum = (__VU % 10) + 1;
  const token = data.tokens[userNum];
  if (!token) {
    sleep(1);
    return;
  }

  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // 1. Nearby stations
  group('Station Discovery', () => {
    const res = http.get(
      `${BFF_URL}/stations/nearby?lat=10.768&lon=106.696&radius=10&limit=10`
    );
    stationLookupDuration.add(res.timings.duration);
    check(res, {
      'nearby stations 200': (r) => r.status === 200,
      'has stations': (r) => {
        try { return r.json('data').length > 0; } catch { return false; }
      },
    });
  });

  sleep(0.5);

  // 2. Station detail
  group('Station Detail', () => {
    const res = http.get(`${BFF_URL}/stations/by-code/251401000004`);
    check(res, {
      'station detail 200': (r) => r.status === 200,
      'has connectors': (r) => {
        try { return r.json('connectors').length > 0; } catch { return false; }
      },
    });
  });

  sleep(0.5);

  // 3. Wallet balance
  group('Wallet', () => {
    const res = http.get(`${BFF_URL}/wallet/balance`, { headers });
    walletBalanceDuration.add(res.timings.duration);
    check(res, {
      'balance 200': (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // 4. Active session check
  group('Session Check', () => {
    const res = http.get(`${BFF_URL}/sessions/active`, { headers });
    check(res, {
      'active session 200 or 204': (r) => r.status === 200 || r.status === 204,
    });
  });

  sleep(0.5);

  // 5. Session history
  group('Session History', () => {
    const res = http.get(`${BFF_URL}/sessions/history?pageSize=10`, { headers });
    check(res, {
      'history 200': (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // 6. Profile
  group('Profile', () => {
    const res = http.get(`${BFF_URL}/profile`, { headers });
    check(res, {
      'profile 200': (r) => r.status === 200,
    });
  });

  sleep(1 + Math.random() * 2); // Random think time 1-3s
}
