import logging
import uuid
from typing import List, Optional
from datetime import datetime

from app.clients.csharp_api import CSharpApiClient
from app.exceptions import CSharpServiceUnavailableException
from app.schemas.common import PaginatedResponse
from app.schemas.repair import (
    RepairTicketCreateSchema,
    RepairTicketResponseSchema,
    RepairTicketUpdateSchema,
    RepairMetricsSummarySchema,
)

logger = logging.getLogger("LabTrack.Python.Services.RepairService")


class RepairService:
    """
    Service responsible for orchestrating maintenance and repair ticket operations,
    synchronizing state updates with the core C# backend, and retrieving repair metrics.
    """

    def __init__(self, csharp_client: CSharpApiClient):
        self.csharp_client = csharp_client

    async def get_ticket_by_id(self, ticket_id: uuid.UUID) -> RepairTicketResponseSchema:
        """
        Retrieves a specific repair ticket by ID from the C# core service.
        """
        endpoint = f"/api/repairs/{ticket_id}"
        try:
            response_data = await self.csharp_client.get(endpoint)
            return RepairTicketResponseSchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to fetch repair ticket '{ticket_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def list_tickets(
        self,
        asset_id: Optional[uuid.UUID] = None,
        status: Optional[str] = None,
        priority: Optional[str] = None,
        skip: int = 0,
        limit: int = 50,
    ) -> List[RepairTicketResponseSchema]:
        """
        Retrieves repair tickets matching filter criteria from the primary persistence store.
        """
        endpoint = "/api/repairs"
        params = {"skip": skip, "limit": limit}
        if asset_id:
            params["asset_id"] = str(asset_id)
        if status:
            params["status"] = status
        if priority:
            params["priority"] = priority

        try:
            response_data = await self.csharp_client.get(endpoint, params=params)
            items = response_data.get("items", []) if isinstance(response_data, dict) else response_data
            return [RepairTicketResponseSchema.model_validate(item) for item in items]
        except Exception as err:
            logger.error(f"Failed to list repair tickets: {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def create_ticket(self, payload: RepairTicketCreateSchema) -> RepairTicketResponseSchema:
        """
        Logs a new maintenance or repair ticket by delegating persistence to the primary C# API.
        """
        endpoint = "/api/repairs"
        try:
            response_data = await self.csharp_client.post(
                endpoint, json_data=payload.model_dump(mode="json")
            )
            return RepairTicketResponseSchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to create repair ticket for asset '{payload.asset_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def update_ticket(
        self, ticket_id: uuid.UUID, payload: RepairTicketUpdateSchema
    ) -> RepairTicketResponseSchema:
        """
        Updates the status, technician assignment, or details for an existing repair ticket.
        """
        endpoint = f"/api/repairs/{ticket_id}"
        update_data = payload.model_dump(exclude_unset=True, mode="json")
        try:
            response_data = await self.csharp_client.put(endpoint, json_data=update_data)
            return RepairTicketResponseSchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to update repair ticket '{ticket_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))

    async def get_repair_metrics_summary(self) -> RepairMetricsSummarySchema:
        """
        Retrieves global maintenance health metrics, open ticket counts, and mean resolution times.
        """
        endpoint = "/api/repairs/metrics/summary"
        try:
            response_data = await self.csharp_client.get(endpoint)
            return RepairMetricsSummarySchema.model_validate(response_data)
        except Exception as err:
            logger.error(f"Failed to retrieve repair metrics summary: {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))