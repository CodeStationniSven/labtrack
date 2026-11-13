from pydantic import BaseModel, Field, ConfigDict
from typing import Generic, TypeVar, Optional, List, Any, Dict
from datetime import datetime
import uuid

# Generic type variable for standard payload structures
DataT = TypeVar("DataT")


class CoreBaseSchema(BaseModel):
    """
    Core base Pydantic schema enforcing shared configuration settings 
    across all microservice data structures.
    """
    model_config = ConfigDict(
        from_attributes=True,
        populate_by_name=True,
        str_strip_whitespace=True,
        use_enum_values=True
    )


class EntityIdSchema(CoreBaseSchema):
    """Base schema for resources defined by a UUID primary key."""
    id: uuid.UUID = Field(default_factory=uuid.uuid4, description="Unique identifier (UUIDv4)")


class TimestampedSchema(CoreBaseSchema):
    """Audit schema for resources tracking creation and modification timestamps."""
    created_at: datetime = Field(default_factory=datetime.utcnow, description="Record creation timestamp (UTC)")
    updated_at: Optional[datetime] = Field(None, description="Record last update timestamp (UTC)")


class AuditEntitySchema(EntityIdSchema, TimestampedSchema):
    """Combined base schema for persistent entity DTOs requiring ID and timestamps."""
    pass


class PaginationMetadata(CoreBaseSchema):
    """Metadata schema accompanying paginated collection responses."""
    total_count: int = Field(..., ge=0, description="Total number of matching records in database")
    page: int = Field(..., ge=1, description="Current page index (1-based)")
    page_size: int = Field(..., ge=1, le=100, description="Number of items returned per page")
    total_pages: int = Field(..., ge=0, description="Total calculated pages available")
    has_next: bool = Field(..., description="Flag indicating if a subsequent page exists")
    has_previous: bool = Field(..., description="Flag indicating if a preceding page exists")


class PaginatedResponse(CoreBaseSchema, Generic[DataT]):
    """Generic wrapper envelope for paginated resource lists."""
    items: List[DataT] = Field(default_factory=list, description="List of paginated items")
    meta: PaginationMetadata = Field(..., description="Pagination navigation metadata")


class APIResponseEnvelope(CoreBaseSchema, Generic[DataT]):
    """Standardized top-level response envelope mirroring C# API response models."""
    success: bool = Field(True, description="Indicates whether the API request was successful")
    message: str = Field("Request processed successfully.", description="Status message or summary")
    data: Optional[DataT] = Field(None, description="Response payload")
    errors: Optional[List[str]] = Field(None, description="List of validation or processing error messages")
    correlation_id: Optional[str] = Field(None, description="Request correlation ID for end-to-end tracing")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Response generation timestamp (UTC)")


class ErrorDetailSchema(CoreBaseSchema):
    """Standardized structure for operational and exception errors."""
    code: str = Field(..., description="Domain-specific error code (e.g., ERR_ASSET_NOT_FOUND)")
    message: str = Field(..., description="Human-readable error description")
    field: Optional[str] = Field(None, description="Target field associated with validation failure")
    details: Optional[Dict[str, Any]] = Field(None, description="Additional context or exception details")