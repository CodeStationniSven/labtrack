-- =============================================================================
-- LabTrack Dynamic SQL & Generic Query Builder Utilities
-- Database Engine: PostgreSQL 14+
-- Module: Dynamic Filtering, Polymorphic Search, & Audited Table Management
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Function: fn_search_assets_dynamic
-- Purpose: Builds and executes a dynamic SQL query to search assets with flexible
--          filters (category, status, keyword search, date ranges, and pagination).
-- Safe from SQL injection using QUOTE_LITERAL, QUOTE_IDENT, and parameterized EXECUTE.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_search_assets_dynamic(
    p_category VARCHAR(100) DEFAULT NULL,
    p_status VARCHAR(50) DEFAULT NULL,
    p_search_term VARCHAR(255) DEFAULT NULL,
    p_created_after TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_limit INT DEFAULT 20,
    p_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    name VARCHAR(255),
    category VARCHAR(100),
    serial_number VARCHAR(100),
    status VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE,
    total_count BIGINT
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_sql TEXT;
    v_where_clauses TEXT[] := ARRAY[]::TEXT[];
    v_where_str TEXT := '';
BEGIN
    -- Construct dynamic query base
    v_sql := '
        SELECT 
            a.id,
            a.name,
            a.category,
            a.serial_number,
            a.status,
            a.created_at,
            COUNT(*) OVER() AS total_count
        FROM assets a';

    -- Build condition array safely
    IF p_category IS NOT NULL THEN
        v_where_clauses := array_append(v_where_clauses, 'a.category = ' || quote_literal(p_category));
    END IF;

    IF p_status IS NOT NULL THEN
        v_where_clauses := array_append(v_where_clauses, 'a.status = ' || quote_literal(p_status));
    END IF;

    IF p_search_term IS NOT NULL AND TRIM(p_search_term) <> '' THEN
        v_where_clauses := array_append(
            v_where_clauses, 
            '(a.name ILIKE ' || quote_literal('%' || p_search_term || '%') || 
            ' OR a.serial_number ILIKE ' || quote_literal('%' || p_search_term || '%') || ')'
        );
    END IF;

    IF p_created_after IS NOT NULL THEN
        v_where_clauses := array_append(v_where_clauses, 'a.created_at >= ' || quote_literal(p_created_after));
    END IF;

    -- Combine WHERE filters if present
    IF array_length(v_where_clauses, 1) > 0 THEN
        v_where_str := ' WHERE ' || array_to_string(v_where_clauses, ' AND ');
    END IF;

    -- Append sorting and safe parameter bindings for pagination
    v_sql := v_sql || v_where_str || ' ORDER BY a.created_at DESC LIMIT $1 OFFSET $2';

    -- Execute dynamic statement with limit/offset parameterization
    RETURN QUERY EXECUTE v_sql USING p_limit, p_offset;
END;
$$;


-- -----------------------------------------------------------------------------
-- Function: fn_get_entity_audit_trail_dynamic
-- Purpose: Dynamically queries audit history for any table that implements
--          the LabTrack standard audit columns (id, entity_id, updated_at).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_get_entity_audit_trail_dynamic(
    p_target_table VARCHAR(63),
    p_entity_id UUID,
    p_limit INT DEFAULT 50
)
RETURNS TABLE (
    audit_id UUID,
    entity_id UUID,
    action_type VARCHAR(20),
    changed_by VARCHAR(255),
    change_payload JSONB,
    performed_at TIMESTAMP WITH TIME ZONE
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_query TEXT;
    v_safe_table_name VARCHAR(63);
BEGIN
    -- Sanitize table identifier to prevent injection via object names
    v_safe_table_name := quote_ident(p_target_table);

    -- Build safe dynamic query using target table
    v_query := format(
        'SELECT id, entity_id, action_type, changed_by, change_payload, performed_at 
         FROM %I 
         WHERE entity_id = $1 
         ORDER BY performed_at DESC 
         LIMIT $2',
        v_safe_table_name
    );

    RETURN QUERY EXECUTE v_query USING p_entity_id, p_limit;
EXCEPTION
    WHEN undefined_table THEN
        RAISE EXCEPTION 'Audit log table % does not exist.', p_target_table
            USING HINT = 'Ensure the audit table name matches standard schema naming conventions.';
END;
$$;

COMMIT;