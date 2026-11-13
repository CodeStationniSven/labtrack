from fastapi import APIRouter, HTTPException, Depends, status, Query
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

router = APIRouter()

class RepairTicketBase(BaseModel):
    asset_id: uuid.UUID = Field(..., description="Target asset UUID requiring repair")
    reported_by_user_id: uuid.UUID = Field(..., description="UUID of the user reporting the issue")
    assigned_technician_id: Optional[uuid.UUID] = Field(None, description="UUID of assigned technician")
    title: str = Field(..., min_length=3, max_length=150, description="Brief summary of the issue")
    description: str = Field(..., min_length=5, max_length=2000, description="Detailed problem description")
    priority: str = Field("Medium", description="Priority level (Low, Medium, High, Critical)")
    status: str = Field("Open", description="Ticket state (Open, InProgress, OnHold, Resolved, Cancelled)")

class RepairTicketCreate(RepairTicketBase):
    pass

class RepairTicketUpdate(BaseModel):
    assigned_technician_id: Optional[uuid.UUID] = None
    title: Optional[str] = Field(None, min_length=3, max_length=150)
    description: Optional[str] = Field(None, min_length=5, max_length=2000)
    priority: Optional[str] = None
    status: Optional[str] = None

class RepairTicketResponse(RepairTicketBase):
    id: uuid.UUID
    created_at: datetime = Field(default_factory=datetime.utcnow)
    resolved_at: Optional[datetime] = None

    class Config:
        from_attributes = True

class RepairMetricsSummary(BaseModel):
    total_open_tickets: int
    critical_priority_count: int
    in_progress_count: int
    avg_resolution_time_hours: float

@router.get("/", response_model=List[RepairTicketResponse], status_code=status.HTTP_200_OK)
async def list_repair_tickets(
    asset_id: Optional[uuid.UUID] = Query(None, description="Filter tickets by asset UUID"),
    status_filter: Optional[str] = Query(None, alias="status", description="Filter by ticket status"),
    priority: Optional[str] = Query(None, description="Filter by priority level"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100)
):
    """
    Retrieves a paginated list of repair tickets with optional filters for asset, status, and priority.
    """
    sample_tickets = [
        RepairTicketResponse(
            id=uuid.uuid4(),
            asset_id=asset_id or uuid.uuid4(),
            reported_by_user_id=uuid.uuid4(),
            assigned_technician_id=uuid.uuid4(),
            title="GPU Artifacting under CUDA workloads",
            description="Display outputs visual artifacts when running intensive PyTorch training scripts.",
            priority=priority or "High",
            status=status_filter or "InProgress",
            created_at=datetime.utcnow()
        ),
        RepairTicketResponse(
            id=uuid.uuid4(),
            asset_id=asset_id or uuid.uuid4(),
            reported_by_user_id=uuid.uuid4(),
            assigned_technician_id=None,
            title="PSU Coil Whine & Intermittent Shutdowns",
            description="Workstation turns off abruptly under heavy load spikes.",
            priority="Critical",
            status="Open",
            created_at=datetime.utcnow()
        )
    ]
    return sample_tickets

@router.get("/{ticket_id}", response_model=RepairTicketResponse, status_code=status.HTTP_200_OK)
async def get_repair_ticket(ticket_id: uuid.UUID):
    """
    Retrieves detailed information for a single repair ticket by its UUID.
    """
    if ticket_id == uuid.UUID("00000000-0000-0000-0000-000000000000"):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Repair ticket with ID '{ticket_id}' was not found."
        )

    return RepairTicketResponse(
        id=ticket_id,
        asset_id=uuid.uuid4(),
        reported_by_user_id=uuid.uuid4(),
        assigned_technician_id=uuid.uuid4(),
        title="Network Interface Controller Fault",
        description="10GbE SFP+ card fails to negotiate link speed upon cold boot.",
        priority="Medium",
        status="Open",
        created_at=datetime.utcnow()
    )

@router.post("/", response_model=RepairTicketResponse, status_code=status.HTTP_201_CREATED)
async def create_repair_ticket(payload: RepairTicketCreate):
    """
    Submits a new repair ticket to track hardware or software maintenance workflows.
    """
    new_ticket = RepairTicketResponse(
        id=uuid.uuid4(),
        asset_id=payload.asset_id,
        reported_by_user_id=payload.reported_by_user_id,
        assigned_technician_id=payload.assigned_technician_id,
        title=payload.title,
        description=payload.description,
        priority=payload.priority,
        status=payload.status,
        created_at=datetime.utcnow()
    )
    return new_ticket

@router.put("/{ticket_id}", response_model=RepairTicketResponse, status_code=status.HTTP_200_OK)
async def update_repair_ticket(ticket_id: uuid.UUID, payload: RepairTicketUpdate):
    """
    Updates status, technician assignment, priority, or details for an existing ticket.
    """
    resolved_time = datetime.utcnow() if payload.status == "Resolved" else None

    return RepairTicketResponse(
        id=ticket_id,
        asset_id=uuid.uuid4(),
        reported_by_user_id=uuid.uuid4(),
        assigned_technician_id=payload.assigned_technician_id,
        title=payload.title or "Updated Ticket Title",
        description=payload.description or "Updated description for repair procedure.",
        priority=payload.priority or "High",
        status=payload.status or "InProgress",
        created_at=datetime.utcnow(),
        resolved_at=resolved_time
    )

@router.get("/summary/metrics", response_model=RepairMetricsSummary, status_code=status.HTTP_200_OK)
async def get_repair_metrics():
    """
    Aggregates repair ticket status counts, workload intensity, and resolution statistics.
    """
    return RepairMetricsSummary(
        total_open_tickets=8,
        critical_priority_count=2,
        in_progress_count=4,
        avg_resolution_time_hours=18.5
    )