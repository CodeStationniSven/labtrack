LabTrack Financial Ledger & Asset Valuation API Specification
This specification documents the RESTful API endpoints, accounting procedures, and financial reporting contracts for the LabTrack Financial Management Module.

1. Overview & System Architecture
The Financial Management Module provides strict oversight of equipment capital acquisition, departmental cost center budgets, double-entry transaction ledgers, and automated asset depreciation calculations.

Base Endpoint: http://localhost/api/v1/financial

Authentication: Bearer Token (Admin, Auditor, or Technician roles)

Underlying Database Views: financial.v_financial_summary_executive, financial.v_cost_center_budget_breakdown, financial.v_asset_depreciation_schedule_projection

2. API Endpoints
2.1 Executive Financial Summary
Returns high-level institutional valuation metrics, aggregated budget consumption, accumulated depreciation, and maintenance expenditure commitments.

HTTP Method: GET

Path: /api/v1/financial/summary

Role Permission: Admin, Auditor

Response (200 OK)
JSON
{
  "total_allocated_budget_usd": 500000.00,
  "total_remaining_budget_usd": 482500.00,
  "total_budget_spent_usd": 17500.00,
  "budget_utilization_pct": 3.50,
  "total_tracked_financial_assets": 3,
  "total_asset_purchase_value_usd": 850000.00,
  "total_asset_salvage_value_usd": 50000.00,
  "total_accumulated_depreciation_usd": 240000.00,
  "total_current_net_book_value_usd": 610000.00,
  "lifetime_completed_repairs_cost_usd": 0.00,
  "pending_repair_commitments_usd": 1250.00
}
2.2 Cost Center Budget Breakdown
Retrieves department-level budget allocations, remaining headroom, and asset valuation rollups per cost center.

HTTP Method: GET

Path: /api/v1/financial/cost-centers

Query Parameters: fiscal_year=2026, department=Biological Sciences

Role Permission: Admin, Auditor

Response (200 OK)
JSON
{
  "page_number": 1,
  "page_size": 25,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "cost_center_id": "60000000-0000-0000-0000-000000000001",
      "cost_center_code": "CC-GENOMICS-2026",
      "cost_center_name": "Genomics Operations & Equipment",
      "department": "Biological Sciences",
      "fiscal_year": 2026,
      "manager_name": "Dr. Sarah Connor",
      "manager_email": "admin@labtrack.io",
      "allocated_budget_usd": 500000.00,
      "remaining_budget_usd": 482500.00,
      "spent_budget_usd": 17500.00,
      "budget_spent_pct": 3.50,
      "is_active": true,
      "assigned_assets_count": 1,
      "total_asset_purchase_val_usd": 850000.00,
      "total_asset_book_val_usd": 610000.00,
      "total_transactions_count": 12
    }
  ]
}
2.3 Asset Depreciation Schedule & Projections
Lists per-asset depreciation schedules, remaining useful life in months, and current net book values.

HTTP Method: GET

Path: /api/v1/financial/depreciation-schedules

Query Parameters: cost_center_code=CC-GENOMICS-2026, status=Active Depreciation

Role Permission: Admin, Auditor, Technician

Response (200 OK)
JSON
{
  "page_number": 1,
  "page_size": 25,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "ledger_id": "61000000-0000-0000-0000-000000000001",
      "asset_id": "23000000-0000-0000-0000-000000000001",
      "asset_name": "Illumina NovaSeq 6000",
      "serial_number": "SN-ILL-990231",
      "category": "DNA Sequencer",
      "asset_status": "Available",
      "cost_center_code": "CC-GENOMICS-2026",
      "in_service_date": "2024-01-15",
      "useful_life_months": 60,
      "depreciation_method": "Straight-Line",
      "purchase_price_usd": 850000.00,
      "salvage_value_usd": 50000.00,
      "accumulated_depreciation_usd": 240000.00,
      "current_book_value_usd": 610000.00,
      "monthly_depreciation_usd": 13333.33,
      "annual_depreciation_usd": 160000.00,
      "months_in_service": 33,
      "remaining_useful_life_months": 27,
      "depreciation_status": "Active Depreciation"
    }
  ]
}
2.4 Record Ledger Transaction
Executes a financial transaction against a cost center and asset ledger (e.g., maintenance chargebacks, salvage adjustments, or capital write-offs).

HTTP Method: POST

Path: /api/v1/financial/transactions

Headers: X-Idempotency-Key: <unique_uuid>

Role Permission: Admin

Request Payload
JSON
{
  "cost_center_id": "60000000-0000-0000-0000-000000000001",
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "transaction_type": "MaintenanceExpense",
  "amount_usd": 1250.00,
  "description": "Viton seal replacement and optical chamber calibration",
  "reference_repair_ticket_id": "51000000-0000-0000-0000-000000000001"
}
Response (201 Created)
JSON
{
  "transaction_id": "62000000-0000-0000-0000-000000000001",
  "cost_center_id": "60000000-0000-0000-0000-000000000001",
  "transaction_type": "MaintenanceExpense",
  "amount_usd": 1250.00,
  "updated_remaining_budget_usd": 481250.00,
  "posted_at": "2026-10-21T19:18:00Z",
  "status": "Posted"
}