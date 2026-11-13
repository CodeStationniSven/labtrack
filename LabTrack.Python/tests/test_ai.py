import pytest
from unittest.mock import AsyncMock, MagicMock
from datetime import datetime
import uuid

from app.services.ai_service import AIService
from app.schemas.ai import (
    ChatRequestSchema,
    ChatResponseSchema,
    DiagnosticsAnalysisRequestSchema,
    DiagnosticsAnalysisResponseSchema,
)
from app.exceptions import LLMInferenceException


@pytest.fixture
def mock_llm_client():
    """Fixture to provide a mocked LLMClient instance."""
    return MagicMock()


@pytest.fixture
def ai_service(mock_llm_client):
    """Fixture to instantiate AIService with the mocked LLMClient."""
    return AIService(llm_client=mock_llm_client)


@pytest.mark.asyncio
async def test_process_chat_message_success(ai_service, mock_llm_client):
    # Arrange
    test_session_id = uuid.uuid4()
    chat_request = ChatRequestSchema(
        session_id=test_session_id,
        prompt="How do I recalibrate the centrifuge?",
        context_asset_id=uuid.uuid4()
    )

    mock_completion_response = MagicMock()
    mock_completion_response.content = "To recalibrate the centrifuge, access panel B..."
    mock_completion_response.tokens_used = 42

    mock_llm_client.generate_completion = AsyncMock(return_value=mock_completion_response)

    # Act
    response = await ai_service.process_chat_message(chat_request)

    # Assert
    assert isinstance(response, ChatResponseSchema)
    assert response.session_id == test_session_id
    assert response.response == "To recalibrate the centrifuge, access panel B..."
    assert response.tokens_used == 42
    assert isinstance(response.created_at, datetime)
    mock_llm_client.generate_completion.assert_called_once()


@pytest.mark.asyncio
async def test_process_chat_message_llm_exception(ai_service, mock_llm_client):
    # Arrange
    chat_request = ChatRequestSchema(
        prompt="Test error handling",
        context_asset_id=None
    )
    mock_llm_client.generate_completion = AsyncMock(
        side_effect=Exception("LLM Provider connection timeout")
    )

    # Act & Assert
    with pytest.raises(LLMInferenceException) as exc_info:
        await ai_service.process_chat_message(chat_request)

    assert "LLM Provider connection timeout" in str(exc_info.value)


@pytest.mark.asyncio
async def test_analyze_diagnostics_critical_severity(ai_service, mock_llm_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    diagnostics_request = DiagnosticsAnalysisRequestSchema(
        asset_id=test_asset_id,
        error_logs=["ERROR 0x8001: Overheating detected", "CRITICAL thermal limit reached"],
        telemetry_summary="CPU Temp: 98C, GPU Temp: 102C"
    )

    mock_completion_response = MagicMock()
    mock_completion_response.content = "CRITICAL: System experiencing severe thermal throttling."
    mock_llm_client.generate_completion = AsyncMock(return_value=mock_completion_response)

    # Act
    response = await ai_service.analyze_diagnostics(diagnostics_request)

    # Assert
    assert isinstance(response, DiagnosticsAnalysisResponseSchema)
    assert response.asset_id == test_asset_id
    assert response.severity == "Critical"
    assert response.confidence_score == 0.88
    assert len(response.recommended_actions) > 0
    mock_llm_client.generate_completion.assert_called_once()


@pytest.mark.asyncio
async def test_analyze_diagnostics_low_severity(ai_service, mock_llm_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    diagnostics_request = DiagnosticsAnalysisRequestSchema(
        asset_id=test_asset_id,
        error_logs=["WARN: Minor frame drop"],
        telemetry_summary="CPU Temp: 45C"
    )

    mock_completion_response = MagicMock()
    mock_completion_response.content = "LOW severity warning regarding UI rendering drops."
    mock_llm_client.generate_completion = AsyncMock(return_value=mock_completion_response)

    # Act
    response = await ai_service.analyze_diagnostics(diagnostics_request)

    # Assert
    assert isinstance(response, DiagnosticsAnalysisResponseSchema)
    assert response.severity == "Low"