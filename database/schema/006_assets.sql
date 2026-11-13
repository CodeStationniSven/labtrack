-- =============================================================================
-- LabTrack Schema Initialization: 006_assets.sql
-- Database Engine: PostgreSQL 14+
-- Module: Core Physical Assets Catalog, Equipment Telemetry Summaries, & Hardware Telemetry
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: inventory.assets
-- Purpose: Primary registry of laboratory hardware, analytical instruments,
--          workstation PCs, and specialized scientific equipment.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.assets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    lab_bay_id UUID REFERENCES inventory.lab_bays(id) ON DELETE SET NULL,
    name VARCHAR(255) NOT NULL,
    category VARCHAR(100) NOT NULL,
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    model_number VARCHAR(100),
    manufacturer VARCHAR(150),
    status inventory.asset_status NOT NULL DEFAULT 'Available',
    risk_tier inventory.asset_risk_tier NOT NULL DEFAULT 'Standard',
    location_bay VARCHAR(100),
    purchase_date DATE,
    warranty_expiration DATE,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexing for fast catalog search and filtering
CREATE INDEX IF NOT EXISTS idx_assets_category ON inventory.assets(category);
CREATE INDEX IF NOT EXISTS idx_assets_status ON inventory.assets(status);
CREATE INDEX IF NOT EXISTS idx_assets_risk_tier ON inventory.assets(risk_tier);
CREATE INDEX IF NOT EXISTS idx_assets_serial_number ON inventory.assets(serial_number);
CREATE INDEX IF NOT EXISTS idx_assets_lab_bay_id ON inventory.assets(lab_bay_id);
CREATE INDEX IF NOT EXISTS idx_assets_created_at ON inventory.assets(created_at DESC);

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_assets_update_timestamp ON inventory.assets;
CREATE TRIGGER trg_assets_update_timestamp
BEFORE UPDATE ON inventory.assets
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_assets_increment_version ON inventory.assets;
CREATE TRIGGER trg_assets_increment_version
BEFORE UPDATE ON inventory.assets
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();

-- Audit trigger for historical entity mutation tracking
DROP TRIGGER IF EXISTS trg_audit_assets_changes ON inventory.assets;
CREATE TRIGGER trg_audit_assets_changes
AFTER INSERT OR UPDATE OR DELETE ON inventory.assets
FOR EACH ROW
EXECUTE FUNCTION public.fn_audit_table_changes();


-- -----------------------------------------------------------------------------
-- 2. Table: inventory.asset_telemetry_summaries
-- Purpose: Caches real-time hardware status metrics, CPU/RAM utilization,
--          operating temperature, and last heartbeat timestamps per asset.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.asset_telemetry_summaries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL UNIQUE REFERENCES inventory.assets(id) ON DELETE CASCADE,
    cpu_usage_pct NUMERIC(5, 2) DEFAULT 0.00,
    ram_usage_pct NUMERIC(5, 2) DEFAULT 0.00,
    temperature_celsius NUMERIC(5, 2),
    operational_hours NUMERIC(10, 2) DEFAULT 0.00,
    last_ping_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_asset_telemetry_asset_id ON inventory.asset_telemetry_summaries(asset_id);
CREATE INDEX IF NOT EXISTS idx_asset_telemetry_last_ping ON inventory.asset_telemetry_summaries(last_ping_at DESC);

DROP TRIGGER IF EXISTS trg_asset_telemetry_update_timestamp ON inventory.asset_telemetry_summaries;
CREATE TRIGGER trg_asset_telemetry_update_timestamp
BEFORE UPDATE ON inventory.asset_telemetry_summaries
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. View: inventory.v_asset_inventory_overview
-- Purpose: Unified view linking equipment details, location topology, and 
--          real-time operational telemetry for inventory dashboards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_asset_inventory_overview AS
SELECT 
    a.id AS asset_id,
    a.name AS asset_name,
    a.category,
    a.serial_number,
    a.model_number,
    a.manufacturer,
    a.status AS operational_status,
    a.risk_tier,
    a.location_bay,
    b.bay_identifier,
    l.room_number,
    f.name AS facility_name,
    ats.cpu_usage_pct,
    ats.ram_usage_pct,
    ats.temperature_celsius,
    ats.operational_hours,
    ats.last_ping_at,
    a.purchase_date,
    a.warranty_expiration,
    a.version
FROM 
    inventory.assets a
LEFT JOIN 
    inventory.lab_bays b ON a.lab_bay_id = b.id
LEFT JOIN 
    inventory.laboratories l ON b.laboratory_id = l.id
LEFT JOIN 
    inventory.facilities f ON l.facility_id = f.id
LEFT JOIN 
    inventory.asset_telemetry_summaries ats ON a.id = ats.asset_id;


-- -----------------------------------------------------------------------------
-- 4. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE inventory.assets ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory.asset_telemetry_summaries ENABLE ROW LEVEL SECURITY;

-- Read Access: All active users can view physical asset details
DROP POLICY IF EXISTS p_assets_read ON inventory.assets;
CREATE POLICY p_assets_read ON inventory.assets
    FOR SELECT
    USING (TRUE);

-- Telemetry Read Access: Authenticated users can view telemetry status
DROP POLICY IF EXISTS p_asset_telemetry_read ON inventory.asset_telemetry_summaries;
CREATE POLICY p_asset_telemetry_read ON inventory.asset_telemetry_summaries
    FOR SELECT
    USING (TRUE);

COMMIT;