LabTrack Authentication, RBAC, & Security System SpecificationThis document defines the security architecture, Role-Based Access Control (RBAC) rules, Multi-Factor Authentication (MFA) workflows, JWT token life cycles, and session revocation mechanisms for the LabTrack Laboratory Asset Management Platform.1. Authentication & Token Management InfrastructureLabTrack uses a stateful, dual-token JWT architecture (Short-Lived Access Token + Long-Lived Refresh Token) coupled with explicit session invalidation tracking in PostgreSQL (auth.user_sessions and auth.revoked_tokens).1.1 Token SpecificationsToken TypeStandard ExpirationStorage LocationKey ContentsAccess Token (JWT)60 minutesAuthorization Header (Bearer <jwt>)User ID, Role, Email, Session ID, Issued At, ExpiryRefresh Token7 daysHTTP-Only Secure Cookie / Encrypted StoreSession UUID, Hashed Token Value, User ID1.2 JWT Token Payload ContractJSON{
  "sub": "10000000-0000-0000-0000-000000000002",
  "email": "tech.miles@labtrack.io",
  "name": "Miles Dyson",
  "role": "Technician",
  "session_id": "f8c3a1b2-9d4e-4f5a-8b6c-7d8e9f0a1b2c",
  "iss": "https://api.labtrack.io",
  "aud": "https://app.labtrack.io",
  "iat": 1792610220,
  "exp": 1792613820
}
2. Role-Based Access Control (RBAC) MatrixLabTrack enforces strict domain boundaries using PostgreSQL Row Level Security (RLS) and API Gateway authorization middleware.Role NamespaceSystem Access & Operations ScopeAdminUnrestricted access across all schemas (auth, inventory, scheduling, telemetry, maintenance, financial, ai, audit). Full user provisioning, RLS bypass, and security policy modifications.TechnicianManage physical asset states, execute repair tickets, complete preventive maintenance schedules, resolve safety reports, and access AI diagnostic manuals.ResearcherBook asset reservations, check into equipment via QR code tokens, view asset availability, and submit hardware problem reports.AuditorRead-only access across financial, audit, and inventory views (v_asset_details_comprehensive, v_financial_summary_executive, v_dashboard_recent_activity_feed). Cannot perform data mutations.3. Row Level Security (RLS) Execution ModelFor every incoming HTTP request, the backend API sets the session variable inside a PostgreSQL transaction prior to executing any queries:SQL-- Transaction isolation setup for authenticated context
BEGIN;
SET LOCAL app.current_user_id = '10000000-0000-0000-0000-000000000002';

-- Execute query governed by RLS
SELECT * FROM inventory.assets;

COMMIT;
4. Multi-Factor Authentication (MFA) & Security Workflows  Client                     API Gateway                   Auth Database
    |                             |                              |
    |---- 1. POST /auth/login --->|                              |
    |     (Credentials)           |-- 2. Verify Bcrypt Hash ---->|
    |                             |<-- 3. Password Valid --------|
    |                             |                              |
    |<-- 4. 202 MFA Required -----|                              |
    |    (MFA Token / Session)    |                              |
    |                             |                              |
    |---- 5. POST /auth/mfa ---->|                              |
    |     (TOTP Code)             |-- 6. Validate TOTP Secret -->|
    |                             |<-- 7. MFA Verified ----------|
    |                             |                              |
    |<-- 8. 200 OK (Tokens) ------|                              |
Primary Authentication: Validates email and bcrypt password hash against auth.users.MFA Verification: If is_mfa_enabled is TRUE, issues a transient 5-minute MFA challenge token (mfa_pending).TOTP Validation: The user submits a 6-digit Time-based One-Time Password (TOTP) generated via RFC 6238 algorithms.Session Finalization: Upon TOTP verification, a full access token and refresh token are generated and written to auth.user_sessions.5. Session Revocation & Brute-Force DefenseAutomated Invalidation: Failed login attempts increment an isolation counter. Five consecutive failed attempts trigger a 15-minute account lockout log in auth.security_incidents.Explicit Revocation: Calling /api/v1/auth/revoke-session adds the active JWT ID (jti) to auth.revoked_tokens and sets is_active = FALSE on the session record in auth.user_sessions.Rate Limiting: Nginx gateway enforces a strict 5 requests/sec limit on /api/v1/auth/ routes to prevent credential stuffing attacks.