-- =============================================================================
-- LabTrack Migration Script: 001_initial.sql
-- Database Engine: PostgreSQL 14+
-- Description: Complete initial schema deployment incorporating security, core assets,
--              telemetry, ticketing, booking schedules, QR check-ins, and auditing.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 0. Extensions Setup
-- -----------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- Optional extension check for vector embeddings
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        RAISE NOTICE 'pgvector extension not installed. AI knowledge embedding tables will be deferred.';
    END IF;
END $$;


-- -----------------------------------------------------------------------------
-- 1. Security & Authentication Schemas
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS roles (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

INSERT INTO roles (name, description) VALUES
    ('Admin', 'Full administrative access across all laboratory services and user management'),
    ('Technician', 'Maintenance, equipment repair ticketing, and diagnostic telemetry access'),
    ('Researcher', 'Standard operational user; asset booking, safety evaluation, and AI assistance')
ON CONFLICT (name) DO NOTHING;

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_role_id ON users(role_id);

CREATE TABLE IF NOT EXISTS user_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    department VARCHAR(150),
    office_location VARCHAR(100),
    phone_number VARCHAR(30),
    emergency_contact_name VARCHAR(150),
    emergency_contact_phone VARCHAR(30),
    avatar_url VARCHAR(500),
    bio TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_user_profiles_user_id ON user_profiles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_profiles_department ON user_profiles(department);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    replaced_by_token VARCHAR(255)
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_hash ON refresh_tokens(token_hash);

CREATE TABLE IF NOT EXISTS internal_service_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(100) NOT NULL UNIQUE,
    secret_key_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

INSERT INTO internal_service_keys (service_name, secret_key_hash) VALUES
    ('LabTrack.CSharpGateway', '$2b$12$e868d/9JpI26O8jDk2/Y0.vA6K9b5R2G08G39G2.9zR2m4N8u2E4i')
ON CONFLICT (service_name) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 2. Core Physical Assets Catalog
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

CREATE INDEX IF NOT EXISTS idx_assets_category ON assets(category);
CREATE INDEX IF NOT EXISTS idx_assets_status ON assets(status);
CREATE INDEX IF NOT EXISTS idx_assets_serial_number ON assets(serial_number);
CREATE INDEX IF NOT EXISTS idx_assets_created_at ON assets(created_at DESC);

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


-- -----------------------------------------------------------------------------
-- 3. Reservations & Equipment Schedules
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS reservation_schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    reserved_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    approved_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    reservation_code VARCHAR(50) NOT NULL UNIQUE,
    purpose VARCHAR(255) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK (
        status IN ('Pending', 'Approved', 'Active', 'Completed', 'Cancelled', 'Rejected')
    ),
    cancellation_reason VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT chk_reservation_time_range CHECK (end_time > start_time),
    EXCLUDE USING gist (
        asset_id WITH =,
        tstzrange(start_time, end_time, '[)') WITH &&
    ) WHERE (status IN ('Approved', 'Active'))
);

CREATE INDEX IF NOT EXISTS idx_reservations_asset_id ON reservation_schedules(asset_id);
CREATE INDEX IF NOT EXISTS idx_reservations_user_id ON reservation_schedules(reserved_by_user_id);
CREATE INDEX IF NOT EXISTS idx_reservations_status ON reservation_schedules(status);
CREATE INDEX IF NOT EXISTS idx_reservations_time_range ON reservation_schedules(start_time, end_time);


-- -----------------------------------------------------------------------------
-- 4. Workstation & PC Telemetry
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS pc_workstations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID UNIQUE REFERENCES assets(id) ON DELETE SET NULL,
    hostname VARCHAR(100) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL,
    mac_address VARCHAR(17) NOT NULL UNIQUE,
    os_version VARCHAR(100) NOT NULL,
    cpu_model VARCHAR(150),
    ram_gb NUMERIC(6, 2),
    gpu_model VARCHAR(150),
    is_online BOOLEAN NOT NULL DEFAULT FALSE,
    last_ping_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pc_workstations_hostname ON pc_workstations(hostname);
CREATE INDEX IF NOT EXISTS idx_pc_workstations_is_online ON pc_workstations(is_online);

CREATE TABLE IF NOT EXISTS pc_usage_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workstation_id UUID NOT NULL REFERENCES pc_workstations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    logged_in_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    logged_out_at TIMESTAMP WITH TIME ZONE,
    idle_time_seconds INT DEFAULT 0,
    active_duration_minutes NUMERIC(10, 2) GENERATED ALWAYS AS (
        CASE 
            WHEN logged_out_at IS NOT NULL 
            THEN EXTRACT(EPOCH FROM (logged_out_at - logged_in_at)) / 60.0
            ELSE NULL 
        END
    ) STORED,
    termination_reason VARCHAR(100) DEFAULT 'Normal Logout'
);

CREATE INDEX IF NOT EXISTS idx_pc_sessions_workstation_id ON pc_usage_sessions(workstation_id);
CREATE INDEX IF NOT EXISTS idx_pc_sessions_user_id ON pc_usage_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_pc_sessions_logged_in_at ON pc_usage_sessions(logged_in_at);

