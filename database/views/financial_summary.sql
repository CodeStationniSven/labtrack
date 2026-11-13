-- =============================================================================
-- LabTrack View Initialization: backend/database/views/financial_summary.sql
-- Database Engine: PostgreSQL 14+
-- Module: Executive Financial Oversight, Asset Valuation, Cost Center Budgets,
--          & Depreciation Rollups
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. View: financial.v_financial_summary_executive
-- Purpose: High-level financial KPIs aggregating institutional asset valuation,
--          total capital expenditure, accumulated depreciation, net book value,
--          and allocated vs. remaining cost center budgets across all facilities.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW financial.v_financial_summary_executive AS
SELECT
    -- Total Cost Center Budget Allocation & Consumption
    COALESCE(SUM(cc.allocated_budget_usd), 0.00) AS total_allocated_budget_usd,
    COALESCE(SUM(cc.remaining_budget_usd), 0.00) AS total_remaining_budget_usd,
    COALESCE(SUM(cc.allocated_budget_usd - cc.remaining_budget_usd), 0.00) AS total_budget_spent_usd,
    ROUND(
        CASE 
            WHEN SUM(cc.allocated_budget_usd) > 0 
            THEN (SUM(cc.allocated_budget_usd - cc.remaining_budget_usd) / SUM(cc.allocated_budget_usd)) * 100.0
            ELSE 0.00 
        END, 
        2
    ) AS budget_utilization_pct,

    -- Asset Ledger Capital Metrics
    COALESCE(COUNT(fl.id), 0) AS total_tracked_financial_assets,
    COALESCE(SUM(fl.purchase_price_usd), 0.00) AS total_asset_purchase_value_usd,
    COALESCE(SUM(fl.salvage_value_usd), 0.00) AS total_asset_salvage_value_usd,
    COALESCE(SUM(fl.accumulated_depreciation_usd), 0.00) AS total_accumulated_depreciation_usd,
    COALESCE(SUM(fl.current_book_value_usd), 0.00) AS total_current_net_book_value_usd,

    -- Maintenance & Operational Expenditure
    COALESCE(
        (SELECT SUM(actual_cost_usd) FROM maintenance.repair_tickets WHERE status = 'Closed'), 
        0.00
    ) AS lifetime_completed_repairs_cost_usd,
    COALESCE(
        (SELECT SUM(estimated_cost_usd) FROM maintenance.repair_tickets WHERE status NOT IN ('Closed', 'Cancelled')), 
        0.00
    ) AS pending_repair_commitments_usd
FROM 
    financial.cost_centers cc
FULL OUTER JOIN 
    financial.asset_financial_ledgers fl ON TRUE;


-- -----------------------------------------------------------------------------
-- 2. View: financial.v_cost_center_budget_breakdown
-- Purpose: Department-level breakdown of cost center allocations, active asset
--          counts, ledger transactions, and remaining budget headroom.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW financial.v_cost_center_budget_breakdown AS
SELECT 
    cc.id AS cost_center_id,
    cc.code AS cost_center_code,
    cc.name AS cost_center_name,
    cc.department,
    cc.fiscal_year,
    u.full_name AS manager_name,
    u.email AS manager_email,
    cc.allocated_budget_usd,
    cc.remaining_budget_usd,
    (cc.allocated_budget_usd - cc.remaining_budget_usd) AS spent_budget_usd,
    ROUND(
        CASE 
            WHEN cc.allocated_budget_usd > 0 
            THEN ((cc.allocated_budget_usd - cc.remaining_budget_usd) / cc.allocated_budget_usd) * 100.0 
            ELSE 0.00 
        END, 
        2
    ) AS budget_spent_pct,
    cc.is_active,
    COALESCE(asset_stats.asset_count, 0) AS assigned_assets_count,
    COALESCE(asset_stats.total_asset_purchase_val, 0.00) AS total_asset_purchase_val_usd,
    COALESCE(asset_stats.total_asset_book_val, 0.00) AS total_asset_book_val_usd,
    COALESCE(tx_stats.total_transactions_count, 0) AS total_transactions_count
