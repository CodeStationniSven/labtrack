-- =============================================================================
-- LabTrack Schema Initialization: 022_idempotency.sql
-- Database Engine: PostgreSQL 14+
-- Module: Application Idempotency Control, Request Deduplication, & Inflight Locking
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: audit.idempotency_keys
-- Purpose: Caches unique API request keys, flight states, payload checksums,
--          and execution responses to guarantee safe HTTP retries and prevent
--          duplicate mutations across distributed microservices or clients.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.idempotency_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
    idempotency_key VARCHAR(255) NOT NULL UNIQUE,
    request_path VARCHAR(255) NOT NULL,
    request_params_hash VARCHAR(64) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'InFlight' CHECK (
        status IN ('InFlight', 'Completed', 'Failed')
    ),
    response_status_code INT,
    response_payload JSONB,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexing for fast request intercepts and automated key expiration purges
CREATE INDEX IF NOT EXISTS idx_idempotency_key_lookup ON audit.idempotency_keys(idempotency_key);
CREATE INDEX IF NOT EXISTS idx_idempotency_user_id ON audit.idempotency_keys(user_id);
CREATE INDEX IF NOT EXISTS idx_idempotency_expires_at ON audit.idempotency_keys(expires_at DESC);

-- Attach automatic timestamp trigger
DROP TRIGGER IF EXISTS trg_idempotency_keys_update_timestamp ON audit.idempotency_keys;
CREATE TRIGGER trg_idempotency_keys_update_timestamp
BEFORE UPDATE ON audit.idempotency_keys
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Function: audit.fn_acquire_idempotency_lock
-- Purpose: Atomically registers an incoming request key. If the key exists and
--          is completed, returns cached result. If inflight, signals concurrency lock.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_acquire_idempotency_lock(
    p_user_id UUID,
    p_idempotency_key VARCHAR(255),
    p_request_path VARCHAR(255),
    p_request_params_hash VARCHAR(64),
    p_ttl_seconds INT DEFAULT 86400
)
RETURNS TABLE (
    lock_acquired BOOLEAN,
    current_status VARCHAR(30),
    cached_response_code INT,
    cached_response_payload JSONB,
    message VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_existing RECORD;
    v_expires_at TIMESTAMP WITH TIME ZONE;
BEGIN
    v_expires_at := NOW() + (p_ttl_seconds || ' seconds')::INTERVAL;

    -- Query existing idempotency record with row lock
    SELECT * INTO v_existing
    FROM audit.idempotency_keys
    WHERE idempotency_key = p_idempotency_key
    FOR UPDATE;

    IF v_existing.id IS NOT NULL THEN
        -- Verify parameter hash matches to detect key reuse with altered payloads
        IF v_existing.request_params_hash <> p_request_params_hash THEN
            RETURN QUERY SELECT FALSE, v_existing.status, NULL::INT, NULL::JSONB, 'Idempotency key re-used with different request payload parameters.'::VARCHAR(255);
            RETURN;
        END IF;

        IF v_existing.status = 'Completed' THEN
            RETURN QUERY SELECT FALSE, v_existing.status, v_existing.response_status_code, v_existing.response_payload, 'Request already processed. Returning cached response.'::VARCHAR(255);
            RETURN;
        ELSIF v_existing.status = 'InFlight' THEN
            RETURN QUERY SELECT FALSE, v_existing.status, NULL::INT, NULL::JSONB, 'Request currently in-flight by another concurrent thread.'::VARCHAR(255);
            RETURN;
        ELSE
            -- Allow retry if prior execution failed
            UPDATE audit.idempotency_keys
            SET status = 'InFlight',
                expires_at = v_expires_at,
                updated_at = NOW()
            WHERE id = v_existing.id;

            RETURN QUERY SELECT TRUE, 'InFlight'::VARCHAR(30), NULL::INT, NULL::JSONB, 'Retrying previously failed request execution lock acquired.'::VARCHAR(255);
            RETURN;
        END IF;
    END IF;

    -- Insert new idempotency key registration
    INSERT INTO audit.idempotency_keys (
        user_id,
        idempotency_key,
        request_path,
        request_params_hash,
        status,
        expires_at
    ) VALUES (
        p_user_id,
        p_idempotency_key,
        p_request_path,
        p_request_params_hash,
        'InFlight',
        v_expires_at
    );

    RETURN QUERY SELECT TRUE, 'InFlight'::VARCHAR(30), NULL::INT, NULL::JSONB, 'Idempotency lock acquired successfully.'::VARCHAR(255);
END;
$$;


-- -----------------------------------------------------------------------------
-- 3. Function: audit.fn_release_idempotency_lock
-- Purpose: Finalizes the execution state of an idempotency key and caches the
--          outgoing HTTP status and JSON response body.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_release_idempotency_lock(
    p_idempotency_key VARCHAR(255),
    p_status_code INT,
    p_response_payload JSONB,
    p_is_success BOOLEAN DEFAULT TRUE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE audit.idempotency_keys
    SET status = CASE WHEN p_is_success THEN 'Completed' ELSE 'Failed' END,
        response_status_code = p_status_code,
        response_payload = p_response_payload,
        updated_at = NOW()
    WHERE idempotency_key = p_idempotency_key;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: audit.fn_cleanup_expired_idempotency_keys
-- Purpose: Routine data purging procedure to eliminate expired idempotency entries.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_cleanup_expired_idempotency_keys()
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM audit.idempotency_keys
    WHERE expires_at < NOW();

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE audit.idempotency_keys ENABLE ROW LEVEL SECURITY;

-- Idempotency Read/Write Policy: Users manage their own request keys; Admins access all
DROP POLICY IF EXISTS p_idempotency_keys_self ON audit.idempotency_keys;
CREATE POLICY p_idempotency_keys_self ON audit.idempotency_keys
    FOR ALL
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

COMMIT;