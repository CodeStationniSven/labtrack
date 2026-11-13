-- =============================================================================
-- LabTrack Universal Ticketing & Service Request Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Consolidated Ticket Queue, State Workflows, & Technician Assignments
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: universal_tickets
-- Purpose: Unified ticketing table linking repair requests, problem reports, 
--          and general facility inquiries into a single processing queue.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS universal_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    ticket_type VARCHAR(40) NOT NULL CHECK (
        ticket_type IN ('Repair', 'IncidentReport', 'SafetyAudit', 'FacilityInquiry', 'Calibration')
    ),
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    reporter_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assigned_technician_id UUID REFERENCES users(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    priority VARCHAR(30) NOT NULL DEFAULT 'Medium' CHECK (
        priority IN ('Low', 'Medium', 'High', 'Critical')
    ),
    status VARCHAR(50) NOT NULL DEFAULT 'Open' CHECK (
        status IN ('Open', 'Triaged', 'In Progress', 'Awaiting Parts', 'Resolved', 'Closed', 'Cancelled')
    ),
    estimated_cost_usd NUMERIC(12, 2) DEFAULT 0.00,
    actual_cost_usd NUMERIC(12, 2) DEFAULT 0.00,
    due_date TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for multi-criteria ticket queue filtering
CREATE INDEX IF NOT EXISTS idx_universal_tickets_type_status 
    ON universal_tickets(ticket_type, status);

CREATE INDEX IF NOT EXISTS idx_universal_tickets_assigned_tech 
    ON universal_tickets(assigned_technician_id);

CREATE INDEX IF NOT EXISTS idx_universal_tickets_reporter 
    ON universal_tickets(reporter_id);

CREATE INDEX IF NOT EXISTS idx_universal_tickets_asset 
    ON universal_tickets(asset_id);

CREATE INDEX IF NOT EXISTS idx_universal_tickets_priority 
    ON universal_tickets(priority);


-- Auto-generate human-readable universal ticket numbers (e.g., TKT-20261021-XXXX)
CREATE OR REPLACE FUNCTION fn_generate_universal_ticket_number()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.ticket_number IS NULL OR NEW.ticket_number = '' THEN
        NEW.ticket_number := 'TKT-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || SUBSTRING(NEW.id::text FROM 1 FOR 6);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_universal_ticket_number ON universal_tickets;
CREATE TRIGGER trg_set_universal_ticket_number
BEFORE INSERT ON universal_tickets
FOR EACH ROW
EXECUTE FUNCTION fn_generate_universal_ticket_number();


-- -----------------------------------------------------------------------------
-- Table: ticket_activity_logs
-- Purpose: Audit log recording ticket status changes, technician reassignment,
--          and progress notes across the ticket lifecycle.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ticket_activity_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES universal_tickets(id) ON DELETE CASCADE,
    performed_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    activity_type VARCHAR(50) NOT NULL CHECK (
        activity_type IN ('Created', 'StatusChange', 'Assignment', 'CommentAdded', 'CostUpdated', 'Resolved')
    ),
    previous_status VARCHAR(50),
    new_status VARCHAR(50),
    note TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ticket_activity_ticket_id 
    ON ticket_activity_logs(ticket_id, created_at ASC);


-- -----------------------------------------------------------------------------
-- Function: fn_get_technician_workload_summary
-- Purpose: Aggregates active ticket counts, priority distributions, and mean
--          resolution velocity per technician for resource dispatching.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_technician_workload_summary()
RETURNS TABLE (
    technician_id UUID,
    technician_name VARCHAR(150),
    technician_email VARCHAR(255),
    active_tickets_count BIGINT,
    critical_tickets_count BIGINT,
    resolved_past_30_days BIGINT,
    avg_resolution_hours NUMERIC(8, 2)
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.id AS technician_id,
        u.full_name AS technician_name,
        u.email AS technician_email,
        COUNT(t.id) FILTER (WHERE t.status IN ('Open', 'Triaged', 'In Progress', 'Awaiting Parts'))::BIGINT AS active_tickets_count,
        COUNT(t.id) FILTER (WHERE t.status IN ('Open', 'Triaged', 'In Progress', 'Awaiting Parts') AND t.priority = 'Critical')::BIGINT AS critical_tickets_count,
        COUNT(t.id) FILTER (WHERE t.status = 'Resolved' AND t.resolved_at >= NOW() - INTERVAL '30 days')::BIGINT AS resolved_past_30_days,
        COALESCE(
            AVG(EXTRACT(EPOCH FROM (t.resolved_at - t.created_at)) / 3600.0) FILTER (WHERE t.status = 'Resolved' AND t.resolved_at >= NOW() - INTERVAL '30 days'), 
            0.00
        )::NUMERIC(8, 2) AS avg_resolution_hours
    FROM 
        users u
    JOIN 
        roles r ON u.role_id = r.id
    LEFT JOIN 
        universal_tickets t ON u.id = t.assigned_technician_id
    WHERE 
        r.name IN ('Technician', 'Admin')
    GROUP BY 
        u.id, u.full_name, u.email
    ORDER BY 
        active_tickets_count DESC;
END;
$$;

COMMIT;