-- =============================================================================
-- LabTrack Financials, Cost Allocation & Equipment Valuation Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Ledger Transactions, Depreciation Tracking, Operational Budgets & Cost Centers
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: cost_centers
-- Purpose: Defines organizational departments, grant accounts, or research units 
--          responsible for equipment purchases, maintenance billing, and operational usage.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS cost_centers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    department VARCHAR(150) NOT NULL,
    manager_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    allocated_budget_usd NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cost_centers_code ON cost_centers(code);
CREATE INDEX IF NOT EXISTS idx_cost_centers_department ON cost_centers(department);

-- Attach updated_at timestamp trigger
DROP TRIGGER IF EXISTS trg_cost_centers_update_timestamp ON cost_centers;
CREATE TRIGGER trg_cost_centers_update_timestamp
BEFORE UPDATE ON cost_centers
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- Table: asset_financial_ledgers
-- Purpose: Tracks capital expenditure, purchase values, salvage estimations,
--          depreciation models, and association with organizational cost centers.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asset_financial_ledgers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL UNIQUE REFERENCES assets(id) ON DELETE CASCADE,
    cost_center_id UUID REFERENCES cost_centers(id) ON DELETE SET NULL,
    purchase_price_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    salvage_value_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    useful_life_years INT NOT NULL DEFAULT 5 CHECK (useful_life_years > 0),
    depreciation_method VARCHAR(40) NOT NULL DEFAULT 'Straight-Line' CHECK (
        depreciation_method IN ('Straight-Line', 'Double-Declining-Balance', 'Sum-Of-Years-Digits', 'None')
    ),
    po_number VARCHAR(100),
    vendor_name VARCHAR(150),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_asset_financial_asset_id ON asset_financial_ledgers(asset_id);
CREATE INDEX IF NOT EXISTS idx_asset_financial_cost_center ON asset_financial_ledgers(cost_center_id);

-- Attach updated_at timestamp trigger
DROP TRIGGER IF EXISTS trg_asset_financial_ledgers_update_timestamp ON asset_financial_ledgers;
CREATE TRIGGER trg_asset_financial_ledgers_update_timestamp
BEFORE UPDATE ON asset_financial_ledgers
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- Table: financial_transactions
-- Purpose: Itemized double-entry or debit/credit allocation logs for repairs,
--          routine servicing, consumable parts, and equipment hourly billing.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS financial_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    transaction_code VARCHAR(50) NOT NULL UNIQUE,
    cost_center_id UUID NOT NULL REFERENCES cost_centers(id) ON DELETE RESTRICT,
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    repair_ticket_id UUID REFERENCES repair_tickets(id) ON DELETE SET NULL,
    universal_ticket_id UUID REFERENCES universal_tickets(id) ON DELETE SET NULL,
    charged_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    transaction_type VARCHAR(40) NOT NULL CHECK (
        transaction_type IN ('RepairExpense', 'MaintenancePart', 'UsageFee', 'DepreciationWriteOff', 'Procurement')
    ),
    amount_usd NUMERIC(12, 2) NOT NULL CHECK (amount_usd >= 0.00),
    description TEXT NOT NULL,
    posted_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_financial_tx_cost_center ON financial_transactions(cost_center_id);
CREATE INDEX IF NOT EXISTS idx_financial_tx_asset_id ON financial_transactions(asset_id);
CREATE INDEX IF NOT EXISTS idx_financial_tx_posted_at ON financial_transactions(posted_at DESC);
CREATE INDEX IF NOT EXISTS idx_financial_tx_type ON financial_transactions(transaction_type);

-- Trigger to auto-generate transaction reference codes (e.g., TXN-20261021-XXXX)
CREATE OR REPLACE FUNCTION fn_generate_financial_txn_code()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.transaction_code IS NULL OR NEW.transaction_code = '' THEN
        NEW.transaction_code := 'TXN-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_financial_txn_code ON financial_transactions;
CREATE TRIGGER trg_set_financial_txn_code
BEFORE INSERT ON financial_transactions
FOR EACH ROW
EXECUTE FUNCTION fn_generate_financial_txn_code();


