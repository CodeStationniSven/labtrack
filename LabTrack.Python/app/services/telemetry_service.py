import logging
from datetime import datetime, timezone
from typing import Any, Optional

logger = logging.getLogger("labtrack.telemetry")


class TelemetryService:
    """Stores and aggregates PC telemetry received from workstation agents."""

    def __init__(self) -> None:
        # In-memory store keyed by workstation_id. Replace with DB/Redis later.
        self._store: dict[str, list[dict[str, Any]]] = {}

    async def record(self, workstation_id: str, payload: dict[str, Any]) -> dict[str, Any]:
        entry = {
            "workstation_id": workstation_id,
            "received_at": datetime.now(timezone.utc).isoformat(),
            **payload,
        }
        self._store.setdefault(workstation_id, []).append(entry)
        logger.info("telemetry_recorded workstation=%s", workstation_id)
        return entry

    async def get_latest(self, workstation_id: str) -> Optional[dict[str, Any]]:
        entries = self._store.get(workstation_id)
        if not entries:
            return None
        return entries[-1]

    async def get_history(
        self, workstation_id: str, limit: int = 100
    ) -> list[dict[str, Any]]:
        return self._store.get(workstation_id, [])[-limit:]

    async def get_average_metrics(self, workstation_id: str) -> dict[str, float]:
        entries = self._store.get(workstation_id, [])
        if not entries:
            return {"cpu_pct": 0.0, "memory_pct": 0.0, "samples": 0}

        cpu = [e.get("cpu_pct", 0.0) for e in entries if "cpu_pct" in e]
        mem = [e.get("memory_pct", 0.0) for e in entries if "memory_pct" in e]

        return {
            "cpu_pct": sum(cpu) / len(cpu) if cpu else 0.0,
            "memory_pct": sum(mem) / len(mem) if mem else 0.0,
            "samples": len(entries),
        }


# Module-level singleton — typical FastAPI pattern
telemetry_service = TelemetryService()