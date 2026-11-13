from fastapi import APIRouter, HTTPException, Depends, status, Query
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

router = APIRouter()

class AssetBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=100, description="Name of the laboratory asset")
    serial_number: str = Field(..., min_length=1, max_length=50, description="Unique serial number")
    category: str = Field(..., description="Asset category (e.g., Workstation, Server, Network Device)")
    location: Optional[str] = Field(None, description="Physical lab room or rack location")
    is_active: bool = Field(True, description="Active status indicator")

class AssetCreate(AssetBase):
    pass

class AssetUpdate(BaseModel):
    name: Optional[str] = Field(None, min_length=1, max_length=100)
    serial_number: Optional[str] = Field(None, min_length=1, max_length=50)
    category: Optional[str] = None
    location: Optional[str] = None
    is_active: Optional[bool] = None

class AssetResponse(AssetBase):
    id: uuid.UUID
    created_at: datetime = Field(default_factory=datetime.utcnow)
    updated_at: Optional[datetime] = None

    class Config:
        from_attributes = True

class AssetTelemetryOverview(BaseModel):
    asset_id: uuid.UUID
    cpu_usage_pct: float
    memory_usage_pct: float
    gpu_usage_pct: Optional[float] = None
    temperature_celsius: float
    status: str  # Normal, Warning, Critical
    last_reported: datetime = Field(default_factory=datetime.utcnow)

@router.get("/", response_model=List[AssetResponse], status_code=status.HTTP_200_OK)
async def list_assets(
    category: Optional[str] = Query(None, description="Filter assets by category"),
    is_active: Optional[bool] = Query(None, description="Filter assets by active status"),
    skip: int = Query(0, ge=0),
    limit: int = Query(50, ge=1, le=100)
):
    """
    Retrieves a paginated list of laboratory assets with optional filtering.
    """
    # Simulated database retrieval
    sample_assets = [
        AssetResponse(
            id=uuid.uuid4(),
            name="AI Training Rig 01",
            serial_number="SN-GPU-9001",
            category="Workstation",
            location="Room 302 - Bay A",
            is_active=True
        ),
        AssetResponse(
            id=uuid.uuid4(),
            name="Main Switch Rack",
            serial_number="SN-NET-4040",
            category="Network Device",
            location="Server Closet B",
            is_active=True
        )
    ]
    return sample_assets

@router.get("/{asset_id}", response_model=AssetResponse, status_code=status.HTTP_200_OK)
async def get_asset(asset_id: uuid.UUID):
    """
    Retrieves detailed information for a single asset by its UUID.
    """
    if asset_id == uuid.UUID("00000000-0000-0000-0000-000000000000"):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Asset with ID '{asset_id}' was not found."
        )

    return AssetResponse(
        id=asset_id,
        name="Precision Workstation 5820",
        serial_number="SN-WORKSTATION-55",
        category="Workstation",
        location="Room 101 - Station 04",
        is_active=True
    )

@router.post("/", response_model=AssetResponse, status_code=status.HTTP_201_CREATED)
async def create_asset(payload: AssetCreate):
    """
    Registers a new asset in the laboratory management catalog.
    """
    new_asset = AssetResponse(
        id=uuid.uuid4(),
        name=payload.name,
        serial_number=payload.serial_number,
        category=payload.category,
        location=payload.location,
        is_active=payload.is_active,
        created_at=datetime.utcnow()
    )
    return new_asset

@router.put("/{asset_id}", response_model=AssetResponse, status_code=status.HTTP_200_OK)
async def update_asset(asset_id: uuid.UUID, payload: AssetUpdate):
    """
    Updates mutable properties of an existing asset record.
    """
    return AssetResponse(
        id=asset_id,
        name=payload.name or "Updated Workstation",
        serial_number=payload.serial_number or "SN-UPDATED-00",
        category=payload.category or "Workstation",
        location=payload.location,
        is_active=payload.is_active if payload.is_active is not None else True,
        updated_at=datetime.utcnow()
    )

@router.get("/{asset_id}/telemetry/summary", response_model=AssetTelemetryOverview, status_code=status.HTTP_200_OK)
async def get_asset_telemetry_summary(asset_id: uuid.UUID):
    """
    Fetches real-time telemetry metrics summary for a specific PC hardware asset.
    """
    return AssetTelemetryOverview(
        asset_id=asset_id,
        cpu_usage_pct=42.5,
        memory_usage_pct=68.1,
        gpu_usage_pct=88.3,
        temperature_celsius=71.2,
        status="Normal",
        last_reported=datetime.utcnow()
    )