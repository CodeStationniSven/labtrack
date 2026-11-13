LabTrack Work Order & Maintenance Repair Execution API SpecificationThis specification documents the RESTful API contract, lifecycle state machine, and resource assignment protocols for the LabTrack Work Order & Repair Execution System.1. Overview & System ArchitectureThe Repair Execution module coordinates work orders, technician assignments, spare part inventory allocations, and hardware restoration workflows following hardware failure or scheduled maintenance.Base Endpoint: http://localhost/api/v1/maintenance/repairsAuthentication: Bearer Token (Admin or Technician roles for execution; Auditor for view)Core Schemas: maintenance, inventory, financial, auditState Machine Alignment: Open $\rightarrow$ Assigned $\rightarrow$ In Progress $\rightarrow$ Waiting For Parts $\rightarrow$ Resolved $\rightarrow$ Closed2. API Endpoints2.1 Retrieve Repair Work OrdersReturns a paginated list of repair tickets filtered by status, priority, technician, or asset.HTTP Method: GETPath: /api/v1/maintenance/repairsQuery Parameters: page=1, size=20, status=In Progress, priority=Critical, technician_id=10000000-0000-0000-0000-000000000002Role Permission: Admin, Technician, AuditorResponse (200 OK)JSON{
  "page_number": 1,
  "page_size": 20,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "ticket_id": "51000000-0000-0000-0000-000000000001",
      "ticket_number": "REP-20261021-0001",
      "problem_report_id": "50000000-0000-0000-0000-000000000001",
      "asset_id": "23000000-0000-0000-0000-000000000003",
      "asset_name": "Thermo Fisher Cryo-EM",
      "assigned_technician_name": "Miles Dyson",
      "title": "Replace Vacuum Seal Valve on Cryo-EM",
      "priority": "Critical",
      "status": "In Progress",
      "estimated_cost_usd": 1250.00,
      "actual_cost_usd": null,
      "started_at": "2026-10-21T17:19:00Z",
      "created_at": "2026-10-21T17:18:00Z"
    }
  ]
}
2.2 Get Repair Ticket DetailsRetrieves complete work order metadata including problem source, assigned technician, parts consumed, and forensic change logs.HTTP Method: GETPath: /api/v1/maintenance/repairs/{id}Role Permission: Admin, Technician, AuditorResponse (200 OK)JSON{
  "ticket_id": "51000000-0000-0000-0000-000000000001",
  "ticket_number": "REP-20261021-0001",
  "problem_report": {
    "report_id": "50000000-0000-0000-0000-000000000001",
    "report_number": "PR-20261021-0001",
    "title": "Vacuum Leak in Cryo Chamber",
    "has_safety_hazard": true
  },
  "asset": {
    "asset_id": "23000000-0000-0000-0000-000000000003",
    "asset_name": "Thermo Fisher Cryo-EM",
    "serial_number": "SN-TF-EM-8841",
    "location_bay": "Bay-C3 Cryo-EM"
  },
  "assigned_technician": {
    "user_id": "10000000-0000-0000-0000-000000000002",
    "full_name": "Miles Dyson",
    "email": "tech.miles@labtrack.io"
  },
  "title": "Replace Vacuum Seal Valve on Cryo-EM",
  "description": "Disassemble chamber assembly and replace damaged Viton O-ring seal.",
  "priority": "Critical",
  "status": "In Progress",
  "estimated_cost_usd": 1250.00,
  "actual_cost_usd": 0.00,
  "started_at": "2026-10-21T17:19:00Z",
  "parts_allocated": [
    {
      "part_number": "SEAL-VITON-44B",
      "description": "Viton High-Vacuum Ring Seal",
      "quantity": 1,
      "unit_cost_usd": 450.00
    }
  ]
}
2.3 Transition Ticket StateUpdates the operational status of a repair ticket (e.g., placing on hold for spare parts, flagging resolution, or closing the work order).HTTP Method: PUTPath: /api/v1/maintenance/repairs/{id}/statusHeaders: X-Idempotency-Key: <unique_uuid>Role Permission: Admin, TechnicianRequest PayloadJSON{
  "status": "Waiting For Parts",
  "reason": "Awaiting shipment of OEM turbomolecular pump bearing kit.",
  "updated_estimated_cost_usd": 1850.00
}
Response (200 OK)JSON{
  "ticket_id": "51000000-0000-0000-0000-000000000001",
  "ticket_number": "REP-20261021-0001",
  "previous_status": "In Progress",
  "current_status": "Waiting For Parts",
  "updated_at": "2026-10-21T19:22:00Z"
}
2.4 Complete Repair & Recommission AssetFinalizes the work order, records actual labor and part costs, logs resolution summary, posts financial chargeback, and restores the linked physical asset state.HTTP Method: POSTPath: /api/v1/maintenance/repairs/{id}/completeHeaders: X-Idempotency-Key: <unique_uuid>Role Permission: Admin, TechnicianRequest PayloadJSON{
  "actual_cost_usd": 1250.00,
  "resolution_summary": "Replaced Viton O-ring valve seal and recalibrated vacuum chamber to 2.5 Nm torque specification. Pressure holding stable at 10^-7 Torr.",
  "outcome": "Passed",
  "recommission_asset_status": "Available"
}
Response (200 OK)JSON{
  "ticket_id": "51000000-0000-0000-0000-000000000001",
  "ticket_number": "REP-20261021-0001",
  "status": "Resolved",
  "outcome": "Passed",
  "actual_cost_usd": 1250.00,
  "resolved_at": "2026-10-21T19:22:00Z",
  "asset_restored": {
    "asset_id": "23000000-0000-0000-0000-000000000003",
    "status": "Available"
  },
  "ledger_transaction_id": "62000000-0000-0000-0000-000000000001"
}