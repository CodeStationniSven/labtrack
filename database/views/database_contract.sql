-- =============================================================================
-- LabTrack System Initialization Contract: database_contract.sql
-- Database Engine: PostgreSQL 14+
-- Purpose: Unified master contract orchestrating extensions, domain schemas,
--          type definitions, cross-module triggers, and deployment verification.
-- =============================================================================

BEGIN;

-- Setup temporary session bypass user context for system verification
SET LOCAL app.current_user_id = '00000000-0000-0000-0000-000000000001';

-- -----------------------------------------------------------------------------
-- 1. EXTENSIONS & DOMAIN SCHEMAS
-- Establish necessary database extensions and domain boundary schemas.
-- -----------------------------------------------------------------------------

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Optional pgvector extension for AI semantic search embeddings
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        BEGIN
            CREATE EXTENSION vector;
        EXCEPTION
            WHEN OTHERS THEN
                RAISE NOTICE 'pgvector extension not installed in environment. Falling back to text embedding storage.';
        END;
    END IF;
END $$;

-- Domain Schema Namespaces
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS scheduling;
CREATE SCHEMA IF NOT EXISTS telemetry;
CREATE SCHEMA IF NOT EXISTS maintenance;
CREATE SCHEMA IF NOT EXISTS financial;
CREATE SCHEMA IF NOT EXISTS ai;
CREATE SCHEMA IF NOT EXISTS audit;


-- -----------------------------------------------------------------------------
-- 2. ENUMERATED TYPES & COMMON TYPES
-- Unified system state enumerations across modules.
-- -----------------------------------------------------------------------------

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'asset_status' AND typnamespace = 'inventory'::regnamespace) THEN
        CREATE TYPE inventory.asset_status AS ENUM (
            'Available', 'In Use', 'Reserved', 'Under Maintenance', 'Calibration Required', 'Decommissioned'
        );
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ticket_priority' AND typnamespace = 'maintenance'::regnamespace) THEN
        CREATE TYPE maintenance.ticket_priority AS ENUM (
            'Low', 'Medium', 'High', 'Critical'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ticket_status' AND typnamespace = 'maintenance'::regnamespace) THEN
        CREATE TYPE maintenance.ticket_status AS ENUM (
            'Open', 'Assigned', 'In Progress', 'Waiting For Parts', 'Resolved', 'Closed', 'Cancelled'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'maintenance_outcome' AND typnamespace = 'maintenance'::regnamespace) THEN
        CREATE TYPE maintenance.maintenance_outcome AS ENUM (
            'Passed', 'PassedWithObservations', 'Failed', 'Incomplete'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'forensic_category' AND typnamespace = 'audit'::regnamespace) THEN
        CREATE TYPE audit.forensic_category AS ENUM (
            'AUTHENTICATION', 'AUTHORIZATION', 'DATA_MUTATION', 'CONFIG_MUTATION', 'SAFETY_OVERRIDE', 'FINANCIAL_TRANSACTION'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'notification_channel' AND typnamespace = 'audit'::regnamespace) THEN
        CREATE TYPE audit.notification_channel AS ENUM (
            'InApp', 'Email', 'Push', 'Webhook'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'depreciation_method' AND typnamespace = 'financial'::regnamespace) THEN
        CREATE TYPE financial.depreciation_method AS ENUM (
            'Straight-Line', 'Declining-Balance', 'Sum-Of-Years-Digits'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'transaction_type' AND typnamespace = 'financial'::regnamespace) THEN
        CREATE TYPE financial.transaction_type AS ENUM (
            'Purchase', 'MaintenanceExpense', 'DepreciationWriteOff', 'UsageChargeback', 'SalvageAdjustment'
        );
    END IF;
END $$;


-- -----------------------------------------------------------------------------
-- 3. COMMON CORE TRIGGER FUNCTIONS
-- Shared infrastructure utilities for automatic timestamps, versioning, and auditing.
-- -----------------------------------------------------------------------------

-- Timestamp Auto-Updater
CREATE OR REPLACE FUNCTION public.fn_update_timestamp()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$;

-- Optimistic Concurrency Control (OCC) Version Incrementer
CREATE OR REPLACE FUNCTION public.fn_increment_entity_version()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.version := OLD.version + 1;
    RETURN NEW;
END;
$$;

-- Generic Table Mutation Auditor
CREATE OR REPLACE FUNCTION public.fn_audit_table_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_actor_id UUID;
    v_record_id UUID;
    v_payload_before JSONB := NULL;
    v_payload_after JSONB := NULL;
BEGIN
    v_actor_id := NULLIF(current_setting('app.current_user_id', true), '')::uuid;

    IF TG_OP = 'DELETE' THEN
        v_record_id := OLD.id;
        v_payload_before := to_jsonb(OLD);
    ELSIF TG_OP = 'UPDATE' THEN
        v_record_id := NEW.id;
        v_payload_before := to_jsonb(OLD);
        v_payload_after := to_jsonb(NEW);
    ELSIF TG_OP = 'INSERT' THEN
        v_record_id := NEW.id;
        v_payload_after := to_jsonb(NEW);
    END IF;

    PERFORM audit.fn_append_forensic_ledger_entry(
        v_actor_id,
        '0.0.0.0',
        'DATA_MUTATION'::audit.forensic_category,
        TG_OP,
        TG_TABLE_SCHEMA,
        TG_TABLE_NAME,
        v_record_id,
        v_payload_before,
        v_payload_after
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. SYSTEM DEPLOYMENT INTEGRITY VERIFICATION FUNCTION
-- Checks table, view, and function counts to validate deployment completeness.
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_verify_labtrack_schema_deployment()
RETURNS TABLE (
    schema_name VARCHAR(64),
    tables_count INT,
    views_count INT,
    functions_count INT,
    status VARCHAR(20)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH target_schemas AS (
        SELECT unnest(ARRAY['auth', 'inventory', 'scheduling', 'telemetry', 'maintenance', 'financial', 'ai', 'audit']) AS s_name
    )
    SELECT 
        ts.s_name::VARCHAR(64) AS schema_name,
        COALESCE(t.t_cnt, 0)::INT AS tables_count,
        COALESCE(v.v_cnt, 0)::INT AS views_count,
        COALESCE(f.f_cnt, 0)::INT AS functions_count,
        CASE 
            WHEN COALESCE(t.t_cnt, 0) + COALESCE(v.v_cnt, 0) + COALESCE(f.f_cnt, 0) > 0 THEN 'VERIFIED'::VARCHAR(20)
            ELSE 'MISSING'::VARCHAR(20)
        END AS status
    FROM 
        target_schemas ts
    LEFT JOIN (
        SELECT table_schema, COUNT(*) AS t_cnt 
        FROM information_schema.tables 
        WHERE table_type = 'BASE TABLE' 
        GROUP BY table_schema
    ) t ON ts.s_name = t.table_schema
    LEFT JOIN (
        SELECT table_schema, COUNT(*) AS v_cnt 
        FROM information_schema.tables 
        WHERE table_type = 'VIEW' 
        GROUP BY table_schema
    ) v ON ts.s_name = v.table_schema
    LEFT JOIN (
        SELECT routine_schema, COUNT(*) AS f_cnt 
        FROM information_schema.routines 
        WHERE routine_type = 'FUNCTION' 
        GROUP BY routine_schema
    ) f ON ts.s_name = f.routine_schema
    ORDER BY 
        ts.s_name ASC;
END;
$$;

COMMIT;