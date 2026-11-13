-- =============================================================================
-- LabTrack Migration Script: 003_auth_sessions.sql
-- Database Engine: PostgreSQL 14+
-- Description: Advanced session management, multi-factor authentication (MFA)
--              state tracking, password reset/verification tokens, and active
--              user security session invalidation policies.
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. Table: user_mfa_settings
-- Purpose: Holds multi-factor authentication enrollment, TOTP secrets, and
--          one-time emergency backup recovery codes.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_mfa_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    is_mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    totp_secret_encrypted VARCHAR(512),
    backup_codes_hashed TEXT[] DEFAULT '{}',
    enrolled_at TIMESTAMP WITH TIME ZONE,
    last_verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mfa_settings_user_id ON user_mfa_settings(user_id);

-- Attach updated_at timestamp trigger
DROP TRIGGER IF EXISTS trg_user_mfa_settings_update_timestamp ON user_mfa_settings;
CREATE TRIGGER trg_user_mfa_settings_update_timestamp
BEFORE UPDATE ON user_mfa_settings
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: active_user_sessions
-- Purpose: Tracks active stateful login sessions, browser user agents, client IP
--          addresses, and device fingerprints across active JWT sessions.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS active_user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_token_hash VARCHAR(255) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL,
    user_agent TEXT,
    device_fingerprint VARCHAR(255),
    is_current_session BOOLEAN NOT NULL DEFAULT TRUE,
    last_activity_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_active_sessions_user_id 
    ON active_user_sessions(user_id);

CREATE INDEX IF NOT EXISTS idx_active_sessions_token_hash 
    ON active_user_sessions(session_token_hash);

CREATE INDEX IF NOT EXISTS idx_active_sessions_expires_at 
    ON active_user_sessions(expires_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Table: user_password_resets
-- Purpose: Manages single-use, time-bound password reset and email verification
--          tokens with rate-limiting constraints.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_password_resets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reset_token_hash VARCHAR(255) NOT NULL UNIQUE,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_password_resets_user_id ON user_password_resets(user_id);
CREATE INDEX IF NOT EXISTS idx_password_resets_token_hash ON user_password_resets(reset_token_hash);


-- -----------------------------------------------------------------------------
-- 4. Function: fn_revoke_all_user_sessions
-- Purpose: Security utility to instantly revoke all refresh tokens and active
--          sessions for a user (e.g., password resets or compromised accounts).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_revoke_all_user_sessions(
    p_user_id UUID
)
RETURNS TABLE (
    revoked_refresh_tokens INT,
    revoked_active_sessions INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_refresh_count INT;
    v_session_count INT;
BEGIN
    -- Revoke all unexpired JWT refresh tokens
    UPDATE refresh_tokens
    SET is_revoked = TRUE,
        updated_at = NOW()
    WHERE user_id = p_user_id
      AND is_revoked = FALSE;

    GET DIAGNOSTICS v_refresh_count = ROW_COUNT;

    -- Delete active browser/client session records
    DELETE FROM active_user_sessions
    WHERE user_id = p_user_id;

    GET DIAGNOSTICS v_session_count = ROW_COUNT;

    RETURN QUERY SELECT v_refresh_count, v_session_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------

ALTER TABLE user_mfa_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE active_user_sessions ENABLE ROW LEVEL SECURITY;

-- User MFA Policy: Users can only view their own MFA configuration
DROP POLICY IF EXISTS p_user_mfa_self_read ON user_mfa_settings;
CREATE POLICY p_user_mfa_self_read ON user_mfa_settings
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Active Sessions Policy: Users manage their own sessions; Admins manage all
DROP POLICY IF EXISTS p_active_sessions_self_read ON active_user_sessions;
CREATE POLICY p_active_sessions_self_read ON active_user_sessions
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

COMMIT;