Markdown# LabTrack PostgreSQL Database Architecture & Forensic Model Specification

This specification documents the low-level database schema design, security model, cryptographic audit mechanisms, and domain schema topology powering the **LabTrack** Laboratory Asset Management Platform.

---

## 1. Relational Architecture & Modular Schemas

The LabTrack database engine relies on a modular **multi-schema architecture** on PostgreSQL 14+ / 16. Domain separation ensures zero cross-domain state leakages and strict boundary isolation.

                                +-----------------------+
                                |     public / Core     |
                                |  (Verifiers/Contract) |
                                +-----------------------+
                                            |
    +------------------+--------------------+--------------------+------------------+
    |                  |                    |                    |                  |
    v                  v                    v                    v                  v
+---------------+  +---------------+    +---------------+    +---------------+  +---------------+
|     auth      |  |   inventory   |    |  scheduling   |    |   telemetry   |  |  maintenance  |
| (Identity/RLS)|  | (Assets/Bays) |    | (QR/Bookings) |    |  (PCs/Switches|  | (Tickets/EHS) |
+---------------+  +---------------+    +---------------+    +---------------+  +---------------+
|                  |                    |                    |                  |
+------------------+--------------------+--------------------+------------------+
|
v
+-------------------------------------------------+
|   financial  |      ai       |      audit       |
|  (Ledgers)   |  (pgvector)   | (SHA-256 Ledger) |
+-------------------------------------------------+
### 1.1 Schemas Matrix

| Schema Name | Scope & Domain Entities | Primary Key Strategy |
| :--- | :--- | :--- |
| **`auth`** | Users, roles, permissions, MFA credentials, active sessions, revoked JWT tokens, security incidents. | `UUIDv4` |
| **`inventory`** | Facilities, laboratories, lab bays, asset catalog, categories, telemetry summary rollups, staging buffers. | `UUIDv4` |
| **`scheduling`** | QR token signatures, reservation bookings, usage sessions, and conflict queues. | `UUIDv4` |
| **`telemetry`** | Workstation PC specs, network switches, VLAN mappings, switch port statuses, and live sensor readings. | `UUIDv4` |
| **`maintenance`** | Problem reports, EHS safety hazard overrides, repair tickets, preventive maintenance routines, replacement requests. | `UUIDv4` |
| **`financial`** | Department cost centers, asset financial ledgers, double-entry transactions, depreciation schedules. | `UUIDv4` |
| **`ai`** | Embeddings (`vector(1536)`), RAG manuals, LLM chat sessions, diagnostic thread histories. | `UUIDv4` |
| **`audit`** | Cryptographic forensic event ledger, system notifications, request idempotency locks, transactional email queue. | `BIGINT` (Sequential) & `UUIDv4` |

---

## 2. Row Level Security (RLS) & Multi-Tenancy Execution

All transactional tables across `inventory`, `scheduling`, `maintenance`, `telemetry`, `financial`, and `ai` implement PostgreSQL **Row Level Security (RLS)** policies.

### 2.1 Security Context Variable
Backend connection pools establish user context prior to query execution using session variables:

```sql
-- Enforced on every database connection/transaction boundary
SET LOCAL app.current_user_id = '10000000-0000-0000-0000-000000000002';
2.2 Standard RLS Policy PatternSQLALTER TABLE inventory.assets ENABLE ROW LEVEL SECURITY;

CREATE POLICY asset_isolation_policy ON inventory.assets
    FOR ALL
    USING (
        -- Admin Bypasses
        EXISTS (
            SELECT 1 FROM auth.users u
            JOIN auth.roles r ON u.role_id = r.id
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::UUID
              AND r.name = 'Admin'
        )
        OR
        -- Facility / Department Access Constraint
        facility_id IN (
            SELECT facility_id FROM auth.user_facility_permissions
            WHERE user_id = NULLIF(current_setting('app.current_user_id', true), '')::UUID
        )
    );
3. Cryptographic Tamper-Evident Forensic LedgerThe audit.forensic_ledger table guarantees continuous data integrity via an append-only SHA-256 block-chained architecture.3.1 Hash Chaining EquationFor every new audit record $n$:$$H_n = \text{SHA-256}\left( H_{n-1} \mathbin{\Vert} \text{EventUUID} \mathbin{\Vert} \text{ActorID} \mathbin{\Vert} \text{Action} \mathbin{\Vert} \text{PayloadJSON} \mathbin{\Vert} \text{Timestamp} \right)$$where $H_0 = \text{"0000000000000000000000000000000000000000000000000000000000000000"}$.3.2 Block Hash Generator TriggerSQLCREATE OR REPLACE FUNCTION audit.fn_generate_forensic_hash()
RETURNS TRIGGER AS $$
DECLARE
    v_prev_hash TEXT;
BEGIN
    -- Obtain the hash of the immediately preceding audit row
    SELECT payload_hash INTO v_prev_hash
    FROM audit.forensic_ledger
    ORDER BY sequence_id DESC
    LIMIT 1;

    IF v_prev_hash IS NULL THEN
        v_prev_hash := '0000000000000000000000000000000000000000000000000000000000000000';
    END IF;

    -- Calculate current row hash
    NEW.previous_hash := v_prev_hash;
    NEW.payload_hash := encode(
        digest(
            v_prev_hash || 
            NEW.id::text || 
            COALESCE(NEW.actor_user_id::text, '00000000-0000-0000-0000-000000000000') || 
            NEW.action_type || 
            NEW.entity_name || 
            COALESCE(NEW.payload::text, '{}') || 
            EXTRACT(EPOCH FROM NEW.created_at)::text,
            'sha256'
        ),
        'hex'
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
4. Analytical Views & Materialized Performance LayerTo optimize API latency and simplify backend queries, LabTrack maintains four core reporting views:View NamePrimary Source TablesPurposeinventory.v_asset_details_comprehensiveinventory.assets, inventory.lab_bays, inventory.laboratories, inventory.facilities, telemetry.pc_workstations, scheduling.qr_tokensSingle-object 360-degree asset profiling, real-time status counters, location mapping, and telemetry overlays.maintenance.v_dashboard_maintenance_metricsmaintenance.problem_reports, maintenance.repair_tickets, maintenance.preventive_schedules, inventory.assetsOperational KPI dashboard metrics, EHS hazard flags, overdue maintenance tasks, and MTTR aggregations.financial.v_financial_summary_executivefinancial.cost_centers, financial.asset_financial_ledgers, financial.ledger_transactions, maintenance.repair_ticketsExecutive financial summary, institutional asset valuation, accumulated depreciation, and cost center budget health.inventory.v_laboratory_capacity_utilizationinventory.facilities, inventory.laboratories, inventory.lab_bays, inventory.assets, telemetry.pc_workstationsFacility space utilization percentages, lab bay max capacities, asset density, and operational readiness rollups.5. Schema Deployment VerificationThe system includes a verification function to validate database integrity during deployment or automated testing:SQLSELECT * FROM public.fn_verify_labtrack_schema_deployment();
Expected Output:Returns schema_name, table_count, view_count, function_count, and status = 'VALID'.