LabTrack Load & Performance Testing Guide
This document provides comprehensive instructions for configuring, executing, and analyzing load, stress, and endurance tests for the LabTrack enterprise laboratory management platform using k6.

1. Overview & Test Scope
The LabTrack load testing suite evaluates system stability, throughput, latency profiles, database connection pool saturation, and resource utilization under high concurrency.

                  +-----------------------------------+
                  |        k6 Load Generator          |
                  +-----------------------------------+
                                    |
                                    v  (HTTP / WSS Load)
                  +-----------------------------------+
                  |     Nginx Reverse Proxy Gateway   |
                  +-----------------------------------+
                       |                         |
                       v                         v
        +---------------------------+     +---------------------------+
        |   .NET 8 Backend API      |     |    Python AI RAG Engine   |
        +---------------------------+     +---------------------------+
                       |                         |
                       +------------+------------+
                                    |
                                    v
                  +-----------------------------------+
                  |     PostgreSQL 16 Database        |
                  |  (Connection Pool / RLS / OCC)    |
                  +-----------------------------------+
Key Load Test Scenarios
Authentication Spike (auth-load.js): Simulates a high burst of user logins and JWT token acquisitions to test password hashing overhead and database auth table throughput.

Asset QR Check-In & OCC Concurrency (asset-concurrency-load.js): Floods asset reservation and QR check-in endpoints to validate Optimistic Concurrency Control (OCC) conflict handling (HTTP 409) and distributed idempotency locking.

AI RAG Vector Search (ai-vector-load.js): Evaluates Python FastAPI and PostgreSQL pgvector index performance under concurrent semantic similarity queries.

SignalR Telemetry Streaming (signalr-telemetry-load.js): Simulates hundreds of concurrent workstation clients maintaining persistent WebSocket connections to /hubs/telemetry while streaming hardware metrics.

2. Prerequisites & Tooling
To execute the load test suite, ensure Grafana k6 is installed on your load testing machine or CI runner:

Install k6:

Bash
# Windows (via winget)
winget install Grafana.k6

# macOS (via Homebrew)
brew install k6

# Linux (Debian/Ubuntu)
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6
3. Test Suite Directory Structure
backend/tests/load/
├── k6/
│   ├── auth-load.js                 # Authentication spike test
│   ├── asset-concurrency-load.js    # Asset reservation & OCC test
│   ├── ai-vector-load.js            # Python pgvector RAG load test
│   └── signalr-telemetry-load.js    # WebSocket telemetry connection test
└── README.md                        # This documentation file
4. Executing Load Tests
4.1 Running Individual k6 Scenarios
Navigate to the load test directory and execute k6 specifying the target script:

Bash
# Run authentication load test against local staging
k6 run backend/tests/load/k6/auth-load.js

# Run asset concurrency load test with custom target environment variable
k6 run -e BASE_URL=http://staging.labtrack.io backend/tests/load/k6/asset-concurrency-load.js
4.2 Sample k6 Script (asset-concurrency-load.js)
JavaScript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // Ramp up to 20 users
    { duration: '1m', target: 50 },   // Hold at 50 concurrent users
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<250'], // 95% of requests must complete below 250ms
    http_req_failed: ['rate<0.01'],   // Error rate must be less than 1%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export default function () {
  const idempotencyKey = uuidv4();
  const url = `${BASE_URL}/api/v1/scheduling/qr/check-in`;
  
  const payload = JSON.stringify({
    status: 'In Use',
    notes: 'Automated k6 load test check-in'
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Idempotency-Key': idempotencyKey,
    },
  };

  const res = http.post(url, payload, params);

  check(res, {
    'status is 200, 201 or 409 (OCC Conflict)': (r) => 
      r.status === 200 || r.status === 201 || r.status === 409,
  });

  sleep(1);
}
5. Performance Thresholds & Metrics
LabTrack load tests enforce strict service level objectives (SLOs):

Metric	Target Threshold	Description
HTTP Request Duration (P95)	<250 ms	95th percentile response time for standard transactional endpoints.
HTTP Request Duration (P99)	<500 ms	99th percentile response time under peak concurrency.
Error Rate (http_req_failed)	<0.1%	Maximum allowable unexpected server errors (excluding expected HTTP 409 OCC conflicts).
WebSocket Connection Success	100%	SignalR WebSocket handshakes must succeed without timeout during connection storms.