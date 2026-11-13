-- =============================================================================
-- LabTrack Schema Initialization: 002_schemas.sql
-- Database Engine: PostgreSQL 14+
-- Module: Domain Schemas, Search Path Configuration, & Multi-Tenant Namespace Partitioning
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Custom Namespace Schemas
-- Purpose: Partition core LabTrack subsystems into clean database namespaces
--          to enforce explicit boundary isolation, simplified grant management,
--          and modular data maintenance.
-- -----------------------------------------------------------------------------

-- Core Identity, Authorization, and User Metadata
CREATE SCHEMA IF NOT EXISTS auth;

-- Laboratory Equipment, Physical Assets, and Hardware Inventory
CREATE SCHEMA IF NOT EXISTS inventory;

-- Booking Schedules, Time Slot Allocation, and QR Check-ins
CREATE SCHEMA IF NOT EXISTS scheduling;

-- Universal Service Tickets, Maintenance Schedules, and Repairs
CREATE SCHEMA IF NOT EXISTS maintenance;

-- Workstation Telemetry, High-Frequency Logs, and Usage Tracking
CREATE SCHEMA IF NOT EXISTS telemetry;

-- Operational Financials, Ledger Transactions, and Cost Centers
CREATE SCHEMA IF NOT EXISTS financial;

-- Cryptographic Forensic Ledgers, Audit Logs, and Chain of Custody
CREATE SCHEMA IF NOT EXISTS audit;

-- Diagnostic Vector Embeddings, LLM Chat Sessions, and Evaluations
CREATE SCHEMA IF NOT EXISTS ai;


-- -----------------------------------------------------------------------------
-- 2. Database Search Path & Default Privileges
-- -----------------------------------------------------------------------------

-- Set default search path across created namespaces
SET search_path TO public, auth, inventory, scheduling, maintenance, telemetry, financial, audit, ai;

-- Default Grant Configurations (Restrictive baseline for application roles)
ALTER DEFAULT PRIVILEGES REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;

COMMIT;