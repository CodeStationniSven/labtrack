import logging
from typing import AsyncGenerator
from fastapi import Depends
from httpx import AsyncClient

from app.config import settings
from app.clients.csharp_api import CSharpApiClient
from app.clients.llm_client import LLMClient
from app.security import TokenPayload, verify_jwt_token
from app.security.internal_auth import ServiceIdentity, verify_internal_service_signature
from app.services.ai_service import AIService
from app.services.asset_service import AssetService
from app.services.classification_service import ClassificationService
from app.services.context_service import ContextService
from app.services.estimation_service import EstimationService
from app.services.repair_service import RepairService
from app.services.safety_service import SafetyService

logger = logging.getLogger("LabTrack.Python.Dependencies")


# --- HTTP Client Dependencies ---

async def get_http_client() -> AsyncGenerator[AsyncClient, None]:
    """
    Yields an asynchronous HTTPX client instance for external API communication.
    Ensures connection pooling and proper resource cleanup on request completion.
    """
    async with AsyncClient(timeout=30.0) as client:
        yield client


# --- Low-Level API Client Dependencies ---

def get_csharp_api_client(
    http_client: AsyncClient = Depends(get_http_client)
) -> CSharpApiClient:
    """
    Provides an instance of the CSharpApiClient configured with base gateway settings.
    """
    return CSharpApiClient(
        base_url=settings.CSHARP_API_BASE_URL,
        client=http_client
    )


def get_llm_client(
    http_client: AsyncClient = Depends(get_http_client)
) -> LLMClient:
    """
    Provides an instance of the LLMClient populated with API credentials and provider configuration.
    """
    return LLMClient(
        api_key=settings.LLM_API_KEY,
        base_url=settings.LLM_BASE_URL,
        client=http_client
    )


# --- Service Layer Dependencies ---

def get_ai_service(
    llm_client: LLMClient = Depends(get_llm_client)
) -> AIService:
    """Provides AIService for interactive support chat and log diagnostics."""
    return AIService(llm_client=llm_client)


def get_asset_service(
    csharp_client: CSharpApiClient = Depends(get_csharp_api_client)
) -> AssetService:
    """Provides AssetService for laboratory equipment management and telemetry."""
    return AssetService(csharp_client=csharp_client)


def get_classification_service(
    llm_client: LLMClient = Depends(get_llm_client)
) -> ClassificationService:
    """Provides ClassificationService for automated hardware categorization and tagging."""
    return ClassificationService(llm_client=llm_client)


def get_context_service(
    csharp_client: CSharpApiClient = Depends(get_csharp_api_client)
) -> ContextService:
    """Provides ContextService for assembling distributed asset diagnostics summaries."""
    return ContextService(csharp_client=csharp_client)


def get_estimation_service(
    llm_client: LLMClient = Depends(get_llm_client)
) -> EstimationService:
    """Provides EstimationService for repair cost and downtime predictions."""
    return EstimationService(llm_client=llm_client)


def get_repair_service(
    csharp_client: CSharpApiClient = Depends(get_csharp_api_client)
) -> RepairService:
    """Provides RepairService for maintenance tickets and metric processing."""
    return RepairService(csharp_client=csharp_client)


def get_safety_service(
    llm_client: LLMClient = Depends(get_llm_client)
) -> SafetyService:
    """Provides SafetyService for EHS risk evaluation, PPE verification, and safety permits."""
    return SafetyService(llm_client=llm_client)


# --- Security & Auth Dependency Shortcuts ---

def get_current_user(
    token_payload: TokenPayload = Depends(verify_jwt_token)
) -> TokenPayload:
    """Shortcut dependency to enforce JWT user authentication and extract identity claims."""
    return token_payload


def get_internal_service(
    service_identity: ServiceIdentity = Depends(verify_internal_service_signature)
) -> ServiceIdentity:
    """Shortcut dependency to enforce HMAC signature validation for inter-service calls."""
    return service_identity