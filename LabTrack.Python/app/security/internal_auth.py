from fastapi import Request, HTTPException, status, Depends
from pydantic import BaseModel, Field
import hmac
import hashlib
import time
import os
import logging
from typing import Optional

logger = logging.getLogger("LabTrack.Python.Security")

INTERNAL_SERVICE_SECRET = os.getenv(
    "INTERNAL_SERVICE_SECRET", 
    "LabTrack_Internal_Service_Secret_Key_Change_In_Production_2026"
)
MAX_TIMESTAMP_DELTA_SECONDS = 300  # 5 minutes tolerance to prevent replay attacks


class ServiceIdentity(BaseModel):
    """Represents the identity and metadata of an authenticated internal service."""
    service_name: str = Field(..., description="Name of the calling internal service (e.g., 'LabTrack.CSharp.Gateway')")
    timestamp: int = Field(..., description="Epoch timestamp sent with signature")


def verify_internal_service_signature(
    request: Request,
    x_service_name: Optional[str] = Request.headers.get("X-Service-Name"),
    x_timestamp: Optional[str] = Request.headers.get("X-Timestamp"),
    x_signature: Optional[str] = Request.headers.get("X-Signature")
) -> ServiceIdentity:
    """
    Dependency that authenticates inter-service communication (e.g., C# Gateway <-> Python AI Microservice)
    using HMAC-SHA256 request signatures over timestamped payloads.
    """
    if not x_service_name or not x_timestamp or not x_signature:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing required internal service authentication headers."
        )

    # Validate timestamp freshness to prevent replay attacks
    try:
        req_timestamp = int(x_timestamp)
    except ValueError:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Invalid timestamp header format."
        )

    current_timestamp = int(time.time())
    if abs(current_timestamp - req_timestamp) > MAX_TIMESTAMP_DELTA_SECONDS:
        logger.warning(
            f"Inter-service auth failed: Request timestamp delta exceeded threshold for service '{x_service_name}'."
        )
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Request timestamp is outside the allowed clock skew tolerance window."
        )

    # Reconstruct expected HMAC-SHA256 signature string: "<service_name>:<timestamp>"
    signature_payload = f"{x_service_name}:{x_timestamp}".encode("utf-8")
    expected_signature = hmac.new(
        INTERNAL_SERVICE_SECRET.encode("utf-8"),
        signature_payload,
        hashlib.sha256
    ).hexdigest()

    # Constant-time comparison to protect against timing attacks
    if not hmac.compare_digest(expected_signature, x_signature):
        logger.warning(
            f"Inter-service auth failed: Signature mismatch for internal service '{x_service_name}'."
        )
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Invalid inter-service HMAC signature."
        )

    return ServiceIdentity(
        service_name=x_service_name,
        timestamp=req_timestamp
    )