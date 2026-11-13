from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

from app.schemas.common import CoreBaseSchema, AuditEntitySchema


# --- Damage Report Schemas ---

class DamageReportBaseSchema(CoreBaseSchema):
    asset_id: uuid.UUID = Field(..., description="Target laboratory asset UUID")
    reported_by_user_id: uuid.UUID = Field(..., description="UUID of the user reporting the damage")
    severity: str = Field(..., description="Severity level (e.g., 'Minor', 'Major', 'Critical')")
    damage_type: str = Field(..., description="Classification (e.g., 'Hardware', 'Physical Exterior', 'Liquid', 'Electrical')")
    description: str = Field(..., min_length=5, max_length=1000, description="Detailed description of the observed damage")


class DamageReportCreateSchema(DamageReportBaseSchema):
    pass


class DamageReportUpdateSchema(CoreBaseSchema):
    severity: Optional[str] = Field(None, description="Updated severity level")
    damage_type: Optional[str] = Field(None, description="Updated damage classification")
    description: Optional[str] = Field(None, min_length=5, max_length=1000, description="Updated damage description")
    is_resolved: Optional[bool] = Field(None, description="Updated resolution state")


class DamageReportResponseSchema(DamageReportBaseSchema, AuditEntitySchema):
    is_resolved: bool = Field(False, description="Flag indicating whether the damage report is resolved")
    resolved_at: Optional[datetime] = Field(None, description="Timestamp when the damage was marked resolved (UTC)")


# --- Damage Analytics Schemas ---

class DamageAssessmentSummarySchema(CoreBaseSchema):
    total_active_reports: int = Field(..., ge=0, description="Count of unresolved damage reports")
    critical_count: int = Field(..., ge=0, description="Count of critical severity damage reports")
    major_count: int = Field(..., ge=0, description="Count of major severity damage reports")
    minor_count: int = Field(..., ge=0, description="Count of minor severity damage reports")
    estimated_repair_cost_total: float = Field(..., ge=0.0, description="Total estimated cost to repair all reported damages")