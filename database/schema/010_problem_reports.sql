-- =============================================================================
-- LabTrack Schema Initialization: 010_problem_reports.sql
-- Database Engine: PostgreSQL 14+
-- Module: Defect Incident Reporting, Safety Hazards, & Triage Workflows
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: maintenance.problem_reports
-- Purpose: Captures user-submitted equipment defects, operational incidents,
--          facility hazards, and EHS safety compliance reports.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.problem_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID REFERENCES inventory.assets(id) ON DELETE SET NULL,
    reported_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    report_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    severity_level maintenance.ticket_priority NOT NULL DEFAULT 'Medium',
    category VARCHAR(80) NOT NULL DEFAULT 'General Defect',
    status maintenance.ticket_status NOT NULL DEFAULT 'Open',
    has_safety_hazard BOOLEAN NOT NULL DEFAULT FALSE,
    attachment_urls TEXT[] DEFAULT '{}',
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for fast incident tracking and triage dashboard filtering
CREATE INDEX IF NOT EXISTS idx_problem_reports_asset_id ON maintenance.problem_reports(asset_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_reported_by ON maintenance.problem_reports(reported_by_user_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_status ON maintenance.problem_reports(status);
CREATE INDEX IF NOT EXISTS idx_problem_reports_severity ON maintenance.problem_reports(severity_level);
CREATE INDEX IF NOT EXISTS idx_problem_reports_safety_hazard ON maintenance.problem_reports(has_safety_hazard) WHERE has_safety_hazard = TRUE;

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_problem_reports_update_timestamp ON maintenance.problem_reports;
CREATE TRIGGER trg_problem_reports_update_timestamp
BEFORE UPDATE ON maintenance.problem_reports
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_problem_reports_increment_version ON maintenance.problem_reports;
CREATE TRIGGER trg_problem_reports_increment_version
BEFORE UPDATE ON maintenance.problem_reports
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 2. Trigger Function: maintenance.fn_auto_generate_report_number
-- Purpose: Generates standardized human-readable report codes (e.g., PR-20261021-XXXX)
--          upon initial incident creation if omitted by the client.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION maintenance.fn_auto_generate_report_number()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.report_number IS NULL OR NEW.report_number = '' THEN
        NEW.report_number := 'PR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_problem_report_number ON maintenance.problem_reports;
CREATE TRIGGER trg_set_problem_report_number
BEFORE INSERT ON maintenance.problem_reports
FOR EACH ROW
EXECUTE FUNCTION maintenance.fn_auto_generate_report_number();


-- -----------------------------------------------------------------------------
-- 3. Trigger Function: maintenance.fn_flag_safety_hazard_asset_status
-- Purpose: Automatically transitions linked asset status to 'Under Maintenance' 
--          and triggers high-priority safety isolation when a hazardous report is filed.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION maintenance.fn_flag_safety_hazard_asset_status()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.has_safety_hazard = TRUE AND NEW.asset_id IS NOT NULL THEN
        UPDATE inventory.assets
        SET status = 'Under Maintenance',
            updated_at = NOW()
        WHERE id = NEW.asset_id
          AND status <> 'Decommissioned';
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_flag_safety_hazard_asset ON maintenance.problem_reports;
CREATE TRIGGER trg_flag_safety_hazard_asset
AFTER INSERT OR UPDATE OF has_safety_hazard ON maintenance.problem_reports
FOR EACH ROW
EXECUTE FUNCTION maintenance.fn_flag_safety_hazard_asset_status();


-- -----------------------------------------------------------------------------
-- 4. View: maintenance.v_open_problem_reports_summary
-- Purpose: Aggregates active incident reports, linked assets, and user profiles
--          for technician and EHS compliance dashboards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW maintenance.v_open_problem_reports_summary AS
SELECT 
    pr.id AS report_id,
    pr.report_number,
    pr.title,
    pr.category,
    pr.severity_level,
    pr.status,
    pr.has_safety_hazard,
    pr.asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.location_bay,
    pr.reported_by_user_id,
    u.full_name AS reported_by_user_name,
    u.email AS reported_by_user_email,
    pr.created_at,
    pr.updated_at
FROM 
    maintenance.problem_reports pr
LEFT JOIN 
    inventory.assets a ON pr.asset_id = a.id
LEFT JOIN 
    auth.users u ON pr.reported_by_user_id = u.id
WHERE 
    pr.status NOT IN ('Closed', 'Rejected', 'Resolved')
ORDER BY 
    pr.has_safety_hazard DESC,
    CASE pr.severity_level 
        WHEN 'Critical' THEN 1 
        WHEN 'High' THEN 2 
        WHEN 'Medium' THEN 3 
        WHEN 'Low' THEN 4 
    END ASC,
    pr.created_at ASC;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.problem_reports ENABLE ROW LEVEL SECURITY;

-- Read Access: Users view their own reports; Admins and Technicians view all reports
DROP POLICY IF EXISTS p_problem_reports_read ON maintenance.problem_reports;
CREATE POLICY p_problem_reports_read ON maintenance.problem_reports
    FOR SELECT
    USING (
        reported_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician', 'Auditor')
        )
    );

-- Insert Access: Any authenticated user can submit an incident report
DROP POLICY IF EXISTS p_problem_reports_insert ON maintenance.problem_reports;
CREATE POLICY p_problem_reports_insert ON maintenance.problem_reports
    FOR INSERT
    WITH CHECK (
        reported_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Update Access: Users update their own open reports; Admins and Technicians manage all
DROP POLICY IF EXISTS p_problem_reports_update ON maintenance.problem_reports;
CREATE POLICY p_problem_reports_update ON maintenance.problem_reports
    FOR UPDATE
    USING (
        reported_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;