LabTrack Core API Gateway & REST Interface Specification
This document defines the RESTful API contract for the LabTrack API Gateway, exposing endpoints across identity management, asset control, maintenance workflows, telemetry tracking, and financial operations.

1. System Architecture & Standards
Base URL: http://localhost/api/v1 (via Nginx Reverse Proxy)

Authentication: HTTP Bearer JWT Token (Authorization: Bearer <token>)

Content Negotiation: application/json

Concurrency Control: Optimistic Concurrency Control using version fields on mutable resources.

Idempotency: Non-idempotent operations accept an X-Idempotency-Key header for request deduplication.

2. Global Request & Response Models
2.1 Standard Paginated Envelope
JSON
{
  "page_number": 1,
  "page_size": 25,
  "total_records": 150,
  "total_pages": 6,
  "data": []
}
2.2 Standard Error Response
JSON
{
  "error_code": "RESOURCE_LOCKED",
  "message": "The requested asset is currently locked by an active reservation session.",
  "timestamp": "2026-10-21T19:15:00Z",
  "details": {
    "asset_id": "23000000-0000-0000-0000-000000000001",
    "active_reservation_id": "31000000-0000-0000-0000-000000000001"
  }
}
3. Endpoints Matrix
3.1 Authentication & Session Management (/auth)
POST /auth/login
Authenticates a user and issues JWT access and refresh tokens.

Request Payload:

JSON
{
  "email": "tech.miles@labtrack.io",
  "password": "TechPassword123!"
}
Response (200 OK):

JSON
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refresh_token": "d98f7a2b-3c4d-5e6f-7a8b-9c0d1e2f3a4b",
  "token_type": "Bearer",
  "expires_in_seconds": 3600,
  "user": {
    "id": "10000000-0000-0000-0000-000000000002",
    "email": "tech.miles@labtrack.io",
    "full_name": "Miles Dyson",
    "role": "Technician"
  }
}
POST /auth/revoke-session
Revokes all active JWT and refresh tokens for a user session.

Request Payload:

JSON
{
  "user_id": "10000000-0000-0000-0000-000000000002",
  "reason": "Administrative Session Invalidation"
}
Response (200 OK):

JSON
{
  "revoked_sessions_count": 2,
  "status": "Success"
}
3.2 Inventory & Asset Management (/assets)
GET /assets
Retrieves paginated assets using the v_asset_details_comprehensive view.

Query Parameters: page=1, size=25, category=DNA_SEQUENCER, status=Available, facility_code=FAC-BIO-01

Response (200 OK):

JSON
{
  "page_number": 1,
  "page_size": 25,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "asset_id": "23000000-0000-0000-0000-000000000001",
      "asset_name": "Illumina NovaSeq 6000",
      "category": "DNA Sequencer",
      "serial_number": "SN-ILL-990231",
      "model_number": "NovaSeq 6000",
      "manufacturer": "Illumina Inc.",
      "asset_status": "Available",
      "risk_tier": "HighRisk",
      "facility_name": "Central BioTech Center",
      "laboratory_name": "Genomics Core Lab",
      "lab_room_number": "Room 301",
      "bay_identifier": "Bay-A1 Sequencers",
      "active_qr_token_code": "QR-ASSET-001-NOVASEQ",
      "cpu_usage_pct": 12.50,
      "temperature_celsius": 24.50,
      "open_problem_reports_count": 0,
      "active_repair_tickets_count": 0
    }
  ]
}
GET /assets/{id}
Returns details for a single asset by UUID.

Response (200 OK): (Single JSON object from v_asset_details_comprehensive)

3.3 Scheduling & QR Check-In (/scheduling)
POST /scheduling/reservations
Creates an asset reservation.

Headers: X-Idempotency-Key: <unique_uuid>

Request Payload:

JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "purpose": "Whole Genome Sequencing Batch #5",
  "start_time": "2026-10-25T09:00:00Z",
  "end_time": "2026-10-25T17:00:00Z"
}
Response (201 Created):

JSON
{
  "reservation_id": "31000000-0000-0000-0000-000000000003",
  "reservation_code": "RSV-20261025-0003",
  "status": "Pending",
  "created_at": "2026-10-21T19:15:00Z"
}
POST /scheduling/qr/check-in
Validates a QR token code to initiate an on-site equipment usage session.

Request Payload:

JSON
{
  "token_code": "QR-ASSET-001-NOVASEQ"
}
Response (200 OK):

JSON
{
  "check_in_status": "Success",
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "asset_name": "Illumina NovaSeq 6000",
  "reservation_id": "31000000-0000-0000-0000-000000000001",
  "session_start_time": "2026-10-21T19:15:00Z"
}
3.4 Maintenance & Incident Tickets (/maintenance)
POST /maintenance/problem-reports
Submits a hardware problem report and flags potential safety hazards.

Request Payload:

JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "title": "Coolant Fluid Pressure Drop",
  "description": "Primary cooling loop pressure dropped below 15 PSI during run.",
  "severity_level": "High",
  "category": "Thermal & Cooling",
  "has_safety_hazard": true
}
Response (201 Created):

JSON
{
  "report_id": "50000000-0000-0000-0000-000000000002",
  "report_number": "PR-20261021-0002",
  "status": "Open",
  "has_safety_hazard": true,
  "created_at": "2026-10-21T19:15:00Z"
}
POST /maintenance/repair-tickets
Converts a problem report into an assigned maintenance repair ticket.

Request Payload:

JSON
{
  "problem_report_id": "50000000-0000-0000-0000-000000000002",
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "assigned_technician_id": "10000000-0000-0000-0000-000000000002",
  "title": "Replace Primary Coolant Pump Seal",
  "description": "Inspect and replace worn pump assembly fittings.",
  "priority": "High",
  "estimated_cost_usd": 850.00
}
Response (201 Created):

JSON
{
  "ticket_id": "51000000-0000-0000-0000-000000000002",
  "ticket_number": "REP-20261021-0002",
  "status": "Assigned",
  "created_at": "2026-10-21T19:15:00Z"
}
3.5 Operational Reporting & Dashboards (/dashboard)
GET /dashboard/kpis
Retrieves consolidated operational readiness and maintenance metrics.

Response (200 OK):

JSON
{
  "asset_kpis": {
    "total_assets": 3,
    "available_count": 2,
    "in_use_count": 0,
    "under_maintenance_count": 1,
    "operational_readiness_pct": 66.67
  },
  "maintenance_metrics": {
    "open_problem_reports_count": 1,
    "active_safety_hazards_count": 1,
    "active_repair_tickets_count": 1,
    "critical_repair_tickets_count": 1,
    "overdue_maintenance_tasks_count": 0,
    "avg_repair_mttr_hours_last_30d": 0.00
  },
  "utilization": {
    "active_reservations_now": 0,
    "reservations_starting_next_24h": 1,
    "pending_approvals_count": 1
  }
}