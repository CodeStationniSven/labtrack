-- =============================================================================
-- LabTrack Preventive Maintenance, Calibration & Service Schedules Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Recurring Maintenance Schedules, Calibration Runs, & Service Compliance
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: maintenance_schedules
-- Purpose: Defines recurring preventive maintenance and calibration intervals
--          for equipment assets (e.g., weekly, monthly, annual routines).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance_schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    schedule_type VARCHAR(40) NOT NULL CHECK (
        schedule_type IN ('Preventive', 'Calibration', 'SafetyInspection', 'DeepClean', 'SoftwarePatch')
    ),
    frequency_interval_days INT NOT NULL CHECK (frequency_interval_days > 0),
    last_performed_at TIMESTAMP WITH TIME ZONE,
    next_due_at TIMESTAMP WITH TIME ZONE NOT NULL,
    assigned_technician_id UUID REFERENCES users(id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_maint_schedules_asset ON maintenance_schedules(asset_id);
CREATE INDEX IF NOT EXISTS idx_maint_schedules_next_due ON maintenance_schedules(next_due_at ASC);
CREATE INDEX IF NOT EXISTS idx_maint_schedules_active_due ON maintenance_schedules(is_active, next_due_at);

-- Attach updated_at timestamp trigger
DROP TRIGGER IF EXISTS trg_maint_schedules_update_timestamp ON maintenance_schedules;
CREATE TRIGGER trg_maint_schedules_update_timestamp
BEFORE UPDATE ON maintenance_schedules
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- Table: maintenance_execution_logs
-- Purpose: Records completed or attempted maintenance procedures, operational 
--          outcomes, pass/fail calibration states, and technician sign-offs.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance_execution_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    maintenance_schedule_id UUID REFERENCES maintenance_schedules(id) ON DELETE SET NULL,
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    performed_by_technician_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    universal_ticket_id UUID REFERENCES universal_tickets(id) ON DELETE SET NULL,
    outcome_status VARCHAR(30) NOT NULL DEFAULT 'Passed' CHECK (
        outcome_status IN ('Passed', 'Failed', 'Passed-With-Conditions', 'Incomplete')
    ),
    calibration_readings JSONB DEFAULT '{}'::jsonb,
    notes TEXT,
    duration_minutes INT DEFAULT 0,
    performed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_maint_execution_schedule ON maintenance_execution_logs(maintenance_schedule_id);
CREATE INDEX IF NOT EXISTS idx_maint_execution_asset ON maintenance_execution_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_maint_execution_performed_at ON maintenance_execution_logs(performed_at DESC);


-- -----------------------------------------------------------------------------
-- Function: fn_complete_maintenance_task
-- Purpose: Logs a completed maintenance/calibration task and automatically
--          recalculates the next due date based on the defined schedule interval.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_complete_maintenance_task(
    p_schedule_id UUID,
    p_technician_id UUID,
    p_outcome_status VARCHAR(30) DEFAULT 'Passed',
    p_calibration_readings JSONB DEFAULT '{}'::jsonb,
    p_notes TEXT DEFAULT NULL,
    p_duration_minutes INT DEFAULT 0,
    p_ticket_id UUID DEFAULT NULL
)
RETURNS TABLE (
    log_id UUID,
    next_due_at TIMESTAMP WITH TIME ZONE,
    message VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_schedule RECORD;
    v_log_id UUID := uuid_generate_v4();
    v_next_due TIMESTAMP WITH TIME ZONE;
BEGIN
    -- Query maintenance schedule details
    SELECT id, asset_id, frequency_interval_days, is_active
    INTO v_schedule
    FROM maintenance_schedules
    WHERE id = p_schedule_id
    FOR UPDATE;

    IF v_schedule.id IS NULL THEN
        RETURN QUERY SELECT NULL::UUID, NULL::TIMESTAMP WITH TIME ZONE, 'Maintenance schedule not found.'::VARCHAR(255);
        RETURN;
    END IF;

    -- Compute next due timestamp from current execution time
    v_next_due := NOW() + (v_schedule.frequency_interval_days || ' days')::INTERVAL;

    -- Record execution log entry
    INSERT INTO maintenance_execution_logs (
        id,
        maintenance_schedule_id,
        asset_id,
        performed_by_technician_id,
        universal_ticket_id,
        outcome_status,
        calibration_readings,
        notes,
        duration_minutes,
        performed_at
    ) VALUES (
        v_log_id,
        p_schedule_id,
        v_schedule.asset_id,
        p_technician_id,
        p_ticket_id,
        p_outcome_status,
        p_calibration_readings,
        p_notes,
        p_duration_minutes,
        NOW()
    );

    -- Recalculate schedule interval timestamps
    UPDATE maintenance_schedules
    SET last_performed_at = NOW(),
        next_due_at = v_next_due,
        updated_at = NOW()
    WHERE id = p_schedule_id;

    RETURN QUERY SELECT v_log_id, v_next_due, 'Maintenance task logged and schedule updated successfully.'::VARCHAR(255);
END;
$$;


-- -----------------------------------------------------------------------------
-- View: v_overdue_maintenance_schedules
-- Purpose: Identifies all equipment assets with overdue or impending service
--          schedules to feed technician dispatch dashboards and system alerts.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_overdue_maintenance_schedules AS
SELECT 
    ms.id AS schedule_id,
    ms.asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.location_bay,
    ms.title AS task_title,
    ms.schedule_type,
    ms.frequency_interval_days,
    ms.last_performed_at,
    ms.next_due_at,
    ROUND(EXTRACT(EPOCH FROM (NOW() - ms.next_due_at)) / 86400.0, 1) AS days_overdue,
    u.full_name AS assigned_technician_name,
    u.email AS assigned_technician_email
FROM 
    maintenance_schedules ms
JOIN 
    assets a ON ms.asset_id = a.id
LEFT JOIN 
    users u ON ms.assigned_technician_id = u.id
WHERE 
    ms.is_active = TRUE
    AND ms.next_due_at < NOW()
ORDER BY 
    ms.next_due_at ASC;


-- -----------------------------------------------------------------------------
-- Row Level Security (RLS) Policies
-- -----------------------------------------------------------------------------

ALTER TABLE maintenance_schedules ENABLE ROW LEVEL SECURITY;
ALTER TABLE maintenance_execution_logs ENABLE ROW LEVEL SECURITY;

-- Read Policy: All authenticated users can inspect active maintenance status
DROP POLICY IF EXISTS p_maintenance_schedules_read ON maintenance_schedules;
CREATE POLICY p_maintenance_schedules_read ON maintenance_schedules
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_maintenance_logs_read ON maintenance_execution_logs;
CREATE POLICY p_maintenance_logs_read ON maintenance_execution_logs
    FOR SELECT
    USING (TRUE);

COMMIT;