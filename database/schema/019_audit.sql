-- =============================================================================
-- LabTrack Schema Initialization: 019_audit.sql
-- Database Engine: PostgreSQL 14+
-- Module: Tamper-Evident Security Ledgers, Block-Chained Audit Logs, & Compliance Verification
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: audit.forensic_ledger
-- Purpose: Tamper-evident, cryptographically linked append-only audit trail.
--          Utilizes pgcrypto SHA-256 block-chaining (combining previous entry's hash
--          with current row contents) to enforce data integrity and detect manual edits.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.forensic_ledger (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    actor_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    actor_ip_address VARCHAR(45),
    category audit.forensic_category NOT NULL DEFAULT 'CONFIG_MUTATION',
    action VARCHAR(100) NOT NULL,
    schema_name VARCHAR(64) NOT NULL,
    table_name VARCHAR(64) NOT NULL,
    record_id UUID,
    payload_before JSONB,
    payload_after JSONB,
    previous_hash VARCHAR(64) NOT NULL DEFAULT '0000000000000000000000000000000000000000000000000000000000000000',
    current_hash VARCHAR(64) NOT NULL
);

-- Indexing for fast incident investigation, actor profiling, and compliance temporal filtering
CREATE INDEX IF NOT EXISTS idx_forensic_ledger_actor ON audit.forensic_ledger(actor_user_id);
CREATE INDEX IF NOT EXISTS idx_forensic_ledger_category ON audit.forensic_ledger(category);
CREATE INDEX IF NOT EXISTS idx_forensic_ledger_table_record ON audit.forensic_ledger(schema_name, table_name, record_id);
CREATE INDEX IF NOT EXISTS idx_forensic_ledger_timestamp ON audit.forensic_ledger(event_timestamp DESC);


