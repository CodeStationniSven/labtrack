-- =============================================================================
-- LabTrack Schema Initialization: 003_enums.sql
-- Database Engine: PostgreSQL 14+
-- Module: Domain-Wide Custom Enumerated Types (ENUMs)
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Identity, Access Control, & Security ENUMs
-- -----------------------------------------------------------------------------

-- Standardized role levels across the application
CREATE TYPE auth.user_role AS ENUM (
    'Admin',
    'Technician',
    'Researcher',
    'Auditor',
    'Guest'
);

-- MFA verification methods
CREATE TYPE auth.mfa_type AS ENUM (
    'TOTP',
    'SMS',
    'BackupCode',
    'HardwareKey'
);


-- -----------------------------------------------------------------------------
-- 2. Physical Asset & Inventory ENUMs
-- -----------------------------------------------------------------------------

-- Operational availability state of lab equipment
CREATE TYPE inventory.asset_status AS ENUM (
    'Available',
    'In Use',
    'Under Maintenance',
    'Calibration Required',
    'Decommissioned',
    'Unknown'
);

-- Environmental risk classification for laboratory equipment
CREATE TYPE inventory.asset_risk_tier AS ENUM (
    'Standard',
    'Biohazard',
    'HighVoltage',
    'Radiation',
    'Cryogenic'
);


-- -----------------------------------------------------------------------------
-- 3. Reservations & Equipment Scheduling ENUMs
-- -----------------------------------------------------------------------------

-- Lifecycle status of equipment bookings
CREATE TYPE scheduling.reservation_status AS ENUM (
    'Pending',
    'Approved',
    'Active',
    'Completed',
    'Cancelled',
    'Rejected'
);

-- Types of QR scanning interactions
CREATE TYPE scheduling.qr_action_type AS ENUM (
    'Check-In',
    'Check-Out',
    'Verification-Only',
    'Maintenance-Scan'
);


-- -----------------------------------------------------------------------------
-- 4. Ticketing, Maintenance, & Service Workflows ENUMs
-- -----------------------------------------------------------------------------

-- Categorization for service/repair requests
CREATE TYPE maintenance.ticket_type AS ENUM (
    'Repair',
    'IncidentReport',
    'SafetyAudit',
    'FacilityInquiry',
    'Calibration'
);

-- Priority ranking for operational triage
CREATE TYPE maintenance.ticket_priority AS ENUM (
    'Low',
    'Medium',
    'High',
    'Critical'
);

-- Consolidated ticket status tracking
CREATE TYPE maintenance.ticket_status AS ENUM (
    'Open',
    'Triaged',
    'In Progress',
    'Awaiting Parts',
    'Resolved',
    'Closed',
    'Cancelled'
);

-- Service/Calibration execution outcomes
CREATE TYPE maintenance.maintenance_outcome AS ENUM (
    'Passed',
    'Failed',
    'Passed-With-Conditions',
    'Incomplete'
);


-- -----------------------------------------------------------------------------
-- 5. Financials & Cost Allocation ENUMs
-- -----------------------------------------------------------------------------

-- Accounting methods for equipment valuation over time
CREATE TYPE financial.depreciation_method AS ENUM (
    'Straight-Line',
    'Double-Declining-Balance',
    'Sum-Of-Years-Digits',
    'None'
);

-- Transaction categorization for cost centers
CREATE TYPE financial.transaction_type AS ENUM (
    'RepairExpense',
    'MaintenancePart',
    'UsageFee',
    'DepreciationWriteOff',
    'Procurement'
);


-- -----------------------------------------------------------------------------
-- 6. Forensic Audit & System Notifications ENUMs
-- -----------------------------------------------------------------------------

-- High-level security categories for tamper-evident ledger tracking
CREATE TYPE audit.forensic_category AS ENUM (
    'AUTH_FAILURE',
    'PRIVILEGE_ELEVATION',
    'DATA_EXPORT',
    'CONFIG_MUTATION',
    'HARDWARE_DISPATCH',
    'SAFETY_OVERRIDE'
);

-- Outbound delivery channels for system alerts
CREATE TYPE audit.notification_channel AS ENUM (
    'InApp',
    'Email',
    'Webhook',
    'Push'
);

COMMIT;