-- =============================================================================
-- LabTrack Schema Initialization: 009_reservations.sql
-- Database Engine: PostgreSQL 14+
-- Module: Equipment Schedules, Temporal Booking Exclusion, & Approval Workflows
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: scheduling.reservation_schedules
-- Purpose: Manages equipment reservations, calendar allocations, and approval states.
--          Utilizes PostgreSQL GiST temporal exclusion constraints (tstzrange) to 
--          guarantee zero overlapping approved or active bookings at the DB layer.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS scheduling.reservation_schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    reserved_by_user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    approved_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    reservation_code VARCHAR(50) NOT NULL UNIQUE,
    purpose VARCHAR(255) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    status scheduling.reservation_status NOT NULL DEFAULT 'Pending',
    cancellation_reason VARCHAR(255),
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT chk_reservation_time_range CHECK (end_time > start_time),
    EXCLUDE USING gist (
        asset_id WITH =,
        tstzrange(start_time, end_time, '[)') WITH &&
    ) WHERE (status IN ('Approved', 'Active'))
);

-- Indexing for fast booking searches and range filtering
CREATE INDEX IF NOT EXISTS idx_reservations_asset_id ON scheduling.reservation_schedules(asset_id);
CREATE INDEX IF NOT EXISTS idx_reservations_user_id ON scheduling.reservation_schedules(reserved_by_user_id);
CREATE INDEX IF NOT EXISTS idx_reservations_status ON scheduling.reservation_schedules(status);
CREATE INDEX IF NOT EXISTS idx_reservations_time_range ON scheduling.reservation_schedules USING gist (tstzrange(start_time, end_time, '[)'));

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_reservation_schedules_update_timestamp ON scheduling.reservation_schedules;
CREATE TRIGGER trg_reservation_schedules_update_timestamp
BEFORE UPDATE ON scheduling.reservation_schedules
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_reservations_increment_version ON scheduling.reservation_schedules;
CREATE TRIGGER trg_reservations_increment_version
BEFORE UPDATE ON scheduling.reservation_schedules
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 2. Trigger Function: scheduling.fn_auto_generate_reservation_code
-- Purpose: Assigns a human-readable booking code (e.g., RSV-20261021-XXXX)
--          upon initial reservation creation if omitted by the client.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION scheduling.fn_auto_generate_reservation_code()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.reservation_code IS NULL OR NEW.reservation_code = '' THEN
        NEW.reservation_code := 'RSV-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_reservation_code ON scheduling.reservation_schedules;
CREATE TRIGGER trg_set_reservation_code
BEFORE INSERT ON scheduling.reservation_schedules
FOR EACH ROW
EXECUTE FUNCTION scheduling.fn_auto_generate_reservation_code();


-- -----------------------------------------------------------------------------
-- 3. Function: scheduling.fn_check_reservation_availability
-- Purpose: Evaluates whether a target asset is available for a given time window
--          or if a conflict exists, accounting for equipment operational status.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION scheduling.fn_check_reservation_availability(
    p_asset_id UUID,
    p_start_time TIMESTAMP WITH TIME ZONE,
    p_end_time TIMESTAMP WITH TIME ZONE,
    p_ignore_reservation_id UUID DEFAULT NULL
)
RETURNS TABLE (
    is_available BOOLEAN,
    conflict_reservation_id UUID,
    conflict_reason VARCHAR(255)
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_asset_status inventory.asset_status;
    v_conflict_id UUID;
BEGIN
    -- Validate asset status
    SELECT status INTO v_asset_status
    FROM inventory.assets
    WHERE id = p_asset_id;

    IF v_asset_status IS NULL THEN
        RETURN QUERY SELECT FALSE, NULL::UUID, 'Asset does not exist.'::VARCHAR(255);
        RETURN;
    ELSIF v_asset_status IN ('Under Maintenance', 'Decommissioned', 'Unknown') THEN
        RETURN QUERY SELECT FALSE, NULL::UUID, format('Asset is currently %s.', v_asset_status)::VARCHAR(255);
        RETURN;
    END IF;

    -- Check for overlapping active or approved bookings
    SELECT id INTO v_conflict_id
    FROM scheduling.reservation_schedules
    WHERE asset_id = p_asset_id
      AND status IN ('Approved', 'Active')
      AND tstzrange(start_time, end_time, '[)') && tstzrange(p_start_time, p_end_time, '[)')
      AND (p_ignore_reservation_id IS NULL OR id <> p_ignore_reservation_id)
    LIMIT 1;

    IF v_conflict_id IS NOT NULL THEN
        RETURN QUERY SELECT FALSE, v_conflict_id, 'Overlapping approved/active reservation exists.'::VARCHAR(255);
    ELSE
        RETURN QUERY SELECT TRUE, NULL::UUID, 'Time slot is available.'::VARCHAR(255);
    END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. View: scheduling.v_upcoming_reservations_overview
-- Purpose: Aggregates schedule intervals, asset metadata, and researcher profiles.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW scheduling.v_upcoming_reservations_overview AS
SELECT 
    r.id AS reservation_id,
    r.reservation_code,
    r.asset_id,
    a.name AS asset_name,
    a.serial_number,
    r.reserved_by_user_id,
    u.full_name AS reserved_by_user_name,
    u.email AS reserved_by_user_email,
    r.approved_by_user_id,
    app.full_name AS approved_by_user_name,
    r.purpose,
    r.start_time,
    r.end_time,
    ROUND(EXTRACT(EPOCH FROM (r.end_time - r.start_time)) / 3600.0, 2) AS duration_hours,
    r.status,
    r.version
FROM 
    scheduling.reservation_schedules r
JOIN 
    inventory.assets a ON r.asset_id = a.id
JOIN 
    auth.users u ON r.reserved_by_user_id = u.id
LEFT JOIN 
    auth.users app ON r.approved_by_user_id = app.id
ORDER BY 
    r.start_time ASC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE scheduling.reservation_schedules ENABLE ROW LEVEL SECURITY;

-- Read Access: Users can view all reservations to evaluate schedule calendars
DROP POLICY IF EXISTS p_reservations_read ON scheduling.reservation_schedules;
CREATE POLICY p_reservations_read ON scheduling.reservation_schedules
    FOR SELECT
    USING (TRUE);

-- Insert Access: Authenticated users can submit pending reservation requests for themselves
DROP POLICY IF EXISTS p_reservations_insert ON scheduling.reservation_schedules;
CREATE POLICY p_reservations_insert ON scheduling.reservation_schedules
    FOR INSERT
    WITH CHECK (
        reserved_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Update Access: Users modify their own pending requests; Admins/Technicians update any
DROP POLICY IF EXISTS p_reservations_update ON scheduling.reservation_schedules;
CREATE POLICY p_reservations_update ON scheduling.reservation_schedules
    FOR UPDATE
    USING (
        reserved_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;