-- -----------------------------------------------------------------------------
-- 2. Function: audit.fn_append_forensic_ledger_entry
-- Purpose: Atomically computes SHA-256 hash linking the previous record's hash
--          to current event parameters before appending to the audit ledger.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_append_forensic_ledger_entry(
    p_actor_user_id UUID,
    p_actor_ip VARCHAR(45),
    p_category audit.forensic_category,
    p_action VARCHAR(100),
    p_schema_name VARCHAR(64),
    p_table_name VARCHAR(64),
    p_record_id UUID,
    p_payload_before JSONB DEFAULT NULL,
    p_payload_after JSONB DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
    v_prev_hash VARCHAR(64);
    v_curr_hash VARCHAR(64);
    v_timestamp TIMESTAMP WITH TIME ZONE := NOW();
    v_inserted_id BIGINT;
BEGIN
    -- Acquire last chained hash with exclusive sequence locking
    SELECT current_hash INTO v_prev_hash
    FROM audit.forensic_ledger
    ORDER BY id DESC
    LIMIT 1
    FOR UPDATE;

    IF v_prev_hash IS NULL THEN
        v_prev_hash := '0000000000000000000000000000000000000000000000000000000000000000';
    END IF;

    -- Compute cryptographic SHA-256 block hash
    v_curr_hash := encode(
        digest(
            v_prev_hash || 
            v_timestamp::text || 
            COALESCE(p_actor_user_id::text, '') || 
            COALESCE(p_action, '') || 
            COALESCE(p_schema_name, '') || 
            COALESCE(p_table_name, '') || 
            COALESCE(p_record_id::text, '') || 
            COALESCE(p_payload_after::text, ''),
            'sha256'
        ),
        'hex'
    );

    -- Insert chained audit record
    INSERT INTO audit.forensic_ledger (
        event_timestamp,
        actor_user_id,
        actor_ip_address,
        category,
        action,
        schema_name,
        table_name,
        record_id,
        payload_before,
        payload_after,
        previous_hash,
        current_hash
    ) VALUES (
        v_timestamp,
        p_actor_user_id,
        p_actor_ip,
        p_category,
        p_action,
        p_schema_name,
        p_table_name,
        p_record_id,
        p_payload_before,
        p_payload_after,
        v_prev_hash,
        v_curr_hash
    )
    RETURNING id INTO v_inserted_id;

    RETURN v_inserted_id;
END;
$$;


-- -----------------------------------------------------------------------------
-- 3. Function: audit.fn_verify_forensic_ledger_integrity
-- Purpose: Scans the cryptographic hash chain sequentially to verify ledger
--          integrity and isolate index points of manual tampering or truncation.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_verify_forensic_ledger_integrity()
RETURNS TABLE (
    is_valid BOOLEAN,
    failed_at_id BIGINT,
    failure_reason VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_rec RECORD;
    v_expected_prev_hash VARCHAR(64) := '0000000000000000000000000000000000000000000000000000000000000000';
    v_recalculated_hash VARCHAR(64);
BEGIN
    FOR v_rec IN 
        SELECT * FROM audit.forensic_ledger ORDER BY id ASC
    LOOP
        -- Verify previous hash continuity
        IF v_rec.previous_hash <> v_expected_prev_hash THEN
            RETURN QUERY SELECT FALSE, v_rec.id, format('Broken hash chain. Expected previous: %s, Found: %s', v_expected_prev_hash, v_rec.previous_hash)::VARCHAR(255);
            RETURN;
        END IF;

        -- Recalculate hash for current node
        v_recalculated_hash := encode(
            digest(
                v_rec.previous_hash || 
                v_rec.event_timestamp::text || 
                COALESCE(v_rec.actor_user_id::text, '') || 
                COALESCE(v_rec.action, '') || 
                COALESCE(v_rec.schema_name, '') || 
                COALESCE(v_rec.table_name, '') || 
                COALESCE(v_rec.record_id::text, '') || 
                COALESCE(v_rec.payload_after::text, ''),
                'sha256'
            ),
            'hex'
        );

        -- Verify node payload hash matches stored hash
        IF v_rec.current_hash <> v_recalculated_hash THEN
            RETURN QUERY SELECT FALSE, v_rec.id, format('Hash mismatch at ID %s. Recorded: %s, Calculated: %s', v_rec.id, v_rec.current_hash, v_recalculated_hash)::VARCHAR(255);
            RETURN;
        END IF;

        v_expected_prev_hash := v_rec.current_hash;
    END LOOP;

    RETURN QUERY SELECT TRUE, NULL::BIGINT, 'Ledger cryptographic integrity verified successfully.'::VARCHAR(255);
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. View: audit.v_forensic_ledger_overview
-- Purpose: Consolidates forensic logs, actor user profiles, and operational details.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW audit.v_forensic_ledger_overview AS
SELECT 
    l.id AS ledger_id,
    l.event_timestamp,
    l.actor_user_id,
    u.full_name AS actor_name,
    u.email AS actor_email,
    r.name AS actor_role,
    l.actor_ip_address,
    l.category,
    l.action,
    l.schema_name,
    l.table_name,
    l.record_id,
    l.payload_before,
    l.payload_after,
    l.current_hash
FROM 
    audit.forensic_ledger l
LEFT JOIN 
    auth.users u ON l.actor_user_id = u.id
LEFT JOIN 
    auth.roles r ON u.role_id = r.id
ORDER BY 
    l.id DESC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE audit.forensic_ledger ENABLE ROW LEVEL SECURITY;

-- Read Access: Restricted exclusively to Auditors and System Administrators
DROP POLICY IF EXISTS p_forensic_ledger_read ON audit.forensic_ledger;
CREATE POLICY p_forensic_ledger_read ON audit.forensic_ledger
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

-- Write/Update Restrictions: Block manual client mutations (appends routed through SECURITY DEFINER RPC functions)
REVOKE UPDATE, DELETE ON audit.forensic_ledger FROM PUBLIC;

COMMIT;