FROM 
    financial.cost_centers cc
LEFT JOIN 
    auth.users u ON cc.manager_user_id = u.id

-- Aggregated Asset Stats per Cost Center
LEFT JOIN (
    SELECT 
        cost_center_id,
        COUNT(*) AS asset_count,
        SUM(purchase_price_usd) AS total_asset_purchase_val,
        SUM(current_book_value_usd) AS total_asset_book_val
    FROM 
        financial.asset_financial_ledgers
    GROUP BY 
        cost_center_id
) asset_stats ON cc.id = asset_stats.cost_center_id

-- Aggregated Ledger Transaction Counts per Cost Center
LEFT JOIN (
    SELECT 
        cost_center_id,
        COUNT(*) AS total_transactions_count
    FROM 
        financial.ledger_transactions
    GROUP BY 
        cost_center_id
) tx_stats ON cc.id = tx_stats.cost_center_id

ORDER BY 
    cc.fiscal_year DESC, 
    cc.code ASC;


-- -----------------------------------------------------------------------------
-- 3. View: financial.v_asset_depreciation_schedule_projection
-- Purpose: Detailed per-asset depreciation breakdown, remaining useful life 
--          in months, annual straight-line run-rate, and financial risk state.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW financial.v_asset_depreciation_schedule_projection AS
SELECT 
    fl.id AS ledger_id,
    fl.asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.category,
    a.status AS asset_status,
    cc.code AS cost_center_code,
    fl.in_service_date,
    fl.useful_life_months,
    fl.depreciation_method,
    fl.purchase_price_usd,
    fl.salvage_value_usd,
    fl.accumulated_depreciation_usd,
    fl.current_book_value_usd,
    
    -- Calculated monthly & annual depreciation run rate
    ROUND(
        CASE 
            WHEN fl.useful_life_months > 0 
            THEN (fl.purchase_price_usd - fl.salvage_value_usd) / fl.useful_life_months
            ELSE 0.00 
        END, 
        2
    ) AS monthly_depreciation_usd,

    ROUND(
        CASE 
            WHEN fl.useful_life_months > 0 
            THEN ((fl.purchase_price_usd - fl.salvage_value_usd) / fl.useful_life_months) * 12.0
            ELSE 0.00 
        END, 
        2
    ) AS annual_depreciation_usd,

    -- Age & Remaining useful life calculations
    GREATEST(
        0, 
        (EXTRACT(YEAR FROM AGE(CURRENT_DATE, fl.in_service_date)) * 12 + EXTRACT(MONTH FROM AGE(CURRENT_DATE, fl.in_service_date)))::INT
    ) AS months_in_service,

    GREATEST(
        0, 
        fl.useful_life_months - (EXTRACT(YEAR FROM AGE(CURRENT_DATE, fl.in_service_date)) * 12 + EXTRACT(MONTH FROM AGE(CURRENT_DATE, fl.in_service_date)))::INT
    ) AS remaining_useful_life_months,

    -- Depreciation Status Indicator
    CASE 
        WHEN fl.current_book_value_usd <= fl.salvage_value_usd THEN 'Fully Depreciated'
        WHEN (EXTRACT(YEAR FROM AGE(CURRENT_DATE, fl.in_service_date)) * 12 + EXTRACT(MONTH FROM AGE(CURRENT_DATE, fl.in_service_date))) >= fl.useful_life_months THEN 'Past Useful Life'
        ELSE 'Active Depreciation'
    END AS depreciation_status

FROM 
    financial.asset_financial_ledgers fl
JOIN 
    inventory.assets a ON fl.asset_id = a.id
LEFT JOIN 
    financial.cost_centers cc ON fl.cost_center_id = cc.id
ORDER BY 
    fl.in_service_date ASC;

COMMIT;