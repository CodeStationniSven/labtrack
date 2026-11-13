-- =============================================================================
-- LabTrack Migration Script: 002_production_hardening.sql
-- Database Engine: PostgreSQL 14+
-- Description: Production hardening migration adding missing audit trail tables,
--              safety evaluations, AI knowledge vector schemas, table partitioning,
--              and RLS (Row Level Security) policies across core domains.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Safety Evaluations & Compliance Schema
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS safety_evaluations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    evaluated_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    overall_risk_level VARCHAR(30) NOT NULL CHECK (
        overall_risk_level IN ('Low', 'Medium', 'High', 'Critical')
    ),
    is_operation_permitted BOOLEAN NOT NULL DEFAULT TRUE,
    compliance_notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_safety_evaluations_asset_id 
    ON safety_evaluations(asset_id);

CREATE INDEX IF NOT EXISTS idx_safety_evaluations_risk_level 
    ON safety_evaluations(overall_risk_level);


-- -----------------------------------------------------------------------------
-- 2. Audit Trail Infrastructure Setup
-- -----------------------------------------------------------------------------

-- Core Assets Audit Table
CREATE TABLE IF NOT EXISTS assets_audit (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_id UUID NOT NULL,
    action_type VARCHAR(20) NOT NULL,
    changed_by VARCHAR(255) NOT NULL,
    change_payload JSONB NOT NULL,
    performed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_assets_audit_entity_id 
    ON assets_audit(entity_id);

CREATE INDEX IF NOT EXISTS idx_assets_audit_performed_at 
    ON assets_audit(performed_at DESC);

-- Attach Audit Trigger to Core Assets
DROP TRIGGER IF EXISTS trg_audit_assets_changes ON assets;
CREATE TRIGGER trg_audit_assets_changes
AFTER INSERT OR UPDATE OR DELETE ON assets
FOR EACH ROW
EXECUTE FUNCTION fn_audit_table_changes();

-- Repair Tickets Audit Table
CREATE TABLE IF NOT EXISTS repair_tickets_audit (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_id UUID NOT NULL,
    action_type VARCHAR(20) NOT NULL,
    changed_by VARCHAR(255) NOT NULL,
    change_payload JSONB NOT NULL,
    performed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_repair_audit_entity_id 
    ON repair_tickets_audit(entity_id);

-- Attach Audit Trigger to Repair Tickets
DROP TRIGGER IF EXISTS trg_audit_repair_tickets_changes ON repair_tickets;
CREATE TRIGGER trg_audit_repair_tickets_changes
AFTER INSERT OR UPDATE OR DELETE ON repair_tickets
FOR EACH ROW
EXECUTE FUNCTION fn_audit_table_changes();


-- -----------------------------------------------------------------------------
-- 3. Row Level Security (RLS) & Multi-tenant Access Policies
-- -----------------------------------------------------------------------------

-- Enable RLS on Sensitive Schemas
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE reservation_schedules ENABLE ROW LEVEL SECURITY;
ALTER TABLE system_notifications ENABLE ROW LEVEL SECURITY;

-- User Profile Read Policy (Users can read their own profile, Admins/Techs read all)
DROP POLICY IF EXISTS p_user_profiles_read ON user_profiles;
CREATE POLICY p_user_profiles_read ON user_profiles
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

-- System Notifications Read Policy (Users only access their personal inbox)
DROP POLICY IF EXISTS p_system_notifications_recipient_read ON system_notifications;
CREATE POLICY p_system_notifications_recipient_read ON system_notifications
    FOR SELECT
    USING (
        recipient_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM users u 
            JOIN roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );


-- -----------------------------------------------------------------------------
-- 4. Database Maintenance & Statistics Optimization
-- -----------------------------------------------------------------------------

-- Adjust Autovacuum & Vacuum Scale Factor for High-Frequency Telemetry
ALTER TABLE pc_telemetry_logs SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02
);

ALTER TABLE pc_usage_sessions SET (
    autovacuum_vacuum_scale_factor = 0.10,
    autovacuum_analyze_scale_factor = 0.05
);

COMMIT;