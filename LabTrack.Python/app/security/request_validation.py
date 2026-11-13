from fastapi import Request, HTTPException, status
from pydantic import BaseModel, Field, ValidationError
from typing import Type, TypeVar, Any, Dict, Optional
import json
import logging

logger = logging.getLogger("LabTrack.Python.Validation")

T = TypeVar("T", bound=BaseModel)


class RequestValidationHelper:
    """
    Helper class for programmatic request payload validation, sanitization,
    and manual JSON body deserialization outside standard FastAPI route signature injections.
    """

    @staticmethod
    async def parse_and_validate(request: Request, schema_class: Type[T]) -> T:
        """
        Extracts JSON content from an incoming HTTP request and validates it 
        against a specified Pydantic schema model.
        """
        try:
            body_bytes = await request.body()
            if not body_bytes:
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST,
                    detail="Request body cannot be empty."
                )

            json_payload = json.loads(body_bytes.decode("utf-8"))
            return schema_class.model_validate(json_payload)

        except json.JSONDecodeError as err:
            logger.warning(f"Failed to parse JSON request body: {str(err)}")
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Malformed JSON body payload."
            )
        except ValidationError as err:
            logger.warning(f"Schema validation error on path '{request.url.path}': {err.errors()}")
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=err.errors()
            )

    @staticmethod
    def validate_dict(data: Dict[str, Any], schema_class: Type[T]) -> T:
        """
        Validates an existing dictionary payload directly against a Pydantic schema model.
        """
        try:
            return schema_class.model_validate(data)
        except ValidationError as err:
            logger.error(f"Dictionary validation failed: {err.errors()}")
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=err.errors()
            )


def sanitize_input_string(value: str) -> str:
    """
    Sanitizes string inputs by stripping outer whitespace and escaping potential target XSS tokens.
    """
    if not value:
        return ""
    
    cleaned = value.strip()
    # Basic XSS sanitization replacing high-risk script characters
    cleaned = cleaned.replace("<", "&lt;").replace(">", "&gt;")
    return cleaned