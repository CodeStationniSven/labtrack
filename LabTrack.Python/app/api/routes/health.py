from fastapi import APIRouter, status
from pydantic import BaseModel, Field
from datetime import datetime
import platform
import psutil
import os
import logging

from sqlalchemy import text
from sqlalchemy.ext.asyncio import create_async_engine

from app.db.vector_db import check_vector_db_health

logger = logging.getLogger("labtrack.health")

router = APIRouter()


DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql+asyncpg://postgres:sven292005@localhost:5432/labtrack_db"
)

_engine = create_async_engine(
    DATABASE_URL,
    echo=False,
    pool_size=5,
    max_overflow=10,
    pool_pre_ping=True,
)


async def _check_database() -> dict:
    try:
        async with _engine.connect() as conn:
            row = (await conn.execute(
                text("SELECT version(), current_database(), current_user")
            )).fetchone()

            vector_row = (await conn.execute(
                text("SELECT extversion FROM pg_extension WHERE extname = 'vector'")
            )).fetchone()

            return {
                "status": "connected",
                "database": row[1] if row else None,
                "user": row[2] if row else None,
                "server_version": (row[0] or "").split(",")[0] if row else None,
                "pgvector_version": vector_row[0] if vector_row else "not_installed",
            }
    except Exception as e:
        logger.error("Database health check failed: %s", e)
        return {"status": "disconnected", "error": str(e)}


class DatabaseHealth(BaseModel):
    status: str
    database: str | None = None
    user: str | None = None
    server_version: str | None = None
    pgvector_version: str | None = None
    error: str | None = None


class VectorDatabaseHealth(BaseModel):
    status: str
    database: str | None = None
    server_version: str | None = None
    pgvector_version: str | None = None
    error: str | None = None


class SystemHealthResponse(BaseModel):
    status: str
    service_name: str = "LabTrack.Python AI Engine"
    timestamp: datetime = Field(default_factory=datetime.utcnow)
    version: str = "1.0.0"
    system_metrics: dict
    database: DatabaseHealth
    vector_database: VectorDatabaseHealth


@router.get("/", response_model=SystemHealthResponse, status_code=status.HTTP_200_OK)
async def check_health():
    cpu_percent = psutil.cpu_percent(interval=None)
    memory_info = psutil.virtual_memory()

    metrics = {
        "os": platform.system(),
        "python_version": platform.python_version(),
        "cpu_usage_pct": cpu_percent,
        "memory_used_mb": round(memory_info.used / (1024 * 1024), 2),
        "memory_total_mb": round(memory_info.total / (1024 * 1024), 2),
        "memory_usage_pct": memory_info.percent,
    }

    db_health = await _check_database()
    vector_health = await check_vector_db_health()

    is_degraded = (
        cpu_percent > 90.0
        or memory_info.percent > 90.0
        or db_health["status"] != "connected"
    )
    status_label = "degraded" if is_degraded else "healthy"

    return SystemHealthResponse(
        status=status_label,
        system_metrics=metrics,
        database=DatabaseHealth(**db_health),
        vector_database=VectorDatabaseHealth(**vector_health),
    )