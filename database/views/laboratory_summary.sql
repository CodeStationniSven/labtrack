-- =============================================================================
-- LabTrack View Initialization: backend/database/views/laboratory_summary.sql
-- Database Engine: PostgreSQL 14+
-- Module: Multi-Facility Space Utilization, Lab Bay Capacities, & Equipment Density
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. View: inventory.v_facility_laboratory_hierarchy
-- Purpose: Hierarchical lookup linking facilities, individual laboratories, 
--          lab bays, assigned room numbers, and site managers.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_facility_laboratory_hierarchy AS
SELECT 
    f.id AS facility_id,
    f.code AS facility_code,
    f.name AS facility_name,
    f.address AS facility_address,
    l.id AS laboratory_id,
    l.name AS laboratory_name,
    l.room_number,
    b.id AS lab_bay_id,
    b.bay_identifier,
    b.max_capacity AS bay_max_capacity,
    COALESCE(asset_counts.total_assets, 0) AS bay_current_assets_count
FROM 
    inventory.facilities f
JOIN 
    inventory.laboratories l ON f.id = l.facility_id
LEFT JOIN 
    inventory.lab_bays b ON l.id = b.laboratory_id
LEFT JOIN (
    SELECT 
        lab_bay_id, 
        COUNT(*) AS total_assets
    FROM 
        inventory.assets
    GROUP BY 
        lab_bay_id
) asset_counts ON b.id = asset_counts.lab_bay_id
ORDER BY 
    f.code ASC, 
    l.room_number ASC, 
    b.bay_identifier ASC;


-- -----------------------------------------------------------------------------
-- 2. View: inventory.v_laboratory_capacity_utilization
-- Purpose: Aggregates laboratory-level space utilization, bay capacities,
--          equipment density, and operational risk distribution.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_laboratory_capacity_utilization AS
SELECT 
    l.id AS laboratory_id,
    l.name AS laboratory_name,
    l.room_number,
    f.id AS facility_id,
    f.name AS facility_name,
    f.code AS facility_code,
    COALESCE(bay_stats.total_bays_count, 0) AS total_bays_count,
    COALESCE(bay_stats.total_max_capacity, 0) AS total_max_capacity,
    COALESCE(asset_stats.total_assets_count, 0) AS total_assets_count,
    
    -- Capacity utilization percentage
    ROUND(
        CASE 
            WHEN COALESCE(bay_stats.total_max_capacity, 0) > 0 
            THEN (COALESCE(asset_stats.total_assets_count, 0)::NUMERIC / bay_stats.total_max_capacity::NUMERIC) * 100.0
            ELSE 0.00 
        END, 
        2
    ) AS space_utilization_pct,

    -- Asset operational state breakdown within lab
    COALESCE(asset_stats.available_assets_count, 0) AS available_assets_count,
    COALESCE(asset_stats.in_use_assets_count, 0) AS in_use_assets_count,
    COALESCE(asset_stats.under_maintenance_assets_count, 0) AS under_maintenance_assets_count,
    COALESCE(asset_stats.critical_risk_assets_count, 0) AS critical_risk_assets_count,

    -- Workstation PC density
    COALESCE(workstation_stats.total_workstations_count, 0) AS total_workstations_count

FROM 
    inventory.laboratories l
JOIN 
    inventory.facilities f ON l.facility_id = f.id

-- Aggregated Bay Capacities
LEFT JOIN (
    SELECT 
        laboratory_id,
        COUNT(id) AS total_bays_count,
        SUM(max_capacity) AS total_max_capacity
    FROM 
        inventory.lab_bays
    GROUP BY 
        laboratory_id
) bay_stats ON l.id = bay_stats.laboratory_id

-- Aggregated Asset Stats
LEFT JOIN (
    SELECT 
        b.laboratory_id,
        COUNT(a.id) AS total_assets_count,
        COUNT(a.id) FILTER (WHERE a.status = 'Available') AS available_assets_count,
        COUNT(a.id) FILTER (WHERE a.status = 'In Use') AS in_use_assets_count,
        COUNT(a.id) FILTER (WHERE a.status = 'Under Maintenance') AS under_maintenance_assets_count,
        COUNT(a.id) FILTER (WHERE a.risk_tier = 'Critical') AS critical_risk_assets_count
    FROM 
        inventory.assets a
    JOIN 
        inventory.lab_bays b ON a.lab_bay_id = b.id
    GROUP BY 
        b.laboratory_id
) asset_stats ON l.id = asset_stats.laboratory_id

-- Aggregated Workstation Count
LEFT JOIN (
    SELECT 
        b.laboratory_id,
        COUNT(w.id) AS total_workstations_count
    FROM 
        telemetry.pc_workstations w
    JOIN 
        inventory.assets a ON w.asset_id = a.id
    JOIN 
        inventory.lab_bays b ON a.lab_bay_id = b.id
    GROUP BY 
        b.laboratory_id
) workstation_stats ON l.id = workstation_stats.laboratory_id

ORDER BY 
    f.code ASC, 
    l.room_number ASC;


-- -----------------------------------------------------------------------------
-- 3. View: inventory.v_facility_operational_summary
-- Purpose: Executive roll-up summarizing total facilities, laboratory counts,
--          hardware distribution, and overall facility readiness metrics.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_facility_operational_summary AS
SELECT 
    f.id AS facility_id,
    f.code AS facility_code,
    f.name AS facility_name,
    f.address,
    COUNT(DISTINCT l.id) AS total_laboratories_count,
    COUNT(DISTINCT b.id) AS total_lab_bays_count,
    COALESCE(SUM(b.max_capacity), 0) AS total_facility_capacity,
    COALESCE(fac_assets.total_assets_count, 0) AS total_assets_count,
    COALESCE(fac_assets.available_assets_count, 0) AS available_assets_count,
    COALESCE(fac_assets.under_maintenance_count, 0) AS under_maintenance_count,
    ROUND(
        CASE 
            WHEN COALESCE(fac_assets.total_assets_count, 0) > 0 
            THEN (COALESCE(fac_assets.available_assets_count, 0)::NUMERIC / fac_assets.total_assets_count::NUMERIC) * 100.0
            ELSE 0.00 
        END, 
        2
    ) AS facility_operational_readiness_pct
FROM 
    inventory.facilities f
LEFT JOIN 
    inventory.laboratories l ON f.id = l.facility_id
LEFT JOIN 
    inventory.lab_bays b ON l.id = b.laboratory_id

-- Aggregated Asset Stats per Facility
LEFT JOIN (
    SELECT 
        l.facility_id,
        COUNT(a.id) AS total_assets_count,
        COUNT(a.id) FILTER (WHERE a.status = 'Available') AS available_assets_count,
        COUNT(a.id) FILTER (WHERE a.status = 'Under Maintenance') AS under_maintenance_count
    FROM 
        inventory.assets a
    JOIN 
        inventory.lab_bays b ON a.lab_bay_id = b.id
    JOIN 
        inventory.laboratories l ON b.laboratory_id = l.id
    GROUP BY 
        l.facility_id
) fac_assets ON f.id = fac_assets.facility_id

GROUP BY 
    f.id, f.code, f.name, f.address, fac_assets.total_assets_count, fac_assets.available_assets_count, fac_assets.under_maintenance_count
ORDER BY 
    f.code ASC;

COMMIT;