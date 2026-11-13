-- =============================================================================
-- LabTrack Audit Stored Procedures & Log Processing Engine
-- Database Engine: PostgreSQL 14+
-- Module: High-Volume Audit Trail Aggregation, Log Rotation & Archival
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Procedure: sp_archive_historical_audits
-- Purpose: Safely moves audit trail records older than a retention threshold
--          from high-volume tables (e.g., assets_audit, repair_tickets_audit)
--          into compressed cold-storage tables to maintain OLTP query speed.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE sp_archive_historical_audits(
    p_retention_days INT DEFAULT 180,
    INOUT p_archived_count INT DEFAULT 0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_cutoff_date TIMESTAMP WITH TIME ZONE;
    v_assets_count INT := 0;
    v_repair_count INT := 0;
BEGIN
    v_cutoff_date := NOW() - (p_retention_days || ' days')::INTERVAL;

    -- Ensure cold storage archive tables exist
    CREATE TABLE IF NOT EXISTS assets_audit_archive (
        LIKE assets_audit INCLUDING ALL
    );

    CREATE TABLE IF NOT EXISTS repair_tickets_audit_archive (
        LIKE repair_tickets_audit INCLUDING ALL
    );

    -- 1. Archive assets_audit records
    WITH moved_rows AS (
        DELETE FROM assets_audit
        WHERE performed_at < v_cutoff_date
        RETURNING *
    )
    INSERT INTO assets_audit_archive
    SELECT * FROM moved_rows;

    GET DIAGNOSTICS v_assets_count = ROW_COUNT;

    -- 2. Archive repair_tickets_audit records
    WITH moved_rows AS (
        DELETE FROM repair_tickets_audit
        WHERE performed_at < v_cutoff_date
        RETURNING *
    )
    INSERT INTO repair_tickets_audit_archive
    SELECT * FROM moved_rows;

    GET DIAGNOSTICS v_repair_count = ROW_COUNT;

    p_archived_count := v_assets_count + v_repair_count;

    RAISE NOTICE 'Audit Archival Complete. Retained records newer than %. Total rows archived: % (Assets: %, Repair: %)', 
        v_cutoff_date, p_archived_count, v_assets_count, v_repair_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_get_entity_audit_history
-- Purpose: Unified point-in-time timeline query for tracked entity mutations across
--          the system, returning delta states in chronological order.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_entity_audit_history(
    p_entity_table VARCHAR(63),
    p_entity_id UUID
)
RETURNS TABLE (
    audit_id UUID,
    entity_id UUID,
    action_type VARCHAR(20),
    changed_by VARCHAR(255),
    change_payload JSONB,
    performed_at TIMESTAMP WITH TIME ZONE
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    CASE LOWER(p_entity_table)
        WHEN 'assets' THEN
            RETURN QUERY
            SELECT a.id, a.entity_id, a.action_type, a.changed_by, a.change_payload, a.performed_at
            FROM assets_audit a
            WHERE a.entity_id = p_entity_id
            ORDER BY a.performed_at DESC;

        WHEN 'repair_tickets' THEN
            RETURN QUERY
            SELECT r.id, r.entity_id, r.action_type, r.changed_by, r.change_payload, r.performed_at
            FROM repair_tickets_audit r
            WHERE r.entity_id = p_entity_id
            ORDER BY r.performed_at DESC;

        ELSE
            RAISE EXCEPTION 'Unsupported audit entity table: %', p_entity_table
                USING HINT = 'Supported tables are: assets, repair_tickets';
    END CASE;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_generate_user_activity_audit_report
-- Purpose: Aggregates system interactions, mutations, and authentication attempts
--          per user across audit logs and forensic entries for compliance reviews.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_generate_user_activity_audit_report(
    p_user_id UUID,
    p_start_date TIMESTAMP WITH TIME ZONE DEFAULT (NOW() - INTERVAL '30 days'),
    p_end_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
    activity_source VARCHAR(50),
    action_type VARCHAR(80),
    target_resource VARCHAR(255),
    details JSONB,
    occurred_at TIMESTAMP WITH TIME ZONE
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_user_email VARCHAR(255);
BEGIN
    -- Fetch target user email for textual audit logs
    SELECT email INTO v_user_email FROM users WHERE id = p_user_id;

    RETURN QUERY
    -- Forensic Chain Events
    SELECT 
        'Forensic Ledger'::VARCHAR(50) AS activity_source,
        f.action_category::VARCHAR(80) AS action_type,
        f.resource_target::VARCHAR(255) AS target_resource,
        f.payload AS details,
        f.recorded_at AS occurred_at
    FROM forensic_audit_chain f
    WHERE f.actor_id = p_user_id
      AND f.recorded_at BETWEEN p_start_date AND p_end_date

    UNION ALL

    -- Assets Audit Mutations
    SELECT 
        'Assets Audit'::VARCHAR(50) AS activity_source,
        aa.action_type::VARCHAR(80) AS action_type,
        ('assets/' || aa.entity_id::text)::VARCHAR(255) AS target_resource,
        aa.change_payload AS details,
        aa.performed_at AS occurred_at
    FROM assets_audit aa
    WHERE (aa.changed_by = p_user_id::text OR aa.changed_by = v_user_email)
      AND aa.performed_at BETWEEN p_start_date AND p_end_date

    UNION ALL

    -- Ticket Activity Logs
    SELECT 
        'Ticket Audit'::VARCHAR(50) AS activity_source,
        t.activity_type::VARCHAR(80) AS action_type,
        ('tickets/' || t.ticket_id::text)::VARCHAR(255) AS target_resource,
        jsonb_build_object('note', t.note, 'prev_status', t.previous_status, 'new_status', t.new_status) AS details,
        t.created_at AS occurred_at
    FROM ticket_activity_logs t
    WHERE t.performed_by_user_id = p_user_id
      AND t.created_at BETWEEN p_start_date AND p_end_date

    ORDER BY occurred_at DESC;
END;
$$;

COMMIT;