-- =============================================================================
-- LabTrack Maintenance & Repair Tickets Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Equipment Maintenance Tickets, Parts Tracking, & State Transitions
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: repair_tickets
-- Purpose: Tracks maintenance requests, hardware repairs, labor, parts costs, 
--          and resolution status for lab assets.
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

-- Auto-generate human-readable ticket numbers (e.g., REP-20261021-XXXX)
CREATE OR REPLACE FUNCTION fn_generate_repair_ticket_number()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.ticket_number IS NULL OR NEW.ticket_number = '' THEN
        NEW.ticket_number := 'REP-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_repair_ticket_number ON repair_tickets;
CREATE TRIGGER trg_set_repair_ticket_number
BEFORE INSERT ON repair_tickets
FOR EACH ROW
EXECUTE FUNCTION fn_generate_repair_ticket_number();

-- -----------------------------------------------------------------------------
-- Table: repair_itemized_parts
-- Purpose: Tracks individual hardware components, line costs, and serials
--          used during a repair ticket lifecycle.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS repair_itemized_parts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    repair_ticket_id UUID NOT NULL REFERENCES repair_tickets(id) ON DELETE CASCADE,
    part_name VARCHAR(200) NOT NULL,
    part_number VARCHAR(100),
    quantity INT NOT NULL DEFAULT 1 CHECK (quantity > 0),
    unit_cost_usd NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    total_cost_usd NUMERIC(12, 2) GENERATED ALWAYS AS (quantity * unit_cost_usd) STORED,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_repair_parts_ticket_id ON repair_itemized_parts(repair_ticket_id);

-- Trigger to recalculate total parts cost on repair ticket automatically
CREATE OR REPLACE FUNCTION fn_sync_repair_parts_cost()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_target_ticket_id UUID;
BEGIN
    v_target_ticket_id := COALESCE(NEW.repair_ticket_id, OLD.repair_ticket_id);

    UPDATE repair_tickets
    SET parts_cost_usd = COALESCE(
        (SELECT SUM(total_cost_usd) FROM repair_itemized_parts WHERE repair_ticket_id = v_target_ticket_id), 
        0.00
    ),
    updated_at = NOW()
    WHERE id = v_target_ticket_id;

    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_sync_repair_parts_cost_insert_update ON repair_itemized_parts;
CREATE TRIGGER trg_sync_repair_parts_cost_insert_update
AFTER INSERT OR UPDATE OR DELETE ON repair_itemized_parts
FOR EACH ROW
EXECUTE FUNCTION fn_sync_repair_parts_cost();

COMMIT;