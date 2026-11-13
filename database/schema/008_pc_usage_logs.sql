-- =============================================================================
-- LabTrack Schema Initialization: 008_pc_usage_logs.sql
-- Database Engine: PostgreSQL 14+
-- Module: Workstation Inventory, Active User Sessions, Telemetry Logs, & Partitioning
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: telemetry.pc_workstations
-- Purpose: Specific hardware specifications and real-time connectivity status
--          for PC assets deployed across laboratory workbenches and instrument bays.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.pc_workstations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID UNIQUE REFERENCES inventory.assets(id) ON DELETE SET NULL,
    hostname VARCHAR(100) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL,
    mac_address VARCHAR(17) NOT NULL UNIQUE,
    os_version VARCHAR(100) NOT NULL,
    cpu_model VARCHAR(150),
    ram_gb NUMERIC(6, 2),
    gpu_model VARCHAR(150),
    is_online BOOLEAN NOT NULL DEFAULT FALSE,
    last_ping_at TIMESTAMP WITH TIME ZONE,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pc_workstations_hostname ON telemetry.pc_workstations(hostname);
CREATE INDEX IF NOT EXISTS idx_pc_workstations_is_online ON telemetry.pc_workstations(is_online);
CREATE INDEX IF NOT EXISTS idx_pc_workstations_asset_id ON telemetry.pc_workstations(asset_id);

-- Attach automatic timestamp and OCC version triggers
DROP TRIGGER IF EXISTS trg_pc_workstations_update_timestamp ON telemetry.pc_workstations;
CREATE TRIGGER trg_pc_workstations_update_timestamp
BEFORE UPDATE ON telemetry.pc_workstations
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_pc_workstations_increment_version ON telemetry.pc_workstations;
CREATE TRIGGER trg_pc_workstations_increment_version
BEFORE UPDATE ON telemetry.pc_workstations
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 2. Table: telemetry.pc_usage_sessions
-- Purpose: Records active researcher OS user login/logout events, idle duration,
--          and automated session termination metrics.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.pc_usage_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workstation_id UUID NOT NULL REFERENCES telemetry.pc_workstations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
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

CREATE INDEX IF NOT EXISTS idx_pc_sessions_workstation_id ON telemetry.pc_usage_sessions(workstation_id);
CREATE INDEX IF NOT EXISTS idx_pc_sessions_user_id ON telemetry.pc_usage_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_pc_sessions_logged_in_at ON telemetry.pc_usage_sessions(logged_in_at DESC);


-- -----------------------------------------------------------------------------
-- 3. Table: telemetry.pc_telemetry_logs (Declaratively Partitioned by Range)
-- Purpose: High-frequency time-series telemetry store for workstation metrics 
--          (CPU, RAM, GPU, thermals, I/O rates) partitioned monthly by recorded_at.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.pc_telemetry_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY,
    workstation_id UUID NOT NULL REFERENCES telemetry.pc_workstations(id) ON DELETE CASCADE,
    cpu_usage_pct NUMERIC(5, 2) NOT NULL,
    ram_usage_pct NUMERIC(5, 2) NOT NULL,
    gpu_usage_pct NUMERIC(5, 2),
    disk_usage_pct NUMERIC(5, 2) NOT NULL,
    cpu_temp_celsius NUMERIC(5, 2),
    gpu_temp_celsius NUMERIC(5, 2),
    network_rx_kbps NUMERIC(10, 2) DEFAULT 0.00,
    network_tx_kbps NUMERIC(10, 2) DEFAULT 0.00,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (id, recorded_at)
) PARTITION BY RANGE (recorded_at);

-- Performance Indexing across time-series partitions
CREATE INDEX IF NOT EXISTS idx_pc_telemetry_workstation_time 
    ON telemetry.pc_telemetry_logs(workstation_id, recorded_at DESC);

-- Initial Monthly Partitions
CREATE TABLE IF NOT EXISTS telemetry.pc_telemetry_logs_y2026m10 PARTITION OF telemetry.pc_telemetry_logs
    FOR VALUES FROM ('2026-10-01 00:00:00+00') TO ('2026-11-01 00:00:00+00');

CREATE TABLE IF NOT EXISTS telemetry.pc_telemetry_logs_y2026m11 PARTITION OF telemetry.pc_telemetry_logs
    FOR VALUES FROM ('2026-11-01 00:00:00+00') TO ('2026-12-01 00:00:00+00');

CREATE TABLE IF NOT EXISTS telemetry.pc_telemetry_logs_y2026m12 PARTITION OF telemetry.pc_telemetry_logs
    FOR VALUES FROM ('2026-12-01 00:00:00+00') TO ('2027-01-01 00:00:00+00');

CREATE TABLE IF NOT EXISTS telemetry.pc_telemetry_logs_default PARTITION OF telemetry.pc_telemetry_logs DEFAULT;


