-- =============================================================================
-- LabTrack View Initialization: backend/database/views/dashboard.sql
-- Database Engine: PostgreSQL 14+
-- Module: Operational Command Center, Facility Status, Maintenance Metrics & KPIs
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. View: inventory.v_dashboard_asset_status_kpis
-- Purpose: Real-time high-level inventory distribution across operational states.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_dashboard_asset_status_kpis AS
SELECT
    COUNT(*) AS total_assets,
    COUNT(*) FILTER (WHERE status = 'Available') AS available_count,
    COUNT(*) FILTER (WHERE status = 'In Use') AS in_use_count,
    COUNT(*) FILTER (WHERE status = 'Under Maintenance') AS under_maintenance_count,
    COUNT(*) FILTER (WHERE status = 'Calibration Required') AS calibration_required_count,
    COUNT(*) FILTER (WHERE status = 'Decommissioned') AS decommissioned_count,
    ROUND(
        (COUNT(*) FILTER (WHERE status IN ('Available', 'In Use'))::NUMERIC / NULLIF(COUNT(*), 0)::NUMERIC) * 100.0, 
        2
    ) AS operational_readiness_pct
FROM 
    inventory.assets;


-- -----------------------------------------------------------------------------
-- 2. View: maintenance.v_dashboard_maintenance_metrics
-- Purpose: Aggregates open incident tickets, critical hazard flags, overdue 
--          preventive schedules, and average repair turnaround metrics.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW maintenance.v_dashboard_maintenance_metrics AS
SELECT
    (SELECT COUNT(*) FROM maintenance.problem_reports WHERE status NOT IN ('Closed', 'Rejected', 'Resolved')) AS open_problem_reports_count,
    (SELECT COUNT(*) FROM maintenance.problem_reports WHERE has_safety_hazard = TRUE AND status NOT IN ('Closed', 'Rejected', 'Resolved')) AS active_safety_hazards_count,
    (SELECT COUNT(*) FROM maintenance.repair_tickets WHERE status NOT IN ('Closed', 'Cancelled')) AS active_repair_tickets_count,
    (SELECT COUNT(*) FROM maintenance.repair_tickets WHERE priority = 'Critical' AND status NOT IN ('Closed', 'Cancelled')) AS critical_repair_tickets_count,
    (SELECT COUNT(*) FROM maintenance.maintenance_schedules WHERE is_active = TRUE AND next_due_at < NOW()) AS overdue_maintenance_tasks_count,
    COALESCE(
        (
            SELECT ROUND(AVG(EXTRACT(EPOCH FROM (resolved_at - started_at)) / 3600.0)::NUMERIC, 2)
            FROM maintenance.repair_tickets
            WHERE status = 'Resolved' AND started_at IS NOT NULL AND resolved_at IS NOT NULL
              AND resolved_at >= (NOW() - INTERVAL '30 days')
        ), 
        0.00
    ) AS avg_repair_mttr_hours_last_30d;


-- -----------------------------------------------------------------------------
-- 3. View: scheduling.v_dashboard_utilization_summary
-- Purpose: Evaluates active reservation count and 24-hour booking throughput.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW scheduling.v_dashboard_utilization_summary AS
SELECT
    COUNT(*) FILTER (WHERE status = 'Active' AND NOW() BETWEEN start_time AND end_time) AS active_reservations_now,
    COUNT(*) FILTER (WHERE status IN ('Approved', 'Active') AND start_time >= NOW() AND start_time <= (NOW() + INTERVAL '24 hours')) AS reservations_starting_next_24h,
    COUNT(*) FILTER (WHERE status = 'Pending') AS pending_approvals_count
FROM 
    scheduling.reservation_schedules;


-- -----------------------------------------------------------------------------
-- 4. View: audit.v_dashboard_recent_activity_feed
-- Purpose: Consolidated timeline of top recent system events, tickets, and safety incidents.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW audit.v_dashboard_recent_activity_feed AS
SELECT 
    'ProblemReport' AS event_category,
    pr.id AS reference_id,
    pr.report_number AS reference_code,
    pr.title,
    pr.severity_level::VARCHAR(30) AS priority_or_severity,
    pr.status::VARCHAR(30) AS status,
    u.full_name AS actor_name,
    pr.created_at AS event_timestamp
FROM 
    maintenance.problem_reports pr
LEFT JOIN 
    auth.users u ON pr.reported_by_user_id = u.id

UNION ALL

SELECT 
    'RepairTicket' AS event_category,
    rt.id AS reference_id,
    rt.ticket_number AS reference_code,
    rt.title,
    rt.priority::VARCHAR(30) AS priority_or_severity,
    rt.status::VARCHAR(30) AS status,
    tech.full_name AS actor_name,
    rt.created_at AS event_timestamp
FROM 
    maintenance.repair_tickets rt
LEFT JOIN 
    auth.users tech ON rt.assigned_technician_id = tech.id

UNION ALL

SELECT 
    'ForensicAudit' AS event_category,
    fl.record_id AS reference_id,
    fl.action AS reference_code,
    format('%s on %s.%s', fl.action, fl.schema_name, fl.table_name) AS title,
    fl.category::VARCHAR(30) AS priority_or_severity,
    'Recorded' AS status,
    u.full_name AS actor_name,
    fl.event_timestamp AS event_timestamp
FROM 
    audit.forensic_ledger fl
LEFT JOIN 
    auth.users u ON fl.actor_user_id = u.id

ORDER BY 
    event_timestamp DESC
LIMIT 50;

COMMIT;