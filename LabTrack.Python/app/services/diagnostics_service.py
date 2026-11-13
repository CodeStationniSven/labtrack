import logging
from typing import Any, Optional

from app.services.telemetry_service import telemetry_service

logger = logging.getLogger("labtrack.diagnostics")


class DiagnosticsService:
    """
    Analyzes telemetry data and returns a health verdict + recommended actions.
    Used by the AI assistant and the health-check endpoint.
    """

    CPU_WARN = 80.0
    CPU_CRITICAL = 90.0
    MEM_WARN = 80.0
    MEM_CRITICAL = 90.0

    async def diagnose_workstation(self, workstation_id: str) -> dict[str, Any]:
        latest = await telemetry_service.get_latest(workstation_id)
        if latest is None:
            return {
                "workstation_id": workstation_id,
                "status": "unknown",
                "reason": "No telemetry data available",
                "recommendations": [],
            }

        cpu = float(latest.get("cpu_pct", 0.0))
        mem = float(latest.get("memory_pct", 0.0))

        status, recommendations = self._evaluate(cpu, mem)

        return {
            "workstation_id": workstation_id,
            "status": status,
            "cpu_pct": cpu,
            "memory_pct": mem,
            "recommendations": recommendations,
            "checked_at": latest.get("received_at"),
        }

    def _evaluate(self, cpu: float, mem: float) -> tuple[str, list[str]]:
        recommendations: list[str] = []
        status = "healthy"

        if cpu >= self.CPU_CRITICAL:
            status = "critical"
            recommendations.append(
                "CPU utilization above 90%. Investigate runaway processes or schedule maintenance."
            )
        elif cpu >= self.CPU_WARN:
            status = "degraded"
            recommendations.append(
                "CPU utilization above 80%. Monitor for sustained load."
            )

        if mem >= self.MEM_CRITICAL:
            status = "critical"
            recommendations.append(
                "Memory usage above 90%. Consider restart or memory upgrade."
            )
        elif mem >= self.MEM_WARN:
            if status == "healthy":
                status = "degraded"
            recommendations.append(
                "Memory usage above 80%. Close unused applications."
            )

        return status, recommendations

    async def diagnose_workstation_summary(
        self, workstation_id: str
    ) -> Optional[dict[str, Any]]:
        avg = await telemetry_service.get_average_metrics(workstation_id)
        if avg["samples"] == 0:
            return None
        status, recommendations = self._evaluate(avg["cpu_pct"], avg["memory_pct"])
        return {
            "workstation_id": workstation_id,
            "status": status,
            "average_cpu_pct": avg["cpu_pct"],
            "average_memory_pct": avg["memory_pct"],
            "samples": avg["samples"],
            "recommendations": recommendations,
        }


diagnostics_service = DiagnosticsService()