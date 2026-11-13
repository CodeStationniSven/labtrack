-- =============================================================================
-- LabTrack Schema Initialization: 013_replacement.sql
-- Database Engine: PostgreSQL 14+
-- Module: Equipment End-of-Life, Asset Replacement Requests, & Capital Planning
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: maintenance.replacement_requests
-- Purpose: Tracks capital replacement workflows for aging, unrepairable, or
--          obsolete equipment assets, linking repair history with procurement.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.replacement_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES inventory.assets(id) ON DELETE CASCADE,
    requested_by_user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    reviewed_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    request_number VARCHAR(50) NOT NULL UNIQUE,
    reason VARCHAR(100) NOT NULL CHECK (
        reason IN ('BeyondEconomicalRepair', 'EndOfLife', 'ObsoleteTechnology', 'SafetyNonCompliance', 'CapacityExpansion')
    ),
    justification TEXT NOT NULL,
    estimated_replacement_cost_usd NUMERIC(12, 2) NOT NULL CHECK (estimated_replacement_cost_usd >= 0.00),
    status VARCHAR(30) NOT NULL DEFAULT 'Submitted' CHECK (
        status IN ('Submitted', 'UnderReview', 'Approved', 'Rejected', 'ProcurementInitiated', 'Completed')
    ),
    review_notes TEXT,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for fast procurement and managerial review
CREATE INDEX IF NOT EXISTS idx_replacement_requests_asset ON maintenance.replacement_requests(asset_id);
CREATE INDEX IF NOT EXISTS idx_replacement_requests_requested_by ON maintenance.replacement_requests(requested_by_user_id);
CREATE INDEX IF NOT EXISTS idx_replacement_requests_status ON maintenance.replacement_requests(status);
CREATE INDEX IF NOT EXISTS idx_replacement_requests_created_at ON maintenance.replacement_requests(created_at DESC);

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_replacement_requests_update_timestamp ON maintenance.replacement_requests;
CREATE TRIGGER trg_replacement_requests_update_timestamp
BEFORE UPDATE ON maintenance.replacement_requests
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_replacement_requests_increment_version ON maintenance.replacement_requests;
CREATE TRIGGER trg_replacement_requests_increment_version
BEFORE UPDATE ON maintenance.replacement_requests
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 2. Trigger Function: maintenance.fn_auto_generate_replacement_request_number
-- Purpose: Generates human-readable request codes (e.g., REPL-20261021-XXXX).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION maintenance.fn_auto_generate_replacement_request_number()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.request_number IS NULL OR NEW.request_number = '' THEN
        NEW.request_number := 'REPL-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_replacement_request_number ON maintenance.replacement_requests;
CREATE TRIGGER trg_set_replacement_request_number
BEFORE INSERT ON maintenance.replacement_requests
FOR EACH ROW
EXECUTE FUNCTION maintenance.fn_auto_generate_replacement_request_number();


-- -----------------------------------------------------------------------------
-- 3. View: maintenance.v_replacement_requests_overview
-- Purpose: Consolidates replacement requests with asset age, maintenance cost,
--          and cost center allocation for financial decision-making.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW maintenance.v_replacement_requests_overview AS
SELECT 
    rr.id AS request_id,
    rr.request_number,
    rr.reason,
    rr.status,
    rr.estimated_replacement_cost_usd,
    rr.asset_id,
    a.name AS asset_name,
    a.serial_number,
    a.category,
    a.status AS current_asset_status,
    fl.purchase_price_usd AS original_purchase_price,
    cc.code AS cost_center_code,
    cc.name AS cost_center_name,
    req_u.full_name AS requested_by_name,
    req_u.email AS requested_by_email,
    rev_u.full_name AS reviewed_by_name,
    rr.created_at,
    rr.updated_at
FROM 
    maintenance.replacement_requests rr
JOIN 
    inventory.assets a ON rr.asset_id = a.id
LEFT JOIN 
    financial.asset_financial_ledgers fl ON a.id = fl.asset_id
LEFT JOIN 
    financial.cost_centers cc ON fl.cost_center_id = cc.id
LEFT JOIN 
    auth.users req_u ON rr.requested_by_user_id = req_u.id
LEFT JOIN 
    auth.users rev_u ON rr.reviewed_by_user_id = rev_u.id
ORDER BY 
    rr.created_at DESC;


-- -----------------------------------------------------------------------------
-- 4. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.replacement_requests ENABLE ROW LEVEL SECURITY;

-- Read Access: Requesters view their requests; Admins, Technicians, and Auditors view all
DROP POLICY IF EXISTS p_replacement_requests_read ON maintenance.replacement_requests;
CREATE POLICY p_replacement_requests_read ON maintenance.replacement_requests
    FOR SELECT
    USING (
        requested_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician', 'Auditor')
        )
    );

-- Insert Access: Authenticated users can create replacement requests
DROP POLICY IF EXISTS p_replacement_requests_insert ON maintenance.replacement_requests;
CREATE POLICY p_replacement_requests_insert ON maintenance.replacement_requests
    FOR INSERT
    WITH CHECK (
        requested_by_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Update Access: Admins and Technicians manage requests and record approvals
DROP POLICY IF EXISTS p_replacement_requests_update ON maintenance.replacement_requests;
CREATE POLICY p_replacement_requests_update ON maintenance.replacement_requests
    FOR UPDATE
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;