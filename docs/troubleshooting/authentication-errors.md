LabTrack Authentication & Security Troubleshooting Guide
This document provides a comprehensive troubleshooting guide for diagnosing and resolving authentication failures, token lifecycle errors, Row-Level Security (RLS) context issues, Multi-Factor Authentication (MFA) validation bugs, and account security lockouts within the LabTrack platform.

1. Authentication Error Handling Standards
All authentication and authorization errors returned by the LabTrack API Gateway and .NET 8 backend adhere to a standardized JSON error response envelope:

JSON
{
  "error_code": "TOKEN_EXPIRED",
  "message": "The provided JWT access token has expired. Please use your refresh token to acquire a new session.",
  "timestamp": "2026-10-21T19:30:00Z",
  "details": {
    "token_issued_at": "2026-10-21T18:30:00Z",
    "token_expiry": "2026-10-21T19:30:00Z"
  }
}
2. Common Authentication Error Codes Matrix
Error Code	HTTP Status	Description / Trigger	Primary Root Cause	Resolution Steps
UNAUTHORIZED	401 Unauthorized	Missing Authorization header or malformed Bearer token format.	Client failed to attach JWT or prefixed incorrectly (e.g., missing Bearer).	Ensure request headers include Authorization: Bearer <jwt_access_token>.
TOKEN_EXPIRED	401 Unauthorized	JWT access token lifetime (60 minutes) has elapsed.	Normal expiration of short-lived token.	Submit refresh token to /api/v1/auth/refresh to obtain a new access token.
INVALID_REFRESH_TOKEN	403 Forbidden	Refresh token is invalid, expired (> 7 days), or previously revoked.	Session expired or explicitly revoked via /auth/revoke-session.	Prompt user to re-authenticate via /api/v1/auth/login.
MFA_REQUIRED	202 Accepted	Primary credentials valid, but account requires TOTP challenge.	User has MFA enabled (is_mfa_enabled = TRUE).	Proceed to submit 6-digit TOTP code to /api/v1/auth/mfa.
INVALID_TOTP_CODE	400 Bad Request	Submitted Time-based One-Time Password verification failed.	Incorrect code entered or severe server/client clock skew (> 30s).	Verify authenticator app time synchronization and re-enter code.
**`ACCOUNT_LOCKED``	423 Locked	Account temporarily frozen due to excessive failed login attempts.	5 consecutive failed authentication passwords.	Wait 15 minutes for automated unlock or request administrator reset.
RLS_CONTEXT_MISSING	500 Internal Error	Database transaction executed without setting app.current_user_id.	Backend middleware failed to inject user context before querying RLS tables.	Verify ASP.NET Core database context interceptor and transaction setup.
3. Detailed Troubleshooting Scenarios
3.1 Scenario A: JWT Expiration & Refresh Flow Failures
Symptom: Users experience sudden logouts or 401 Unauthorized responses during long diagnostic sessions.

Diagnosis: Inspect token expiration claim (exp) against current system time. Verify if the client application correctly intercepts 401 responses to execute token refresh requests.

Resolution:

Check that the refresh token endpoint (/api/v1/auth/refresh) is reachable.

Verify that auth.user_sessions records show is_active = TRUE for the active session UUID.

3.2 Scenario B: Row-Level Security (RLS) Context Errors (RLS_CONTEXT_MISSING)
Symptom: Database queries return empty data sets or throw policy violation exceptions despite valid user credentials.

Diagnosis: Check PostgreSQL server logs for missing session variables. RLS policies evaluate current_setting('app.current_user_id', true) as null or empty.

Resolution: Ensure the backend EF Core repository or Dapper connection factory wraps queries in explicit transaction blocks setting the session variable:

SQL
SET LOCAL app.current_user_id = '<user_uuid>';
3.3 Scenario C: Brute-Force Defense & Account Lockout
Symptom: Valid login credentials result in 423 Locked responses.

Diagnosis: Query auth.security_incidents to review recent failed authentication attempts and IP throttling triggers.

Resolution:

SQL
SELECT incident_id, user_id, ip_address, incident_type, created_at
FROM auth.security_incidents
WHERE incident_type = 'BRUTE_FORCE_LOCKOUT'
ORDER BY created_at DESC
LIMIT 10;
If verified as legitimate, administrators can manually clear the lockout timestamp in auth.users.

4. Diagnostic SQL Toolkit for Administrators
Run the following administrative queries directly against the PostgreSQL 16 database to troubleshoot session and security state:

4.1 Inspect Active User Sessions
SQL
SELECT 
    us.session_id,
    u.email,
    r.name AS role_name,
    us.ip_address,
    us.created_at,
    us.expires_at,
    us.is_active
FROM auth.user_sessions us
JOIN auth.users u ON us.user_id = u.id
JOIN auth.roles r ON u.role_id = r.id
WHERE us.is_active = TRUE
ORDER BY us.created_at DESC;
4.2 Verify Revoked Token Blacklist
SQL
SELECT 
    token_jti,
    user_id,
    revocation_reason,
    revoked_at
FROM auth.revoked_tokens
ORDER BY revoked_at DESC
LIMIT 50;