CREATE TABLE IF NOT EXISTS pc_telemetry_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workstation_id UUID NOT NULL REFERENCES pc_workstations(id) ON DELETE CASCADE,
    cpu_usage_pct NUMERIC(5, 2) NOT NULL,
    ram_usage_pct NUMERIC(5, 2) NOT NULL,
    gpu_usage_pct NUMERIC(5, 2),
    disk_usage_pct NUMERIC(5, 2) NOT NULL,
    cpu_temp_celsius NUMERIC(5, 2),
    gpu_temp_celsius NUMERIC(5, 2),
    network_rx_kbps NUMERIC(10, 2) DEFAULT 0.00,
    network_tx_kbps NUMERIC(10, 2) DEFAULT 0.00,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pc_telemetry_workstation_time 
    ON pc_telemetry_logs(workstation_id, recorded_at DESC);


-- -----------------------------------------------------------------------------
-- 5. Maintenance, Incidents & Repair Tickets
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS repair_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    requested_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    assigned_technician_id UUID REFERENCES users(id) ON DELETE SET NULL,
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    priority VARCHAR(30) NOT NULL DEFAULT 'Medium' CHECK (
        priority IN ('Low', 'Medium', 'High', 'Critical')
    ),
    status VARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK (
        status IN ('Pending', 'In Progress', 'Awaiting Parts', 'Resolved', 'Cancelled')
    ),
    repair_tier VARCHAR(50) DEFAULT 'In-House Repair',
    parts_cost_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    labor_cost_usd NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    estimated_downtime_hours NUMERIC(8, 2) DEFAULT 0.00,
    resolved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_repair_tickets_asset_id ON repair_tickets(asset_id);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_status ON repair_tickets(status);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_priority ON repair_tickets(priority);
CREATE INDEX IF NOT EXISTS idx_repair_tickets_technician ON repair_tickets(assigned_technician_id);

CREATE TABLE IF NOT EXISTS problem_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    reported_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    report_number VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    severity_level VARCHAR(30) NOT NULL CHECK (
        severity_level IN ('Low', 'Medium', 'High', 'Critical')
    ),
    category VARCHAR(80) NOT NULL DEFAULT 'General Defect',
    status VARCHAR(50) NOT NULL DEFAULT 'Open' CHECK (
        status IN ('Open', 'Under Review', 'In Progress', 'Resolved', 'Closed', 'Rejected')
    ),
    has_safety_hazard BOOLEAN NOT NULL DEFAULT FALSE,
    attachment_urls TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_problem_reports_asset_id ON problem_reports(asset_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_reported_by ON problem_reports(reported_by_user_id);
CREATE INDEX IF NOT EXISTS idx_problem_reports_status ON problem_reports(status);
CREATE INDEX IF NOT EXISTS idx_problem_reports_severity ON problem_reports(severity_level);


-- -----------------------------------------------------------------------------
-- 6. QR Code Verification & Check-ins
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS qr_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    token_code VARCHAR(255) NOT NULL UNIQUE,
    checksum VARCHAR(64) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    generated_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_tokens_asset_id ON qr_tokens(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_tokens_code ON qr_tokens(token_code);

CREATE TABLE IF NOT EXISTS qr_checkin_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    qr_token_id UUID NOT NULL REFERENCES qr_tokens(id) ON DELETE RESTRICT,
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action_type VARCHAR(30) NOT NULL CHECK (
        action_type IN ('Check-In', 'Check-Out', 'Verification-Only', 'Maintenance-Scan')
    ),
    location_bay VARCHAR(100),
    latitude NUMERIC(10, 7),
    longitude NUMERIC(10, 7),
    is_successful BOOLEAN NOT NULL DEFAULT TRUE,
    failure_reason VARCHAR(255),
    scanned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_qr_checkin_token_id ON qr_checkin_logs(qr_token_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_asset_id ON qr_checkin_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_user_id ON qr_checkin_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_qr_checkin_scanned_at ON qr_checkin_logs(scanned_at DESC);


-- -----------------------------------------------------------------------------
-- 7. System Notifications & Alerts
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS system_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    recipient_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    related_asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL DEFAULT 'System' CHECK (
        notification_type IN ('System', 'Maintenance', 'SafetyAlert', 'Reservation', 'Telemetry')
    ),
    priority VARCHAR(30) NOT NULL DEFAULT 'Medium' CHECK (
        priority IN ('Low', 'Medium', 'High', 'Critical')
    ),
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    action_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notifications_recipient_user 
    ON system_notifications(recipient_user_id, is_read, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_notifications_related_asset 
    ON system_notifications(related_asset_id);


-- -----------------------------------------------------------------------------
-- 8. Automated Triggers & Maintenance Functions
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_update_timestamp()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_users_update_timestamp ON users;
CREATE TRIGGER trg_users_update_timestamp BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_assets_update_timestamp ON assets;
CREATE TRIGGER trg_assets_update_timestamp BEFORE UPDATE ON assets FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_repair_tickets_update_timestamp ON repair_tickets;
CREATE TRIGGER trg_repair_tickets_update_timestamp BEFORE UPDATE ON repair_tickets FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_reservation_schedules_update_timestamp ON reservation_schedules;
CREATE TRIGGER trg_reservation_schedules_update_timestamp BEFORE UPDATE ON reservation_schedules FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_pc_workstations_update_timestamp ON pc_workstations;
CREATE TRIGGER trg_pc_workstations_update_timestamp BEFORE UPDATE ON pc_workstations FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_qr_tokens_update_timestamp ON qr_tokens;
CREATE TRIGGER trg_qr_tokens_update_timestamp BEFORE UPDATE ON qr_tokens FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

COMMIT;