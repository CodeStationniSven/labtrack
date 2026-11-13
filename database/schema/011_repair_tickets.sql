-- =============================================================================
-- LabTrack Schema Initialization: 011_repair_tickets.sql
-- Database Engine: PostgreSQL 14+
-- Module: Work Orders, Diagnostic Repairs, Technician Assignments, & Activity Logs
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: maintenance.repair_tickets
-- Purpose: Formal maintenance work orders tracking equipment repair lifecycle,
--          technician dispatch, resolution notes, and downtime metrics.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.repair_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    problem_report_id UUID REFERENCES maintenance.problem_reports(id) ON DELETE SET NULL,
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    assigned_technician_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    created_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    priority maintenance.ticket_priority NOT NULL DEFAULT 'Medium',
    status maintenance.ticket_status NOT NULL DEFAULT 'Open',
    estimated_cost_usd NUMERIC(12, 2) DEFAULT 0.00,
    actual_cost_usd NUMERIC(12, 2) DEFAULT 0.00,
    resolution_notes TEXT,
    started_at TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for fast ticket triage and technician queue management
CREATE INDEX IF NOT EXISTS idx_repair_tickets_asset ON maintenance.repair_tickets(asset_id);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_technician ON maintenance.repair_tickets(assigned_technician_id);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_status ON maintenance.repair_tickets(status);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_priority ON maintenance.repair_tickets(priority);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_created_at ON maintenance.repair_tickets(created_at DESC);

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_repair_tickets_update_timestamp ON maintenance.repair_tickets;
CREATE TRIGGER trg_repair_tickets_update_timestamp
BEFORE UPDATE ON maintenance.repair_tickets
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_repair_tickets_increment_version ON maintenance.repair_tickets;
CREATE TRIGGER trg_repair_tickets_increment_version
BEFORE UPDATE ON maintenance.repair_tickets
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();

-- Audit trigger for historical repair order mutation tracking
DROP TRIGGER IF EXISTS trg_audit_repair_tickets_changes ON maintenance.repair_tickets;
CREATE TRIGGER trg_audit_repair_tickets_changes
AFTER INSERT OR UPDATE OR DELETE ON maintenance.repair_tickets
FOR EACH ROW
EXECUTE FUNCTION public.fn_audit_table_changes();


-- -----------------------------------------------------------------------------
-- 2. Table: maintenance.ticket_activity_logs
-- Purpose: Itemized timeline logging state transitions, technician work notes,
--          part replacements, and communication updates on repair tickets.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.ticket_activity_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES maintenance.repair_tickets(id) ON DELETE CASCADE,
    performed_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    activity_type VARCHAR(80) NOT NULL DEFAULT 'NoteAdded',
    previous_status maintenance.ticket_status,
    new_status maintenance.ticket_status,
    note TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ticket_logs_ticket_id ON maintenance.ticket_activity_logs(ticket_id);
CREATE INDEX IF NOT EXISTS idx_ticket_logs_created_at ON maintenance.ticket_activity_logs(created_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Trigger Function: maintenance.fn_auto_generate_repair_ticket_number
-- Purpose: Generates standardized human-readable repair codes (e.g., REP-20261021-XXXX)
--          upon initial ticket creation if omitted by the client.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION maintenance.fn_auto_generate_repair_ticket_number()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.ticket_number IS NULL OR NEW.ticket_number = '' THEN
        NEW.ticket_number := 'REP-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_repair_ticket_number ON maintenance.repair_tickets;
CREATE TRIGGER trg_set_repair_ticket_number
BEFORE INSERT ON maintenance.repair_tickets
FOR EACH ROW
EXECUTE FUNCTION maintenance.fn_auto_generate_repair_ticket_number();


-- -----------------------------------------------------------------------------
-- 4. View: maintenance.v_active_repair_work_orders
-- Purpose: Comprehensive view aggregating work orders, assigned technicians,
--          downtime calculations, and linked physical asset specifications.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW maintenance.v_active_repair_work_orders AS
SELECT 
    rt.id AS ticket_id,
    rt.ticket_number,
    rt.title,
    rt.priority,
    rt.status,
    rt.asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.location_bay,
    rt.assigned_technician_id,
    tech.full_name AS assigned_technician_name,
    tech.email AS assigned_technician_email,
    rt.created_by_user_id,
    creator.full_name AS created_by_user_name,
    rt.estimated_cost_usd,
    rt.actual_cost_usd,
    rt.started_at,
    CASE 
        WHEN rt.started_at IS NOT NULL AND rt.resolved_at IS NULL 
        THEN ROUND(EXTRACT(EPOCH FROM (NOW() - rt.started_at)) / 3600.0, 2)
        WHEN rt.started_at IS NOT NULL AND rt.resolved_at IS NOT NULL 
        THEN ROUND(EXTRACT(EPOCH FROM (rt.resolved_at - rt.started_at)) / 3600.0, 2)
        ELSE 0.00 
    END AS elapsed_work_hours,
    rt.created_at,
    rt.updated_at
FROM 
    maintenance.repair_tickets rt
JOIN 
    inventory.assets a ON rt.asset_id = a.id
LEFT JOIN 
    auth.users tech ON rt.assigned_technician_id = tech.id
LEFT JOIN 
    auth.users creator ON rt.created_by_user_id = creator.id
WHERE 
    rt.status NOT IN ('Closed', 'Cancelled')
ORDER BY 
    CASE rt.priority 
        WHEN 'Critical' THEN 1 
        WHEN 'High' THEN 2 
        WHEN 'Medium' THEN 3 
        WHEN 'Low' THEN 4 
    END ASC,
    rt.created_at ASC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.repair_tickets ENABLE ROW LEVEL SECURITY;
ALTER TABLE maintenance.ticket_activity_logs ENABLE ROW LEVEL SECURITY;

-- Read Access: Users view tickets they created; Admins, Technicians, and Auditors view all
DROP POLICY IF EXISTS p_repair_tickets_read ON maintenance.repair_tickets;
CREATE POLICY p_repair_tickets_read ON maintenance.repair_tickets
    FOR SELECT
    USING (
        created_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR assigned_technician_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician', 'Auditor')
        )
    );

-- Read Access for Activity Logs
DROP POLICY IF EXISTS p_ticket_activity_logs_read ON maintenance.ticket_activity_logs;
CREATE POLICY p_ticket_activity_logs_read ON maintenance.ticket_activity_logs
    FOR SELECT
    USING (TRUE);

COMMIT;