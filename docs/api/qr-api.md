LabTrack QR Identification & On-Site Equipment Check-In API Specification
This document defines the RESTful API contract and token validation protocols for the LabTrack QR Identification Module, governing dynamic token generation, physically scanned check-ins, active session handshakes, and check-out workflows.

1. System Architecture & Security Model
Base Endpoint: http://localhost/api/v1/scheduling/qr

Authentication: Bearer Token (Admin, Technician, or Researcher roles)

Token Standard: Cryptographically signed UUID tokens mapped to physical assets (scheduling.qr_tokens).

Active Session Lock: QR check-ins require an approved reservation or an available asset state. Successfully checking in updates the asset's operational status to In Use and logs an entry in scheduling.usage_sessions.

2. API Endpoints
2.1 Generate / Rotate QR Token
Generates a new active QR token string for a physical asset, automatically invalidating any previously assigned active token.

HTTP Method: POST

Path: /api/v1/scheduling/qr/generate

Role Permission: Admin, Technician

Request Payload
JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "expiration_days": 365
}
Response (201 Created)
JSON
{
  "qr_token_id": "30000000-0000-0000-0000-000000000001",
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "token_code": "QR-ASSET-001-NOVASEQ",
  "is_active": true,
  "created_at": "2026-10-21T19:22:00Z",
  "expires_at": "2027-10-21T19:22:00Z"
}
2.2 On-Site QR Code Check-In
Validates a scanned QR token string, matches it against active reservation schedules or asset availability, and initiates a physical usage session.

HTTP Method: POST

Path: /api/v1/scheduling/qr/check-in

Headers: X-Idempotency-Key: <unique_uuid>

Role Permission: Admin, Technician, Researcher

Request Payload
JSON
{
  "token_code": "QR-ASSET-001-NOVASEQ"
}
Response (200 OK)
JSON
{
  "session_id": "32000000-0000-0000-0000-000000000001",
  "check_in_status": "Success",
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "asset_name": "Illumina NovaSeq 6000",
  "reservation_id": "31000000-0000-0000-0000-000000000001",
  "user_id": "10000000-0000-0000-0000-000000000003",
  "session_start_time": "2026-10-21T19:22:00Z",
  "asset_updated_status": "In Use"
}
Response (409 Conflict - Asset Unavailable)
JSON
{
  "error_code": "RESERVATION_CONFLICT",
  "message": "Asset is currently locked under maintenance or reserved by another user.",
  "timestamp": "2026-10-21T19:22:00Z",
  "details": {
    "asset_id": "23000000-0000-0000-0000-000000000001",
    "current_status": "Under Maintenance"
  }
}
2.3 On-Site QR Code Check-Out
Terminates an active usage session, updates the asset status back to Available, and records the total operational duration.

HTTP Method: POST

Path: /api/v1/scheduling/qr/check-out

Role Permission: Admin, Technician, Researcher

Request Payload
JSON
{
  "session_id": "32000000-0000-0000-0000-000000000001"
}
Response (200 OK)
JSON
{
  "session_id": "32000000-0000-0000-0000-000000000001",
  "check_out_status": "Completed",
  "session_start_time": "2026-10-21T18:00:00Z",
  "session_end_time": "2026-10-21T19:22:00Z",
  "duration_minutes": 82,
  "asset_restored_status": "Available"
}
2.4 Validate QR Token Metadata
Lookup utility for mobile barcode scanners to inspect asset metadata prior to performing an official check-in handshake.

HTTP Method: GET

Path: /api/v1/scheduling/qr/validate/{token_code}

Role Permission: Admin, Technician, Researcher

Response (200 OK)
JSON
{
  "token_code": "QR-ASSET-001-NOVASEQ",
  "is_valid": true,
  "asset": {
    "asset_id": "23000000-0000-0000-0000-000000000001",
    "asset_name": "Illumina NovaSeq 6000",
    "category": "DNA Sequencer",
    "asset_status": "Available",
    "facility_name": "Central BioTech Center",
    "laboratory_name": "Genomics Core Lab",
    "lab_room_number": "Room 301"
  }
}