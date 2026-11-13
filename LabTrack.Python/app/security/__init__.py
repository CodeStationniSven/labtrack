"""
LabTrack Security Package Initialization.

Provides cryptographic verification and token authentication utilities for
securing the FastAPI microservice and validating JSON Web Tokens (JWT) issued
by the primary C# ASP.NET Core identity service.
"""

from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any
import jwt
from datetime import datetime
import logging

logger = logging.getLogger(__name__)

# Security scheme instance for Bearer token extraction
security_scheme = HTTPBearer(auto_error=False)


class TokenPayload(BaseModel):
    """Data model representing decoded JWT claims originating from the C# backend."""

    sub: str = Field(..., description="Subject identifier (User ID)")
    email: Optional[str] = Field(None, description="User email address")
    roles: List[str] = Field(default_factory=list, description="User roles and permissions")
    exp: int = Field(..., description="Expiration timestamp (UTC epoch seconds)")
    iss: Optional[str] = Field(None, description="Token issuer identifier")
    aud: Optional[str] = Field(None, description="Token target audience")


class SecurityConfig(BaseModel):
    """Configuration model for JWT signature validation."""

    secret_key: str = Field("LabTrack_Secret_Key_For_Development_Only_Change_In_Production_2026", description="JWT HMAC shared secret key")
    algorithm: str = Field("HS256", description="Cryptographic signing algorithm")
    issuer: str = Field("LabTrack.API", description="Expected token issuer string")
    audience: str = Field("LabTrack.Services", description="Expected token audience string")


def verify_jwt_token(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security_scheme),
    config: SecurityConfig = SecurityConfig()
) -> TokenPayload:
    """
    Dependency handler that extracts and validates the JWT Bearer token from incoming HTTP requests.
    Enforces expiration checking and claim verification aligned with the C# auth service.
    """
    if credentials is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Authentication credentials were not provided.",
            headers={"WWW-Authenticate": "Bearer"}
        )

    token = credentials.credentials

    try:
        payload_dict = jwt.decode(
            token,
            config.secret_key,
            algorithms=[config.algorithm],
            options={"verify_exp": True}
        )

        # Normalize role claims (handling standard JWT claim names or ASP.NET Identity claim URIs)
        roles_claim = (
            payload_dict.get("roles") 
            or payload_dict.get("role") 
            or payload_dict.get("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", [])
        )
        if isinstance(roles_claim, str):
            roles_claim = [roles_claim]

        return TokenPayload(
            sub=payload_dict.get("sub", ""),
            email=payload_dict.get("email"),
            roles=roles_claim,
            exp=payload_dict.get("exp", 0),
            iss=payload_dict.get("iss"),
            aud=payload_dict.get("aud")
        )

    except jwt.ExpiredSignatureError:
        logger.warning("Rejected expired JWT token.")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token has expired.",
            headers={"WWW-Authenticate": "Bearer"}
        )
    except jwt.InvalidTokenError as err:
        logger.warning(f"Invalid JWT token signature/claims: {str(err)}")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid authentication token.",
            headers={"WWW-Authenticate": "Bearer"}
        )


__all__ = [
    "TokenPayload",
    "SecurityConfig",
    "verify_jwt_token",
]