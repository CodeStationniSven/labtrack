-- =============================================================================
-- LabTrack QR Code Check-in & Equipment Dispatch Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Physical Asset Tokenization, QR Check-in Sessions, & Verification
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: qr_tokens
-- Purpose: Stores active and revoked QR verification tokens associated with
--          physical laboratory equipment, workstation bays, and storage racks.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS qr_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    token_code VARCHAR(255) NOT NULL UNIQUE,
    checksum VARCHAR(64) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    generated_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_tokens_asset_id ON qr_tokens(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_tokens_code ON qr_tokens(token_code);

-- -----------------------------------------------------------------------------
-- Table: qr_checkin_logs
-- Purpose: Audit log recording physical QR code scans, researcher check-in/out
--          timestamps, location coordinates, and session status.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS qr_checkin_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    qr_token_id UUID NOT NULL REFERENCES qr_tokens(id) ON DELETE RESTRICT,
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action_type VARCHAR(30) NOT NULL CHECK (
        action_type IN ('Check-In', 'Check-Out', 'Verification-Only', 'Maintenance-Scan')
    ),
    location_bay VARCHAR(100),
    latitude NUMERIC(10, 7),
    longitude NUMERIC(10, 7),
    is_successful BOOLEAN NOT NULL DEFAULT TRUE,
    failure_reason VARCHAR(255),
    scanned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_checkin_token_id ON qr_checkin_logs(qr_token_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_asset_id ON qr_checkin_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_user_id ON qr_checkin_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_scanned_at ON qr_checkin_logs(scanned_at DESC);

-- -----------------------------------------------------------------------------
-- Function: fn_process_qr_checkin
-- Purpose: Validates a scanned QR token and records the check-in session for
--          a user while updating asset operational status atomically.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_process_qr_checkin(
    p_token_code VARCHAR(255),
    p_user_id UUID,
    p_action_type VARCHAR(30) DEFAULT 'Check-In',
    p_location_bay VARCHAR(100) DEFAULT NULL
)
RETURNS TABLE (
    success BOOLEAN,
    message VARCHAR(255),
    asset_id UUID,
    checkin_log_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_token_record RECORD;
    v_log_id UUID := uuid_generate_v4();
BEGIN
    -- Locate active QR token
    SELECT t.id AS token_id, t.asset_id, t.is_active, t.expires_at, a.status AS asset_status
    INTO v_token_record
    FROM qr_tokens t
    JOIN assets a ON t.asset_id = a.id
    WHERE t.token_code = p_token_code;

    -- Token validation checks
    IF v_token_record.token_id IS NULL THEN
        RETURN QUERY SELECT FALSE, 'Invalid QR code token.'::VARCHAR(255), NULL::UUID, NULL::UUID;
        RETURN;
    END IF;

    IF NOT v_token_record.is_active THEN
        INSERT INTO qr_checkin_logs (id, qr_token_id, asset_id, user_id, action_type, location_bay, is_successful, failure_reason)
        VALUES (v_log_id, v_token_record.token_id, v_token_record.asset_id, p_user_id, p_action_type, p_location_bay, FALSE, 'QR Code is revoked or inactive.');
        
        RETURN QUERY SELECT FALSE, 'QR Code is deactivated.'::VARCHAR(255), v_token_record.asset_id, v_log_id;
        RETURN;
    END IF;

    IF v_token_record.expires_at IS NOT NULL AND v_token_record.expires_at < NOW() THEN
        INSERT INTO qr_checkin_logs (id, qr_token_id, asset_id, user_id, action_type, location_bay, is_successful, failure_reason)
        VALUES (v_log_id, v_token_record.token_id, v_token_record.asset_id, p_user_id, p_action_type, p_location_bay, FALSE, 'QR Code token has expired.');

        RETURN QUERY SELECT FALSE, 'QR Code token has expired.'::VARCHAR(255), v_token_record.asset_id, v_log_id;
        RETURN;
    END IF;

    -- Record valid log entry
    INSERT INTO qr_checkin_logs (id, qr_token_id, asset_id, user_id, action_type, location_bay, is_successful)
    VALUES (v_log_id, v_token_record.token_id, v_token_record.asset_id, p_user_id, p_action_type, p_location_bay, TRUE);

    -- Update asset state based on scan action
    IF p_action_type = 'Check-In' THEN
        UPDATE assets SET status = 'In Use', updated_at = NOW() WHERE id = v_token_record.asset_id;
    ELSIF p_action_type = 'Check-Out' THEN
        UPDATE assets SET status = 'Available', updated_at = NOW() WHERE id = v_token_record.asset_id;
    END IF;

    RETURN QUERY SELECT TRUE, 'Check-in processed successfully.'::VARCHAR(255), v_token_record.asset_id, v_log_id;
END;
$$;

COMMIT;