-- -----------------------------------------------------------------------------
-- 4. Function: telemetry.fn_record_pc_heartbeat
-- Purpose: Ingests agent telemetry, updates current workstation connectivity state,
--          and updates asset_telemetry_summaries in a single atomic transaction.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION telemetry.fn_record_pc_heartbeat(
    p_workstation_id UUID,
    p_cpu_usage NUMERIC(5, 2),
    p_ram_usage NUMERIC(5, 2),
    p_disk_usage NUMERIC(5, 2),
    p_gpu_usage NUMERIC(5, 2) DEFAULT NULL,
    p_cpu_temp NUMERIC(5, 2) DEFAULT NULL,
    p_gpu_temp NUMERIC(5, 2) DEFAULT NULL,
    p_network_rx NUMERIC(10, 2) DEFAULT 0.00,
    p_network_tx NUMERIC(10, 2) DEFAULT 0.00
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_asset_id UUID;
BEGIN
    -- Update workstation live ping state
    UPDATE telemetry.pc_workstations
    SET is_online = TRUE,
        last_ping_at = NOW(),
        updated_at = NOW()
    WHERE id = p_workstation_id
    RETURNING asset_id INTO v_asset_id;

    -- Append high-frequency time-series record
    INSERT INTO telemetry.pc_telemetry_logs (
        workstation_id,
        cpu_usage_pct,
        ram_usage_pct,
        gpu_usage_pct,
        disk_usage_pct,
        cpu_temp_celsius,
        gpu_temp_celsius,
        network_rx_kbps,
        network_tx_kbps,
        recorded_at
    ) VALUES (
        p_workstation_id,
        p_cpu_usage,
        p_ram_usage,
        p_gpu_usage,
        p_disk_usage,
        p_cpu_temp,
        p_gpu_temp,
        p_network_rx,
        p_network_tx,
        NOW()
    );

    -- Sync summary metrics to linked physical asset if exists
    IF v_asset_id IS NOT NULL THEN
        INSERT INTO inventory.asset_telemetry_summaries (
            asset_id,
            cpu_usage_pct,
            ram_usage_pct,
            temperature_celsius,
            last_ping_at,
            updated_at
        ) VALUES (
            v_asset_id,
            p_cpu_usage,
            p_ram_usage,
            p_cpu_temp,
            NOW(),
            NOW()
        )
        ON CONFLICT (asset_id) DO UPDATE
        SET cpu_usage_pct = EXCLUDED.cpu_usage_pct,
            ram_usage_pct = EXCLUDED.ram_usage_pct,
            temperature_celsius = EXCLUDED.temperature_celsius,
            last_ping_at = EXCLUDED.last_ping_at,
            updated_at = EXCLUDED.updated_at;
    END IF;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. View: telemetry.v_workstation_health_summaries
-- Purpose: Real-time dashboard view highlighting online status, hardware resource
--          pressure, and current logged-in active sessions.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW telemetry.v_workstation_health_summaries AS
SELECT 
    w.id AS workstation_id,
    w.hostname,
    w.ip_address,
    w.is_online,
    w.last_ping_at,
    a.id AS asset_id,
    a.name AS asset_name,
    s.id AS active_session_id,
    u.full_name AS active_user_name,
    u.email AS active_user_email,
    s.logged_in_at AS session_started_at,
    ROUND(EXTRACT(EPOCH FROM (NOW() - w.last_ping_at)) / 60.0, 1) AS minutes_since_last_ping
FROM 
    telemetry.pc_workstations w
LEFT JOIN 
    inventory.assets a ON w.asset_id = a.id
LEFT JOIN 
    telemetry.pc_usage_sessions s ON w.id = s.workstation_id AND s.logged_out_at IS NULL
LEFT JOIN 
    auth.users u ON s.user_id = u.id;


-- -----------------------------------------------------------------------------
-- 6. Maintenance & Security Configuration
-- -----------------------------------------------------------------------------

-- Custom autovacuum tuning for telemetry logs
ALTER TABLE telemetry.pc_usage_sessions SET (
    autovacuum_vacuum_scale_factor = 0.10,
    autovacuum_analyze_scale_factor = 0.05
);

ALTER TABLE telemetry.pc_telemetry_logs SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02
);

-- Enable RLS
ALTER TABLE telemetry.pc_workstations ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.pc_usage_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.pc_telemetry_logs ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_pc_workstations_read ON telemetry.pc_workstations;
CREATE POLICY p_pc_workstations_read ON telemetry.pc_workstations
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_pc_sessions_read ON telemetry.pc_usage_sessions;
CREATE POLICY p_pc_sessions_read ON telemetry.pc_usage_sessions
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_pc_telemetry_read ON telemetry.pc_telemetry_logs;
CREATE POLICY p_pc_telemetry_read ON telemetry.pc_telemetry_logs
    FOR SELECT
    USING (TRUE);

COMMIT;