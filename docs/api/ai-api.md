LabTrack AI Diagnostic Engine & RAG Service API Specification
The LabTrack AI Engine provides RESTful endpoints and asynchronous integration channels for Retrieval-Augmented Generation (RAG), vector similarity searches, maintenance diagnostic assist, and automated telemetry anomaly explanation.

1. Authentication & System Architecture
Base Endpoint: http://localhost:8000 (Direct) or http://localhost/api/v1/ai (Nginx Gateway)

Authentication: Bearer Token (JWT issued by .NET 8 Auth Service passed via HTTP Header)

Underlying Stack: FastApi (Python 3.11), pgvector (PostgreSQL 16), PyTorch / HuggingFace Transformers.

2. API Endpoints
2.1 Diagnostic Chat & RAG Assistant Thread
Initiates or continues an interactive troubleshooting session for a specific physical asset or repair ticket.

HTTP Method: POST

Path: /api/v1/ai/chat/completions

Headers:

HTTP
Authorization: Bearer <jwt_access_token>
Content-Type: application/json
Request Payload
JSON
{
  "session_id": "70000000-0000-0000-0000-000000000001",
  "asset_id": "23000000-0000-0000-0000-000000000003",
  "repair_ticket_id": "51000000-0000-0000-0000-000000000001",
  "prompt": "What are the primary torque specs for replacing the Viton seal on the Krios G4 vacuum assembly?",
  "temperature": 0.2,
  "stream": false
}
Response (200 OK)
JSON
{
  "session_id": "70000000-0000-0000-0000-000000000001",
  "message_id": "a1b2c3d4-e5f6-7890-abcd-1234567890ef",
  "sender_role": "assistant",
  "response": "According to the Thermo Fisher Krios G4 maintenance manual (Section 4.2), tighten the retaining flange bolts in a cross-pattern to 2.5 Nm using a calibrated torque driver.",
  "tokens_used": {
    "prompt_tokens": 45,
    "completion_tokens": 48,
    "total_tokens": 93
  },
  "referenced_sources": [
    {
      "source_id": "doc-manual-kriosg4-v2",
      "title": "Thermo Fisher Krios G4 Service Manual",
      "similarity_score": 0.924
    }
  ],
  "created_at": "2026-10-21T19:14:00Z"
}
2.2 Vector Similarity Search (Knowledge Base Query)
Queries the vector database directly for relevant equipment manuals, fault code histories, and maintenance documentation.

HTTP Method: POST

Path: /api/v1/ai/knowledge/search

Request Payload
JSON
{
  "query": "vacuum pressure fluctuation optical laser alignment",
  "category_filter": "MICROSCOPY_CRYO_EM",
  "top_k": 3
}
Response (200 OK)
JSON
{
  "query": "vacuum pressure fluctuation optical laser alignment",
  "results_count": 2,
  "matches": [
    {
      "chunk_id": "b0000000-0000-0000-0000-000000000001",
      "document_title": "Cryo-EM Vacuum Pump Fault Code Guide",
      "content_preview": "Rapid pressure drop in sample transfer chamber usually indicates a degraded Viton O-ring or valve seal leak...",
      "similarity_score": 0.887,
      "metadata": {
        "failure_code": "VACUUM_LEAK_PUMP",
        "section": "Diagnostic Troubleshooting"
      }
    },
    {
      "chunk_id": "b0000000-0000-0000-0000-000000000002",
      "document_title": "Optical Calibration Procedures",
      "content_preview": "Ensure laser path is unobstructed before re-pressurizing vacuum chamber...",
      "similarity_score": 0.742,
      "metadata": {
        "failure_code": "OPTIC_ALIGNMENT_ERR",
        "section": "Laser Realignment"
      }
    }
  ]
}
2.3 Automated Anomaly Root-Cause Analysis
Analyzes a stream of telemetry data to generate an automated problem explanation and recommended initial triage steps.

HTTP Method: POST

Path: /api/v1/ai/telemetry/analyze

Request Payload
JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000002",
  "telemetry_metrics": {
    "cpu_usage_pct": 88.20,
    "ram_usage_pct": 79.40,
    "temperature_celsius": 68.20,
    "operational_hours": 3105.00
  }
}
Response (200 OK)
JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000002",
  "risk_assessment": "Elevated Thermal & Load Pattern",
  "suggested_failure_code": "THERM_OVERHEATING",
  "explanation": "Temperature sustained at 68.2C with high CPU load. Recommended check of rack airflow and fan intake filter status.",
  "action_recommended": "Inspect intake filters and check cooling fan RPMs within 24 hours."
}
2.4 Service Health Endpoint
HTTP Method: GET

Path: /health

Response (200 OK)
JSON
{
  "status": "healthy",
  "service": "LabTrack AI Diagnostic Engine",
  "version": "1.0.0",
  "vector_store_status": "connected",
  "device": "cuda:0"
}