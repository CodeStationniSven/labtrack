"""
LabTrack Pydantic Schemas Package Initialization.

Exports core data transfer objects (DTOs) and request/response validation schemas
used across FastAPI endpoint routes and domain service layers.
"""

from pydantic import BaseModel, Field
from typing import List, Optional, Any, Dict
from datetime import datetime
import uuid


class BaseSchema(BaseModel):
    """Base Pydantic model with common ORM and serialization configurations."""

    class Config:
        from_attributes = True
        populate_by_name = True


class PaginationParams(BaseModel):
    """Standard pagination request parameters."""

    skip: int = Field(0, ge=0, description="Number of records to skip")
    limit: int = Field(50, ge=1, le=100, description="Maximum records to return")


class StandardResponse(BaseSchema):
    """Generic wrapper response schema for API operations."""

    success: bool = Field(True, description="Indicates if operation succeeded")
    message: str = Field("Operation completed successfully", description="Status message")
    data: Optional[Any] = Field(None, description="Payload data")


__all__ = [
    "BaseSchema",
    "PaginationParams",
    "StandardResponse",
]# schemas package