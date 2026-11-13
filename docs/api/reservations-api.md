LabTrack Reservation Scheduling & Conflict Engine API SpecificationThis document defines the RESTful API contract, availability validation algorithms, and conflict-resolution rules for the LabTrack Reservation Scheduling Module, governing equipment booking, approval queues, time-slot management, and active session enforcement.1. System Architecture & Validation RulesBase Endpoint: http://localhost/api/v1/scheduling/reservationsAuthentication: Bearer Token (Admin, Technician, or Researcher roles)Core Schemas: scheduling, inventory, auditConflict Rule: The system enforces strict time-interval non-overlap per asset. A reservation request is rejected if:$$\text{RequestedStart} < \text{ExistingEnd} \quad \land \quad \text{RequestedEnd} > \text{ExistingStart}$$for any reservation on the same asset_id where status \in ('Approved', 'Active', 'Pending').Asset Readiness Rule: Assets with status set to Under Maintenance, Calibration Required, or Decommissioned cannot be reserved.2. API Endpoints2.1 Search & List ReservationsRetrieves paginated asset bookings with filtering by date range, user, facility, or operational state.HTTP Method: GETPath: /api/v1/scheduling/reservationsQuery Parameters: page=1, size=20, asset_id=23000000-0000-0000-0000-000000000001, status=Approved, start_date=2026-10-21, end_date=2026-10-31Role Permission: Admin, Technician, Researcher, AuditorResponse (200 OK)JSON{
  "page_number": 1,
  "page_size": 20,
  "total_records": 1,
  "total_pages": 1,
  "data": [
    {
      "reservation_id": "31000000-0000-0000-0000-000000000001",
      "reservation_code": "RSV-20261021-0001",
      "asset_id": "23000000-0000-0000-0000-000000000001",
      "asset_name": "Illumina NovaSeq 6000",
      "reserved_by_user_id": "10000000-0000-0000-0000-000000000003",
      "reserved_by_name": "John Doe",
      "purpose": "High-Throughput RNA Sequencing Run #12",
      "start_time": "2026-10-25T08:00:00Z",
      "end_time": "2026-10-25T16:00:00Z",
      "status": "Approved",
      "approved_by_user_id": "10000000-0000-0000-0000-000000000001",
      "created_at": "2026-10-21T14:30:00Z"
    }
  ]
}
2.2 Create Booking ReservationSubmits a time-slot reservation request for a specific lab asset.HTTP Method: POSTPath: /api/v1/scheduling/reservationsHeaders: X-Idempotency-Key: <unique_uuid>Role Permission: Admin, Technician, ResearcherRequest PayloadJSON{
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "purpose": "Whole Genome Sequencing Batch #5",
  "start_time": "2026-10-26T09:00:00Z",
  "end_time": "2026-10-26T17:00:00Z"
}
Response (201 Created)JSON{
  "reservation_id": "31000000-0000-0000-0000-000000000003",
  "reservation_code": "RSV-20261021-0003",
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "status": "Pending",
  "start_time": "2026-10-26T09:00:00Z",
  "end_time": "2026-10-26T17:00:00Z",
  "created_at": "2026-10-21T19:24:00Z"
}
Response (409 Conflict - Schedule Overlap)JSON{
  "error_code": "RESERVATION_SCHEDULE_CONFLICT",
  "message": "The requested time interval overlaps with an existing approved reservation.",
  "timestamp": "2026-10-21T19:24:00Z",
  "details": {
    "asset_id": "23000000-0000-0000-0000-000000000001",
    "conflicting_reservation_id": "31000000-0000-0000-0000-000000000001",
    "conflicting_start_time": "2026-10-26T08:00:00Z",
    "conflicting_end_time": "2026-10-26T12:00:00Z"
  }
}
2.3 Approve or Reject ReservationTransitions a pending booking request into an approved schedule or rejects it with a reason code.HTTP Method: PATCHPath: /api/v1/scheduling/reservations/{id}/statusRole Permission: Admin, TechnicianRequest PayloadJSON{
  "status": "Approved",
  "approval_notes": "Facility manager verified reagent supply and power feed readiness."
}
Response (200 OK)JSON{
  "reservation_id": "31000000-0000-0000-0000-000000000003",
  "reservation_code": "RSV-20261021-0003",
  "status": "Approved",
  "approved_by_user_id": "10000000-0000-0000-0000-000000000001",
  "updated_at": "2026-10-21T19:24:00Z"
}
2.4 Cancel ReservationCancels an upcoming approved or pending booking, releasing the reserved slot for other researchers.HTTP Method: POSTPath: /api/v1/scheduling/reservations/{id}/cancelRole Permission: Admin, Technician, or the reservation owner (Researcher)Request PayloadJSON{
  "cancellation_reason": "Sample preparation delayed due to centrifuge maintenance."
}
Response (200 OK)JSON{
  "reservation_id": "31000000-0000-0000-0000-000000000003",
  "status": "Cancelled",
  "cancelled_at": "2026-10-21T19:24:00Z"
}
2.5 Check Slot AvailabilityUtility endpoint for real-time calendar pickers to verify availability across a date window without creating a reservation entity.HTTP Method: POSTPath: /api/v1/scheduling/reservations/check-availabilityRole Permission: Admin, Technician, ResearcherRequest PayloadJSON{
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "start_time": "2026-10-27T10:00:00Z",
  "end_time": "2026-10-27T14:00:00Z"
}
Response (200 OK)JSON{
  "asset_id": "23000000-0000-0000-0000-000000000001",
  "is_available": true,
  "asset_status": "Available",
  "conflicting_reservations_count": 0
}