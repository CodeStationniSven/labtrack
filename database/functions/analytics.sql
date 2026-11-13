-- =============================================================================
-- LabTrack Analytics & Reporting Functions
-- Database Engine: PostgreSQL 14+
-- Module: System Telemetry, Repair Cost Aggregation, & Asset Operational Metrics
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Function: fn_get_repair_cost_summary
-- Purpose: Aggregates historical repair costs, itemized by replacement parts, 
--          labor, and average turnaround times across laboratory categories.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_repair_cost_summary(
    p_start_date TIMESTAMP WITH TIME ZONE DEFAULT (NOW() - INTERVAL '30 days'),
    p_end_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
    hardware_category VARCHAR(100),
    total_tickets_resolved BIGINT,
    total_parts_cost_usd NUMERIC(12, 2),
    total_labor_cost_usd NUMERIC(12, 2),
    total_combined_cost_usd NUMERIC(12, 2),
    avg_downtime_hours NUMERIC(8, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COALESCE(a.category, 'Uncategorized') AS hardware_category,
        COUNT(r.id)::BIGINT AS total_tickets_resolved,
        COALESCE(SUM(r.parts_cost_usd), 0.00)::NUMERIC(12, 2) AS total_parts_cost_usd,
        COALESCE(SUM(r.labor_cost_usd), 0.00)::NUMERIC(12, 2) AS total_labor_cost_usd,
        COALESCE(SUM(r.parts_cost_usd + r.labor_cost_usd), 0.00)::NUMERIC(12, 2) AS total_combined_cost_usd,
        COALESCE(AVG(EXTRACT(EPOCH FROM (r.resolved_at - r.created_at)) / 3600.0), 0.00)::NUMERIC(8, 2) AS avg_downtime_hours
    FROM 
        repair_tickets r
    JOIN 
        assets a ON r.asset_id = a.id
    WHERE 
        r.status = 'Resolved'
        AND r.resolved_at BETWEEN p_start_date AND p_end_date
    GROUP BY 
        a.category
    ORDER BY 
        total_combined_cost_usd DESC;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_get_asset_reliability_metrics
-- Purpose: Calculates Mean Time Between Failures (MTBF) and Mean Time To Repair (MTTR)
--          metrics for individual assets or asset categories.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_asset_reliability_metrics(
    p_asset_id UUID DEFAULT NULL
)
RETURNS TABLE (
    asset_id UUID,
    asset_name VARCHAR(255),
    category VARCHAR(100),
    total_failures BIGINT,
    mttr_hours NUMERIC(8, 2),
    current_status VARCHAR(50)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        a.id AS asset_id,
        a.name AS asset_name,
        a.category,
        COUNT(r.id)::BIGINT AS total_failures,
        COALESCE(
            AVG(EXTRACT(EPOCH FROM (r.resolved_at - r.created_at)) / 3600.0), 
            0.00
        )::NUMERIC(8, 2) AS mttr_hours,
        a.status AS current_status
    FROM 
        assets a
    LEFT JOIN 
        repair_tickets r ON a.id = r.asset_id
    WHERE 
        (p_asset_id IS NULL OR a.id = p_asset_id)
    GROUP BY 
        a.id, a.name, a.category, a.status
    ORDER BY 
        total_failures DESC;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_get_safety_compliance_overview
-- Purpose: Summarizes safety evaluation outcomes, hazardous environmental flag counts,
--          and permit approval rates across laboratory equipment zones.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_safety_compliance_overview()
RETURNS TABLE (
    total_evaluations BIGINT,
    permitted_operations BIGINT,
    prohibited_operations BIGINT,
    critical_risk_count BIGINT,
    compliance_rate_pct NUMERIC(5, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(s.id)::BIGINT AS total_evaluations,
        COUNT(s.id) FILTER (WHERE s.is_operation_permitted = TRUE)::BIGINT AS permitted_operations,
        COUNT(s.id) FILTER (WHERE s.is_operation_permitted = FALSE)::BIGINT AS prohibited_operations,
        COUNT(s.id) FILTER (WHERE LOWER(s.overall_risk_level) = 'critical')::BIGINT AS critical_risk_count,
        CASE 
            WHEN COUNT(s.id) = 0 THEN 100.00
            ELSE ROUND((COUNT(s.id) FILTER (WHERE s.is_operation_permitted = TRUE)::NUMERIC / COUNT(s.id)::NUMERIC) * 100.0, 2)
        END AS compliance_rate_pct
    FROM 
        safety_evaluations s;
END;
$$;

COMMIT;