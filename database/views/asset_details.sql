-- =============================================================================
-- LabTrack View Initialization: backend/database/views/asset_details.sql
-- Database Engine: PostgreSQL 14+
-- Module: Consolidated Physical Asset Profile, Telemetry, Location, & Financial Metadata
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- View: inventory.v_asset_details_comprehensive
-- Purpose: Unified, wide-column read view joining core asset records with their 
--          hierarchical facility/lab location, real-time telemetry summaries, 
--          financial ledger valuation, linked PC workstation details, active QR code, 
--          and open maintenance/reservation counts. Designed for high-performance 
--          backend lookup endpoints and dashboard cards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_asset_details_comprehensive AS
SELECT 
    -- Core Asset Metadata
    a.id AS asset_id,
    a.name AS asset_name,
    a.category,
    a.serial_number,
    a.model_number,
    a.manufacturer,
    a.status AS asset_status,
    a.risk_tier,
    a.location_bay,
    a.purchase_date,
    a.warranty_expiration,
    a.version AS asset_version,
    a.created_at AS asset_created_at,
    a.updated_at AS asset_updated_at,

    -- Location Hierarchy
    b.id AS lab_bay_id,
    b.bay_identifier,
    l.id AS laboratory_id,
    l.name AS laboratory_name,
    l.room_number AS lab_room_number,
    f.id AS facility_id,
    f.name AS facility_name,
    f.code AS facility_code,

    -- Live Telemetry Summaries
    ts.cpu_usage_pct,
    ts.ram_usage_pct,
    ts.temperature_celsius,
    ts.operational_hours,
    ts.last_ping_at AS telemetry_last_ping_at,

    -- Workstation PC Infrastructure (if applicable)
    w.id AS workstation_id,
    w.hostname AS workstation_hostname,
    w.ip_address AS workstation_ip,
    w.mac_address AS workstation_mac,
    w.os_version AS workstation_os,
    w.is_online AS workstation_is_online,

    -- Financial & Cost Center Ledger Summary
    fl.purchase_price_usd,
    fl.salvage_value_usd,
    fl.useful_life_months,
    fl.depreciation_method,
    fl.accumulated_depreciation_usd,
    fl.current_book_value_usd,
    cc.id AS cost_center_id,
    cc.code AS cost_center_code,
    cc.name AS cost_center_name,

    -- QR Code Identification Token
    qr.token_code AS active_qr_token_code,

    -- Aggregated Incident & Schedule Operational Indicators
    COALESCE(pr_cnt.open_problem_reports_count, 0) AS open_problem_reports_count,
    COALESCE(rt_cnt.active_repair_tickets_count, 0) AS active_repair_tickets_count,
    COALESCE(rsv_cnt.upcoming_reservations_count, 0) AS upcoming_reservations_count

FROM 
    inventory.assets a
LEFT JOIN 
    inventory.lab_bays b ON a.lab_bay_id = b.id
LEFT JOIN 
    inventory.laboratories l ON b.laboratory_id = l.id
LEFT JOIN 
    inventory.facilities f ON l.facility_id = f.id
LEFT JOIN 
    inventory.asset_telemetry_summaries ts ON a.id = ts.asset_id
LEFT JOIN 
    telemetry.pc_workstations w ON a.id = w.asset_id
LEFT JOIN 
    financial.asset_financial_ledgers fl ON a.id = fl.asset_id
LEFT JOIN 
    financial.cost_centers cc ON fl.cost_center_id = cc.id
LEFT JOIN 
    scheduling.qr_tokens qr ON a.id = qr.asset_id AND qr.is_active = TRUE

-- Subquery aggregations for dynamic counters
LEFT JOIN (
    SELECT asset_id, COUNT(*) AS open_problem_reports_count
    FROM maintenance.problem_reports
    WHERE status NOT IN ('Closed', 'Rejected', 'Resolved')
    GROUP BY asset_id
) pr_cnt ON a.id = pr_cnt.asset_id

LEFT JOIN (
    SELECT asset_id, COUNT(*) AS active_repair_tickets_count
    FROM maintenance.repair_tickets
    WHERE status NOT IN ('Closed', 'Cancelled')
    GROUP BY asset_id
) rt_cnt ON a.id = rt_cnt.asset_id

LEFT JOIN (
    SELECT asset_id, COUNT(*) AS upcoming_reservations_count
    FROM scheduling.reservation_schedules
    WHERE status IN ('Approved', 'Active')
      AND end_time > NOW()
    GROUP BY asset_id
) rsv_cnt ON a.id = rsv_cnt.asset_id;

COMMIT;