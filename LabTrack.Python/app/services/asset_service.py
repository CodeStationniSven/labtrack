import logging
import uuid
from typing import List, Optional
from datetime import datetime

from app.clients.csharp_api import CSharpApiClient
from app.exceptions import AssetNotFoundException, CSharpServiceUnavailableException
from app.schemas.asset import (
    AssetCreateSchema,
    AssetResponseSchema,
    AssetUpdateSchema,
    AssetTelemetryOverviewSchema,
)

logger = logging.getLogger("LabTrack.Python.Services.AssetService")


class AssetService:
    """
    Service responsible for orchestrating laboratory asset management operations,
    synchronizing state with the C# core backend, and retrieving asset telemetry overviews.
    """

    def __init__(self, csharp_client: CSharpApiClient):
        self.csharp_client = csharp_client

    async def get_asset_by_id(self, asset_id: uuid.UUID) -> AssetResponseSchema:
        """
        Retrieves detailed information for a specific laboratory asset by ID
        from the primary C# backend service.
        """
        endpoint = f"/api/assets/{asset_id}"
        try:
            response_data = await self.csharp_client.get(endpoint)
            if not response_data:
                raise AssetNotFoundException(asset_id=str(asset_id))
            return AssetResponseSchema.model_validate(response_data)
        except AssetNotFoundException:
            raise
        except Exception as err:
            logger.error(f"Failed to fetch asset '{asset_id}' from C# backend: {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def list_assets(
        self, skip: int = 0, limit: int = 50, category: Optional[str] = None
    ) -> List[AssetResponseSchema]:
        """
        Retrieves a paginated list of laboratory assets from the primary storage service.
        """
        endpoint = "/api/assets"
        params = {"skip": skip, "limit": limit}
        if category:
            params["category"] = category

        try:
            response_data = await self.csharp_client.get(endpoint, params=params)
            items = response_data.get("items", []) if isinstance(response_data, dict) else response_data
            return [AssetResponseSchema.model_validate(item) for item in items]
        except Exception as err:
            logger.error(f"Failed to list assets from C# backend: {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def create_asset(self, payload: AssetCreateSchema) -> AssetResponseSchema:
        """
        Registers a new laboratory asset by delegating persistence to the primary C# API.
        """
        endpoint = "/api/assets"
        try:
            response_data = await self.csharp_client.post(
                endpoint, json_data=payload.model_dump(mode="json")
            )
            return AssetResponseSchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to create asset '{payload.name}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def update_asset(
        self, asset_id: uuid.UUID, payload: AssetUpdateSchema
    ) -> AssetResponseSchema:
        """
        Updates metadata and physical configuration for an existing laboratory asset.
        """
        endpoint = f"/api/assets/{asset_id}"
        update_data = payload.model_dump(exclude_unset=True, mode="json")
        try:
            response_data = await self.csharp_client.put(endpoint, json_data=update_data)
            if not response_data:
                raise AssetNotFoundException(asset_id=str(asset_id))
            return AssetResponseSchema.model_validate(response_data)
        except AssetNotFoundException:
            raise
        except Exception as err:
            logger.error(f"Failed to update asset '{asset_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def get_asset_telemetry_overview(
        self, asset_id: uuid.UUID
    ) -> AssetTelemetryOverviewSchema:
        """
        Retrieves real-time utilization, hardware status, and thermal metrics for a specific asset.
        """
        endpoint = f"/api/assets/{asset_id}/telemetry/latest"
        try:
            response_data = await self.csharp_client.get(endpoint)
            if not response_data:
                # Return default/fallback telemetry overview structure if no metric stream has logged yet
                return AssetTelemetryOverviewSchema(
                    asset_id=asset_id,
                    cpu_usage_pct=0.0,
                    memory_usage_pct=0.0,
                    gpu_usage_pct=None,
                    temperature_celsius=0.0,
                    status="Unknown",
                    last_reported=datetime.utcnow(),
                )
            return AssetTelemetryOverviewSchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to retrieve telemetry overview for asset '{asset_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))