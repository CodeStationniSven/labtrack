-- =============================================================================
-- LabTrack Schema Initialization: 015_network.sql
-- Database Engine: PostgreSQL 14+
-- Module: Network Interfaces, Switch Port Mappings, VLAN Allocations, & Incident Auditing
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: telemetry.network_switches
-- Purpose: Primary registry of managed network switches, rack locations, and
--          core network infrastructure distribution units across facilities.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.network_switches (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    facility_id UUID REFERENCES inventory.facilities(id) ON DELETE SET NULL,
    laboratory_id UUID REFERENCES inventory.laboratories(id) ON DELETE SET NULL,
    hostname VARCHAR(100) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL UNIQUE,
    mac_address VARCHAR(17) NOT NULL UNIQUE,
    model_number VARCHAR(100),
    total_ports INT NOT NULL DEFAULT 48 CHECK (total_ports > 0),
    location_rack VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_network_switches_hostname ON telemetry.network_switches(hostname);
CREATE INDEX IF NOT EXISTS idx_network_switches_facility ON telemetry.network_switches(facility_id);
CREATE INDEX IF NOT EXISTS idx_network_switches_lab ON telemetry.network_switches(laboratory_id);

-- Attach automatic timestamp trigger
DROP TRIGGER IF EXISTS trg_network_switches_update_timestamp ON telemetry.network_switches;
CREATE TRIGGER trg_network_switches_update_timestamp
BEFORE UPDATE ON telemetry.network_switches
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: telemetry.network_ports
-- Purpose: Individual physical switch ports mapping VLAN isolation zones, speed
--          negotiation, PoE configurations, and operational link states.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.network_ports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    switch_id UUID NOT NULL REFERENCES telemetry.network_switches(id) ON DELETE CASCADE,
    port_number INT NOT NULL CHECK (port_number > 0),
    port_name VARCHAR(50) NOT NULL,
    vlan_id INT NOT NULL DEFAULT 1 CHECK (vlan_id BETWEEN 1 AND 4094),
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    link_status VARCHAR(20) NOT NULL DEFAULT 'Down' CHECK (
        link_status IN ('Up', 'Down', 'Disabled', 'Error-Disabled')
    ),
    speed_mbps INT DEFAULT 1000 CHECK (speed_mbps >= 0),
    poe_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    CONSTRAINT uq_switch_port UNIQUE (switch_id, port_number)
);

CREATE INDEX IF NOT EXISTS idx_network_ports_switch ON telemetry.network_ports(switch_id);
CREATE INDEX IF NOT EXISTS idx_network_ports_vlan ON telemetry.network_ports(vlan_id);
CREATE INDEX IF NOT EXISTS idx_network_ports_status ON telemetry.network_ports(link_status);

