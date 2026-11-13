-- =============================================================================
-- LabTrack Migration Script: 006_forensics.sql
-- Database Engine: PostgreSQL 14+
-- Description: Digital forensics tracking, tamper-evident audit hashing,
--              chain-of-custody logging, security event monitoring, and
--              immutable cryptographic telemetry integrity verification.
-- =============================================================================

BEGIN;

-- Enable UUID and pgcrypto extensions for forensic SHA-256 hashing
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- -----------------------------------------------------------------------------
-- 1. Table: forensic_audit_chain
-- Purpose: Provides an immutable append-only ledger for critical security actions.
--          Implements a cryptographic block-chaining mechanism (prev_hash -> current_hash)
--          to ensure detection of unauthorized direct database tampering.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forensic_audit_chain (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_id UUID NOT NULL DEFAULT uuid_generate_v4(),
    actor_id UUID REFERENCES users(id) ON DELETE SET NULL,
    actor_ip VARCHAR(45) NOT NULL,
    action_category VARCHAR(80) NOT NULL CHECK (
        action_category IN (
            'AUTH_FAILURE', 'PRIVILEGE_ELEVATION', 'DATA_EXPORT', 
            'CONFIG_MUTATION', 'HARDWARE_DISPATCH', 'SAFETY_OVERRIDE'
        )
    ),
    resource_target VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    prev_hash VARCHAR(64) NOT NULL,
    current_hash VARCHAR(64) NOT NULL,
    recorded_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for rapid chronological and actor forensic investigations
CREATE INDEX IF NOT EXISTS idx_forensic_chain_recorded_at 
    ON forensic_audit_chain(recorded_at DESC);

CREATE INDEX IF NOT EXISTS idx_forensic_chain_actor_id 
    ON forensic_audit_chain(actor_id);

CREATE INDEX IF NOT EXISTS idx_forensic_chain_category 
    ON forensic_audit_chain(action_category);


-- -----------------------------------------------------------------------------
-- Function: fn_append_forensic_chain_entry
-- Purpose: Atomically computes the SHA-256 hash of the payload merged with the
--          previous row's hash, appending an unalterable block to the ledger.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_append_forensic_chain_entry(
    p_actor_id UUID,
    p_actor_ip VARCHAR(45),
    p_action_category VARCHAR(80),
    p_resource_target VARCHAR(255),
    p_payload JSONB
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
    v_prev_hash VARCHAR(64);
    v_current_hash VARCHAR(64);
    v_new_id BIGINT;
    v_timestamp TIMESTAMP WITH TIME ZONE := NOW();
BEGIN
    -- Acquire last hash in sequence; default to genesis string if chain is empty
    SELECT current_hash INTO v_prev_hash
    FROM forensic_audit_chain
    ORDER BY id DESC
    LIMIT 1;

    IF v_prev_hash IS NULL THEN
        v_prev_hash := 'GENESIS_00000000000000000000000000000000000000000000000000000000';
    END IF;

    -- Compute cryptographic hash over entry components
    v_current_hash := encode(
        digest(
            v_prev_hash || 
            COALESCE(p_actor_id::text, 'SYSTEM') || 
            p_actor_ip || 
            p_action_category || 
            p_resource_target || 
            p_payload::text || 
            v_timestamp::text,
            'sha256'
        ),
        'hex'
    );

    -- Insert record into append-only ledger
    INSERT INTO forensic_audit_chain (
        actor_id,
        actor_ip,
        action_category,
        resource_target,
        payload,
        prev_hash,
        current_hash,
        recorded_at
    ) VALUES (
        p_actor_id,
        p_actor_ip,
        p_action_category,
        p_resource_target,
        p_payload,
        v_prev_hash,
        v_current_hash,
        v_timestamp
    )
    RETURNING id INTO v_new_id;

    RETURN v_new_id;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_verify_forensic_chain_integrity
-- Purpose: Audit validation routine that recalculates the cryptographic chain
--          from entry 1 to tail to detect manual SQL modification or row deletion.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_verify_forensic_chain_integrity()
RETURNS TABLE (
    is_valid BOOLEAN,
    tampered_entry_id BIGINT,
    expected_hash VARCHAR(64),
    actual_hash VARCHAR(64),
    total_entries_verified BIGINT
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_rec RECORD;
    v_calculated_hash VARCHAR(64);
    v_running_prev_hash VARCHAR(64) := 'GENESIS_00000000000000000000000000000000000000000000000000000000';
    v_count BIGINT := 0;
BEGIN
    FOR v_rec IN 
        SELECT id, actor_id, actor_ip, action_category, resource_target, payload, prev_hash, current_hash, recorded_at
        FROM forensic_audit_chain
        ORDER BY id ASC
    LOOP
        v_count := v_count + 1;

        -- Verify previous hash link integrity
        IF v_rec.prev_hash <> v_running_prev_hash THEN
            RETURN QUERY SELECT FALSE, v_rec.id, v_running_prev_hash, v_rec.prev_hash, v_count;
            RETURN;
        END IF;

        -- Recalculate SHA-256 for current row
        v_calculated_hash := encode(
            digest(
                v_rec.prev_hash || 
                COALESCE(v_rec.actor_id::text, 'SYSTEM') || 
                v_rec.actor_ip || 
                v_rec.action_category || 
                v_rec.resource_target || 
                v_rec.payload::text || 
                v_rec.recorded_at::text,
                'sha256'
            ),
            'hex'
        );

        -- Verify stored hash matches computed hash
        IF v_rec.current_hash <> v_calculated_hash THEN
            RETURN QUERY SELECT FALSE, v_rec.id, v_calculated_hash, v_rec.current_hash, v_count;
            RETURN;
        END IF;

        v_running_prev_hash := v_rec.current_hash;
    END LOOP;

    RETURN QUERY SELECT TRUE, NULL::BIGINT, NULL::VARCHAR(64), NULL::VARCHAR(64), v_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 2. Table: hardware_chain_of_custody
-- Purpose: Legal/Compliance chain-of-custody tracking for forensic assets,
--          high-value lab equipment, and confidential storage media.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS hardware_chain_of_custody (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    released_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    received_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    transfer_reason VARCHAR(255) NOT NULL,
    source_location VARCHAR(150) NOT NULL,
    destination_location VARCHAR(150) NOT NULL,
    verification_signature_hash VARCHAR(255) NOT NULL,
    transferred_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chain_custody_asset_id 
    ON hardware_chain_of_custody(asset_id);

CREATE INDEX IF NOT EXISTS idx_chain_custody_transferred_at 
    ON hardware_chain_of_custody(transferred_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Immutability Enforcers & Security Hardening
-- -----------------------------------------------------------------------------

-- Prevent UPDATE or DELETE operations on the cryptographic chain via database rules
CREATE OR REPLACE RULE rule_prevent_forensic_chain_update AS 
    ON UPDATE TO forensic_audit_chain DO INSTEAD NOTHING;

CREATE OR REPLACE RULE rule_prevent_forensic_chain_delete AS 
    ON DELETE TO forensic_audit_chain DO INSTEAD NOTHING;

-- Enable Row Level Security
ALTER TABLE forensic_audit_chain ENABLE ROW LEVEL SECURITY;
ALTER TABLE hardware_chain_of_custody ENABLE ROW LEVEL SECURITY;

-- Read Policy: Admin/Auditor role access only
DROP POLICY IF EXISTS p_forensic_chain_admin_read ON forensic_audit_chain;
CREATE POLICY p_forensic_chain_admin_read ON forensic_audit_chain
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

COMMIT;