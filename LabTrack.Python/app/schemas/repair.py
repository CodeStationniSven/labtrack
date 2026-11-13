from pydantic import Field
from typing import List, Optional
from datetime import datetime
import uuid

from app.schemas.common import CoreBaseSchema, AuditEntitySchema


# --- Repair Ticket Schemas ---

class RepairTicketBaseSchema(CoreBaseSchema):
    asset_id: uuid.UUID = Field(..., description="Target asset UUID requiring maintenance or repair")
    reported_by_user_id: uuid.UUID = Field(..., description="UUID of the user who logged the repair request")
    assigned_technician_id: Optional[uuid.UUID] = Field(None, description="UUID of the assigned service technician")
    title: str = Field(..., min_length=3, max_length=150, description="Summary title of the issue or maintenance request")
    description: str = Field(..., min_length=5, max_length=2000, description="Detailed breakdown of the issue or repair procedures")
    priority: str = Field("Medium", description="Priority level (e.g., 'Low', 'Medium', 'High', 'Critical')")
    status: str = Field("Open", description="Ticket state (e.g., 'Open', 'InProgress', 'OnHold', 'Resolved', 'Cancelled')")


class RepairTicketCreateSchema(RepairTicketBaseSchema):
    pass


class RepairTicketUpdateSchema(CoreBaseSchema):
    assigned_technician_id: Optional[uuid.UUID] = Field(None, description="Updated assigned technician UUID")
    title: Optional[str] = Field(None, min_length=3, max_length=150, description="Updated ticket title")
    description: Optional[str] = Field(None, min_length=5, max_length=2000, description="Updated issue description")
    priority: Optional[str] = Field(None, description="Updated priority level")
    status: Optional[str] = Field(None, description="Updated ticket lifecycle status")


class RepairTicketResponseSchema(RepairTicketBaseSchema, AuditEntitySchema):
    resolved_at: Optional[datetime] = Field(None, description="Timestamp when the ticket was transitioned to Resolved state (UTC)")


# --- Repair Metrics & Summary Schemas ---

class RepairMetricsSummarySchema(CoreBaseSchema):
    total_open_tickets: int = Field(..., ge=0, description="Count of all active open or in-progress repair tickets")
    critical_priority_count: int = Field(..., ge=0, description="Count of open tickets flagged with Critical priority")
    in_progress_count: int = Field(..., ge=0, description="Count of tickets currently under active technician repair")
    avg_resolution_time_hours: float = Field(..., ge=0.0, description="Average time in hours to resolve repair tickets")