from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

from app.schemas import BaseSchema


# --- Asset Management Schemas ---

class AssetBaseSchema(BaseSchema):
    name: str = Field(..., min_length=1, max_length=100, description="Name of the laboratory asset")
    serial_number: str = Field(..., min_length=1, max_length=50, description="Unique serial number")
    category: str = Field(..., description="Asset category (e.g., Workstation, Server, Network Device)")
    location: Optional[str] = Field(None, description="Physical lab room or rack location")
    is_active: bool = Field(True, description="Active status indicator")


class AssetCreateSchema(AssetBaseSchema):
    pass


class AssetUpdateSchema(BaseSchema):
    name: Optional[str] = Field(None, min_length=1, max_length=100)
    serial_number: Optional[str] = Field(None, min_length=1, max_length=50)
    category: Optional[str] = Field(None, description="Updated asset category")
    location: Optional[str] = Field(None, description="Updated physical location")
    is_active: Optional[bool] = Field(None, description="Updated active status")


class AssetResponseSchema(AssetBaseSchema):
    id: uuid.UUID
    created_at: datetime = Field(default_factory=datetime.utcnow)
    updated_at: Optional[datetime] = None


# --- Asset Telemetry Overview Schemas ---

class AssetTelemetryOverviewSchema(BaseSchema):
    asset_id: uuid.UUID
    cpu_usage_pct: float = Field(..., ge=0.0, le=100.0, description="Current CPU utilization percentage")
    memory_usage_pct: float = Field(..., ge=0.0, le=100.0, description="Current Memory utilization percentage")
    gpu_usage_pct: Optional[float] = Field(None, ge=0.0, le=100.0, description="Current GPU utilization percentage")
    temperature_celsius: float = Field(..., description="Hardware core temperature in Celsius")
    status: str = Field(..., description="System status indicator (Normal, Warning, Critical)")
    last_reported: datetime = Field(default_factory=datetime.utcnow)