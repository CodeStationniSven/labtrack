-- =============================================================================
-- LabTrack Schema Initialization: reference_data.sql
-- Database Engine: PostgreSQL 14+
-- Module: Static System Reference Data, Enumeration Lookup Tables, & ISO Standards
-- =============================================================================

BEGIN;

-- Setup temporary session bypass user context for system reference initialization
SET LOCAL app.current_user_id = '00000000-0000-0000-0000-000000000001';

-- -----------------------------------------------------------------------------
-- 1. Table: inventory.ref_asset_categories
-- Purpose: Standardized taxonomy for physical laboratory hardware classification.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.ref_asset_categories (
    code VARCHAR(50) PRIMARY KEY,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    requires_calibration BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

INSERT INTO inventory.ref_asset_categories (code, display_name, description, requires_calibration) VALUES
    ('GENOMICS_SEQUENCER', 'DNA/RNA Sequencer', 'Next-generation sequencing machines and flow cell processors', TRUE),
    ('MICROSCOPY_CRYO_EM', 'Cryo-Electron Microscope', 'High-resolution transmission and cryogenic electron microscopes', TRUE),
    ('SPECTROMETRY_MASS', 'Mass Spectrometer', 'High-performance mass spectrometers and chromatography units', TRUE),
    ('COMPUTE_AI_WORKSTATION', 'AI/HPC Compute Workstation', 'High-density GPU nodes and dedicated AI workstation rigs', FALSE),
    ('CENTRIFUGE_HIGH_SPEED', 'Ultracentrifuge', 'High-speed refrigerated analytical centrifuges', TRUE),
    ('STORAGE_CRYO_FREEZER', 'Ultra-Low Temp Freezer', '-80C storage units and liquid nitrogen dewars', TRUE),
    ('NETWORK_SWITCH_MANAGED', 'Managed Network Switch', 'Rackmount L2/L3 managed network infrastructure switches', FALSE)
ON CONFLICT (code) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 2. Table: maintenance.ref_failure_codes
-- Purpose: Categorized fault codes for defect triage, EHS hazards, and repair logs.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS maintenance.ref_failure_codes (
    code VARCHAR(50) PRIMARY KEY,
    category VARCHAR(80) NOT NULL,
    short_description VARCHAR(150) NOT NULL,
    default_priority maintenance.ticket_priority NOT NULL DEFAULT 'Medium',
    is_safety_hazard_by_default BOOLEAN NOT NULL DEFAULT FALSE
);

INSERT INTO maintenance.ref_failure_codes (code, category, short_description, default_priority, is_safety_hazard_by_default) VALUES
    ('ELEC_SHORT_CIRCUIT', 'Electrical Fault', 'Electrical short, blown fuse, or arc fault detected', 'Critical', TRUE),
    ('VACUUM_LEAK_PUMP', 'Pneumatic & Vacuum', 'Chamber pressure loss or vacuum pump degradation', 'High', FALSE),
    ('THERM_OVERHEATING', 'Thermal & Cooling', 'Coolant leak, thermal cutoff trigger, or fan failure', 'High', TRUE),
    ('OPTIC_ALIGNMENT_ERR', 'Optical Systems', 'Laser path misaligned or sensor calibration drift', 'Medium', FALSE),
    ('NET_INTERFACE_DOWN', 'Network Connectivity', 'NIC failure, link loss, or port flapping', 'Low', FALSE),
    ('SOFTWARE_CRASH_OS', 'Operating System', 'System freeze, driver crash, or OS boot sequence loop', 'Medium', FALSE)
ON CONFLICT (code) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 3. Table: audit.ref_compliance_standards
-- Purpose: ISO, GLP, GMP, and OSHA regulatory standards tracked by LabTrack.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.ref_compliance_standards (
    id VARCHAR(50) PRIMARY KEY,
    organization VARCHAR(100) NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT
);

INSERT INTO audit.ref_compliance_standards (id, organization, title, description) VALUES
    ('ISO-17025', 'ISO', 'General requirements for the competence of testing and calibration laboratories', 'Standard governing calibration protocols and measurement validity.'),
    ('FDA-21-CFR-PART-11', 'US FDA', 'Electronic Records; Electronic Signatures', 'Compliance governing audit trails, security ledgers, and electronic sign-offs.'),
    ('OSHA-1910-1450', 'OSHA', 'Occupational exposure to hazardous chemicals in laboratories', 'Laboratory safety standards and emergency hazard management.')
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 4. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE inventory.ref_asset_categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE maintenance.ref_failure_codes ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.ref_compliance_standards ENABLE ROW LEVEL SECURITY;

-- Read Access: Open to all authenticated system roles
DROP POLICY IF EXISTS p_ref_asset_categories_read ON inventory.ref_asset_categories;
CREATE POLICY p_ref_asset_categories_read ON inventory.ref_asset_categories FOR SELECT USING (TRUE);

DROP POLICY IF EXISTS p_ref_failure_codes_read ON maintenance.ref_failure_codes;
CREATE POLICY p_ref_failure_codes_read ON maintenance.ref_failure_codes FOR SELECT USING (TRUE);

DROP POLICY IF EXISTS p_ref_compliance_standards_read ON audit.ref_compliance_standards;
CREATE POLICY p_ref_compliance_standards_read ON audit.ref_compliance_standards FOR SELECT USING (TRUE);

COMMIT;