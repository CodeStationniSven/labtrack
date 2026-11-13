-- =============================================================================
-- LabTrack Schema Initialization: 005_laboratories.sql
-- Database Engine: PostgreSQL 14+
-- Module: Multi-Facility Topology, Physical Laboratory Bays, Workstations, & Environmental Zones
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: inventory.facilities
-- Purpose: Primary registry of buildings, research campuses, or institutional complexes
--          housing laboratory facilities.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.facilities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    address VARCHAR(300),
    city VARCHAR(100),
    state_province VARCHAR(100),
    postal_code VARCHAR(30),
    country VARCHAR(100) DEFAULT 'United States',
    primary_contact_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_facilities_code ON inventory.facilities(code);
CREATE INDEX IF NOT EXISTS idx_facilities_is_active ON inventory.facilities(is_active);

DROP TRIGGER IF EXISTS trg_facilities_update_timestamp ON inventory.facilities;
CREATE TRIGGER trg_facilities_update_timestamp
BEFORE UPDATE ON inventory.facilities
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: inventory.laboratories
-- Purpose: Physical laboratory rooms or suites within a facility, defining bio-safety 
--          levels (BSL), access controls, and environmental parameters.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.laboratories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    facility_id UUID NOT NULL REFERENCES inventory.facilities(id) ON DELETE CASCADE,
    room_number VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    biosafety_level VARCHAR(20) NOT NULL DEFAULT 'BSL-1' CHECK (
        biosafety_level IN ('BSL-1', 'BSL-2', 'BSL-3', 'BSL-4', 'Cleanroom', 'General')
    ),
    max_occupancy INT DEFAULT 10 CHECK (max_occupancy >= 0),
    lab_manager_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    requires_escort BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT uq_facility_room UNIQUE (facility_id, room_number)
);

CREATE INDEX IF NOT EXISTS idx_laboratories_facility_id ON inventory.laboratories(facility_id);
CREATE INDEX IF NOT EXISTS idx_laboratories_room_number ON inventory.laboratories(room_number);
CREATE INDEX IF NOT EXISTS idx_laboratories_bsl ON inventory.laboratories(biosafety_level);

DROP TRIGGER IF EXISTS trg_laboratories_update_timestamp ON inventory.laboratories;
CREATE TRIGGER trg_laboratories_update_timestamp
BEFORE UPDATE ON inventory.laboratories
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. Table: inventory.lab_bays
-- Purpose: Sub-divided functional areas, workbenches, fume hoods, or physical bays
--          within a specific laboratory for targeted asset assignment.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.lab_bays (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    laboratory_id UUID NOT NULL REFERENCES inventory.laboratories(id) ON DELETE CASCADE,
    bay_identifier VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    power_capacity_kva NUMERIC(6, 2),
    has_gas_hookup BOOLEAN NOT NULL DEFAULT FALSE,
    has_fume_hood BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT uq_lab_bay UNIQUE (laboratory_id, bay_identifier)
);

CREATE INDEX IF NOT EXISTS idx_lab_bays_laboratory_id ON inventory.lab_bays(laboratory_id);

DROP TRIGGER IF EXISTS trg_lab_bays_update_timestamp ON inventory.lab_bays;
CREATE TRIGGER trg_lab_bays_update_timestamp
BEFORE UPDATE ON inventory.lab_bays
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 4. View: inventory.v_facility_topology_overview
-- Purpose: Consolidates facilities, labs, bay capacity, and managers into a 
--          hierarchical topological overview for system dashboards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW inventory.v_facility_topology_overview AS
SELECT 
    f.id AS facility_id,
    f.code AS facility_code,
    f.name AS facility_name,
    l.id AS laboratory_id,
    l.room_number,
    l.name AS laboratory_name,
    l.biosafety_level,
    l.max_occupancy,
    u.full_name AS lab_manager_name,
    u.email AS lab_manager_email,
    COUNT(b.id) AS total_bays_count,
    f.is_active AS facility_is_active,
    l.is_active AS laboratory_is_active
FROM 
    inventory.facilities f
JOIN 
    inventory.laboratories l ON f.id = l.facility_id
LEFT JOIN 
    auth.users u ON l.lab_manager_user_id = u.id
LEFT JOIN 
    inventory.lab_bays b ON l.id = b.laboratory_id
GROUP BY 
    f.id, f.code, f.name, l.id, l.room_number, l.name, l.biosafety_level, l.max_occupancy, u.full_name, u.email, f.is_active, l.is_active;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE inventory.facilities ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory.laboratories ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory.lab_bays ENABLE ROW LEVEL SECURITY;

-- Read Access: All authenticated internal roles can inspect facility topology
DROP POLICY IF EXISTS p_facilities_public_read ON inventory.facilities;
CREATE POLICY p_facilities_public_read ON inventory.facilities
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_laboratories_public_read ON inventory.laboratories;
CREATE POLICY p_laboratories_public_read ON inventory.laboratories
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_lab_bays_public_read ON inventory.lab_bays;
CREATE POLICY p_lab_bays_public_read ON inventory.lab_bays
    FOR SELECT
    USING (TRUE);

COMMIT;