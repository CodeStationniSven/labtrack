-- =============================================================================
-- LabTrack Workstation & PC Telemetry Usage Tracking Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Workstation Telemetry Logs, Session Tracking, & PC Aggregations
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: pc_workstations
-- Purpose: Holds hardware specifications, network addresses, and active state
--          for PC units deployed across laboratory bays.
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

-- -----------------------------------------------------------------------------
-- Table: pc_usage_sessions
-- Purpose: Records user logon/logoff durations, idle periods, and associated
--          researcher sessions on workstation hardware.
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- Table: pc_telemetry_logs
-- Purpose: High-frequency telemetry snapshots capturing system resource utilization
--          (CPU, RAM, GPU, Disk, Network) sent by the LabTrack background agent.
-- -----------------------------------------------------------------------------
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
-- Function: fn_get_workstation_usage_summary
-- Purpose: Generates operational analytics per PC unit including total active hours,
--          average CPU/RAM load, and peak temperatures over a given timeframe.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_workstation_usage_summary(
    p_start_date TIMESTAMP WITH TIME ZONE DEFAULT (NOW() - INTERVAL '7 days'),
    p_end_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
)
RETURNS TABLE (
    workstation_id UUID,
    hostname VARCHAR(100),
    total_sessions BIGINT,
    total_active_hours NUMERIC(10, 2),
    avg_cpu_usage_pct NUMERIC(5, 2),
    avg_ram_usage_pct NUMERIC(5, 2),
    max_cpu_temp_celsius NUMERIC(5, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        w.id AS workstation_id,
        w.hostname,
        COUNT(DISTINCT s.id)::BIGINT AS total_sessions,
        COALESCE(
            SUM(EXTRACT(EPOCH FROM (COALESCE(s.logged_out_at, NOW()) - s.logged_in_at)) / 3600.0), 
            0.00
        )::NUMERIC(10, 2) AS total_active_hours,
        COALESCE(AVG(t.cpu_usage_pct), 0.00)::NUMERIC(5, 2) AS avg_cpu_usage_pct,
        COALESCE(AVG(t.ram_usage_pct), 0.00)::NUMERIC(5, 2) AS avg_ram_usage_pct,
        COALESCE(MAX(t.cpu_temp_celsius), 0.00)::NUMERIC(5, 2) AS max_cpu_temp_celsius
    FROM 
        pc_workstations w
    LEFT JOIN 
        pc_usage_sessions s ON w.id = s.workstation_id 
        AND s.logged_in_at BETWEEN p_start_date AND p_end_date
    LEFT JOIN 
        pc_telemetry_logs t ON w.id = t.workstation_id 
        AND t.recorded_at BETWEEN p_start_date AND p_end_date
    GROUP BY 
        w.id, w.hostname
    ORDER BY 
        total_active_hours DESC;
END;
$$;

COMMIT;