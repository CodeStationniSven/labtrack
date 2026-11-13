from fastapi import APIRouter, HTTPException, Depends, status
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime
import uuid

router = APIRouter()

class ChatMessage(BaseModel):
    role: str = Field(..., description="Role of the sender (e.g., 'user', 'assistant', 'system')")
    content: str = Field(..., description="Text content of the message")
    timestamp: datetime = Field(default_factory=datetime.utcnow)

class ChatRequest(BaseModel):
    session_id: Optional[uuid.UUID] = Field(None, description="Existing session ID or null for new session")
    prompt: str = Field(..., min_length=1, description="User query or prompt")
    context_asset_id: Optional[uuid.UUID] = Field(None, description="Optional Asset ID for targeted troubleshooting")

class ChatResponse(BaseModel):
    session_id: uuid.UUID
    response: str
    tokens_used: int
    created_at: datetime = Field(default_factory=datetime.utcnow)

class DiagnosticsAnalysisRequest(BaseModel):
    asset_id: uuid.UUID
    error_logs: List[str]
    telemetry_summary: Optional[dict] = None

class DiagnosticsAnalysisResponse(BaseModel):
    asset_id: uuid.UUID
    diagnosis: str
    recommended_actions: List[str]
    severity: str  # Low, Medium, High, Critical
    confidence_score: float

@router.post("/chat", response_model=ChatResponse, status_code=status.HTTP_200_OK)
async def handle_ai_chat(request: ChatRequest):
    """
    Handles interactive AI assist queries for lab operators, diagnostic troubleshooting,
    and automated ticket guidance.
    """
    session_id = request.session_id or uuid.uuid4()
    
    # Placeholder for LLM service invocation / RAG retrieval logic
    generated_text = f"LabTrack AI Assistant: Received query regarding '{request.prompt[:30]}...'."
    if request.context_asset_id:
        generated_text += f" Contextualized for Asset ID {request.context_asset_id}."

    return ChatResponse(
        session_id=session_id,
        response=generated_text,
        tokens_used=128
    )

@router.post("/diagnose", response_model=DiagnosticsAnalysisResponse, status_code=status.HTTP_200_OK)
async def analyze_diagnostics(request: DiagnosticsAnalysisRequest):
    """
    Analyzes raw log strings and telemetry data from lab PCs to classify root causes
    and generate automated repair steps.
    """
    if not request.error_logs:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Error logs list cannot be empty for diagnostic analysis."
        )

    # Simulated AI log parsing and failure classification
    has_gpu_error = any("CUDA" in log or "GPU" in log for log in request.error_logs)
    
    if has_gpu_error:
        diagnosis = "GPU Driver instability or hardware rendering failure detected."
        recommended_actions = [
            "Reinstall or roll back NVIDIA display driver.",
            "Inspect physical PCIe seating and power connectors.",
            "Run thermal and VRAM stress benchmarks."
        ]
        severity = "High"
    else:
        diagnosis = "General OS system fault or driver anomaly."
        recommended_actions = [
            "Check system Event Viewer for event IDs 41/1001.",
            "Verify memory integrity with diagnostic check."
        ]
        severity = "Medium"

    return DiagnosticsAnalysisResponse(
        asset_id=request.asset_id,
        diagnosis=diagnosis,
        recommended_actions=recommended_actions,
        severity=severity,
        confidence_score=0.92
    )