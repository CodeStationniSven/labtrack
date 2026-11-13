-- =============================================================================
-- LabTrack Schema Initialization: 021_auth_sessions.sql
-- Database Engine: PostgreSQL 14+
-- Module: Granular Session Revocation Queues, Token Blacklisting, & IP Security Auditing
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: auth.revoked_tokens
-- Purpose: Invalidation blacklist registry for JWT access tokens, compromised
--          refresh keys, and revoked API tokens prior to natural expiration.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.revoked_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
    token_identifier_hash VARCHAR(255) NOT NULL UNIQUE,
    token_type VARCHAR(40) NOT NULL DEFAULT 'AccessToken' CHECK (
        token_type IN ('AccessToken', 'RefreshToken', 'ApiToken', 'PasswordResetToken')
    ),
    revocation_reason VARCHAR(255) DEFAULT 'User Logout',
    revoked_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Performance Indexing for high-speed authentication middleware validation
CREATE INDEX IF NOT EXISTS idx_revoked_tokens_hash ON auth.revoked_tokens(token_identifier_hash);
CREATE INDEX IF NOT EXISTS idx_revoked_tokens_user_id ON auth.revoked_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_revoked_tokens_expires_at ON auth.revoked_tokens(expires_at DESC);


-- -----------------------------------------------------------------------------
-- 2. Table: auth.ip_security_events
-- Purpose: Security logging table for brute-force tracking, failed auth attempts,
--          suspicious IP address origin activity, and geo-velocity shifts.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.ip_security_events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ip_address VARCHAR(45) NOT NULL,
    attempted_email citext,
    user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    event_type VARCHAR(60) NOT NULL CHECK (
        event_type IN ('FailedLogin', 'MfaFailure', 'PasswordResetRequested', 'SessionRevoked', 'BruteForceBlocked')
    ),
    user_agent TEXT,
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ip_events_ip ON auth.ip_security_events(ip_address, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_ip_events_user_id ON auth.ip_security_events(user_id);
CREATE INDEX IF NOT EXISTS idx_ip_events_blocked ON auth.ip_security_events(is_blocked) WHERE is_blocked = TRUE;


-- -----------------------------------------------------------------------------
-- 3. Function: auth.fn_revoke_user_sessions
-- Purpose: Atomically revokes all active sessions and refresh tokens for a target
--          user (e.g., password change, administrative suspension, breach response).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION auth.fn_revoke_user_sessions(
    p_target_user_id UUID,
    p_reason VARCHAR(255) DEFAULT 'Security Invalidation'
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_revoked_count INT := 0;
    v_session RECORD;
BEGIN
    -- Flag all active refresh tokens as revoked
    UPDATE auth.refresh_tokens
    SET is_revoked = TRUE,
        updated_at = NOW()
    WHERE user_id = p_target_user_id
      AND is_revoked = FALSE;

    -- Blacklist active session token hashes
    FOR v_session IN 
        SELECT session_token_hash, expires_at 
        FROM auth.active_user_sessions 
        WHERE user_id = p_target_user_id
    LOOP
        INSERT INTO auth.revoked_tokens (
            user_id,
            token_identifier_hash,
            token_type,
            revocation_reason,
            revoked_at,
            expires_at
        ) VALUES (
            p_target_user_id,
            v_session.session_token_hash,
            'AccessToken',
            p_reason,
            NOW(),
            v_session.expires_at
        )
        ON CONFLICT (token_identifier_hash) DO NOTHING;

        v_revoked_count := v_revoked_count + 1;
    END LOOP;

    -- Delete active sessions records
    DELETE FROM auth.active_user_sessions 
    WHERE user_id = p_target_user_id;

    -- Log forensic audit entry
    PERFORM audit.fn_append_forensic_ledger_entry(
        NULLIF(current_setting('app.current_user_id', true), '')::uuid,
        '0.0.0.0',
        'SAFETY_OVERRIDE'::audit.forensic_category,
        'REVOKE_ALL_SESSIONS',
        'auth',
        'active_user_sessions',
        p_target_user_id,
        NULL,
        jsonb_build_object('revoked_sessions_count', v_revoked_count, 'reason', p_reason)
    );

    RETURN v_revoked_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: auth.fn_cleanup_expired_revoked_tokens
-- Purpose: Data maintenance procedure to purge expired token blacklist records.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION auth.fn_cleanup_expired_revoked_tokens()
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM auth.revoked_tokens
    WHERE expires_at < NOW();

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. View: auth.v_security_incident_ip_summary
-- Purpose: Aggregates recent security anomalies and high-risk IPs for firewall
--          or threat mitigation systems.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW auth.v_security_incident_ip_summary AS
SELECT 
    ip_address,
    COUNT(*) AS total_failed_attempts,
    COUNT(DISTINCT attempted_email) AS targeted_accounts_count,
    MAX(recorded_at) AS last_attempt_at,
    BOOL_OR(is_blocked) AS is_currently_blocked
FROM 
    auth.ip_security_events
WHERE 
    recorded_at > (NOW() - INTERVAL '24 hours')
GROUP BY 
    ip_address
HAVING 
    COUNT(*) >= 5
ORDER BY 
    total_failed_attempts DESC;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE auth.revoked_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.ip_security_events ENABLE ROW LEVEL SECURITY;

-- Revoked Tokens Read Policy: System middleware check
DROP POLICY IF EXISTS p_revoked_tokens_read ON auth.revoked_tokens;
CREATE POLICY p_revoked_tokens_read ON auth.revoked_tokens
    FOR SELECT
    USING (TRUE);

-- Security Events Policy: Restricted exclusively to System Administrators
DROP POLICY IF EXISTS p_ip_security_events_read ON auth.ip_security_events;
CREATE POLICY p_ip_security_events_read ON auth.ip_security_events
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

COMMIT;