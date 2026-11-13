Markdown
# LabTrack Database Architecture & Migration Management

PostgreSQL 14+ database system powering the **LabTrack** Laboratory Asset Management, Telemetry Monitoring, Financial Oversight, and Maintenance Platform.

---

## Architecture & Schema Overview

The database uses a modular **multi-schema architecture** designed for domain isolation, explicit Row Level Security (RLS), and zero-trust data access patterns.

| Schema Namespace | Primary Responsibility |
| :--- | :--- |
| **`auth`** | User credentials, RBAC roles, multi-factor authentication (MFA), JWT revocation lists, and IP security events. |
| **`inventory`** | Multi-facility hierarchy, lab bays, asset lifecycle tracking, telemetry summaries, and bulk staging buffers. |
| **`scheduling`** | QR code token management, asset booking schedules, usage session check-ins, and conflict checking. |
| **`telemetry`** | Workstation PC specs, network switch topology, port assignments, and time-series sensor/heartbeat ingestion. |
| **`maintenance`** | Problem reports, EHS safety hazard flags, repair tickets, preventive maintenance schedules, and replacement requests. |
| **`financial`** | Cost center budget tracking, asset financial ledgers, double-entry ledger transactions, and depreciation schedules. |
| **`ai`** | Vector embeddings (`pgvector`), diagnostic knowledge bases, LLM chat sessions, and assistant thread histories. |
| **`audit`** | Cryptographic SHA-256 block-chained forensic ledgers, in-app notifications, idempotency locks, and email queues. |

---

## Directory Structure

```text
backend/database/
├── database_contract.sql          # Core system contract: extensions, schemas, types, & master triggers
├── reference_data.sql            # Standardized taxonomy, ISO standards, and error codes
├── seeds/
│   └── development.sql           # Sample seed dataset for local environment bootstrapping
├── views/
│   ├── asset_details.sql         # Comprehensive single-asset profiles & counter aggregations
│   ├── dashboard.sql             # Real-time operational KPIs, maintenance metrics, & activity feeds
│   ├── financial_summary.sql     # Executive financial metrics, cost center breakdowns, & depreciation
│   └── laboratory_summary.sql   # Facility hierarchies, space utilization, & lab bay capacities
└── migrations/                   # Sequenced SQL migration scripts (001_auth.sql through 023_email_queue.sql)
Migration Index
The schema is applied via strict numerical ordering:

001_auth.sql – Core identity, roles, and user registry.

002_inventory.sql – Facilities, laboratories, bays, and physical assets catalog.

003_scheduling.sql – QR identification tokens and reservation schedules.

004_telemetry.sql – PC workstations, network hardware, and live ping monitoring.

005_maintenance.sql – Problem reports, EHS hazards, and repair tickets.

006_preventive_maintenance.sql – Recurrent maintenance schedules and execution logs.

007_audit_forensic.sql – Cryptographic tamper-evident forensic event ledger.

008_notifications.sql – System notification feeds and multi-channel delivery logs.

009_ai_vector.sql – Vector embeddings (pgvector) and LLM session histories.

010_financial_ledger.sql – Cost centers, asset valuation, and transaction journals.

011_staging_ingestion.sql – CSV/JSON bulk upload staging buffers and validation engines.

012_session_revocation.sql – JWT blacklisting, security incident logs, and session management.

013_replacement.sql – Capital replacement workflow and end-of-life requests.

014_communications.sql – Threaded ticket discussions and facility broadcast alerts.

015_network.sql – Network switch port mapping, VLANs, and connectivity incidents.

016_notifications.sql – Domain-isolated notification management and cleanup rules.

017_financial.sql – Advanced double-entry accounting procedures and budget recalculations.

018_ai.sql – AI thread token tracking and diagnostic manual search.

019_audit.sql – Cryptographic SHA-256 verification and integrity auditing tools.

020_staging.sql – Bulk import validation and commit processing procedures.

021_auth_sessions.sql – Automated token invalidation and brute-force tracking.

022_idempotency.sql – Distributed API request deduplication and inflight locks.

023_email_queue.sql – Background transactional email queue with exponential backoff.

Quickstart & Local Setup
Requirements
PostgreSQL 14+

Extensions: uuid-ossp, pgcrypto, citext, and optional vector (pgvector).

Local Provisioning Command Sequence
Bash
# 1. Create target database
createdb -U postgres labtrack_db

# 2. Apply Master Contract & System References
psql -U postgres -d labtrack_db -f database_contract.sql
psql -U postgres -d labtrack_db -f reference_data.sql

# 3. Apply Core Migrations (001 through 023)
for file in migrations/*.sql; do
    echo "Applying $file..."
    psql -U postgres -d labtrack_db -f "$file"
done

# 4. Initialize Reporting & Analytical Views
psql -U postgres -d labtrack_db -f views/asset_details.sql
psql -U postgres -d labtrack_db -f views/dashboard.sql
psql -U postgres -d labtrack_db -f views/financial_summary.sql
psql -U postgres -d labtrack_db -f views/laboratory_summary.sql

# 5. Bootstrap Seed Data for Local Development
psql -U postgres -d labtrack_db -f seeds/development.sql
Security Model & Development Rules
Row Level Security (RLS)
All tables enforce explicit Row Level Security policies. App backend connections set the current authenticated user context before issuing queries:

SQL
SET LOCAL app.current_user_id = '10000000-0000-0000-0000-000000000001';
Forensic Audit Verification
To check the cryptographic integrity of the SHA-256 block-chained audit trail:

SQL
SELECT * FROM audit.fn_verify_forensic_ledger_integrity();
Schema Deployment Verification
To verify table, view, and function initialization status across all domain namespaces:

SQL
SELECT * FROM public.fn_verify_labtrack_schema_deploy