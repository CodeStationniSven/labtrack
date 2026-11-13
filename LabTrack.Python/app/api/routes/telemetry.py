from fastapi import APIRouter
from pydantic import BaseModel

router = APIRouter()

class TelemetryData(BaseModel):
    workstation_id: str
    cpu_pct: float
    memory_pct: float

@router.post("/")
async def receive_telemetry(data: TelemetryData):
    return {"status": "received"}