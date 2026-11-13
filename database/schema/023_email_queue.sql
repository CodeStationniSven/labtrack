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

COMMIT;-- =============================================================================
-- LabTrack Schema Initialization: 023_email_queue.sql
-- Database Engine: PostgreSQL 14+
-- Module: Outbound Email Dispatch Queue, Template Processing, & Retry Workers
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: audit.email_templates
-- Purpose: Central registry of parameterized transactional email templates 
--          (e.g., reservation confirmations, ticket assignments, MFA alerts).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.email_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    template_code VARCHAR(100) NOT NULL UNIQUE,
    subject_template VARCHAR(255) NOT NULL,
    body_html_template TEXT NOT NULL,
    body_text_template TEXT,
    description VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_email_templates_code ON audit.email_templates(template_code);
CREATE INDEX IF NOT EXISTS idx_email_templates_active ON audit.email_templates(is_active);

-- Attach automatic timestamp update trigger
DROP TRIGGER IF EXISTS trg_email_templates_update_timestamp ON audit.email_templates;
CREATE TRIGGER trg_email_templates_update_timestamp
BEFORE UPDATE ON audit.email_templates
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: audit.outbound_email_queue
-- Purpose: Persistent transactional queue for background email delivery background
--          workers (e.g., SMTP/SendGrid/SES consumers), complete with exponential 
--          backoff tracking and failure log capture.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.outbound_email_queue (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    template_id UUID REFERENCES audit.email_templates(id) ON DELETE SET NULL,
    recipient_email citext NOT NULL,
    recipient_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    sender_email VARCHAR(255) NOT NULL DEFAULT 'noreply@labtrack.io',
    subject VARCHAR(255) NOT NULL,
    body_html TEXT NOT NULL,
    body_text TEXT,
    template_variables JSONB DEFAULT '{}'::jsonb,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (
        status IN ('Pending', 'Processing', 'Sent', 'Failed', 'Cancelled')
    ),
    retry_count INT NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    max_retries INT NOT NULL DEFAULT 5 CHECK (max_retries >= 0),
    next_attempt_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    error_message TEXT,
    sent_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexing for high-throughput background worker polling and status filtering
CREATE INDEX IF NOT EXISTS idx_email_queue_polling 
    ON audit.outbound_email_queue(status, next_attempt_at) 
    WHERE status IN ('Pending', 'Processing');

CREATE INDEX IF NOT EXISTS idx_email_queue_recipient ON audit.outbound_email_queue(recipient_email);
CREATE INDEX IF NOT EXISTS idx_email_queue_user_id ON audit.outbound_email_queue(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_email_queue_created_at ON audit.outbound_email_queue(created_at DESC);

-- Attach automatic timestamp update trigger
DROP TRIGGER IF EXISTS trg_email_queue_update_timestamp ON audit.outbound_email_queue;
CREATE TRIGGER trg_email_queue_update_timestamp
BEFORE UPDATE ON audit.outbound_email_queue
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. Function: audit.fn_enqueue_email
-- Purpose: Helper procedure to queue a transactional email dispatch request using 
--          either raw HTML or an existing system template code.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_enqueue_email(
    p_recipient_email citext,
    p_subject VARCHAR(255),
    p_body_html TEXT DEFAULT NULL,
    p_template_code VARCHAR(100) DEFAULT NULL,
    p_template_variables JSONB DEFAULT '{}'::jsonb,
    p_recipient_user_id UUID DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
    v_template_id UUID := NULL;
    v_final_subject VARCHAR(255) := p_subject;
    v_final_html TEXT := p_body_html;
    v_queue_id UUID := uuid_generate_v4();
BEGIN
    -- Evaluate template if provided
    IF p_template_code IS NOT NULL THEN
        SELECT id, subject_template, body_html_template 
        INTO v_template_id, v_final_subject, v_final_html
        FROM audit.email_templates
        WHERE template_code = p_template_code 
          AND is_active = TRUE;

        IF v_template_id IS NULL THEN
            RAISE EXCEPTION 'Active email template with code % was not found.', p_template_code;
        END IF;
    END IF;

    IF v_final_html IS NULL THEN
        RAISE EXCEPTION 'Email content body cannot be empty.';
    END IF;

    -- Enqueue email job
    INSERT INTO audit.outbound_email_queue (
        id,
        template_id,
        recipient_email,
        recipient_user_id,
        subject,
        body_html,
        template_variables,
        status,
        next_attempt_at
    ) VALUES (
        v_queue_id,
        v_template_id,
        p_recipient_email,
        p_recipient_user_id,
        v_final_subject,
        v_final_html,
        p_template_variables,
        'Pending',
        NOW()
    );

    RETURN v_queue_id;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: audit.fn_fetch_and_lock_email_batch
-- Purpose: Atomically acquires and locks pending email jobs for queue worker processes 
--          using FOR UPDATE SKIP LOCKED to prevent race conditions in multi-threaded nodes.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_fetch_and_lock_email_batch(
    p_batch_size INT DEFAULT 50
)
RETURNS TABLE (
    queue_id UUID,
    recipient_email citext,
    sender_email VARCHAR(255),
    subject VARCHAR(255),
    body_html TEXT,
    body_text TEXT,
    retry_count INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH pending_jobs AS (
        SELECT id
        FROM audit.outbound_email_queue
        WHERE status = 'Pending'
          AND next_attempt_at <= NOW()
        ORDER BY next_attempt_at ASC
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE audit.outbound_email_queue q
    SET status = 'Processing',
        updated_at = NOW()
    FROM pending_jobs pj
    WHERE q.id = pj.id
    RETURNING 
        q.id AS queue_id,
        q.recipient_email,
        q.sender_email,
        q.subject,
        q.body_html,
        q.body_text,
        q.retry_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. Function: audit.fn_record_email_delivery_result
-- Purpose: Updates status after delivery attempt. Marks as 'Sent' on success or 
--          reschedules with exponential backoff on transient failure.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_record_email_delivery_result(
    p_queue_id UUID,
    p_is_success BOOLEAN,
    p_error_message TEXT DEFAULT NULL
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current_retries INT;
    v_max_retries INT;
    v_backoff_minutes INT;
BEGIN
    SELECT retry_count, max_retries INTO v_current_retries, v_max_retries
    FROM audit.outbound_email_queue
    WHERE id = p_queue_id;

    IF p_is_success THEN
        UPDATE audit.outbound_email_queue
        SET status = 'Sent',
            sent_at = NOW(),
            error_message = NULL,
            updated_at = NOW()
        WHERE id = p_queue_id;
    ELSE
        v_current_retries := v_current_retries + 1;
        
        IF v_current_retries >= v_max_retries THEN
            -- Max retries exhausted
            UPDATE audit.outbound_email_queue
            SET status = 'Failed',
                retry_count = v_current_retries,
                error_message = p_error_message,
                updated_at = NOW()
            WHERE id = p_queue_id;
        ELSE
            -- Exponential backoff formula: 2^retry * 5 minutes
            v_backoff_minutes := (POWER(2, v_current_retries) * 5)::INT;

            UPDATE audit.outbound_email_queue
            SET status = 'Pending',
                retry_count = v_current_retries,
                next_attempt_at = NOW() + (v_backoff_minutes || ' minutes')::INTERVAL,
                error_message = p_error_message,
                updated_at = NOW()
            WHERE id = p_queue_id;
        END IF;
    END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE audit.email_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.outbound_email_queue ENABLE ROW LEVEL SECURITY;

-- Templates Access: Internal read for all; write restricted to Admins
DROP POLICY IF EXISTS p_email_templates_read ON audit.email_templates;
CREATE POLICY p_email_templates_read ON audit.email_templates
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_email_templates_write ON audit.email_templates;
CREATE POLICY p_email_templates_write ON audit.email_templates
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

-- Queue Access: System processing and Admins
DROP POLICY IF EXISTS p_outbound_email_queue_manage ON audit.outbound_email_queue;
CREATE POLICY p_outbound_email_queue_manage ON audit.outbound_email_queue
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;