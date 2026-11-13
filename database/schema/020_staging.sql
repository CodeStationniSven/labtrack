-- =============================================================================
-- LabTrack Schema Initialization: 020_staging.sql
-- Database Engine: PostgreSQL 14+
-- Module: Staging / Ingestion Buffers, Bulk Upload Operations, & Data Sanitization
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: inventory.stg_asset_imports
-- Purpose: Temporary unvalidated staging buffer for bulk asset CSV/JSON imports,
--          procurement batch feeds, and external system sync procedures.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.stg_asset_imports (
    import_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    batch_reference VARCHAR(100) NOT NULL,
    uploaded_by_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    raw_payload JSONB NOT NULL,
    parsed_asset_name VARCHAR(255),
    parsed_serial_number VARCHAR(100),
    parsed_category VARCHAR(100),
    parsed_manufacturer VARCHAR(150),
    parsed_model_number VARCHAR(100),
    parsed_location_bay VARCHAR(100),
    parsed_purchase_date DATE,
    parsed_warranty_expiration DATE,
    validation_status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (
        validation_status IN ('Pending', 'Valid', 'Invalid', 'Processed', 'Failed')
    ),
    validation_errors TEXT[] DEFAULT '{}',
    processed_asset_id UUID REFERENCES inventory.assets(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS idx_stg_asset_batch ON inventory.stg_asset_imports(batch_reference);
CREATE INDEX IF NOT EXISTS idx_stg_asset_status ON inventory.stg_asset_imports(validation_status);
CREATE INDEX IF NOT EXISTS idx_stg_asset_user ON inventory.stg_asset_imports(uploaded_by_user_id);


-- -----------------------------------------------------------------------------
-- 2. Table: telemetry.stg_telemetry_buffer
-- Purpose: High-throughput staging buffer for raw PC / IoT sensor telemetry ingestion
--          prior to batch transformation and insertion into time-series partitions.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS telemetry.stg_telemetry_buffer (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    device_mac_address VARCHAR(17) NOT NULL,
    hostname VARCHAR(100),
    raw_payload JSONB NOT NULL,
    ingested_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_processed BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_stg_telemetry_unprocessed ON telemetry.stg_telemetry_buffer(is_processed, ingested_at) 
    WHERE is_processed = FALSE;
CREATE INDEX IF NOT EXISTS idx_stg_telemetry_mac ON telemetry.stg_telemetry_buffer(device_mac_address);


-- -----------------------------------------------------------------------------
-- 3. Function: inventory.fn_validate_stg_asset_batch
-- Purpose: Validates staged asset records against uniqueness constraints, schema rules,
--          and maps error flags before committing to the main inventory tables.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_validate_stg_asset_batch(
    p_batch_reference VARCHAR(100)
)
RETURNS TABLE (
    total_records INT,
    valid_records INT,
    invalid_records INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_rec RECORD;
    v_errors TEXT[];
    v_total INT := 0;
    v_valid INT := 0;
    v_invalid INT := 0;
BEGIN
    FOR v_rec IN 
        SELECT * FROM inventory.stg_asset_imports 
        WHERE batch_reference = p_batch_reference 
          AND validation_status = 'Pending'
        FOR UPDATE
    LOOP
        v_errors := ARRAY[]::TEXT[];
        v_total := v_total + 1;

        -- Field-level constraint checks
        IF v_rec.parsed_asset_name IS NULL OR length(trim(v_rec.parsed_asset_name)) = 0 THEN
            v_errors := array_append(v_errors, 'Asset name is required.');
        END IF;

        IF v_rec.parsed_serial_number IS NULL OR length(trim(v_rec.parsed_serial_number)) = 0 THEN
            v_errors := array_append(v_errors, 'Serial number is required.');
        ELSIF EXISTS (
            SELECT 1 FROM inventory.assets 
            WHERE serial_number = v_rec.parsed_serial_number
        ) THEN
            v_errors := array_append(v_errors, format('Serial number %s already exists in inventory.', v_rec.parsed_serial_number));
        END IF;

        IF v_rec.parsed_category IS NULL OR length(trim(v_rec.parsed_category)) = 0 THEN
            v_errors := array_append(v_errors, 'Category is required.');
        END IF;

        -- Update record validation state
        IF array_length(v_errors, 1) IS NULL THEN
            UPDATE inventory.stg_asset_imports
            SET validation_status = 'Valid',
                validation_errors = '{}'
            WHERE import_id = v_rec.import_id;
            v_valid := v_valid + 1;
        ELSE
            UPDATE inventory.stg_asset_imports
            SET validation_status = 'Invalid',
                validation_errors = v_errors
            WHERE import_id = v_rec.import_id;
            v_invalid := v_invalid + 1;
        END IF;
    END LOOP;

    RETURN QUERY SELECT v_total, v_valid, v_invalid;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: inventory.fn_commit_stg_asset_batch
-- Purpose: Promotes valid staged assets into production inventory.assets table
--          and generates initial default telemetry summaries and QR tokens.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_commit_stg_asset_batch(
    p_batch_reference VARCHAR(100),
    p_committed_by_user_id UUID
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_rec RECORD;
    v_new_asset_id UUID;
    v_committed_count INT := 0;
BEGIN
    FOR v_rec IN 
        SELECT * FROM inventory.stg_asset_imports 
        WHERE batch_reference = p_batch_reference 
          AND validation_status = 'Valid'
        FOR UPDATE
    LOOP
        v_new_asset_id := uuid_generate_v4();

        -- Insert into core physical assets catalog
        INSERT INTO inventory.assets (
            id,
            name,
            category,
            serial_number,
            model_number,
            manufacturer,
            status,
            risk_tier,
            location_bay,
            purchase_date,
            warranty_expiration,
            created_at,
            updated_at
        ) VALUES (
            v_new_asset_id,
            v_rec.parsed_asset_name,
            v_rec.parsed_category,
            v_rec.parsed_serial_number,
            v_rec.parsed_model_number,
            v_rec.parsed_manufacturer,
            'Available',
            'Standard',
            v_rec.parsed_location_bay,
            v_rec.parsed_purchase_date,
            v_rec.parsed_warranty_expiration,
            NOW(),
            NOW()
        );

        -- Seed empty telemetry summary state
        INSERT INTO inventory.asset_telemetry_summaries (
            asset_id,
            cpu_usage_pct,
            ram_usage_pct,
            temperature_celsius,
            operational_hours,
            updated_at
        ) VALUES (
            v_new_asset_id,
            0.00,
            0.00,
            NULL,
            0.00,
            NOW()
        );

        -- Update staging table tracking flags
        UPDATE inventory.stg_asset_imports
        SET validation_status = 'Processed',
            processed_asset_id = v_new_asset_id,
            processed_at = NOW()
        WHERE import_id = v_rec.import_id;

        v_committed_count := v_committed_count + 1;
    END LOOP;

    RETURN v_committed_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE inventory.stg_asset_imports ENABLE ROW LEVEL SECURITY;
ALTER TABLE telemetry.stg_telemetry_buffer ENABLE ROW LEVEL SECURITY;

-- Staging Asset Access: Restricted to Admins and Technicians
DROP POLICY IF EXISTS p_stg_asset_imports_manage ON inventory.stg_asset_imports;
CREATE POLICY p_stg_asset_imports_manage ON inventory.stg_asset_imports
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

-- Telemetry Staging Access: System processing policy
DROP POLICY IF EXISTS p_stg_telemetry_manage ON telemetry.stg_telemetry_buffer;
CREATE POLICY p_stg_telemetry_manage ON telemetry.stg_telemetry_buffer
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