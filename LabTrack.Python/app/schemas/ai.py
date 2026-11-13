from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime
import uuid

from app.schemas import BaseSchema


# --- AI Chat Schemas ---

class ChatMessageSchema(BaseSchema):
    role: str = Field(..., description="Role of the sender (e.g., 'user', 'assistant', 'system')")
    content: str = Field(..., description="Text content of the message")
    timestamp: datetime = Field(default_factory=datetime.utcnow)


class ChatRequestSchema(BaseSchema):
    session_id: Optional[uuid.UUID] = Field(None, description="Existing session ID or None to initiate a new session")
    prompt: str = Field(..., min_length=1, description="User query or prompt string")
    context_asset_id: Optional[uuid.UUID] = Field(None, description="Optional Asset ID for targeted troubleshooting")


class ChatResponseSchema(BaseSchema):
    session_id: uuid.UUID
    response: str
    tokens_used: int
    created_at: datetime = Field(default_factory=datetime.utcnow)


# --- AI Diagnostics & Analysis Schemas ---

class DiagnosticsAnalysisRequestSchema(BaseSchema):
    asset_id: uuid.UUID = Field(..., description="Target laboratory asset UUID")
    error_logs: List[str] = Field(..., description="Raw log strings or diagnostic dumps")
    telemetry_summary: Optional[Dict[str, Any]] = Field(None, description="Optional telemetry snapshots for context")


class DiagnosticsAnalysisResponseSchema(BaseSchema):
    asset_id: uuid.UUID
    diagnosis: str = Field(..., description="Summary of root cause diagnosis")
    recommended_actions: List[str] = Field(..., description="List of corrective steps")
    severity: str = Field(..., description="Severity level: Low, Medium, High, or Critical")
    confidence_score: float = Field(..., ge=0.0, le=1.0, description="Model confidence score")
    analyzed_at: datetime = Field(default_factory=datetime.utcnow)