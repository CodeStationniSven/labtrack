-- =============================================================================
-- LabTrack Schema Development Seeder: backend/database/seeds/development.sql
-- Database Engine: PostgreSQL 14+
-- Purpose: Complete sample dataset spanning all domain schemas (auth, inventory,
--          scheduling, telemetry, maintenance, audit, financial, ai) for 
--          development, integration testing, and local environment bootstrapping.
-- =============================================================================

BEGIN;

-- Setup temporary session bypass user context for initial seed execution
SET LOCAL app.current_user_id = '00000000-0000-0000-0000-000000000001';

-- -----------------------------------------------------------------------------
-- 1. AUTH SCHEMA SEEDS
-- Setup core system roles, permissions, baseline users, and active sessions.
-- -----------------------------------------------------------------------------

-- Core Roles
INSERT INTO auth.roles (id, name, description) VALUES
    ('00000000-0000-0000-0000-000000000001', 'Admin', 'Full administrative system privileges'),
    ('00000000-0000-0000-0000-000000000002', 'Technician', 'Laboratory hardware technician and maintenance operator'),
    ('00000000-0000-0000-0000-000000000003', 'Researcher', 'Standard researcher and lab facility user'),
    ('00000000-0000-0000-0000-000000000004', 'Auditor', 'Read-only compliance and financial auditor')
ON CONFLICT (id) DO NOTHING;

-- Base Users (Passwords hashed via pgcrypto bcrypt)
INSERT INTO auth.users (id, role_id, email, full_name, password_hash, is_active, is_mfa_enabled) VALUES
    ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'admin@labtrack.io', 'Dr. Sarah Connor', crypt('AdminPassword123!', gen_salt('bf')), TRUE, TRUE),
    ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000002', 'tech.miles@labtrack.io', 'Miles Dyson', crypt('TechPassword123!', gen_salt('bf')), TRUE, FALSE),
    ('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000003', 'researcher.john@labtrack.io', 'John Doe', crypt('ResearchPass123!', gen_salt('bf')), TRUE, FALSE),
    ('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000004', 'auditor.ellen@labtrack.io', 'Ellen Ripley', crypt('AuditPass123!', gen_salt('bf')), TRUE, TRUE)
ON CONFLICT (id) DO NOTHING;

-- User Notification Preferences
INSERT INTO auth.user_notification_preferences (id, user_id, email_on_reservation_update, email_on_maintenance_alert, email_on_security_event, push_on_telemetry_exceeded) VALUES
    (uuid_generate_v4(), '10000000-0000-0000-0000-000000000001', TRUE, TRUE, TRUE, TRUE),
    (uuid_generate_v4(), '10000000-0000-0000-0000-000000000002', TRUE, TRUE, FALSE, TRUE),
    (uuid_generate_v4(), '10000000-0000-0000-0000-000000000003', TRUE, FALSE, FALSE, FALSE)
