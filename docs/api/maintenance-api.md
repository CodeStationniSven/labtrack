LabTrack Maintenance, Incident Tracking, & EHS Safety API Specification
This document defines the RESTful API contract for the LabTrack Maintenance Module, governing problem report submissions, EHS safety hazard overrides, repair ticket workflows, preventive maintenance schedules, and activity audit trails.

1. System Overview & Architecture
Base Endpoint: http://localhost/api/v1/maintenance (or http://localhost/api/v1/maintenance via Gateway)

Authentication: Bearer Token (Admin, Technician, or Researcher roles)

Core Schemas: maintenance, inventory, audit

Underlying Database Views: maintenance.v_dashboard_maintenance_metrics, inventory.v_asset_details_comprehensive

2. API Endpoints
2.1 Submit Problem Report
Submits an hardware incident or functional anomaly. Automatically triggers high-priority alerts if flagged as an EHS safety hazard.

HTTP Method: POST

Path: /api/v1/maintenance/problem-reports

Headers: X-Idempotency-Key: <unique_uuid>

Role Permission: Admin, Technician, Researcher

Request Payload
JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "title": "Vacuum Leak in Cryo Chamber",
  "description": "Cryo-EM vacuum pressure fluctuating rapidly during sample transfer sequence.",
  "severity_level": "Critical",
  "category": "Pneumatic & Vacuum",
  "has_safety_hazard": true
}
Response (201 Created)
JSON
{
  "report_id": "50000000-0000-0000-0000-000000000001",
  "report_number": "PR-20261021-0001",
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "reported_by_user_id": "10000000-0000-0000-0000-000000000003",
  "status": "In Progress",
  "has_safety_hazard": true,
  "created_at": "2026-10-21T17:15:00Z"
}
2.2 List Problem Reports
Retrieves problem reports with optional filtering by severity, safety hazard status, or processing state.

HTTP Method: GET

Path: /api/v1/maintenance/problem-reports

Query Parameters: page=1, size=20, status=In Progress, has_safety_hazard=true

Role Permission: Admin, Technician, Auditor

Response (200 OK)
JSON
{
  "page_number": 1,
  "page_size": 20,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "report_id": "50000000-0000-0000-0000-000000000001",
      "report_number": "PR-20261021-0001",
      "asset_id": "23000000-0000-0000-0000-000000000003",
      "asset_name": "Thermo Fisher Cryo-EM",
      "reporter_name": "John Doe",
      "title": "Vacuum Leak in Cryo Chamber",
      "severity_level": "Critical",
      "category": "Pneumatic & Vacuum",
      "status": "In Progress",
      "has_safety_hazard": true,
      "created_at": "2026-10-21T17:15:00Z"
    }
  ]
}
2.3 Create & Assign Repair Ticket
Converts an existing problem report into an assigned maintenance repair order. Automatically updates asset status to Under Maintenance.

HTTP Method: POST

Path: /api/v1/maintenance/repair-tickets

Headers: X-Idempotency-Key: <unique_uuid>

Role Permission: Admin, Technician

Request Payload
JSON
{
  "problem_report_id": "50000000-0000-0000-0000-000000000001",
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "assigned_technician_id": "10000000-0000-0000-0000-000000000002",
  "title": "Replace Vacuum Seal Valve on Cryo-EM",
  "description": "Disassemble chamber assembly and replace damaged Viton O-ring seal.",
  "priority": "Critical",
  "estimated_cost_usd": 1250.00
}
Response (201 Created)
JSON
{
  "ticket_id": "51000000-0000-0000-0000-000000000001",
  "ticket_number": "REP-20261021-0001",
  "problem_report_id": "50000000-0000-0000-0000-000000000001",
  "assigned_technician_id": "10000000-0000-0000-0000-000000000002",
  "status": "In Progress",
  "priority": "Critical",
  "estimated_cost_usd": 1250.00,
  "started_at": "2026-10-21T17:19:00Z"
}
2.4 Update Repair Ticket Status & Close Work Order
Updates repair progress, logs actual financial expenditure, and resolves the work order.

HTTP Method: PATCH

Path: /api/v1/maintenance/repair-tickets/{id}

Role Permission: Admin, Technician

Request Payload
JSON
{
  "status": "Resolved",
  "actual_cost_usd": 1250.00,
  "resolution_summary": "Replaced Viton O-ring valve seal and recalibrated vacuum chamber to 2.5 Nm torque specification. Pressure holding stable.",
  "outcome": "Passed"
}
Response (200 OK)
JSON
{
  "ticket_id": "51000000-0000-0000-0000-000000000001",
  "ticket_number": "REP-20261021-0001",
  "status": "Resolved",
  "actual_cost_usd": 1250.00,
  "resolved_at": "2026-10-21T19:19:00Z",
  "asset_restored_status": "Available"
}
2.5 Preventive Maintenance Schedules
Retrieves upcoming and overdue recurrent maintenance routines across facility assets.

HTTP Method: GET

Path: /api/v1/maintenance/schedules

Query Parameters: is_overdue=true, assigned_technician_id=10000000-0000-0000-0000-000000000002

Role Permission: Admin, Technician

Response (200 OK)
JSON
{
  "page_number": 1,
  "page_size": 25,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "schedule_id": "52000000-0000-0000-0000-000000000001",
      "asset_id": "23000000-0000-0000-0000-000000000001",
      "asset_name": "Illumina NovaSeq 6000",
      "title": "Bi-Monthly Optical Calibration",
      "schedule_type": "Calibration",
      "frequency_interval_days": 60,
      "last_performed_at": "2026-09-01T08:00:00Z",
      "next_due_at": "2026-10-31T08:00:00Z",
      "is_active": true
    }
  ]
}