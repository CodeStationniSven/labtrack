-- =============================================================================
-- LabTrack Schema Initialization: 017_financial.sql
-- Database Engine: PostgreSQL 14+
-- Module: Operational Financials, Ledger Transactions, Cost Centers, & Asset Depreciation
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: financial.cost_centers
-- Purpose: Primary registry of institutional accounting units, research grants,
--          departmental budgets, and chargeback allocations.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS financial.cost_centers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    department VARCHAR(150),
    manager_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    allocated_budget_usd NUMERIC(14, 2) NOT NULL DEFAULT 0.00 CHECK (allocated_budget_usd >= 0.00),
    remaining_budget_usd NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    fiscal_year INT NOT NULL CHECK (fiscal_year >= 2000),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_cost_centers_code ON financial.cost_centers(code);
CREATE INDEX IF NOT EXISTS idx_cost_centers_fiscal_year ON financial.cost_centers(fiscal_year);
CREATE INDEX IF NOT EXISTS idx_cost_centers_is_active ON financial.cost_centers(is_active);

DROP TRIGGER IF EXISTS trg_cost_centers_update_timestamp ON financial.cost_centers;
CREATE TRIGGER trg_cost_centers_update_timestamp
BEFORE UPDATE ON financial.cost_centers
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: financial.asset_financial_ledgers
-- Purpose: Tracks equipment valuation, acquisition capital, salvage values,
--          depreciation parameters, and accumulated write-offs per asset.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS financial.asset_financial_ledgers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL UNIQUE REFERENCES inventory.assets(id) ON DELETE CASCADE,
    cost_center_id UUID REFERENCES financial.cost_centers(id) ON DELETE SET NULL,
    purchase_price_usd NUMERIC(12, 2) NOT NULL CHECK (purchase_price_usd >= 0.00),
    salvage_value_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (salvage_value_usd >= 0.00),
    useful_life_months INT NOT NULL DEFAULT 60 CHECK (useful_life_months > 0),
    depreciation_method financial.depreciation_method NOT NULL DEFAULT 'Straight-Line',
    accumulated_depreciation_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (accumulated_depreciation_usd >= 0.00),
    current_book_value_usd NUMERIC(12, 2) GENERATED ALWAYS AS (
        GREATEST(0.00, purchase_price_usd - accumulated_depreciation_usd)
    ) STORED,
    in_service_date DATE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_financial_ledgers_asset ON financial.asset_financial_ledgers(asset_id);
CREATE INDEX IF NOT EXISTS idx_financial_ledgers_cost_center ON financial.asset_financial_ledgers(cost_center_id);

DROP TRIGGER IF EXISTS trg_financial_ledgers_update_timestamp ON financial.asset_financial_ledgers;
CREATE TRIGGER trg_financial_ledgers_update_timestamp
BEFORE UPDATE ON financial.asset_financial_ledgers
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. Table: financial.ledger_transactions
-- Purpose: Itemized double-entry or chargeback transactions covering repairs,
--          maintenance parts, usage fees, and depreciation entries.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS financial.ledger_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cost_center_id UUID NOT NULL REFERENCES financial.cost_centers(id) ON DELETE RESTRICT,
    asset_id UUID REFERENCES inventory.assets(id) ON DELETE SET NULL,
    repair_ticket_id UUID REFERENCES maintenance.repair_tickets(id) ON DELETE SET NULL,
    performed_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    transaction_type financial.transaction_type NOT NULL,
    amount_usd NUMERIC(12, 2) NOT NULL CHECK (amount_usd <> 0.00),
    description TEXT NOT NULL,
    transaction_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ledger_tx_cost_center ON financial.ledger_transactions(cost_center_id);
CREATE INDEX IF NOT EXISTS idx_ledger_tx_asset ON financial.ledger_transactions(asset_id);
CREATE INDEX IF NOT EXISTS idx_ledger_tx_repair_ticket ON financial.ledger_transactions(repair_ticket_id);
CREATE INDEX IF NOT EXISTS idx_ledger_tx_date ON financial.ledger_transactions(transaction_date DESC);


-- -----------------------------------------------------------------------------
-- 4. Trigger Function: financial.fn_update_cost_center_budget_on_tx
-- Purpose: Automatically recalculates cost center remaining budget upon
--          insertion of new ledger debit/credit transactions.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION financial.fn_update_cost_center_budget_on_tx()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE financial.cost_centers
    SET remaining_budget_usd = remaining_budget_usd - NEW.amount_usd,
        updated_at = NOW()
    WHERE id = NEW.cost_center_id;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_update_cost_center_budget ON financial.ledger_transactions;
CREATE TRIGGER trg_update_cost_center_budget
AFTER INSERT ON financial.ledger_transactions
FOR EACH ROW
EXECUTE FUNCTION financial.fn_update_cost_center_budget_on_tx();


-- -----------------------------------------------------------------------------
-- 5. View: financial.v_asset_valuation_overview
-- Purpose: Consolidates physical asset details, financial ledger metrics, 
--          cost center affiliations, and calculated book values.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW financial.v_asset_valuation_overview AS
SELECT 
    a.id AS asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.category,
    a.status AS asset_status,
    fl.purchase_price_usd,
    fl.salvage_value_usd,
    fl.useful_life_months,
    fl.depreciation_method,
    fl.accumulated_depreciation_usd,
    fl.current_book_value_usd,
    fl.in_service_date,
    cc.id AS cost_center_id,
    cc.code AS cost_center_code,
    cc.name AS cost_center_name
FROM 
    inventory.assets a
JOIN 
    financial.asset_financial_ledgers fl ON a.id = fl.asset_id
LEFT JOIN 
    financial.cost_centers cc ON fl.cost_center_id = cc.id;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE financial.cost_centers ENABLE ROW LEVEL SECURITY;
ALTER TABLE financial.asset_financial_ledgers ENABLE ROW LEVEL SECURITY;
ALTER TABLE financial.ledger_transactions ENABLE ROW LEVEL SECURITY;

-- Read Access: Admins and Auditors view financial entries; users view cost centers they manage
DROP POLICY IF EXISTS p_cost_centers_read ON financial.cost_centers;
CREATE POLICY p_cost_centers_read ON financial.cost_centers
    FOR SELECT
    USING (
        manager_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

DROP POLICY IF EXISTS p_financial_ledgers_read ON financial.asset_financial_ledgers;
CREATE POLICY p_financial_ledgers_read ON financial.asset_financial_ledgers
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_ledger_transactions_read ON financial.ledger_transactions;
CREATE POLICY p_ledger_transactions_read ON financial.ledger_transactions
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Auditor')
        )
    );

-- Write Access: Restricted exclusively to System Administrators
DROP POLICY IF EXISTS p_financial_write_admin ON financial.cost_centers;
CREATE POLICY p_financial_write_admin ON financial.cost_centers
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

DROP POLICY IF EXISTS p_financial_ledgers_write_admin ON financial.asset_financial_ledgers;
CREATE POLICY p_financial_ledgers_write_admin ON financial.asset_financial_ledgers
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

DROP POLICY IF EXISTS p_ledger_tx_write_admin ON financial.ledger_transactions;
CREATE POLICY p_ledger_tx_write_admin ON financial.ledger_transactions
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

COMMIT;