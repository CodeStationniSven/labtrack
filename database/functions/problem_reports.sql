-- =============================================================================
-- LabTrack Problem Reports & Incident Ticketing Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Incident Reporting, Diagnostics Resolution Logs, & Root Cause Analytics
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: problem_reports
-- Purpose: Captures user-submitted defect reports, operational issues, hardware 
--          failures, and environmental hazards across laboratory bays.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS problem_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    reported_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    report_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    severity_level VARCHAR(30) NOT NULL CHECK (
        severity_level IN ('Low', 'Medium', 'High', 'Critical')
    ),
    category VARCHAR(80) NOT NULL DEFAULT 'General Defect',
    status VARCHAR(50) NOT NULL DEFAULT 'Open' CHECK (
        status IN ('Open', 'Under Review', 'In Progress', 'Resolved', 'Closed', 'Rejected')
    ),
    has_safety_hazard BOOLEAN NOT NULL DEFAULT FALSE,
    attachment_urls TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_problem_reports_asset_id ON problem_reports(asset_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_reported_by ON problem_reports(reported_by_user_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_status ON problem_reports(status);
CREATE INDEX IF NOT EXISTS idx_problem_reports_severity ON problem_reports(severity_level);

-- Trigger to auto-generate human-readable report numbers (e.g., PR-20261021-XXXX)
CREATE OR REPLACE FUNCTION fn_generate_problem_report_number()
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

DROP TRIGGER IF EXISTS trg_set_problem_report_number ON problem_reports;
CREATE TRIGGER trg_set_problem_report_number
BEFORE INSERT ON problem_reports
FOR EACH ROW
EXECUTE FUNCTION fn_generate_problem_report_number();

-- -----------------------------------------------------------------------------
-- Table: problem_resolution_logs
-- Purpose: Records diagnostic investigations, root cause analyses, technician 
--          remediation actions, and closing sign-offs for problem reports.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS problem_resolution_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    problem_report_id UUID NOT NULL REFERENCES problem_reports(id) ON DELETE CASCADE,
    assigned_technician_id UUID REFERENCES users(id) ON DELETE SET NULL,
    root_cause_summary TEXT,
    remediation_steps TEXT NOT NULL,
    parts_replaced TEXT[],
    downtime_duration_hours NUMERIC(8, 2) DEFAULT 0.00,
    is_verified_by_qa BOOLEAN NOT NULL DEFAULT FALSE,
    resolved_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_resolution_logs_report_id ON problem_resolution_logs(problem_report_id);
CREATE INDEX IF NOT EXISTS idx_resolution_logs_technician ON problem_resolution_logs(assigned_technician_id);

-- -----------------------------------------------------------------------------
-- Function: fn_get_problem_report_analytics
-- Purpose: Summarizes incident statistics, mean downtime, and safety hazards 
--          per asset category for executive and compliance dashboards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_problem_report_analytics(
    p_start_date TIMESTAMP WITH TIME ZONE DEFAULT (NOW() - INTERVAL '30 days'),
    p_end_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
    hardware_category VARCHAR(100),
    total_reports BIGINT,
    open_reports BIGINT,
    critical_reports BIGINT,
    safety_hazard_count BIGINT,
    avg_resolution_hours NUMERIC(8, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COALESCE(a.category, 'Unassigned / Facility') AS hardware_category,
        COUNT(pr.id)::BIGINT AS total_reports,
        COUNT(pr.id) FILTER (WHERE pr.status IN ('Open', 'Under Review', 'In Progress'))::BIGINT AS open_reports,
        COUNT(pr.id) FILTER (WHERE pr.severity_level = 'Critical')::BIGINT AS critical_reports,
        COUNT(pr.id) FILTER (WHERE pr.has_safety_hazard = TRUE)::BIGINT AS safety_hazard_count,
        COALESCE(
            AVG(EXTRACT(EPOCH FROM (res.resolved_at - pr.created_at)) / 3600.0), 
            0.00
        )::NUMERIC(8, 2) AS avg_resolution_hours
    FROM 
        problem_reports pr
    LEFT JOIN 
        assets a ON pr.asset_id = a.id
    LEFT JOIN 
        problem_resolution_logs res ON pr.id = res.problem_report_id
    WHERE 
        pr.created_at BETWEEN p_start_date AND p_end_date
    GROUP BY 
        a.category
    ORDER BY 
        total_reports DESC;
END;
$$;

COMMIT;