DROP TRIGGER IF EXISTS trg_network_ports_update_timestamp ON telemetry.network_ports;
CREATE TRIGGER trg_network_ports_update_timestamp
BEFORE UPDATE ON telemetry.network_ports
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. Table: telemetry.pc_network_interfaces
-- Purpose: Network interface card (NIC) topology mapping workstation PCs and
--          lab instruments to active physical switch ports.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.pc_network_interfaces (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workstation_id UUID NOT NULL REFERENCES telemetry.pc_workstations(id) ON DELETE CASCADE,
    network_port_id UUID REFERENCES telemetry.network_ports(id) ON DELETE SET NULL,
    interface_name VARCHAR(50) NOT NULL,
    mac_address VARCHAR(17) NOT NULL UNIQUE,
    ipv4_address VARCHAR(15),
    ipv6_address VARCHAR(45),
    is_primary BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pc_interfaces_workstation ON telemetry.pc_network_interfaces(workstation_id);
CREATE INDEX IF NOT EXISTS idx_pc_interfaces_port ON telemetry.pc_network_interfaces(network_port_id);
CREATE INDEX IF NOT EXISTS idx_pc_interfaces_mac ON telemetry.pc_network_interfaces(mac_address);

DROP TRIGGER IF EXISTS trg_pc_interfaces_update_timestamp ON telemetry.pc_network_interfaces;
CREATE TRIGGER trg_pc_interfaces_update_timestamp
BEFORE UPDATE ON telemetry.pc_network_interfaces
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 4. Table: telemetry.network_incidents
-- Purpose: Captures connectivity dropouts, VLAN security violations, port flap
--          events, and automated network anomaly alerts.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.network_incidents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    switch_id UUID REFERENCES telemetry.network_switches(id) ON DELETE CASCADE,
    port_id UUID REFERENCES telemetry.network_ports(id) ON DELETE CASCADE,
    workstation_id UUID REFERENCES telemetry.pc_workstations(id) ON DELETE SET NULL,
    incident_type VARCHAR(60) NOT NULL CHECK (
        incident_type IN ('LinkDown', 'PortFlapping', 'UnauthorizedMAC', 'HighPacketLoss', 'VlanMismatch', 'HardwareFault')
    ),
    severity maintenance.ticket_priority NOT NULL DEFAULT 'Medium',
    description TEXT NOT NULL,
    is_resolved BOOLEAN NOT NULL DEFAULT FALSE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_network_incidents_switch ON telemetry.network_incidents(switch_id);
CREATE INDEX IF NOT EXISTS idx_network_incidents_port ON telemetry.network_incidents(port_id);
CREATE INDEX IF NOT EXISTS idx_network_incidents_workstation ON telemetry.network_incidents(workstation_id);
CREATE INDEX IF NOT EXISTS idx_network_incidents_unresolved ON telemetry.network_incidents(is_resolved) WHERE is_resolved = FALSE;


-- -----------------------------------------------------------------------------
-- 5. View: telemetry.v_network_topology_mapping
-- Purpose: Unified topology view linking workstations, active NICs, physical
--          switch ports, VLAN assignments, and switch locations.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW telemetry.v_network_topology_mapping AS
SELECT 
    w.id AS workstation_id,
    w.hostname AS workstation_hostname,
    ni.interface_name,
    ni.mac_address AS nic_mac_address,
    ni.ipv4_address,
    p.id AS port_id,
    p.port_name,
    p.port_number,
    p.vlan_id,
    p.link_status,
    p.speed_mbps,
    s.id AS switch_id,
    s.hostname AS switch_hostname,
    s.ip_address AS switch_ip,
    s.location_rack,
    l.room_number AS lab_room_number,
    f.name AS facility_name
FROM 
    telemetry.pc_workstations w
JOIN 
    telemetry.pc_network_interfaces ni ON w.id = ni.workstation_id
LEFT JOIN 
    telemetry.network_ports p ON ni.network_port_id = p.id
LEFT JOIN 
    telemetry.network_switches s ON p.switch_id = s.id
LEFT JOIN 
    inventory.laboratories l ON s.laboratory_id = l.id
LEFT JOIN 
    inventory.facilities f ON s.facility_id = f.id;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE telemetry.network_switches ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.network_ports ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.pc_network_interfaces ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.network_incidents ENABLE ROW LEVEL SECURITY;

-- Read Access: Authenticated internal roles can view network topology
DROP POLICY IF EXISTS p_network_switches_read ON telemetry.network_switches;
CREATE POLICY p_network_switches_read ON telemetry.network_switches
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_network_ports_read ON telemetry.network_ports;
CREATE POLICY p_network_ports_read ON telemetry.network_ports
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_pc_interfaces_read ON telemetry.pc_network_interfaces;
CREATE POLICY p_pc_interfaces_read ON telemetry.pc_network_interfaces
    FOR SELECT
    USING (TRUE);

DROP POLICY IF EXISTS p_network_incidents_read ON telemetry.network_incidents;
CREATE POLICY p_network_incidents_read ON telemetry.network_incidents
    FOR SELECT
    USING (TRUE);

-- Manage Access: Only Admins and Technicians can modify network topology or update incidents
DROP POLICY IF EXISTS p_network_topology_write ON telemetry.network_switches;
CREATE POLICY p_network_topology_write ON telemetry.network_switches
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_network_ports_write ON telemetry.network_ports;
CREATE POLICY p_network_ports_write ON telemetry.network_ports
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_network_incidents_write ON telemetry.network_incidents;
CREATE POLICY p_network_incidents_write ON telemetry.network_incidents
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;