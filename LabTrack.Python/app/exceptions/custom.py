from fastapi import Request, status
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from typing import Optional, Dict, Any
import logging

logger = logging.getLogger(__name__)


class CustomValidationException(Exception):
    """
    Exception raised when business rule or custom domain validation fails
    within Python microservice routes.
    """

    def __init__(
        self,
        message: str,
        field: Optional[str] = None,
        details: Optional[Dict[str, Any]] = None,
    ):
        super().__init__(message)
        self.message = message
        self.field = field
        self.details = details or {}
        self.status_code = status.HTTP_422_UNPROCESSABLE_ENTITY


class CustomValidationErrorResponse(BaseModel):
    """Standardized payload for domain custom validation failures."""

    error: str = "CustomValidationError"
    status_code: int = status.HTTP_422_UNPROCESSABLE_ENTITY
    message: str
    field: Optional[str] = None
    details: Optional[Dict[str, Any]] = None


async def custom_validation_exception_handler(
    request: Request, exc: CustomValidationException
) -> JSONResponse:
    """
    Global FastAPI exception handler for custom validation domain exceptions.
    """
    logger.warning(
        f"Validation Error [{exc.status_code}]: {exc.message} | Field: {exc.field} | Path: {request.url.path}"
    )
    return JSONResponse(
        status_code=exc.status_code,
        content=CustomValidationErrorResponse(
            message=exc.message,
            field=exc.field,
            details=exc.details,
        ).model_dump(),
    )