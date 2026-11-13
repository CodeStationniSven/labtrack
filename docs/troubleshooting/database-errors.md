LabTrack PostgreSQL Database Error Codes, Troubleshooting, & Integrity Recovery GuideThis document provides a comprehensive troubleshooting reference for diagnosing and resolving database-level exceptions, schema constraint violations, Row-Level Security (RLS) failures, cryptographic forensic ledger breaks, and concurrency deadlocks within the LabTrack PostgreSQL 16 database engine.1. Database Error Architecture & SQLSTATE CategoriesLabTrack relies on standard PostgreSQL SQLSTATE error codes propagated through Entity Framework Core and Dapper to the .NET 8 backend API. Database exceptions are caught by global exception middleware and mapped to standardized JSON error responses.2. Common PostgreSQL Error Codes MatrixSQLSTATEError Code NameDescription / Trigger in LabTrackPrimary Root CauseResolution Steps23505unique_violationDuplicate key value violates unique constraint (e.g., duplicate email in auth.users or duplicate QR token code).Inserting a record with a pre-existing unique identifier or serial number.Verify existing records before insertion or use ON CONFLICT clauses where applicable.23503foreign_key_violationInsert or update on table violates foreign key constraint (e.g., assigning a repair ticket to a non-existent asset_id).Referenced parent record does not exist in target schema (inventory, auth, etc.).Ensure valid parent UUIDs exist prior to submitting child transactional payloads.23514check_violationNew row violates a table check constraint (e.g., negative budget amount or invalid percentage range).Input data fails domain validation rules defined in table DDL.Review check constraint rules and validate incoming payload values.42501insufficient_privilegeUser or role lacks permission to execute a query or access a schema object.Direct database connection bypassing RLS or executing operations without proper role grants.Ensure connection pool uses appropriate role credentials and sets RLS execution context.40001serialization_failureTransaction rollback due to serialization failure in repeatable read / serializable isolation.Concurrent transactions attempting conflicting modifications on the same rows.Implement retry logic with exponential backoff in the application data access layer.40P01deadlock_detectedPostgreSQL deadlock engine terminated one of two conflicting transactions.Two concurrent processes locking rows in reverse chronological order (e.g., asset lock vs. inventory lock).Standardize lock acquisition order across application CQRS command handlers.P0001raise_exceptionCustom PL/pgSQL procedural exception raised by database triggers or functions.Business rule violation enforced inside stored procedures or ledger triggers.Inspect error message details returned by the database trigger for specific violation context.3. Deep-Dive Troubleshooting Scenarios3.1 Scenario A: Row-Level Security (RLS) Policy ViolationsSymptom: Queries return zero rows or throw permission denied errors when interacting with tables in inventory, maintenance, or financial schemas.Diagnosis: The PostgreSQL session variable app.current_user_id has not been set for the current connection or transaction, causing RLS policies to evaluate as FALSE for non-admin users.Resolution: Ensure database context interceptors in the .NET backend execute SET LOCAL app.current_user_id = '<user_uuid>'; at the start of every transaction boundary.3.2 Scenario B: Cryptographic Forensic Ledger Hash Chain Breaks (audit.forensic_ledger)Symptom: Integrity verification function audit.fn_verify_forensic_ledger_integrity() returns a broken hash link error.Diagnosis: Direct manual modification or deletion of historical rows in audit.forensic_ledger, which violates the append-only SHA-256 block-chaining mechanism ($H_n = \text{SHA256}(H_{n-1} \parallel \dots)$).Resolution:Identify the exact sequence_id where the hash verification failed.Inspect database transaction logs (pg_audit / WAL) to trace unauthorized database access or tampering.Rebuild or restore audit ledger integrity from verified secure backups if tampering is confirmed.3.3 Scenario C: Extension Missing (pgvector or pgcrypto)Symptom: Migration execution or startup failure reporting ERROR: extension "vector" is not installed.Diagnosis: The custom PostgreSQL 16 Docker image (labtrack/postgres:16-vector) failed to compile or load the pgvector extension during container initialization.Resolution: Verify that the database container is built using the correct Dockerfile referencing the official pgvector repository release (v0.7.0+).4. Diagnostic SQL Toolkit for Database AdministratorsRun these queries directly against the PostgreSQL 16 database to diagnose performance bottlenecks, locks, and forensic integrity:4.1 Verify Cryptographic Forensic Ledger IntegritySQLCREATE OR REPLACE FUNCTION audit.fn_verify_forensic_ledger_integrity()
RETURNS TABLE (
    is_valid BOOLEAN,
    broken_sequence_id BIGINT,
    details TEXT
) AS $$
DECLARE
    r RECORD;
    v_expected_prev TEXT := '0000000000000000000000000000000000000000000000000000000000000000';
    v_calculated_hash TEXT;
BEGIN
    FOR r IN SELECT * FROM audit.forensic_ledger ORDER BY sequence_id ASC LOOP
        IF r.sequence_id > 1 AND r.previous_hash != v_expected_prev THEN
            RETURN QUERY SELECT FALSE, r.sequence_id, 'Previous hash mismatch at sequence_id: ' || r.sequence_id;
            RETURN;
        END IF;
        
        -- Recalculate hash
        v_calculated_hash := encode(
            digest(
                r.previous_hash || 
                r.id::text || 
                COALESCE(r.actor_user_id::text, '00000000-0000-0000-0000-000000000000') || 
                r.action_type || 
                r.entity_name || 
                COALESCE(r.payload::text, '{}') || 
                EXTRACT(EPOCH FROM r.created_at)::text,
                'sha256'
            ),
            'hex'
        );

        IF r.payload_hash != v_calculated_hash THEN
            RETURN QUERY SELECT FALSE, r.sequence_id, 'Payload hash corruption detected at sequence_id: ' || r.sequence_id;
            RETURN;
        END IF;

        v_expected_prev := r.payload_hash;
    END LOOP;

    RETURN QUERY SELECT TRUE, NULL::BIGINT, 'Forensic ledger integrity verified successfully. Zero tampered blocks.';
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
4.2 Inspect Active Database Connections & Resource ConsumptionSQLSELECT 
    pid,
    usename,
    datname,
    client_addr,
    state,
    backend_start,
    query_start,
    query
FROM pg_stat_activity
WHERE datname = 'labtrack_db'
  AND state != 'idle'
ORDER BY query_start ASC;