-- -----------------------------------------------------------------------------
-- Function: fn_calculate_asset_current_value
-- Purpose: Evaluates current net book value (NBV) and accumulated depreciation
--          for an equipment asset based on its acquisition date and ledger specs.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_calculate_asset_current_value(
    p_asset_id UUID
)
RETURNS TABLE (
    asset_id UUID,
    purchase_price_usd NUMERIC(12, 2),
    salvage_value_usd NUMERIC(12, 2),
    useful_life_years INT,
    age_years NUMERIC(5, 2),
    accumulated_depreciation_usd NUMERIC(12, 2),
    current_book_value_usd NUMERIC(12, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_purchase_date DATE;
    v_ledger RECORD;
    v_age_years NUMERIC(5, 2);
    v_depreciation NUMERIC(12, 2) := 0.00;
    v_book_value NUMERIC(12, 2);
BEGIN
    -- Query asset purchase date and ledger details
    SELECT a.purchase_date, fl.purchase_price_usd, fl.salvage_value_usd, fl.useful_life_years, fl.depreciation_method
    INTO v_purchase_date, v_ledger.purchase_price_usd, v_ledger.salvage_value_usd, v_ledger.useful_life_years, v_ledger.depreciation_method
    FROM assets a
    JOIN asset_financial_ledgers fl ON a.id = fl.asset_id
    WHERE a.id = p_asset_id;

    IF v_purchase_date IS NULL OR v_ledger.purchase_price_usd IS NULL THEN
        RETURN Query SELECT p_asset_id, 0.00::NUMERIC(12,2), 0.00::NUMERIC(12,2), 0, 0.00::NUMERIC(5,2), 0.00::NUMERIC(12,2), 0.00::NUMERIC(12,2);
        RETURN;
    END IF;

    -- Calculate asset age in fractional years
    v_age_years := LEAST(
        GREATEST(EXTRACT(EPOCH FROM (NOW() - v_purchase_date::timestamp)) / 31557600.0, 0.0),
        v_ledger.useful_life_years
    )::NUMERIC(5, 2);

    -- Calculate depreciation using chosen model
    IF v_ledger.depreciation_method = 'Straight-Line' THEN
        v_depreciation := GREATEST(
            0.00,
            ((v_ledger.purchase_price_usd - v_ledger.salvage_value_usd) / v_ledger.useful_life_years) * v_age_years
        );
    ELSIF v_ledger.depreciation_method = 'None' THEN
        v_depreciation := 0.00;
    ELSE
        -- Fallback default straight-line calculation for unsupported models
        v_depreciation := GREATEST(
            0.00,
            ((v_ledger.purchase_price_usd - v_ledger.salvage_value_usd) / v_ledger.useful_life_years) * v_age_years
        );
    END IF;

    -- Ensure accumulated depreciation doesn't exceed total depreciable basis
    v_depreciation := LEAST(v_depreciation, v_ledger.purchase_price_usd - v_ledger.salvage_value_usd);
    v_book_value := GREATEST(v_ledger.salvage_value_usd, v_ledger.purchase_price_usd - v_depreciation);

    RETURN QUERY SELECT 
        p_asset_id,
        v_ledger.purchase_price_usd,
        v_ledger.salvage_value_usd,
        v_ledger.useful_life_years,
        v_age_years,
        v_depreciation::NUMERIC(12, 2),
        v_book_value::NUMERIC(12, 2);
END;
$$;


-- -----------------------------------------------------------------------------
-- View: v_cost_center_budget_summaries
-- Purpose: Aggregates total expenses charged against each cost center relative to
--          allocated budget caps to support institutional fiscal monitoring.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_cost_center_budget_summaries AS
SELECT 
    cc.id AS cost_center_id,
    cc.code AS cost_center_code,
    cc.name AS cost_center_name,
    cc.department,
    u.full_name AS manager_name,
    cc.allocated_budget_usd,
    COALESCE(SUM(ft.amount_usd), 0.00) AS total_expenditures_usd,
    (cc.allocated_budget_usd - COALESCE(SUM(ft.amount_usd), 0.00)) AS remaining_budget_usd,
    CASE 
        WHEN cc.allocated_budget_usd > 0 
        THEN ROUND((COALESCE(SUM(ft.amount_usd), 0.00) / cc.allocated_budget_usd) * 100.0, 2)
        ELSE 0.00 
    END AS budget_utilization_pct
FROM 
    cost_centers cc
LEFT JOIN 
    users u ON cc.manager_user_id = u.id
LEFT JOIN 
    financial_transactions ft ON cc.id = ft.cost_center_id
GROUP BY 
    cc.id, cc.code, cc.name, cc.department, u.full_name, cc.allocated_budget_usd;


-- -----------------------------------------------------------------------------
-- Row Level Security (RLS) Policies
-- -----------------------------------------------------------------------------

ALTER TABLE cost_centers ENABLE ROW LEVEL SECURITY;
ALTER TABLE asset_financial_ledgers ENABLE ROW LEVEL SECURITY;
ALTER TABLE financial_transactions ENABLE ROW LEVEL SECURITY;

-- Financials Read Policy: Admin, Auditor, and Department Managers can view records
DROP POLICY IF EXISTS p_cost_centers_read ON cost_centers;
CREATE POLICY p_cost_centers_read ON cost_centers
    FOR SELECT
    USING (
        manager_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

DROP POLICY IF EXISTS p_financial_tx_read ON financial_transactions;
CREATE POLICY p_financial_tx_read ON financial_transactions
    FOR SELECT
    USING (
        charged_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

COMMIT;