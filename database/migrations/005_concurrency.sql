-- =============================================================================
-- LabTrack Migration Script: 005_concurrency.sql
-- Database Engine: PostgreSQL 14+
-- Description: Optimistic concurrency control, row-level locking utilities,
--              deadlock-safe asset dispatching, and atomic reservation queues.
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. Optimistic Concurrency Control (OCC) Version Columns
-- -----------------------------------------------------------------------------

-- Add version column to core assets for OCC updates
ALTER TABLE assets 
ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1;

-- Add version column to reservation schedules
ALTER TABLE reservation_schedules 
ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1;

-- Add version column to universal tickets
ALTER TABLE universal_tickets 
ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1;


-- -----------------------------------------------------------------------------
-- Trigger Function: fn_increment_entity_version
-- Purpose: Auto-increments the record version integer on every update statement
--          to guarantee state consistency across concurrent API workers.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_increment_entity_version()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.version := OLD.version + 1;
    RETURN NEW;
END;
$$;

-- Attach version triggers across mutable entities
DROP TRIGGER IF EXISTS trg_assets_increment_version ON assets;
CREATE TRIGGER trg_assets_increment_version
BEFORE UPDATE ON assets
FOR EACH ROW
EXECUTE FUNCTION fn_increment_entity_version();

DROP TRIGGER IF EXISTS trg_reservations_increment_version ON reservation_schedules;
CREATE TRIGGER trg_reservations_increment_version
BEFORE UPDATE ON reservation_schedules
FOR EACH ROW
EXECUTE FUNCTION fn_increment_entity_version();

DROP TRIGGER IF EXISTS trg_universal_tickets_increment_version ON universal_tickets;
CREATE TRIGGER trg_universal_tickets_increment_version
BEFORE UPDATE ON universal_tickets
FOR EACH ROW
EXECUTE FUNCTION fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 2. Function: fn_atomic_asset_status_transition
-- Purpose: Safely mutates asset operational status using Optimistic Concurrency
--          Control (OCC). Raises exception if concurrent worker modified row.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_atomic_asset_status_transition(
    p_asset_id UUID,
    p_target_status VARCHAR(50),
    p_expected_version INT
)
RETURNS TABLE (
    success BOOLEAN,
    new_version INT,
    current_status VARCHAR(50),
    message VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_actual_version INT;
    v_current_status VARCHAR(50);
BEGIN
    -- Query current asset state
    SELECT version, status INTO v_actual_version, v_current_status
    FROM assets
    WHERE id = p_asset_id
    FOR UPDATE; -- Explicit row lock to prevent race condition during evaluation

    IF v_actual_version IS NULL THEN
        RETURN QUERY SELECT FALSE, 0, NULL::VARCHAR(50), 'Asset not found.'::VARCHAR(255);
        RETURN;
    END IF;

    -- Verify OCC version match
    IF v_actual_version <> p_expected_version THEN
        RETURN QUERY SELECT 
            FALSE, 
            v_actual_version, 
            v_current_status, 
            format('Conflict: Entity was modified by another transaction. Expected version %s, found %s.', p_expected_version, v_actual_version)::VARCHAR(255);
        RETURN;
    END IF;

    -- Update state and trigger version increment
    UPDATE assets
    SET status = p_target_status,
        updated_at = NOW()
    WHERE id = p_asset_id AND version = p_expected_version
    RETURNING version, status INTO v_actual_version, v_current_status;

    RETURN QUERY SELECT TRUE, v_actual_version, v_current_status, 'Asset status updated successfully.'::VARCHAR(255);
END;
$$;


-- -----------------------------------------------------------------------------
-- 3. Function: fn_acquire_asset_for_dispatch
-- Purpose: Deadlock-safe asset acquisition using NOWAIT or SKIP LOCKED for 
--          automated task assignment and background queue processors.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_acquire_asset_for_dispatch(
    p_category VARCHAR(100)
)
RETURNS TABLE (
    acquired_asset_id UUID,
    asset_name VARCHAR(255),
    serial_number VARCHAR(100)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_asset_id UUID;
    v_name VARCHAR(255);
    v_serial VARCHAR(100);
BEGIN
    -- Select and lock the first available asset in category, skipping locked rows
    SELECT id, name, serial_number
    INTO v_asset_id, v_name, v_serial
    FROM assets
    WHERE category = p_category
      AND status = 'Available'
    ORDER BY created_at ASC
    FOR UPDATE SKIP LOCKED
    LIMIT 1;

    IF v_asset_id IS NOT NULL THEN
        -- Mark as allocated
        UPDATE assets
        SET status = 'In Use',
            updated_at = NOW()
        WHERE id = v_asset_id;

        RETURN QUERY SELECT v_asset_id, v_name, v_serial;
    END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: fn_process_pending_reservations_batch
-- Purpose: Concurrent queue worker function that locks and batch-approves pending
--          reservations without causing lock contention or database deadlocks.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_process_pending_reservations_batch(
    p_batch_size INT DEFAULT 10
)
RETURNS TABLE (
    processed_count INT,
    failed_count INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_reservation RECORD;
    v_success_count INT := 0;
    v_fail_count INT := 0;
    v_is_available BOOLEAN;
BEGIN
    -- Iterate through locked pending reservations
    FOR v_reservation IN 
        SELECT id, asset_id, start_time, end_time
        FROM reservation_schedules
        WHERE status = 'Pending'
        ORDER BY created_at ASC
        FOR UPDATE SKIP LOCKED
        LIMIT p_batch_size
    LOOP
        -- Check availability using conflict function
        SELECT is_available INTO v_is_available
        FROM fn_check_reservation_availability(
            v_reservation.asset_id, 
            v_reservation.start_time, 
            v_reservation.end_time, 
            v_reservation.id
        );

        IF v_is_available THEN
            UPDATE reservation_schedules
            SET status = 'Approved',
                updated_at = NOW()
            WHERE id = v_reservation.id;
            v_success_count := v_success_count + 1;
        ELSE
            UPDATE reservation_schedules
            SET status = 'Rejected',
                cancellation_reason = 'Automated system rejection: Conflicting reservation detected.',
                updated_at = NOW()
            WHERE id = v_reservation.id;
            v_fail_count := v_fail_count + 1;
        END IF;
    END LOOP;

    RETURN QUERY SELECT v_success_count, v_fail_count;
END;
$$;

COMMIT;