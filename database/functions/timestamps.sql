-- =============================================================================
-- LabTrack Audit & Automated Timestamp Management Triggers
-- Database Engine: PostgreSQL 14+
-- Module: System Audit Triggers, Temporal Tracking, & Generic Timestamp Maintenance
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Function: fn_update_timestamp
-- Purpose: Reusable trigger function to automatically update the `updated_at`
--          column on any record modification.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_timestamp()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$ BEGIN NEW.updated_at := NOW(); RETURN NEW; END; $$;


-- -----------------------------------------------------------------------------
-- Apply `updated_at` triggers across core application entities
-- -----------------------------------------------------------------------------

-- 1. Users
DROP TRIGGER IF EXISTS trg_users_update_timestamp ON users;
CREATE TRIGGER trg_users_update_timestamp
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION fn_update_timestamp();

-- 2. Assets (if assets table exists in core schema)
DO $$
BEGIN
    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'assets') THEN
        EXECUTE 'DROP TRIGGER IF EXISTS trg_assets_update_timestamp ON assets;';
        EXECUTE 'CREATE TRIGGER trg_assets_update_timestamp BEFORE UPDATE ON assets FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();';
    END IF;
END $$;  -- 3. Repair Tickets DROP TRIGGER IF EXISTS trg_repair_tickets_update_timestamp ON repair_tickets; CREATE TRIGGER trg_repair_tickets_update_timestamp BEFORE UPDATE ON repair_tickets FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();  -- 4. Problem Reports DROP TRIGGER IF EXISTS trg_problem_reports_update_timestamp ON problem_reports; CREATE TRIGGER trg_problem_reports_update_timestamp BEFORE UPDATE ON problem_reports FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();  -- 5. Reservation Schedules DROP TRIGGER IF EXISTS trg_reservation_schedules_update_timestamp ON reservation_schedules; CREATE TRIGGER trg_reservation_schedules_update_timestamp BEFORE UPDATE ON reservation_schedules FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();  -- 6. PC Workstations DROP TRIGGER IF EXISTS trg_pc_workstations_update_timestamp ON pc_workstations; CREATE TRIGGER trg_pc_workstations_update_timestamp BEFORE UPDATE ON pc_workstations FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();  -- 7. QR Tokens DROP TRIGGER IF EXISTS trg_qr_tokens_update_timestamp ON qr_tokens; CREATE TRIGGER trg_qr_tokens_update_timestamp BEFORE UPDATE ON qr_tokens FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();   -- ----------------------------------------------------------------------------- -- Function: fn_audit_table_changes -- Purpose: Generic trigger function to log column-level mutations to dedicated --          audit tables in JSONB payload format. -- ----------------------------------------------------------------------------- CREATE OR REPLACE FUNCTION fn_audit_table_changes() RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_audit_table_name VARCHAR(63);
    v_action VARCHAR(20);
    v_changed_by VARCHAR(255);
    v_payload JSONB;
BEGIN
    v_audit_table_name := TG_TABLE_NAME || '_audit';
    v_action := TG_OP;

    -- Extract active DB user or application context user
    v_changed_by := COALESCE(
        current_setting('app.current_user', true), 
        session_user
    );

    IF (TG_OP = 'DELETE') THEN
        v_payload := to_jsonb(OLD);
        EXECUTE format(
            'INSERT INTO %I (entity_id, action_type, changed_by, change_payload, performed_at) VALUES ($1, $2, $3, $4, NOW())',
            v_audit_table_name
        ) USING OLD.id, v_action, v_changed_by, v_payload;
        RETURN OLD;
    ELSIF (TG_OP = 'UPDATE') THEN
        v_payload := jsonb_build_object(
            'before', to_jsonb(OLD),
            'after', to_jsonb(NEW)
        );
        EXECUTE format(
            'INSERT INTO %I (entity_id, action_type, changed_by, change_payload, performed_at) VALUES ($1, $2, $3, $4, NOW())',
            v_audit_table_name
        ) USING NEW.id, v_action, v_changed_by, v_payload;
        RETURN NEW;
    ELSIF (TG_OP = 'INSERT') THEN
        v_payload := to_jsonb(NEW);
        EXECUTE format(
            'INSERT INTO %I (entity_id, action_type, changed_by, change_payload, performed_at) VALUES ($1, $2, $3, $4, NOW())', v_audit_table_name ) USING NEW.id, v_action, v_changed_by, v_payload; RETURN NEW; END IF;      RETURN NULL; END; $$;

COMMIT;