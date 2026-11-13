-- =============================================================================
-- LabTrack Equipment Reservations & Booking Schedules Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Asset Booking, Time Slot Allocation, Conflict Resolution & Overlap Prevention
-- =============================================================================

BEGIN;

-- Enable UUID and btree_gist extensions for temporal exclusion constraints
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- -----------------------------------------------------------------------------
-- Table: reservation_schedules
-- Purpose: Manages researcher time-slot bookings for lab equipment, enforcing
--          non-overlapping reservations using PostgreSQL exclusion constraints.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS reservation_schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    reserved_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    approved_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    reservation_code VARCHAR(50) NOT NULL UNIQUE,
    purpose VARCHAR(255) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK (
        status IN ('Pending', 'Approved', 'Active', 'Completed', 'Cancelled', 'Rejected')
    ),
    cancellation_reason VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT chk_reservation_time_range CHECK (end_time > start_time),
    -- Enforces no overlapping active/approved reservations for the same asset
    EXCLUDE USING gist (
        asset_id WITH =,
        tstzrange(start_time, end_time, '[)') WITH &&
    ) WHERE (status IN ('Approved', 'Active'))
);

CREATE INDEX IF NOT EXISTS idx_reservations_asset_id ON reservation_schedules(asset_id);
CREATE INDEX IF NOT EXISTS idx_reservations_user_id ON reservation_schedules(reserved_by_user_id);
CREATE INDEX IF NOT EXISTS idx_reservations_status ON reservation_schedules(status);
CREATE INDEX IF NOT EXISTS idx_reservations_time_range ON reservation_schedules(start_time, end_time);

-- Trigger to auto-generate human-readable reservation numbers (e.g., RES-20261021-XXXX)
CREATE OR REPLACE FUNCTION fn_generate_reservation_code()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.reservation_code IS NULL OR NEW.reservation_code = '' THEN
        NEW.reservation_code := 'RES-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_reservation_code ON reservation_schedules;
CREATE TRIGGER trg_set_reservation_code
BEFORE INSERT ON reservation_schedules
FOR EACH ROW
EXECUTE FUNCTION fn_generate_reservation_code();

-- -----------------------------------------------------------------------------
-- Function: fn_check_reservation_availability
-- Purpose: Evaluates if a specified equipment asset is available for booking 
--          within a target time range prior to submitting a request.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_check_reservation_availability(
    p_asset_id UUID,
    p_start_time TIMESTAMP WITH TIME ZONE,
    p_end_time TIMESTAMP WITH TIME ZONE,
    p_exclude_reservation_id UUID DEFAULT NULL
)
RETURNS TABLE (
    is_available BOOLEAN,
    conflict_count BIGINT,
    message VARCHAR(255)
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_conflicts BIGINT;
    v_asset_status VARCHAR(50);
BEGIN
    -- Verify physical asset state
    SELECT status INTO v_asset_status FROM assets WHERE id = p_asset_id;

    IF v_asset_status IS NULL THEN
        RETURN QUERY SELECT FALSE, 0::BIGINT, 'Target asset does not exist.'::VARCHAR(255);
        RETURN;
    ELSIF v_asset_status = 'Under Maintenance' OR v_asset_status = 'Decommissioned' THEN
        RETURN QUERY SELECT FALSE, 0::BIGINT, format('Asset is currently %s.', v_asset_status)::VARCHAR(255);
        RETURN;
    END IF;

    -- Count overlapping approved or active bookings
    SELECT COUNT(*) INTO v_conflicts
    FROM reservation_schedules
    WHERE asset_id = p_asset_id
      AND status IN ('Approved', 'Active')
      AND tstzrange(start_time, end_time, '[)') && tstzrange(p_start_time, p_end_time, '[)')
      AND (p_exclude_reservation_id IS NULL OR id <> p_exclude_reservation_id);

    IF v_conflicts > 0 THEN
        RETURN QUERY SELECT FALSE, v_conflicts, 'Time slot conflicts with an existing reservation.'::VARCHAR(255);
    ELSE
        RETURN QUERY SELECT TRUE, 0::BIGINT, 'Slot is available for reservation.'::VARCHAR(255);
    END IF;
END;
$$;

COMMIT;