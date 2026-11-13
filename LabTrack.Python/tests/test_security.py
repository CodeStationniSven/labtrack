import pytest
import hmac
import hashlib
from datetime import datetime, timedelta
from jose import jwt
from fastapi import HTTPException

from app.security import verify_jwt_token, TokenPayload
from app.security.internal_auth import (
    verify_internal_service_signature,
    ServiceIdentity,
    generate_internal_signature,
)
from app.config import settings


# --- JWT Authentication Tests ---

def create_test_jwt(
    sub: str = "user-123",
    role: str = "Admin",
    exp_delta: timedelta = timedelta(minutes=15),
    secret_key: str = settings.JWT_SECRET_KEY,
    algorithm: str = "HS256",
) -> str:
    """Helper utility to generate JWT tokens for testing."""
    expiration = datetime.utcnow() + exp_delta
    payload = {
        "sub": sub,
        "role": role,
        "exp": int(expiration.timestamp()),
        "iat": int(datetime.utcnow().timestamp()),
    }
    return jwt.encode(payload, secret_key, algorithm=algorithm)


@pytest.mark.asyncio
async def test_verify_jwt_token_valid():
    # Arrange
    token = create_test_jwt(sub="lab-tech-42", role="Technician")

    # Act
    payload = await verify_jwt_token(token=token)

    # Assert
    assert isinstance(payload, TokenPayload)
    assert payload.sub == "lab-tech-42"
    assert payload.role == "Technician"


@pytest.mark.asyncio
async def test_verify_jwt_token_expired():
    # Arrange
    expired_token = create_test_jwt(exp_delta=timedelta(minutes=-10))

    # Act & Assert
    with pytest.raises(HTTPException) as exc_info:
        await verify_jwt_token(token=expired_token)

    assert exc_info.value.status_code == 401
    assert "expired" in exc_info.value.detail.lower() or "token" in exc_info.value.detail.lower()


@pytest.mark.asyncio
async def test_verify_jwt_token_invalid_signature():
    # Arrange
    invalid_token = create_test_jwt(secret_key="wrong_secret_key_123")

    # Act & Assert
    with pytest.raises(HTTPException) as exc_info:
        await verify_jwt_token(token=invalid_token)

    assert exc_info.value.status_code == 401


# --- HMAC Internal Service Authentication Tests ---

@pytest.mark.asyncio
async def test_verify_internal_service_signature_valid():
    # Arrange
    service_name = "LabTrack.CSharpGateway"
    timestamp = str(int(datetime.utcnow().timestamp()))
    signature = generate_internal_signature(
        service_name=service_name,
        timestamp=timestamp,
        secret=settings.INTERNAL_SERVICE_SECRET,
    )

    # Act
    identity = await verify_internal_service_signature(
        x_service_name=service_name,
        x_timestamp=timestamp,
        x_signature=signature,
    )

    # Assert
    assert isinstance(identity, ServiceIdentity)
    assert identity.service_name == service_name


@pytest.mark.asyncio
async def test_verify_internal_service_signature_invalid_hash():
    # Arrange
    service_name = "LabTrack.CSharpGateway"
    timestamp = str(int(datetime.utcnow().timestamp()))
    invalid_signature = "bad_signature_hash_value"

    # Act & Assert
    with pytest.raises(HTTPException) as exc_info:
        await verify_internal_service_signature(
            x_service_name=service_name,
            x_timestamp=timestamp,
            x_signature=invalid_signature,
        )

    assert exc_info.value.status_code == 403


@pytest.mark.asyncio
async def test_verify_internal_service_signature_expired_timestamp():
    # Arrange
    service_name = "LabTrack.CSharpGateway"
    # Stale timestamp (10 minutes old)
    old_timestamp = str(int((datetime.utcnow() - timedelta(minutes=10)).timestamp()))
    signature = generate_internal_signature(
        service_name=service_name,
        timestamp=old_timestamp,
        secret=settings.INTERNAL_SERVICE_SECRET,
    )

    # Act & Assert
    with pytest.raises(HTTPException) as exc_info:
        await verify_internal_service_signature(
            x_service_name=service_name,
            x_timestamp=old_timestamp,
            x_signature=signature,
        )

    assert exc_info.value.status_code == 401
    assert "timestamp" in exc_info.value.detail.lower() or "expired" in exc_info.value.detail.lower()