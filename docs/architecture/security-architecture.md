LabTrack Enterprise Security Architecture & Zero-Trust Defense ModelThis specification details the end-to-end security architecture of the LabTrack Laboratory Asset Management Platform, establishing a zero-trust posture across network gateways, application services, relational storage, and cryptographic audit ledgers.1. Security Architecture Topology                   +---------------------------------------+
                   |          External Clients             |
                   +---------------------------------------+
                                       |
                                       v  (HTTPS / TLS 1.3)
                   +---------------------------------------+
                   |       Nginx Gateway / Reverse Proxy   |
                   |  (Rate Limiting, Security Headers,    |
                   |   SSL Offloading, DDoS Mitigations)   |
                   +---------------------------------------+
                                       |
            +--------------------------+--------------------------+
            | (Bearer JWT)                                        | (Internal REST)
            v                                                     v
+-----------------------+                             +-----------------------+
|  .NET 8 Backend API   |                             |   Python AI Engine    |
| (FluentValidation,    |                             | (FastAPI, Sandboxed   |
|  Identity Handlers)   |                             |  Embedding Workers)   |
+-----------------------+                             +-----------------------+
            |                                                     |
            +--------------------------+--------------------------+
                                       |
                                       v  (Connection Pool + RLS Context)
                   +---------------------------------------+
                   |        PostgreSQL 16 Database         |
                   | (Row Level Security, SHA-256 Chained  |
                   |  Forensic Ledger, pgcrypto Engine)    |
                   +---------------------------------------+
2. Identity, Authentication, & Authorization (IAM)LabTrack implements a stateful, dual-token authentication model paired with strict Role-Based Access Control (RBAC).2.1 Dual-Token ArchitectureAccess Token (JWT): Short-lived (60-minute expiration), signed via HMAC-SHA256, transmitted via the Authorization: Bearer <token> header. Contains explicit claims for sub (User ID), role, email, and session_id.Refresh Token: Long-lived (7-day expiration), securely bound to client session fingerprints, stored in HTTP-Only secure cookies and validated against auth.user_sessions.2.2 Role-Based Access Control (RBAC) MatrixAdmin: Unrestricted platform-wide administrative privileges across all eight schemas (auth, inventory, scheduling, telemetry, maintenance, financial, ai, audit).Technician: Operational authority to execute repair work orders, manage spare parts, complete preventive maintenance routines, and resolve EHS safety reports.Researcher: End-user access limited to asset availability search, reservation bookings, QR code on-site check-in/check-out, and problem report submission.Auditor: Read-only oversight access strictly bounded to financial ledgers, forensic audit trails, and reporting views.3. Database Row Level Security (RLS)To prevent lateral movement and unauthorized data access at the storage layer, all transactional tables enforce PostgreSQL Row Level Security.3.1 Transactional Context InjectionEvery database session connection sets an execution parameter inside the transaction boundary prior to executing queries:SQLBEGIN;
SET LOCAL app.current_user_id = '10000000-0000-0000-0000-000000000002';
-- Query execution governed by RLS policies
SELECT * FROM inventory.assets;
COMMIT;
4. Network Perimeter & Gateway HardeningThe Nginx reverse proxy gateway acts as the primary perimeter defense layer, enforcing strict compliance policies:Rate Limiting Zones:Authentication endpoints (/api/v1/auth/): Capped at 5 requests/sec with burst headroom to mitigate credential stuffing and brute-force attacks.General API traffic (/api/v1/): Capped at 20 requests/sec per client IP.Mandatory Security Headers:Nginxadd_header X-Frame-Options "DENY" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" always;
Payload Restrictions: Maximum request body size capped at 50M for bulk CSV/JSON asset staging imports and high-volume telemetry ingestion buffers.5. Cryptographic Tamper-Evident Forensic AuditingAll critical state modifications are permanently recorded in audit.forensic_ledger, secured via continuous SHA-256 block-chaining:$$H_n = \text{SHA-256}\left( H_{n-1} \mathbin{\Vert} \text{EventUUID} \mathbin{\Vert} \text{ActorID} \mathbin{\Vert} \text{Action} \mathbin{\Vert} \text{PayloadJSON} \mathbin{\Vert} \text{Timestamp} \right)$$This mechanism ensures that any unauthorized modification of historical database records breaks the cryptographic hash chain, immediately exposing tampering during forensic verification queries:SQLSELECT * FROM audit.fn_verify_forensic_ledger_integrity();
6. Distributed Idempotency & Abuse ProtectionRequest Deduplication: Non-idempotent mutation endpoints require an X-Idempotency-Key HTTP header. Inflight requests are registered in audit.idempotency_locks to prevent duplicate financial charges, asset bookings, or repair ticket generation during network retries.Account Lockout Protection: Consecutive failed authentication attempts increment failure counters, triggering automated 15-minute account lockouts and logging security incidents to auth.security_incidents