-- =============================================================================
-- LabTrack Schema Initialization: 012_maintenance.sql
-- Database Engine: PostgreSQL 14+
-- Module: Preventive Maintenance Schedules, Calibration Logs, & Execution History
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: maintenance.maintenance_schedules
-- Purpose: Configures recurring preventive maintenance, calibration cycles,
--          and mandatory compliance inspection intervals per hardware asset.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.maintenance_schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    schedule_type VARCHAR(40) NOT NULL CHECK (
        schedule_type IN ('Preventive', 'Calibration', 'SafetyInspection', 'DeepClean', 'SoftwarePatch')
    ),
    frequency_interval_days INT NOT NULL CHECK (frequency_interval_days > 0),
    last_performed_at TIMESTAMP WITH TIME ZONE,
    next_due_at TIMESTAMP WITH TIME ZONE NOT NULL,
    assigned_technician_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for rapid schedule evaluation and automated task dispatching
CREATE INDEX IF NOT EXISTS idx_maint_schedules_asset ON maintenance.maintenance_schedules(asset_id);
CREATE INDEX IF NOT EXISTS idx_maint_schedules_next_due ON maintenance.maintenance_schedules(next_due_at ASC);
CREATE INDEX IF NOT EXISTS idx_maint_schedules_active_due ON maintenance.maintenance_schedules(is_active, next_due_at);
CREATE INDEX IF NOT EXISTS idx_maint_schedules_technician ON maintenance.maintenance_schedules(assigned_technician_id);

-- Attach automatic timestamp update trigger
DROP TRIGGER IF EXISTS trg_maint_schedules_update_timestamp ON maintenance.maintenance_schedules;
CREATE TRIGGER trg_maint_schedules_update_timestamp
BEFORE UPDATE ON maintenance.maintenance_schedules
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: maintenance.maintenance_execution_logs
-- Purpose: Immutable record of executed maintenance procedures, technician sign-offs,
--          pass/fail calibration telemetry, and linked work orders.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.maintenance_execution_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    maintenance_schedule_id UUID REFERENCES maintenance.maintenance_schedules(id) ON DELETE SET NULL,
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    performed_by_technician_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    repair_ticket_id UUID REFERENCES maintenance.repair_tickets(id) ON DELETE SET NULL,
    outcome_status maintenance.maintenance_outcome NOT NULL DEFAULT 'Passed',
    calibration_readings JSONB DEFAULT '{}'::jsonb,
    notes TEXT,
    duration_minutes INT DEFAULT 0 CHECK (duration_minutes >= 0),
    performed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_maint_execution_schedule ON maintenance.maintenance_execution_logs(maintenance_schedule_id);
CREATE INDEX IF NOT EXISTS idx_maint_execution_asset ON maintenance.maintenance_execution_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_maint_execution_technician ON maintenance.maintenance_execution_logs(performed_by_technician_id);
CREATE INDEX IF NOT EXISTS idx_maint_execution_performed_at ON maintenance.maintenance_execution_logs(performed_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Function: maintenance.fn_complete_maintenance_task
-- Purpose: Transactionally records completed service actions and advances the
--          next due date on the target maintenance schedule.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION maintenance.fn_complete_maintenance_task(
    p_schedule_id UUID,
    p_technician_id UUID,
    p_outcome_status maintenance.maintenance_outcome DEFAULT 'Passed',
    p_calibration_readings JSONB DEFAULT '{}'::jsonb,
    p_notes TEXT DEFAULT NULL,
    p_duration_minutes INT DEFAULT 0,
    p_repair_ticket_id UUID DEFAULT NULL
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
    -- Retrieve maintenance schedule details with explicit lock
    SELECT id, asset_id, frequency_interval_days, is_active
    INTO v_schedule
    FROM maintenance.maintenance_schedules
    WHERE id = p_schedule_id
    FOR UPDATE;

    IF v_schedule.id IS NULL THEN
        RETURN QUERY SELECT NULL::UUID, NULL::TIMESTAMP WITH TIME ZONE, 'Maintenance schedule not found.'::VARCHAR(255);
        RETURN;
    END IF;

    -- Compute next due timestamp from current execution time
    v_next_due := NOW() + (v_schedule.frequency_interval_days || ' days')::INTERVAL;

    -- Insert execution record
    INSERT INTO maintenance.maintenance_execution_logs (
        id,
        maintenance_schedule_id,
        asset_id,
        performed_by_technician_id,
        repair_ticket_id,
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
        p_repair_ticket_id,
        p_outcome_status,
        p_calibration_readings,
        p_notes,
        p_duration_minutes,
        NOW()
    );

    -- Advance maintenance schedule timeline
    UPDATE maintenance.maintenance_schedules
    SET last_performed_at = NOW(),
        next_due_at = v_next_due,
        updated_at = NOW()
    WHERE id = p_schedule_id;

    -- Automatically restore asset status if calibration/maintenance passed
    IF p_outcome_status = 'Passed' THEN
        UPDATE inventory.assets
        SET status = 'Available',
            updated_at = NOW()
        WHERE id = v_schedule.asset_id
          AND status IN ('Under Maintenance', 'Calibration Required');
    END IF;

    RETURN QUERY SELECT v_log_id, v_next_due, 'Maintenance task logged and schedule updated successfully.'::VARCHAR(255);
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. View: maintenance.v_overdue_maintenance_schedules
-- Purpose: Lists overdue and imminent calibration and preventive maintenance
--          tasks to support operational compliance and technician dispatching.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW maintenance.v_overdue_maintenance_schedules AS
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
    u.id AS technician_id,
    u.full_name AS assigned_technician_name,
    u.email AS assigned_technician_email
FROM 
    maintenance.maintenance_schedules ms
JOIN 
    inventory.assets a ON ms.asset_id = a.id
LEFT JOIN 
    auth.users u ON ms.assigned_technician_id = u.id
WHERE 
    ms.is_active = TRUE
    AND ms.next_due_at < NOW()
ORDER BY 
    ms.next_due_at ASC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.maintenance_schedules ENABLE ROW LEVEL SECURITY;
ALTER TABLE maintenance.maintenance_execution_logs ENABLE ROW LEVEL SECURITY;

-- Read Access: All authenticated users can inspect maintenance status
DROP POLICY IF EXISTS p_maintenance_schedules_read ON maintenance.maintenance_schedules;
CREATE POLICY p_maintenance_schedules_read ON maintenance.maintenance_schedules
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_maintenance_logs_read ON maintenance.maintenance_execution_logs;
CREATE POLICY p_maintenance_logs_read ON maintenance.maintenance_execution_logs
    FOR SELECT
    USING (TRUE);

-- Write Access: Only Admins and Technicians can modify schedules or add execution logs
DROP POLICY IF EXISTS p_maintenance_schedules_write ON maintenance.maintenance_schedules;
CREATE POLICY p_maintenance_schedules_write ON maintenance.maintenance_schedules
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_maintenance_logs_insert ON maintenance.maintenance_execution_logs;
CREATE POLICY p_maintenance_logs_insert ON maintenance.maintenance_execution_logs
    FOR INSERT
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;