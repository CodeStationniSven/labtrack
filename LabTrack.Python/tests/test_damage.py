import pytest
from unittest.mock import AsyncMock, MagicMock
from datetime import datetime
import uuid

from app.services.estimation_service import EstimationService
from app.schemas.common import APIResponseEnvelope
from app.exceptions import LLMInferenceException


@pytest.fixture
def mock_llm_client():
    """Fixture providing a mocked LLMClient instance."""
    return MagicMock()


@pytest.fixture
def estimation_service(mock_llm_client):
    """Fixture instantiating EstimationService with the mocked LLMClient."""
    return EstimationService(llm_client=mock_llm_client)


@pytest.mark.asyncio
async def test_estimate_repair_cost_physical_damage(estimation_service, mock_llm_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_llm_response = MagicMock()
    mock_llm_response.content = "Chassis frame bent and mounting bracket broken. Requires minor structural part replacement."
    mock_llm_client.generate_completion = AsyncMock(return_value=mock_llm_response)

    from app.services.estimation_service import RepairCostEstimationRequestSchema, RepairCostEstimationResponseSchema

    request = RepairCostEstimationRequestSchema(
        asset_id=test_asset_id,
        damage_description="Front enclosure cracked and mounting bracket bent after accidental impact.",
        damage_type="Physical Exterior",
        hardware_category="Workstation"
    )

    # Act
    result = await estimation_service.estimate_repair_cost(request)

    # Assert
    assert isinstance(result, RepairCostEstimationResponseSchema)
    assert result.asset_id == test_asset_id
    assert result.estimated_cost_usd == 320.0  # $200 parts + $120 labor for physical
    assert result.estimated_downtime_hours == 36.0
    assert result.recommended_repair_tier == "In-House Repair"
    assert result.cost_breakdown["replacement_parts"] == 200.0
    assert result.cost_breakdown["technical_labor"] == 120.0
    mock_llm_client.generate_completion.assert_called_once()


@pytest.mark.asyncio
async def test_estimate_repair_cost_liquid_electrical_damage(estimation_service, mock_llm_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_llm_response = MagicMock()
    mock_llm_response.content = "Corrosion detected on main logic board due to liquid exposure. Requires complete board swap and vendor servicing."
    mock_llm_client.generate_completion = AsyncMock(return_value=mock_llm_response)

    from app.services.estimation_service import RepairCostEstimationRequestSchema, RepairCostEstimationResponseSchema

    request = RepairCostEstimationRequestSchema(
        asset_id=test_asset_id,
        damage_description="Coolant line leaked onto high-voltage motherboard causing short circuit.",
        damage_type="Liquid / Electrical",
        hardware_category="Server"
    )

    # Act
    result = await estimation_service.estimate_repair_cost(request)

    # Assert
    assert isinstance(result, RepairCostEstimationResponseSchema)
    assert result.asset_id == test_asset_id
    assert result.estimated_cost_usd == 700.0  # $450 parts + $250 labor for electrical/liquid
    assert result.estimated_downtime_hours == 72.0
    assert result.recommended_repair_tier == "Vendor RMA"
    assert result.cost_breakdown["replacement_parts"] == 450.0
    assert result.cost_breakdown["technical_labor"] == 250.0


@pytest.mark.asyncio
async def test_estimate_repair_cost_llm_failure(estimation_service, mock_llm_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_llm_client.generate_completion = AsyncMock(
        side_effect=Exception("LLM service unreachable")
    )

    from app.services.estimation_service import RepairCostEstimationRequestSchema

    request = RepairCostEstimationRequestSchema(
        asset_id=test_asset_id,
        damage_description="Display panel flickering intermittently.",
        damage_type="Hardware",
        hardware_category="Measurement Instrument"
    )

    # Act & Assert
    with pytest.raises(LLMInferenceException) as exc_info:
        await estimation_service.estimate_repair_cost(request)

    assert "LLM service unreachable" in str(exc_info.value)