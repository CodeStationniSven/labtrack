import pytest
from unittest.mock import AsyncMock, MagicMock
from datetime import datetime
import uuid

from app.services.asset_service import AssetService
from app.services.repair_service import RepairService
from app.schemas.asset import (
    AssetCreateSchema,
    AssetResponseSchema,
    AssetUpdateSchema,
    AssetTelemetryOverviewSchema,
)
from app.schemas.repair import (
    RepairTicketCreateSchema,
    RepairTicketResponseSchema,
    RepairTicketUpdateSchema,
    RepairMetricsSummarySchema,
)
from app.exceptions import AssetNotFoundException, CSharpServiceUnavailableException


# --- Fixtures ---

@pytest.fixture
def mock_csharp_client():
    """Fixture providing a mocked CSharpApiClient instance."""
    return MagicMock()


@pytest.fixture
def asset_service(mock_csharp_client):
    """Fixture instantiating AssetService with the mocked CSharpApiClient."""
    return AssetService(csharp_client=mock_csharp_client)


@pytest.fixture
def repair_service(mock_csharp_client):
    """Fixture instantiating RepairService with the mocked CSharpApiClient."""
    return RepairService(csharp_client=mock_csharp_client)


# --- AssetService Unit Tests ---

@pytest.mark.asyncio
async def test_get_asset_by_id_success(asset_service, mock_csharp_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_payload = {
        "id": str(test_asset_id),
        "name": "High-Precision Gas Chromatograph",
        "category": "Measurement Instrument",
        "serial_number": "GC-2026-X99",
        "status": "Active",
        "created_at": datetime.utcnow().isoformat()
    }
    mock_csharp_client.get = AsyncMock(return_value=mock_payload)

    # Act
    result = await asset_service.get_asset_by_id(test_asset_id)

    # Assert
    assert isinstance(result, AssetResponseSchema)
    assert result.id == test_asset_id
    assert result.name == "High-Precision Gas Chromatograph"
    mock_csharp_client.get.assert_called_once_with(f"/api/assets/{test_asset_id}")


@pytest.mark.asyncio
async def test_get_asset_by_id_not_found(asset_service, mock_csharp_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_csharp_client.get = AsyncMock(return_value=None)

    # Act & Assert
    with pytest.raises(AssetNotFoundException) as exc_info:
        await asset_service.get_asset_by_id(test_asset_id)

    assert str(test_asset_id) in str(exc_info.value)


@pytest.mark.asyncio
async def test_create_asset_success(asset_service, mock_csharp_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    create_schema = AssetCreateSchema(
        name="Thermal Imaging Camera",
        category="Measurement Instrument",
        serial_number="TIC-8812"
    )
    mock_response = {
        "id": str(test_asset_id),
        "name": "Thermal Imaging Camera",
        "category": "Measurement Instrument",
        "serial_number": "TIC-8812",
        "status": "Active",
        "created_at": datetime.utcnow().isoformat()
    }
    mock_csharp_client.post = AsyncMock(return_value=mock_response)

    # Act
    result = await asset_service.create_asset(create_schema)

    # Assert
    assert isinstance(result, AssetResponseSchema)
    assert result.id == test_asset_id
    assert result.name == "Thermal Imaging Camera"
    mock_csharp_client.post.assert_called_once()


@pytest.mark.asyncio
async def test_get_asset_telemetry_overview_fallback(asset_service, mock_csharp_client):
    # Arrange
    test_asset_id = uuid.uuid4()
    mock_csharp_client.get = AsyncMock(return_value=None)

    # Act
    result = await asset_service.get_asset_telemetry_overview(test_asset_id)

    # Assert
    assert isinstance(result, AssetTelemetryOverviewSchema)
    assert result.asset_id == test_asset_id
    assert result.status == "Unknown"
    assert result.cpu_usage_pct == 0.0


# --- RepairService Unit Tests ---

@pytest.mark.asyncio
async def test_get_ticket_by_id_success(repair_service, mock_csharp_client):
    # Arrange
    test_ticket_id = uuid.uuid4()
    test_asset_id = uuid.uuid4()
    mock_response = {
        "id": str(test_ticket_id),
        "asset_id": str(test_asset_id),
        "title": "Laser diode replacement",
        "description": "Output optical power dropped below threshold",
        "status": "In Progress",
        "priority": "High",
        "created_at": datetime.utcnow().isoformat()
    }
    mock_csharp_client.get = AsyncMock(return_value=mock_response)

    # Act
    result = await repair_service.get_ticket_by_id(test_ticket_id)

    # Assert
    assert isinstance(result, RepairTicketResponseSchema)
    assert result.id == test_ticket_id
    assert result.priority == "High"
    mock_csharp_client.get.assert_called_once_with(f"/api/repairs/{test_ticket_id}")


@pytest.mark.asyncio
async def test_create_ticket_success(repair_service, mock_csharp_client):
    # Arrange
    test_ticket_id = uuid.uuid4()
    test_asset_id = uuid.uuid4()
    create_schema = RepairTicketCreateSchema(
        asset_id=test_asset_id,
        title="Power Supply Unit Failure",
        description="PSU not delivering steady 12V rail",
        priority="Critical"
    )
    mock_response = {
        "id": str(test_ticket_id),
        "asset_id": str(test_asset_id),
        "title": "Power Supply Unit Failure",
        "description": "PSU not delivering steady 12V rail",
        "status": "Pending",
        "priority": "Critical",
        "created_at": datetime.utcnow().isoformat()
    }
    mock_csharp_client.post = AsyncMock(return_value=mock_response)

    # Act
    result = await repair_service.create_ticket(create_schema)

    # Assert
    assert isinstance(result, RepairTicketResponseSchema)
    assert result.id == test_ticket_id
    assert result.status == "Pending"
    mock_csharp_client.post.assert_called_once()


@pytest.mark.asyncio
async def test_get_repair_metrics_summary_failure(repair_service, mock_csharp_client):
    # Arrange
    mock_csharp_client.get = AsyncMock(side_effect=Exception("Gateway Connection Timeout"))

    # Act & Assert
    with pytest.raises(CSharpServiceUnavailableException) as exc_info:
        await repair_service.get_repair_metrics_summary()

    assert "/api/repairs/metrics/summary" in str(exc_info.value)