LabTrack PC Telemetry & Usage Monitoring API Specification
This specification documents the RESTful API endpoints, real-time WebSocket contracts, and network switch mapping interfaces for the LabTrack PC Telemetry & Usage Module.

1. Overview & Architecture
The PC Telemetry & Usage Module interfaces directly with physical lab workstation nodes and network infrastructure to record hardware diagnostics, monitor active session times, track network port statuses, and push telemetry updates via WebSockets.

Base Endpoint: http://localhost/api/v1/telemetry

WebSocket Endpoint: ws://localhost/hubs/telemetry

Authentication: Bearer Token (Admin or Technician roles for ingestion/management; Researcher for view)

Underlying Tables & Views: telemetry.pc_workstations, inventory.asset_telemetry_summaries, inventory.v_asset_details_comprehensive

2. API Endpoints
2.1 Ingest Telemetry Heartbeat
Ingests periodic hardware metrics (CPU, memory, temperature, uptime) sent by the LabTrack background agent running on workstation PCs.

HTTP Method: POST

Path: /api/v1/telemetry/heartbeat

Headers:

HTTP
Authorization: Bearer <agent_api_token>
Content-Type: application/json
Request Payload
JSON
{
  "asset_id": "23000000-0000-0000-0000-000000000002",
  "workstation_id": "40000000-0000-0000-0000-000000000001",
  "hostname": "WS-GENOMICS-01",
  "cpu_usage_pct": 88.20,
  "ram_usage_pct": 79.40,
  "temperature_celsius": 68.20,
  "operational_hours": 3105.00,
  "is_online": true
}
Response (200 OK)
JSON
{
  "status": "Recorded",
  "asset_id": "23000000-0000-0000-0000-000000000002",
  "recorded_at": "2026-10-21T19:20:00Z"
}
2.2 List Workstation PCs
Retrieves workstation hardware specifications, active IP/MAC bindings, and real-time connectivity status.

HTTP Method: GET

Path: /api/v1/telemetry/workstations

Query Parameters: page=1, size=20, is_online=true, laboratory_id=11000000-0000-0000-0000-000000000001

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
      "workstation_id": "40000000-0000-0000-0000-000000000001",
      "asset_id": "23000000-0000-0000-0000-000000000002",
      "asset_name": "NVIDIA DGX H100 Node",
      "hostname": "WS-GENOMICS-01",
      "ip_address": "192.168.10.45",
      "mac_address": "00:1A:2C:3D:4E:5F",
      "os_version": "Ubuntu 22.04.3 LTS (GNU/Linux 5.15.0-88-generic)",
      "is_online": true,
      "last_ping_at": "2026-10-21T19:20:00Z"
    }
  ]
}
2.3 Network Switch & Port Topology
Returns network switch port assignments, VLAN associations, and connection statuses for lab workstations and managed hardware.

HTTP Method: GET

Path: /api/v1/telemetry/network/topology

Query Parameters: switch_ip=192.168.10.1

Role Permission: Admin, Technician

Response (200 OK)
JSON
{
  "switch_name": "SW-BIO-CORE-01",
  "switch_ip": "192.168.10.1",
  "model": "Cisco Catalyst 9300",
  "total_ports": 48,
  "port_mappings": [
    {
      "port_number": 12,
      "port_name": "GigabitEthernet1/0/12",
      "vlan_id": 100,
      "vlan_name": "LAB_INSTRUMENTS",
      "linked_workstation_id": "40000000-0000-0000-0000-000000000001",
      "linked_asset_name": "NVIDIA DGX H100 Node",
      "port_status": "Up",
      "speed_mbps": 10000
    }
  ]
}
3. Real-Time Telemetry WebSocket Stream
The WebSockets hub pushes periodic metrics directly to dashboard frontend clients.

URL: ws://localhost/hubs/telemetry

Protocol: SignalR / Raw WebSocket JSON

WebSocket Event: TelemetryUpdated
JSON
{
  "event": "TelemetryUpdated",
  "timestamp": "2026-10-21T19:20:00Z",
  "payload": {
    "asset_id": "23000000-0000-0000-0000-000000000002",
    "cpu_usage_pct": 88.20,
    "ram_usage_pct": 79.40,
    "temperature_celsius": 68.20,
    "is_online": true
  }
}