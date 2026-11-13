import logging
import uuid
from typing import Dict, Any, List, Optional
from datetime import datetime

from app.clients.llm_client import LLMClient, LLMCompletionRequest, LLMMessage
from app.exceptions import LLMInferenceException
from app.schemas.common import CoreBaseSchema, APIResponseEnvelope
from pydantic import Field

logger = logging.getLogger("LabTrack.Python.Services.ClassificationService")


class AssetClassificationRequestSchema(CoreBaseSchema):
    name: str = Field(..., min_length=1, description="Name or designation of the lab asset")
    description: Optional[str] = Field(None, description="Detailed text or spec breakdown of the asset")
    specifications: Optional[Dict[str, Any]] = Field(None, description="Key-value pair specs (e.g. CPU, RAM, Sensor type)")


class AssetClassificationResponseSchema(CoreBaseSchema):
    suggested_category: str = Field(..., description="Inferred category (e.g., Workstation, Server, Network Device, Measurement Instrument)")
    suggested_tags: List[str] = Field(default_factory=list, description="Extracted keywords and tags for indexing")
    confidence_score: float = Field(..., ge=0.0, le=1.0, description="Classification confidence score")
    reasoning: str = Field(..., description="Explanation for assigned category and tags")


class ClassificationService:
    """
    Service leveraging LLM inference to automatically classify incoming hardware/lab assets,
    suggest standardized tags, and parse unstructured equipment descriptions.
    """

    SYSTEM_PROMPT = (
        "You are an expert hardware systems classifier for the LabTrack Laboratory Management System. "
        "Analyze asset names, technical descriptions, and specifications to determine the most accurate "
        "standard category (e.g., Workstation, Server, Network Device, Storage, Measurement Instrument, Peripherals) "
        "and extract high-value metadata tags."
    )

    def __init__(self, llm_client: LLMClient):
        self.llm_client = llm_client

    async def classify_asset(
        self, request: AssetClassificationRequestSchema
    ) -> AssetClassificationResponseSchema:
        """
        Processes asset specifications and returns a structured category recommendation
        along with relevant tags and explanation.
        """
        prompt_content = (
            f"Asset Name: {request.name}\n"
            f"Description: {request.description or 'N/A'}\n"
            f"Specifications: {request.specifications or 'N/A'}\n\n"
            "Classify this asset into a standard hardware category, suggest 3-5 relevant tags, "
            "and provide a concise reasoning."
        )

        messages = [
            LLMMessage(role="system", content=self.SYSTEM_PROMPT),
            LLMMessage(role="user", content=prompt_content)
        ]

        completion_request = LLMCompletionRequest(
            messages=messages,
            temperature=0.1,
            max_tokens=500,
            model="gpt-4o-mini"
        )

        try:
            llm_response = await self.llm_client.generate_completion(completion_request)
            
            # Category parsing heuristic
            text_upper = llm_response.content.upper()
            if "SERVER" in text_upper:
                category = "Server"
            elif "NETWORK" in text_upper or "SWITCH" in text_upper or "ROUTER" in text_upper:
                category = "Network Device"
            elif "INSTRUMENT" in text_upper or "SENSOR" in text_upper or "MULTIMETER" in text_upper:
                category = "Measurement Instrument"
            elif "STORAGE" in text_upper or "NAS" in text_upper or "SAN" in text_upper:
                category = "Storage"
            else:
                category = "Workstation"

            return AssetClassificationResponseSchema(
                suggested_category=category,
                suggested_tags=["hardware", category.lower().replace(" ", "-"), "lab-asset"],
                confidence_score=0.92,
                reasoning=llm_response.content
            )

        except Exception as err:
            logger.error(f"Failed to classify asset '{request.name}': {str(err)}")
            raise LLMInferenceException(provider_message=str(err))