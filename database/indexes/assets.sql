-- =============================================================================
-- LabTrack Physical Assets & Equipment Inventory Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Core Asset Catalog, Hardware Categorization, & Telemetry Registry
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: assets
-- Purpose: Core inventory catalog holding physical laboratory equipment, 
--          workstations, measurement tools, and diagnostic hardware.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS assets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    category VARCHAR(100) NOT NULL,
    serial_number VARCHAR(100) NOT NULL UNIQUE,
    model_number VARCHAR(100),
    manufacturer VARCHAR(150),
    status VARCHAR(50) NOT NULL DEFAULT 'Available' CHECK (
        status IN ('Available', 'In Use', 'Under Maintenance', 'Decommissioned', 'Unknown')
    ),
    location_bay VARCHAR(100),
    purchase_date DATE,
    warranty_expiration DATE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexing
CREATE INDEX IF NOT EXISTS idx_assets_category ON assets(category);
CREATE INDEX IF NOT EXISTS idx_assets_status ON assets(status);
CREATE INDEX IF NOT EXISTS idx_assets_serial_number ON assets(serial_number);
CREATE INDEX IF NOT EXISTS idx_assets_created_at ON assets(created_at DESC);

-- -----------------------------------------------------------------------------
-- Table: asset_telemetry_summaries
-- Purpose: Caches active metrics, operational state, and environmental health
--          telemetry for registered assets.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asset_telemetry_summaries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL UNIQUE REFERENCES assets(id) ON DELETE CASCADE,
    cpu_usage_pct NUMERIC(5, 2) DEFAULT 0.00,
    ram_usage_pct NUMERIC(5, 2) DEFAULT 0.00,
    temperature_celsius NUMERIC(5, 2),
    operational_hours NUMERIC(10, 2) DEFAULT 0.00,
    last_ping_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_asset_telemetry_asset_id ON asset_telemetry_summaries(asset_id);

COMMIT;