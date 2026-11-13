-- =============================================================================
-- LabTrack System Reports, Automated Export Views, & Operational Dashboards
-- Database Engine: PostgreSQL 14+
-- Module: Reporting Summaries, Materialized Views, & Operational Aggregations
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Materialized View: mv_asset_operational_status_report
-- Purpose: Pre-aggregates physical asset operational metrics, active bookings,
--          open repair tickets, and last telemetry state for dashboard reporting.
-- -----------------------------------------------------------------------------
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_asset_operational_status_report AS
SELECT 
    a.id AS asset_id,
    a.name AS asset_name,
    a.category,
    a.serial_number,
    a.status AS operational_status,
    a.location_bay,
    ats.cpu_usage_pct,
    ats.ram_usage_pct,
    ats.temperature_celsius,
    ats.last_ping_at,
    COUNT(DISTINCT rs.id) FILTER (WHERE rs.status IN ('Approved', 'Active')) AS active_reservations_count,
    COUNT(DISTINCT rt.id) FILTER (WHERE rt.status IN ('Pending', 'In Progress', 'Awaiting Parts')) AS open_repair_tickets_count,
    COUNT(DISTINCT pr.id) FILTER (WHERE pr.status IN ('Open', 'Under Review', 'In Progress')) AS open_problem_reports_count,
    NOW() AS report_generated_at
FROM 
    assets a
LEFT JOIN 
    asset_telemetry_summaries ats ON a.id = ats.asset_id
LEFT JOIN 
    reservation_schedules rs ON a.id = rs.asset_id
LEFT JOIN 
    repair_tickets rt ON a.id = rt.asset_id
LEFT JOIN 
    problem_reports pr ON a.id = pr.asset_id
GROUP BY 
    a.id, a.name, a.category, a.serial_number, a.status, a.location_bay,
    ats.cpu_usage_pct, ats.ram_usage_pct, ats.temperature_celsius, ats.last_ping_at;

-- Indexing for fast reporting queries on the materialized view
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_asset_report_asset_id 
    ON mv_asset_operational_status_report(asset_id);

CREATE INDEX IF NOT EXISTS idx_mv_asset_report_category_status 
    ON mv_asset_operational_status_report(category, operational_status);


-- -----------------------------------------------------------------------------
-- Function: fn_refresh_operational_reports
-- Purpose: Concurrent refresh trigger function for materialized reporting views.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_refresh_operational_reports()
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_asset_operational_status_report;
END;
$$;


-- -----------------------------------------------------------------------------
-- View: v_monthly_maintenance_financials
-- Purpose: Dynamic view calculating itemized monthly maintenance, parts, 
--          and labor expenditures across all laboratory equipment.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_monthly_maintenance_financials AS
SELECT 
    TO_CHAR(rt.created_at, 'YYYY-MM') AS reporting_month,
    a.category AS hardware_category,
    COUNT(rt.id) AS total_repair_tickets,
    SUM(rt.parts_cost_usd) AS total_parts_cost_usd,
    SUM(rt.labor_cost_usd) AS total_labor_cost_usd,
    SUM(rt.parts_cost_usd + rt.labor_cost_usd) AS total_maintenance_spend_usd,
    AVG(rt.estimated_downtime_hours) AS avg_estimated_downtime_hours
FROM 
    repair_tickets rt
JOIN 
    assets a ON rt.asset_id = a.id
GROUP BY 
    TO_CHAR(rt.created_at, 'YYYY-MM'),
    a.category
ORDER BY 
    reporting_month DESC, 
    total_maintenance_spend_usd DESC;


-- -----------------------------------------------------------------------------
-- View: v_researcher_equipment_utilization
-- Purpose: High-level usage audit summarizing total reservations, QR check-ins,
--          and workstation session hours grouped by user account.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_researcher_equipment_utilization AS
SELECT 
    u.id AS user_id,
    u.full_name,
    u.email,
    r.name AS user_role,
    COUNT(DISTINCT res.id) AS total_reservations_made,
    COUNT(DISTINCT qr.id) FILTER (WHERE qr.is_successful = TRUE) AS successful_qr_checkins,
    COALESCE(SUM(pus.active_duration_minutes) / 60.0, 0.00)::NUMERIC(10, 2) AS total_pc_session_hours,
    u.last_login_at
FROM 
    users u
JOIN 
    roles r ON u.role_id = r.id
LEFT JOIN 
    reservation_schedules res ON u.id = res.reserved_by_user_id
LEFT JOIN 
    qr_checkin_logs qr ON u.id = qr.user_id
LEFT JOIN 
    pc_usage_sessions pus ON u.id = pus.user_id
GROUP BY 
    u.id, u.full_name, u.email, r.name, u.last_login_at;

COMMIT;