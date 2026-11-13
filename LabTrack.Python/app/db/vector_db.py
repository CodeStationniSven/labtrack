import logging
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncEngine, create_async_engine

from app.config import settings

logger = logging.getLogger("labtrack.vector_db")

_vector_engine: AsyncEngine | None = None


def init_vector_db() -> dict:
    global _vector_engine
    try:
        _vector_engine = create_async_engine(
            settings.database_url,
            echo=False,
            pool_pre_ping=True,
        )
        logger.info(
            "Vector DB engine created for main database '%s' @ %s:%s",
            settings.DB_NAME,
            settings.DB_HOST,
            settings.DB_PORT,
        )
        return {"status": "connected", "uri": settings.database_url}
    except Exception as e:
        logger.error(f"Vector DB init failed: {e}")
        return {"status": "disconnected", "error": str(e)}


async def ensure_vector_extension() -> bool:
    if _vector_engine is None:
        return False
    try:
        async with _vector_engine.begin() as conn:
            await conn.execute(text("CREATE EXTENSION IF NOT EXISTS vector"))
        return True
    except Exception as e:
        logger.warning(f"Could not ensure pgvector extension: {e}")
        return False


async def check_vector_db_health() -> dict:
    if _vector_engine is None:
        return {"status": "disconnected", "pgvector_version": "not_installed"}
    try:
        await ensure_vector_extension()
        async with _vector_engine.connect() as conn:
            row = (await conn.execute(
                text("SELECT version(), current_database()")
            )).fetchone()
            vec_row = (await conn.execute(
                text("SELECT extversion FROM pg_extension WHERE extname = 'vector'")
            )).fetchone()

            return {
                "status": "connected",
                "database": row[1] if row else None,
                "server_version": (row[0] or "").split(",")[0] if row else None,
                "pgvector_version": vec_row[0] if vec_row else "not_installed",
            }
    except Exception as e:
        logger.error(f"Vector DB health check failed: {e}")
        return {"status": "disconnected", "pgvector_version": "not_installed", "error": str(e)}


def get_vector_engine() -> AsyncEngine | None:
    return _vector_engine