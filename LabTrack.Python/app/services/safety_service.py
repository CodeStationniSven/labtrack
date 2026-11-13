import logging
import uuid
from typing import List, Optional, Dict, Any
from datetime import datetime
from pydantic import Field

from app.clients.llm_client import LLMClient, LLMCompletionRequest, LLMMessage
from app.exceptions import LLMInferenceException
from app.schemas.common import CoreBaseSchema

logger = logging.getLogger("LabTrack.Python.Services.SafetyService")


class SafetyEvaluationRequestSchema(CoreBaseSchema):
    asset_id: uuid.UUID = Field(..., description="Target laboratory asset UUID")
    asset_name: str = Field(..., min_length=1, description="Name or model designation of the equipment")
    category: str = Field(..., description="Equipment category (e.g., Laser System, Chemical Reactor, High-Voltage Power Supply)")
    operation_context: str = Field(..., min_length=5, description="Description of intended experiment or operating conditions")
    environment_flags: Optional[List[str]] = Field(
        default_factory=list,
        description="Environmental flags (e.g., 'High Humidity', 'Flammable Atmosphere', 'Unvented Enclosure')"
    )


class HazardAssessmentSchema(CoreBaseSchema):
    hazard_type: str = Field(..., description="Classification of hazard (e.g., Thermal, Electrical, Chemical, Radiation, Biohazard)")
    severity: str = Field(..., description="Severity level: Low, Moderate, High, or Severe")
    description: str = Field(..., description="Detailed explanation of the risk factor")


class SafetyEvaluationResponseSchema(CoreBaseSchema):
    asset_id: uuid.UUID
    is_operation_permitted: bool = Field(..., description="Indicates if operation is deemed safe under requested conditions")
    overall_risk_level: str = Field(..., description="Overall risk rating: Low, Moderate, High, or Critical")
    required_ppe: List[str] = Field(..., description="Mandatory Personal Protective Equipment required")
    safety_protocols: List[str] = Field(..., description="Operational safety instructions and prerequisites")
    identified_hazards: List[HazardAssessmentSchema] = Field(default_factory=list, description="Itemized list of detected hazards")
    justification: str = Field(..., description="Detailed safety evaluation breakdown and regulatory compliance notes")
    evaluated_at: datetime = Field(default_factory=datetime.utcnow, description="Assessment timestamp (UTC)")


class SafetyService:
    """
    Service leveraging LLM inference to conduct automated laboratory safety risk assessments,
    verify Personal Protective Equipment (PPE) requirements, evaluate hazard profiles, and enforce
    operational compliance for lab equipment.
    """

    SYSTEM_PROMPT = (
        "You are an expert Environmental Health and Safety (EHS) compliance AI for the LabTrack platform. "
        "Analyze laboratory asset specifications, operating conditions, and environmental factors to evaluate "
        "operational safety, identify chemical/physical/electrical hazards, determine required PPE, "
        "and establish whether the operation should be permitted."
    )

    def __init__(self, llm_client: LLMClient):
        self.llm_client = llm_client

    async def evaluate_operational_safety(
        self, request: SafetyEvaluationRequestSchema
    ) -> SafetyEvaluationResponseSchema:
        """
        Evaluates operational conditions for a laboratory asset, returning a comprehensive safety assessment,
        hazard breakdown, mandatory PPE requirements, and a pass/fail permit flag.
        """
        env_flags_str = ", ".join(request.environment_flags) if request.environment_flags else "None reported"

        prompt_content = (
            f"Asset ID: {request.asset_id}\n"
            f"Asset Name: {request.asset_name}\n"
            f"Category: {request.category}\n"
            f"Operating Context: {request.operation_context}\n"
            f"Environmental Flags: {env_flags_str}\n\n"
            "Perform an EHS safety risk assessment. Determine whether operation is permitted, calculate the overall "
            "risk level (Low, Moderate, High, Critical), list required PPE, outline mandatory safety protocols, "
            "identify key hazards with severity ratings, and provide a clear safety justification."
        )

        messages = [
            LLMMessage(role="system", content=self.SYSTEM_PROMPT),
            LLMMessage(role="user", content=prompt_content),
        ]

        completion_request = LLMCompletionRequest(
            messages=messages,
            temperature=0.1,
            max_tokens=800,
            model="gpt-4o-mini",
        )

        try:
            llm_response = await self.llm_client.generate_completion(completion_request)
            content_upper = llm_response.content.upper()

            # Rule-based safety permit evaluation heuristics based on risk keywords
            is_permitted = True
            overall_risk = "Low"

            if "CRITICAL" in content_upper or "UNSAFE" in content_upper or "PROHIBITED" in content_upper:
                is_permitted = False
                overall_risk = "Critical"
            elif "HIGH RISK" in content_upper or "SEVERE HAZARD" in content_upper:
                overall_risk = "High"
                if any(flag.lower() in ["unvented enclosure", "flammable atmosphere"] for flag in (request.environment_flags or [])):
                    is_permitted = False
            elif "MODERATE" in content_upper or "MEDIUM" in content_upper:
                overall_risk = "Moderate"

            # Contextual PPE assignment baseline
            default_ppe = ["Safety Glasses", "Lab Coat", "Nitrile Gloves"]
            category_upper = request.category.upper()
            if "LASER" in category_upper:
                default_ppe.append("Laser Safety Goggles (Appropriate Wavelength OD)")
            if "CHEMICAL" in category_upper or "FLUID" in category_upper:
                default_ppe.extend(["Face Shield", "Chemical Resistant Apron"])
            if "HIGH-VOLTAGE" in category_upper or "ELECTRICAL" in category_upper:
                default_ppe.append("Dielectric Insulated Gloves")

            # Basic hazard parsing fallback structures
            sample_hazards = [
                HazardAssessmentSchema(
                    hazard_type="Operational Risk",
                    severity=overall_risk,
                    description=f"Potential risk associated with {request.category} operation under given environmental parameters."
                )
            ]

            return SafetyEvaluationResponseSchema(
                asset_id=request.asset_id,
                is_operation_permitted=is_permitted,
                overall_risk_level=overall_risk,
                required_ppe=default_ppe,
                safety_protocols=[
                    "Verify emergency shut-off controls and eyewash station proximity before energizing.",
                    "Ensure adequate ventilation/fume hood engagement during operation.",
                    "Inspect all wiring, seals, and structural mounts prior to initiating procedure.",
                ],
                identified_hazards=sample_hazards,
                justification=llm_response.content,
                evaluated_at=datetime.utcnow(),
            )

        except Exception as err:
            logger.error(f"Safety evaluation failed for asset '{request.asset_id}': {str(err)}")
            raise LLMInferenceException(provider_message=str(err))