from fastapi import APIRouter

from .ai_chat import router as ai_chat_router
from .telemetry import router as telemetry_router
from .recommendations import router as recommendations_router

api_router = APIRouter()

api_router.include_router(ai_chat_router, prefix="/ai", tags=["AI Assistance"])
api_router.include_router(telemetry_router, prefix="/telemetry", tags=["PC Telemetry"])
api_router.include_router(recommendations_router, prefix="/recommendations", tags=["Smart Recommendations"])

__all__ = ["api_router"]