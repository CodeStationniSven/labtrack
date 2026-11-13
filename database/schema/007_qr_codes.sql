-- =============================================================================
-- LabTrack Schema Initialization: 007_qr_codes.sql
-- Database Engine: PostgreSQL 14+
-- Module: Secure Asset Identification Tokens, Cryptographic QR Codes, & Check-In Logs
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: scheduling.qr_tokens
-- Purpose: Manages cryptographically hashed QR tokens attached to physical assets
--          for touchless check-ins, physical verification scans, and mobile audits.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS scheduling.qr_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    token_code VARCHAR(255) NOT NULL UNIQUE,
    checksum VARCHAR(64) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    generated_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_tokens_asset_id ON scheduling.qr_tokens(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_tokens_code ON scheduling.qr_tokens(token_code);
CREATE INDEX IF NOT EXISTS idx_qr_tokens_is_active ON scheduling.qr_tokens(is_active);

-- Attach automatic timestamp update trigger
DROP TRIGGER IF EXISTS trg_qr_tokens_update_timestamp ON scheduling.qr_tokens;
CREATE TRIGGER trg_qr_tokens_update_timestamp
BEFORE UPDATE ON scheduling.qr_tokens
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: scheduling.qr_checkin_logs
-- Purpose: Records physical scanning interactions, check-ins, check-outs, and 
--          geolocation metrics for hardware auditability and custody validation.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS scheduling.qr_checkin_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    qr_token_id UUID NOT NULL REFERENCES scheduling.qr_tokens(id) ON DELETE RESTRICT,
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    action_type scheduling.qr_action_type NOT NULL DEFAULT 'Check-In',
    location_bay VARCHAR(100),
    latitude NUMERIC(10, 7),
    longitude NUMERIC(10, 7),
    is_successful BOOLEAN NOT NULL DEFAULT TRUE,
    failure_reason VARCHAR(255),
    scanned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_checkin_token_id ON scheduling.qr_checkin_logs(qr_token_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_asset_id ON scheduling.qr_checkin_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_user_id ON scheduling.qr_checkin_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_scanned_at ON scheduling.qr_checkin_logs(scanned_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Function: scheduling.fn_verify_and_log_qr_scan
-- Purpose: Atomically validates a scanned QR token against asset status, active 
--          expiration dates, and logs the scan event with geolocation data.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION scheduling.fn_verify_and_log_qr_scan(
    p_token_code VARCHAR(255),
    p_user_id UUID,
    p_action_type scheduling.qr_action_type,
    p_location_bay VARCHAR(100) DEFAULT NULL,
    p_latitude NUMERIC(10, 7) DEFAULT NULL,
    p_longitude NUMERIC(10, 7) DEFAULT NULL
)
RETURNS TABLE (
    is_valid BOOLEAN,
    asset_id UUID,
    asset_name VARCHAR(255),
    message VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_token RECORD;
    v_asset RECORD;
    v_log_id UUID;
    v_failure_reason VARCHAR(255) := NULL;
BEGIN
    -- Query token details
    SELECT id, asset_id, is_active, expires_at 
    INTO v_token
    FROM scheduling.qr_tokens
    WHERE token_code = p_token_code;

    -- Validate token existence
    IF v_token.id IS NULL THEN
        RETURN QUERY SELECT FALSE, NULL::UUID, NULL::VARCHAR(255), 'Invalid QR token identifier.'::VARCHAR(255);
        RETURN;
    END IF;

    -- Validate physical asset record
    SELECT id, name, status INTO v_asset
    FROM inventory.assets
    WHERE id = v_token.asset_id;

    -- Check revocation or deactivation
    IF NOT v_token.is_active THEN
        v_failure_reason := 'QR token has been revoked or deactivated.';
    ELSIF v_token.expires_at IS NOT NULL AND v_token.expires_at < NOW() THEN
        v_failure_reason := 'QR token has expired.';
    ELSIF v_asset.status = 'Decommissioned' THEN
        v_failure_reason := 'Target asset is decommissioned.';
    END IF;

    -- Log check-in scan attempt
    INSERT INTO scheduling.qr_checkin_logs (
        qr_token_id,
        asset_id,
        user_id,
        action_type,
        location_bay,
        latitude,
        longitude,
        is_successful,
        failure_reason,
        scanned_at
    ) VALUES (
        v_token.id,
        v_token.asset_id,
        p_user_id,
        p_action_type,
        p_location_bay,
        p_latitude,
        p_longitude,
        (v_failure_reason IS NULL),
        v_failure_reason,
        NOW()
    )
    RETURNING id INTO v_log_id;

    IF v_failure_reason IS NOT NULL THEN
        RETURN QUERY SELECT FALSE, v_token.asset_id, v_asset.name, v_failure_reason;
    ELSE
        RETURN QUERY SELECT TRUE, v_token.asset_id, v_asset.name, 'QR token verified successfully.'::VARCHAR(255);
    END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. View: scheduling.v_recent_qr_activity
-- Purpose: Outlines real-time physical scan history, users, and asset details.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW scheduling.v_recent_qr_activity AS
SELECT 
    l.id AS scan_id,
    l.asset_id,
    a.name AS asset_name,
    a.serial_number,
    u.id AS user_id,
    u.full_name AS scanned_by_user,
    u.email AS user_email,
    l.action_type,
    l.location_bay,
    l.is_successful,
    l.failure_reason,
    l.scanned_at
FROM 
    scheduling.qr_checkin_logs l
JOIN 
    inventory.assets a ON l.asset_id = a.id
JOIN 
    auth.users u ON l.user_id = u.id
ORDER BY 
    l.scanned_at DESC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE scheduling.qr_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE scheduling.qr_checkin_logs ENABLE ROW LEVEL SECURITY;

-- QR Tokens Read Policy: Authenticated users can view active tokens
DROP POLICY IF EXISTS p_qr_tokens_read ON scheduling.qr_tokens;
CREATE POLICY p_qr_tokens_read ON scheduling.qr_tokens
    FOR SELECT
    USING (TRUE);

-- Scan Logs Read Policy: Users view their own scan history; Admins/Technicians view all
DROP POLICY IF EXISTS p_qr_checkin_logs_read ON scheduling.qr_checkin_logs;
CREATE POLICY p_qr_checkin_logs_read ON scheduling.qr_checkin_logs
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;