ON CONFLICT (user_id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 2. INVENTORY SCHEMA SEEDS
-- Facilities, Laboratories, Bays, Physical Assets, & Telemetry Summaries.
-- -----------------------------------------------------------------------------

INSERT INTO inventory.facilities (id, name, code, address) VALUES
    ('20000000-0000-0000-0000-000000000001', 'Central BioTech Center', 'FAC-BIO-01', '100 Innovation Way, Building A'),
    ('20000000-0000-0000-0000-000000000002', 'Advanced Physics & AI Lab', 'FAC-PHYS-02', '200 Quantum Boulevard, Building B')
ON CONFLICT (id) DO NOTHING;

INSERT INTO inventory.laboratories (id, facility_id, name, room_number) VALUES
    ('21000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'Genomics Core Lab', 'Room 301'),
    ('21000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', 'High-Performance Computing Cluster Lab', 'Room 105')
ON CONFLICT (id) DO NOTHING;

INSERT INTO inventory.lab_bays (id, laboratory_id, bay_identifier, max_capacity) VALUES
    ('22000000-0000-0000-0000-000000000001', '21000000-0000-0000-0000-000000000001', 'Bay-A1 Sequencers', 4),
    ('22000000-0000-0000-0000-000000000002', '21000000-0000-0000-0000-000000000002', 'Bay-C4 AI Compute Racks', 8)
ON CONFLICT (id) DO NOTHING;

-- Core Physical Equipment Assets
INSERT INTO inventory.assets (id, lab_bay_id, name, category, serial_number, model_number, manufacturer, status, risk_tier, location_bay, purchase_date, warranty_expiration) VALUES
    ('23000000-0000-0000-0000-000000000001', '22000000-0000-0000-0000-000000000001', 'Illumina NovaSeq 6000', 'DNA Sequencer', 'SN-ILL-990231', 'NovaSeq 6000', 'Illumina Inc.', 'Available', 'HighRisk', 'Bay-A1', '2024-01-15', '2027-01-15'),
    ('23000000-0000-0000-0000-000000000002', '22000000-0000-0000-0000-000000000002', 'NVIDIA DGX H100 Workstation', 'AI Server PC', 'SN-NV-DGX-8812', 'DGX-H100', 'NVIDIA', 'Available', 'Standard', 'Bay-C4', '2025-06-10', '2028-06-10'),
    ('23000000-0000-0000-0000-000000000003', '22000000-0000-0000-0000-000000000001', 'Thermo Fisher Cryo-EM', 'Electron Microscope', 'SN-TF-CRYO-004', 'Krios G4', 'Thermo Fisher Scientific', 'Under Maintenance', 'Critical', 'Bay-A1', '2023-03-20', '2026-03-20')
ON CONFLICT (id) DO NOTHING;

-- Telemetry Summaries for Assets
INSERT INTO inventory.asset_telemetry_summaries (asset_id, cpu_usage_pct, ram_usage_pct, temperature_celsius, operational_hours, last_ping_at) VALUES
    ('23000000-0000-0000-0000-000000000001', 12.50, 42.00, 24.50, 1420.50, NOW()),
    ('23000000-0000-0000-0000-000000000002', 88.20, 79.40, 68.20, 3105.00, NOW()),
    ('23000000-0000-0000-0000-000000000003', 0.00, 5.00, 18.00, 4890.00, NOW() - INTERVAL '3 days')
ON CONFLICT (asset_id) DO UPDATE
SET cpu_usage_pct = EXCLUDED.cpu_usage_pct,
    ram_usage_pct = EXCLUDED.ram_usage_pct,
    temperature_celsius = EXCLUDED.temperature_celsius,
    last_ping_at = EXCLUDED.last_ping_at;


-- -----------------------------------------------------------------------------
-- 3. SCHEDULING SCHEMA SEEDS
-- QR Identification Tokens and Asset Booking Schedules.
-- -----------------------------------------------------------------------------

INSERT INTO scheduling.qr_tokens (id, asset_id, token_code, checksum, is_active, generated_by_user_id) VALUES
    ('30000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000001', 'QR-ASSET-001-NOVASEQ', encode(digest('QR-ASSET-001-NOVASEQ', 'sha256'), 'hex'), TRUE, '10000000-0000-0000-0000-000000000001'),
    ('30000000-0000-0000-0000-000000000002', '23000000-0000-0000-0000-000000000002', 'QR-ASSET-002-DGXH100', encode(digest('QR-ASSET-002-DGXH100', 'sha256'), 'hex'), TRUE, '10000000-0000-0000-0000-000000000001')
ON CONFLICT (id) DO NOTHING;

INSERT INTO scheduling.reservation_schedules (id, asset_id, reserved_by_user_id, approved_by_user_id, reservation_code, purpose, start_time, end_time, status) VALUES
    ('31000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'RSV-20261022-0001', 'Whole Genome Sequencing Batch #4', '2026-10-22 09:00:00+00', '2026-10-22 17:00:00+00', 'Approved'),
    ('31000000-0000-0000-0000-000000000002', '23000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000003', NULL, 'RSV-20261023-0002', 'Model Fine-Tuning Training Run', '2026-10-23 10:00:00+00', '2026-10-23 14:00:00+00', 'Pending')
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 4. TELEMETRY & NETWORK SCHEMA SEEDS
-- Workstations, Switches, Ports, NICs, and Live Heartbeats.
-- -----------------------------------------------------------------------------

INSERT INTO telemetry.pc_workstations (id, asset_id, hostname, ip_address, mac_address, os_version, cpu_model, ram_gb, gpu_model, is_online, last_ping_at) VALUES
    ('40000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000002', 'lab-dgx-01', '192.168.10.50', '00:1A:2B:3C:4D:5E', 'Ubuntu 24.04 LTS', 'AMD EPYC 9654 96-Core', 512.00, 'NVIDIA H100 80GB', TRUE, NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO telemetry.network_switches (id, facility_id, laboratory_id, hostname, ip_address, mac_address, model_number, total_ports, location_rack) VALUES
    ('41000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', '21000000-0000-0000-0000-000000000002', 'switch-core-b2', '192.168.10.1', 'AA:BB:CC:DD:EE:01', 'Cisco Catalyst 9300', 48, 'Rack-01')
ON CONFLICT (id) DO NOTHING;

INSERT INTO telemetry.network_ports (id, switch_id, port_number, port_name, vlan_id, is_enabled, link_status, speed_mbps) VALUES
    ('42000000-0000-0000-0000-000000000001', '41000000-0000-0000-0000-000000000001', 1, 'GigabitEthernet1/0/1', 100, TRUE, 'Up', 10000)
ON CONFLICT (id) DO NOTHING;

INSERT INTO telemetry.pc_network_interfaces (id, workstation_id, network_port_id, interface_name, mac_address, ipv4_address, is_primary) VALUES
    ('43000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '42000000-0000-0000-0000-000000000001', 'eth0', '00:1A:2B:3C:4D:5E', '192.168.10.50', TRUE)
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 5. MAINTENANCE SCHEMA SEEDS
-- Problem Reports, Repair Tickets, Maintenance Schedules, and Activity Logs.
-- -----------------------------------------------------------------------------

INSERT INTO maintenance.problem_reports (id, asset_id, reported_by_user_id, report_number, title, description, severity_level, category, status, has_safety_hazard) VALUES
    ('50000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 'PR-20261021-0001', 'Vacuum Leak in Cryo Chamber', 'Cryo-EM vacuum pressure fluctuating rapidly during sample transfer.', 'Critical', 'Hardware Fault', 'In Progress', TRUE)
ON CONFLICT (id) DO NOTHING;

INSERT INTO maintenance.repair_tickets (id, problem_report_id, asset_id, assigned_technician_id, created_by_user_id, ticket_number, title, description, priority, status, estimated_cost_usd, started_at) VALUES
    ('51000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'REP-20261021-0001', 'Replace Vacuum Seal Valve on Cryo-EM', 'Disassemble chamber assembly and replace damaged Viton O-ring seal.', 'Critical', 'In Progress', 1250.00, NOW() - INTERVAL '2 hours')
ON CONFLICT (id) DO NOTHING;

INSERT INTO maintenance.maintenance_schedules (id, asset_id, title, description, schedule_type, frequency_interval_days, last_performed_at, next_due_at, assigned_technician_id, is_active) VALUES
    ('52000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000001', 'Bi-Monthly Optical Calibration', 'Align optical lasers and perform fluidics flush.', 'Calibration', 60, NOW() - INTERVAL '50 days', NOW() + INTERVAL '10 days', '10000000-0000-0000-0000-000000000002', TRUE)
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 6. FINANCIAL SCHEMA SEEDS
-- Cost Centers, Asset Ledgers, & Accounting Transactions.
-- -----------------------------------------------------------------------------

INSERT INTO financial.cost_centers (id, code, name, department, manager_user_id, allocated_budget_usd, remaining_budget_usd, fiscal_year) VALUES
    ('60000000-0000-0000-0000-000000000001', 'CC-GENOMICS-2026', 'Genomics Operations & Equipment', 'Biological Sciences', '10000000-0000-0000-0000-000000000001', 500000.00, 482500.00, 2026)
ON CONFLICT (id) DO NOTHING;

INSERT INTO financial.asset_financial_ledgers (id, asset_id, cost_center_id, purchase_price_usd, salvage_value_usd, useful_life_months, depreciation_method, accumulated_depreciation_usd, in_service_date) VALUES
    ('61000000-0000-0000-0000-000000000001', '23000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000001', 850000.00, 50000.00, 60, 'Straight-Line', 240000.00, '2024-01-15')
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 7. AI ASSISTANT SCHEMA SEEDS
-- Sample Knowledge Base Embeddings & Chat Sessions.
-- -----------------------------------------------------------------------------

INSERT INTO ai.chat_sessions (id, user_id, asset_id, repair_ticket_id, title, model_name) VALUES
    ('70000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', '23000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000001', 'Cryo-EM Vacuum Troubleshooting', 'gpt-4o')
ON CONFLICT (id) DO NOTHING;

INSERT INTO ai.chat_messages (id, session_id, sender_role, message_content, prompt_tokens, completion_tokens) VALUES
    (uuid_generate_v4(), '70000000-0000-0000-0000-000000000001', 'user', 'What are the primary torque specs for replacing the Viton seal on the Krios G4 vacuum assembly?', 45, 0),
    (uuid_generate_v4(), '70000000-0000-0000-0000-000000000001', 'assistant', 'According to the Thermo Fisher Krios G4 maintenance manual (Section 4.2), tighten the retaining flange bolts in a cross-pattern to 2.5 Nm using a calibrated torque driver.', 120, 48)
ON CONFLICT DO NOTHING;


-- -----------------------------------------------------------------------------
-- 8. EMAIL TEMPLATES & COMMUNICATIONS SEEDS
-- System Email Templates for Notification Queues.
-- -----------------------------------------------------------------------------

INSERT INTO audit.email_templates (id, template_code, subject_template, body_html_template, body_text_template, description) VALUES
    ('80000000-0000-0000-0000-000000000001', 'RESERVATION_CONFIRMATION', 'Reservation Approved: {{asset_name}}', '<p>Dear {{user_name}},</p><p>Your reservation <strong>{{reservation_code}}</strong> for {{asset_name}} has been approved.</p>', 'Dear {{user_name}}, Your reservation {{reservation_code}} for {{asset_name}} has been approved.', 'Sent upon reservation approval'),
    ('80000000-0000-0000-0000-000000000002', 'SAFETY_HAZARD_ALERT', 'CRITICAL ALERT: Safety Hazard Filed for {{asset_name}}', '<h2>Safety Alert</h2><p>A safety hazard report was filed by {{reported_by}} for asset {{asset_name}}.</p>', 'Safety Alert: A safety hazard report was filed by {{reported_by}} for asset {{asset_name}}.', 'Emergency notification for technicians and admins')
ON CONFLICT (template_code) DO NOTHING;

COMMIT;