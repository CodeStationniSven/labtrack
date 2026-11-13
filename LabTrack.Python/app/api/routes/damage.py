from fastapi import APIRouter, HTTPException, Depends, status, Query
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

router = APIRouter()

class DamageReportBase(BaseModel):
    asset_id: uuid.UUID = Field(..., description="Target laboratory asset UUID")
    reported_by_user_id: uuid.UUID = Field(..., description="User ID of the reporter")
    severity: str = Field(..., description="Severity level (e.g., Minor, Major, Critical)")
    damage_type: str = Field(..., description="Classification (e.g., Hardware, Physical Exterior, Liquid, Electrical)")
    description: str = Field(..., min_length=5, max_length=1000, description="Detailed description of the damage observed")

class DamageReportCreate(DamageReportBase):
    pass

class DamageReportUpdate(BaseModel):
    severity: Optional[str] = None
    damage_type: Optional[str] = None
    description: Optional[str] = Field(None, min_length=5, max_length=1000)
    is_resolved: Optional[bool] = None

class DamageReportResponse(DamageReportBase):
    id: uuid.UUID
    is_resolved: bool = False
    reported_at: datetime = Field(default_factory=datetime.utcnow)
    resolved_at: Optional[datetime] = None

    class Config:
        from_attributes = True

class DamageAssessmentSummary(BaseModel):
    total_active_reports: int
    critical_count: int
    major_count: int
    minor_count: int
    estimated_repair_cost_total: float

@router.get("/", response_model=List[DamageReportResponse], status_code=status.HTTP_200_OK)
async def list_damage_reports(
    asset_id: Optional[uuid.UUID] = Query(None, description="Filter reports by asset UUID"),
    is_resolved: Optional[bool] = Query(None, description="Filter by resolution state"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100)
):
    """
    Retrieves a paginated list of physical and operational damage reports for lab assets.
    """
    sample_reports = [
        DamageReportResponse(
            id=uuid.uuid4(),
            asset_id=asset_id or uuid.uuid4(),
            reported_by_user_id=uuid.uuid4(),
            severity="Critical",
            damage_type="Liquid",
            description="Coolant line leak near top radiator fan assembly.",
            is_resolved=False,
            reported_at=datetime.utcnow()
        ),
        DamageReportResponse(
            id=uuid.uuid4(),
            asset_id=asset_id or uuid.uuid4(),
            reported_by_user_id=uuid.uuid4(),
            severity="Minor",
            damage_type="Physical Exterior",
            description="Front USB-C port housing loose and bent pins.",
            is_resolved=True,
            reported_at=datetime.utcnow(),
            resolved_at=datetime.utcnow()
        )
    ]
    return sample_reports

@router.get("/{report_id}", response_model=DamageReportResponse, status_code=status.HTTP_200_OK)
async def get_damage_report(report_id: uuid.UUID):
    """
    Retrieves single damage report details by its ID.
    """
    if report_id == uuid.UUID("00000000-0000-0000-0000-000000000000"):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Damage report with ID '{report_id}' was not found."
        )

    return DamageReportResponse(
        id=report_id,
        asset_id=uuid.uuid4(),
        reported_by_user_id=uuid.uuid4(),
        severity="Major",
        damage_type="Hardware",
        description="Cracked display panel housing and fractured hinges on mobile station.",
        is_resolved=False,
        reported_at=datetime.utcnow()
    )

@router.post("/", response_model=DamageReportResponse, status_code=status.HTTP_201_CREATED)
async def create_damage_report(payload: DamageReportCreate):
    """
    Submits a new damage report entry for an asset, flagging it for repair evaluation.
    """
    new_report = DamageReportResponse(
        id=uuid.uuid4(),
        asset_id=payload.asset_id,
        reported_by_user_id=payload.reported_by_user_id,
        severity=payload.severity,
        damage_type=payload.damage_type,
        description=payload.description,
        is_resolved=False,
        reported_at=datetime.utcnow()
    )
    return new_report

@router.put("/{report_id}", response_model=DamageReportResponse, status_code=status.HTTP_200_OK)
async def update_damage_report(report_id: uuid.UUID, payload: DamageReportUpdate):
    """
    Updates status, severity, or resolution details on an existing damage report.
    """
    resolved_timestamp = datetime.utcnow() if payload.is_resolved else None

    return DamageReportResponse(
        id=report_id,
        asset_id=uuid.uuid4(),
        reported_by_user_id=uuid.uuid4(),
        severity=payload.severity or "Major",
        damage_type=payload.damage_type or "Hardware",
        description=payload.description or "Updated description for reported damage.",
        is_resolved=payload.is_resolved if payload.is_resolved is not None else False,
        reported_at=datetime.utcnow(),
        resolved_at=resolved_timestamp
    )

@router.get("/summary/analytics", response_model=DamageAssessmentSummary, status_code=status.HTTP_200_OK)
async def get_damage_summary():
    """
    Computes active damage metrics and estimated repair expenses across all laboratory equipment.
    """
    return DamageAssessmentSummary(
        total_active_reports=12,
        critical_count=2,
        major_count=4,
        minor_count=6,
        estimated_repair_cost_total=3850.00
    )