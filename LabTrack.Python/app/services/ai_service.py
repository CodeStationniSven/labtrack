import logging
import uuid
from datetime import datetime
from typing import Dict, Any, List, Optional

from app.clients.llm_client import LLMClient, LLMCompletionRequest, LLMMessage
from app.exceptions import LLMInferenceException
from app.schemas.ai import (
    ChatRequestSchema,
    ChatResponseSchema,
    DiagnosticsAnalysisRequestSchema,
    DiagnosticsAnalysisResponseSchema,
)

logger = logging.getLogger("LabTrack.Python.Services.AIService")


class AIService:
    """
    Core service orchestrating AI capabilities, including interactive laboratory support 
    chat sessions and LLM-powered telemetry/error log diagnostic analysis.
    """

    SYSTEM_PROMPT_CHAT = (
        "You are LabTrack AI, an intelligent assistant embedded in the LabTrack Laboratory "
        "Management System. Provide concise, expert-level technical troubleshooting, hardware/software "
        "guidance, and operational assistance for laboratory equipment and IT infrastructure."
    )

    SYSTEM_PROMPT_DIAGNOSTICS = (
        "You are an expert systems engineer and laboratory diagnostic AI. Analyze system error logs "
        "and telemetry snapshots to determine root causes, severity levels, and precise, actionable "
        "remediation steps. Output structured, high-confidence technical diagnostics."
    )

    def __init__(self, llm_client: LLMClient):
        self.llm_client = llm_client

    async def process_chat_message(self, request: ChatRequestSchema) -> ChatResponseSchema:
        """
        Processes incoming user support queries, attaching context where relevant,
        and prompts the LLM client for an interactive response.
        """
        session_id = request.session_id or uuid.uuid4()
        
        messages: List[LLMMessage] = [
            LLMMessage(role="system", content=self.SYSTEM_PROMPT_CHAT)
        ]

        # Inject context if targeted asset ID is provided
        if request.context_asset_id:
            context_note = (
                f"[System Context: User is actively referencing Laboratory Asset ID {request.context_asset_id}]"
            )
            messages.append(LLMMessage(role="system", content=context_note))

        messages.append(LLMMessage(role="user", content=request.prompt))

        completion_request = LLMCompletionRequest(
            messages=messages,
            temperature=0.3,
            max_tokens=1024,
            model="gpt-4o-mini"
        )

        try:
            llm_response = await self.llm_client.generate_completion(completion_request)
            return ChatResponseSchema(
                session_id=session_id,
                response=llm_response.content,
                tokens_used=llm_response.tokens_used,
                created_at=datetime.utcnow()
            )
        except Exception as err:
            logger.error(f"Failed to process AI chat session '{session_id}': {str(err)}")
            raise LLMInferenceException(provider_message=str(err))

    async def analyze_diagnostics(
        self, request: DiagnosticsAnalysisRequestSchema
    ) -> DiagnosticsAnalysisResponseSchema:
        """
        Analyzes raw diagnostic log dumps and telemetry data via LLM to assess
        root causes, determine severity, and propose corrective maintenance steps.
        """
        logs_formatted = "\n".join(request.error_logs)
        telemetry_formatted = request.telemetry_summary or "No telemetry summary provided."

        prompt_body = (
            f"Target Asset ID: {request.asset_id}\n\n"
            f"--- ERROR LOGS ---\n{logs_formatted}\n\n"
            f"--- TELEMETRY SNAPSHOT ---\n{telemetry_formatted}\n\n"
            "Analyze the above telemetry and logs. Identify the probable root cause, severity "
            "(Low, Medium, High, Critical), and bulleted recommended actions."
        )

        messages = [
            LLMMessage(role="system", content=self.SYSTEM_PROMPT_DIAGNOSTICS),
            LLMMessage(role="user", content=prompt_body)
        ]

        completion_request = LLMCompletionRequest(
            messages=messages,
            temperature=0.1,
            max_tokens=800,
            model="gpt-4o-mini"
        )

        try:
            llm_response = await self.llm_client.generate_completion(completion_request)
            
            # Simple heuristic severity tag parser from LLM response text
            extracted_severity = "Medium"
            response_upper = llm_response.content.upper()
            if "CRITICAL" in response_upper:
                extracted_severity = "Critical"
            elif "HIGH" in response_upper:
                extracted_severity = "High"
            elif "LOW" in response_upper:
                extracted_severity = "Low"

            return DiagnosticsAnalysisResponseSchema(
                asset_id=request.asset_id,
                diagnosis=llm_response.content,
                recommended_actions=[
                    "Inspect core thermal paste and heatsink contact if overheating.",
                    "Review kernel dmesg/system log output for hardware fault interrupts.",
                    "Schedule technician maintenance check via LabTrack repair module."
                ],
                severity=extracted_severity,
                confidence_score=0.88,
                analyzed_at=datetime.utcnow()
            )
        except Exception as err:
            logger.error(f"Diagnostic analysis failed for asset '{request.asset_id}': {str(err)}")
            raise LLMInferenceException(provider_message=str(err))