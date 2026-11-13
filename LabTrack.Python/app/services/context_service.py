import logging
import uuid
from typing import Dict, Any, Optional, List
from datetime import datetime

from app.clients.csharp_api import CSharpApiClient
from app.clients.llm_client import LLMClient
from app.exceptions import CSharpServiceUnavailableException, AssetNotFoundException
from app.schemas.common import CoreBaseSchema
from pydantic import Field

logger = logging.getLogger("LabTrack.Python.Services.ContextService")


class AssetContextSummarySchema(CoreBaseSchema):
    asset_id: uuid.UUID
    name: str
    category: str
    location: Optional[str] = None
    recent_telemetry_status: str = Field("Normal", description="Current status flag based on latest telemetry")
    unresolved_damage_reports_count: int = Field(0, description="Number of active damage reports")
    open_repair_tickets_count: int = Field(0, description="Number of pending/in-progress repair tickets")
    context_text: str = Field(..., description="Synthesized prompt context string for LLM injection")


class ContextService:
    """
    Service responsible for aggregating distributed asset metadata, recent telemetry, 
    active damage reports, and repair ticket history from the C# core backend into a 
    unified diagnostic context payload for AI model consumption.
    """

    def __init__(self, csharp_client: CSharpApiClient):
        self.csharp_client = csharp_client

    async def build_asset_context(self, asset_id: uuid.UUID) -> AssetContextSummarySchema:
        """
        Fetches asset profile, active damage reports, open repair tickets, and latest telemetry
        from the C# backend to synthesize a comprehensive diagnostic prompt context.
        """
        endpoint = f"/api/assets/{asset_id}/context"
        
        try:
            # Retrieve aggregated asset context payload from C# core gateway
            response_data = await self.csharp_client.get(endpoint)
            if not response_data:
                raise AssetNotFoundException(asset_id=str(asset_id))

            asset_name = response_data.get("name", "Unknown Asset")
            category = response_data.get("category", "General Hardware")
            location = response_data.get("location", "Unassigned")
            telemetry_status = response_data.get("recent_telemetry_status", "Normal")
            damage_count = response_data.get("unresolved_damage_reports_count", 0)
            repair_count = response_data.get("open_repair_tickets_count", 0)
            logs_summary = response_data.get("latest_logs_summary", "No recent telemetry warnings logged.")

            # Construct structured context block formatted for LLM system/user prompts
            context_text = (
                f"--- ASSET DIAGNOSTIC CONTEXT ---\n"
                f"Asset ID: {asset_id}\n"
                f"Name: {asset_name} | Category: {category} | Location: {location}\n"
                f"Current Telemetry Status: {telemetry_status}\n"
                f"Unresolved Damage Reports: {damage_count}\n"
                f"Active Repair Tickets: {repair_count}\n"
                f"Recent Telemetry / Incident Digest: {logs_summary}\n"
                f"--------------------------------"
            )

            return AssetContextSummarySchema(
                asset_id=asset_id,
                name=asset_name,
                category=category,
                location=location,
                recent_telemetry_status=telemetry_status,
                unresolved_damage_reports_count=damage_count,
                open_repair_tickets_count=repair_count,
                context_text=context_text,
            )

        except AssetNotFoundException:
            raise
        except Exception as err:
            logger.error(f"Failed to assemble asset context for ID '{asset_id}': {str(err)}")
            raise CSharpServiceUnavailableException(endpoint=endpoint, detail=str(err))