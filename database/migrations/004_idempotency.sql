-- =============================================================================
-- LabTrack Migration Script: 004_idempotency.sql
-- Database Engine: PostgreSQL 14+
-- Description: Idempotency keys, API request deduplication, distributed locking,
--              and transactional request replay prevention.
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. Table: api_idempotency_keys
-- Purpose: Caches API mutation request payloads, processing states, and HTTP responses
--          keyed by unique client idempotency header UUIDs to prevent duplicate execution.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS api_idempotency_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    idempotency_key VARCHAR(255) NOT NULL UNIQUE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    request_path VARCHAR(500) NOT NULL,
    request_method VARCHAR(10) NOT NULL,
    request_params_hash VARCHAR(64) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Processing' CHECK (
        status IN ('Processing', 'Completed', 'Failed')
    ),
    response_status_code INT,
    response_body JSONB,
    response_headers JSONB,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Fast lookup indexes for request validation and expiration cleanup
CREATE INDEX IF NOT EXISTS idx_idempotency_key_lookup 
    ON api_idempotency_keys(idempotency_key);

CREATE INDEX IF NOT EXISTS idx_idempotency_expires_at 
    ON api_idempotency_keys(expires_at DESC);

CREATE INDEX IF NOT EXISTS idx_idempotency_user_id 
    ON api_idempotency_keys(user_id);

-- Attach updated_at timestamp trigger
DROP TRIGGER IF EXISTS trg_api_idempotency_keys_update_timestamp ON api_idempotency_keys;
CREATE TRIGGER trg_api_idempotency_keys_update_timestamp
BEFORE UPDATE ON api_idempotency_keys
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Function: fn_acquire_idempotency_lock
-- Purpose: Atomically registers or retrieves an idempotency key.
--          Returns the existing response if completed, flags concurrent processing,
--          or inserts a new lock record if it's a first-time operation.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_acquire_idempotency_lock(
    p_idempotency_key VARCHAR(255),
    p_user_id UUID,
    p_request_path VARCHAR(500),
    p_request_method VARCHAR(10),
    p_request_params_hash VARCHAR(64),
    p_ttl_seconds INT DEFAULT 86400 -- Default 24-hour retention
)
RETURNS TABLE (
    is_duplicate BOOLEAN,
    current_status VARCHAR(30),
    cached_status_code INT,
    cached_response_body JSONB,
    cached_response_headers JSONB
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_record RECORD;
    v_expires_at TIMESTAMP WITH TIME ZONE;
BEGIN
    v_expires_at := NOW() + (p_ttl_seconds || ' seconds')::INTERVAL;

    -- Search for existing key record
    SELECT 
        status, 
        response_status_code, 
        response_body, 
        response_headers,
        expires_at,
        request_params_hash
    INTO v_record
    FROM api_idempotency_keys
    WHERE idempotency_key = p_idempotency_key;

    IF v_record IS NOT NULL THEN
        -- Check if expired; if expired, remove and allow reuse
        IF v_record.expires_at < NOW() THEN
            DELETE FROM api_idempotency_keys WHERE idempotency_key = p_idempotency_key;
        ELSE
            -- Verify payload hash matches original request
            IF v_record.request_params_hash <> p_request_params_hash THEN
                RAISE EXCEPTION 'Idempotency key collision: parameter mismatch for key %', p_idempotency_key
                    USING HINT = 'Reuse of idempotency keys requires identical request payloads.';
            END IF;

            -- Return cached state
            RETURN QUERY SELECT 
                TRUE, 
                v_record.status::VARCHAR(30), 
                v_record.response_status_code, 
                v_record.response_body, 
                v_record.response_headers;
            RETURN;
        END IF;
    END IF;

    -- Register new idempotency lock record
    INSERT INTO api_idempotency_keys (
        idempotency_key,
        user_id,
        request_path,
        request_method,
        request_params_hash,
        status,
        expires_at
    ) VALUES (
        p_idempotency_key,
        p_user_id,
        p_request_path,
        p_request_method,
        p_request_params_hash,
        'Processing',
        v_expires_at
    );

    RETURN QUERY SELECT 
        FALSE, 
        'Processing'::VARCHAR(30), 
        NULL::INT, 
        NULL::JSONB, 
        NULL::JSONB;
END;
$$;


-- -----------------------------------------------------------------------------
-- 3. Function: fn_complete_idempotency_response
-- Purpose: Updates an existing idempotency lock upon successful/failed endpoint execution,
--          caching the status code and response payload for future replayed requests.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_complete_idempotency_response(
    p_idempotency_key VARCHAR(255),
    p_status_code INT,
    p_response_body JSONB,
    p_response_headers JSONB DEFAULT '{}'::jsonb,
    p_is_success BOOLEAN DEFAULT TRUE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE api_idempotency_keys
    SET status = CASE WHEN p_is_success THEN 'Completed' ELSE 'Failed' END,
        response_status_code = p_status_code,
        response_body = p_response_body,
        response_headers = p_response_headers,
        updated_at = NOW()
    WHERE idempotency_key = p_idempotency_key;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: fn_cleanup_expired_idempotency_keys
-- Purpose: Scheduled retention maintenance procedure to prune expired keys.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_cleanup_expired_idempotency_keys()
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM api_idempotency_keys
    WHERE expires_at < NOW();

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------

ALTER TABLE api_idempotency_keys ENABLE ROW LEVEL SECURITY;

-- Idempotency Read Policy: Users access only their own requested keys; Admins access all
DROP POLICY IF EXISTS p_idempotency_keys_self_read ON api_idempotency_keys;
CREATE POLICY p_idempotency_keys_self_read ON api_idempotency_keys
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