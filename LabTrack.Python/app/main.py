import logging
from datetime import datetime
from contextlib import asynccontextmanager
from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.config import settings
from app.exceptions import (
    AssetNotFoundException,
    CSharpServiceUnavailableException,
    LLMInferenceException,
    LabTrackException,
)
from app.middleware.correlation import CorrelationIdMiddleware
from app.middleware.logging import RequestLoggingMiddleware
from app.schemas.common import APIResponseEnvelope
from app.api.routes.health import _check_database
from app.api.routes import api_router
from app.db.vector_db import init_vector_db, check_vector_db_health

logging.basicConfig(
    level=logging.INFO if not settings.DEBUG else logging.DEBUG,
    format="%(message)s",
)
logging.getLogger("pgserver").setLevel(logging.WARNING)
logging.getLogger("sqlalchemy.engine").setLevel(logging.WARNING)
logger = logging.getLogger("LabTrack.Python.Main")


@asynccontextmanager
async def lifespan(app: FastAPI):
    try:
        db_health = await _check_database()
        vector_result = init_vector_db()
        vector_health = (
            await check_vector_db_health()
            if vector_result["status"] == "connected"
            else {}
        )

        main_status = "CONNECTED" if db_health["status"] == "connected" else "FAILED"
        vec_status = "CONNECTED" if vector_result["status"] == "connected" else "FAILED"
        main_db = db_health.get("database", "Ã¢â‚¬â€")
        main_user = db_health.get("user", "Ã¢â‚¬â€")
        vec_version = vector_health.get("pgvector_version", "Ã¢â‚¬â€")
        ready = "READY" if main_status == "CONNECTED" and vec_status == "CONNECTED" else "DEGRADED"
        now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        w = 56
        line = "+" + "-" * w + "+"
        logger.info(line)
        logger.info(f"| {'LabTrack.Python Ã¢â‚¬â€ AI ENGINE':<{w}} |")
        logger.info(line)
        logger.info(f"| {f'Date        : {now}':<{w}} |")
        logger.info(f"| {f'App         : {settings.APP_NAME}':<{w}} |")
        logger.info(f"| {f'Environment : {settings.ENVIRONMENT}':<{w}} |")
        logger.info(f"| {f'MAIN DB     : {main_status} Ã¢â‚¬â€ {main_db} @ {main_user}':<{w}} |")
        logger.info(f"| {f'VECTOR DB   : {vec_status} Ã¢â‚¬â€ pgvector {vec_version}':<{w}} |")
        logger.info(f"| {f'Status      : {ready}':<{w}} |")
        logger.info(line)
    except Exception as exc:
        logger.exception(f"Startup check failed: {exc}")

    yield

    logger.info(f"Shutting down {settings.APP_NAME}...")


app = FastAPI(
    title=settings.APP_NAME,
    description=(
        "AI-driven microservice for the LabTrack platform, providing automated "
        "diagnostics, hardware classification, cost estimation, and EHS safety evaluation."
    ),
    version="1.0.0",
    docs_url="/docs" if settings.DEBUG else None,
    redoc_url="/redoc" if settings.DEBUG else None,
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.add_middleware(CorrelationIdMiddleware)
app.add_middleware(RequestLoggingMiddleware)

app.include_router(api_router, prefix="/api/v1")


@app.exception_handler(LabTrackException)
async def handle_labtrack_exception(request: Request, exc: LabTrackException):
    correlation_id = getattr(request.state, "correlation_id", None)

    status_code = status.HTTP_500_INTERNAL_SERVER_ERROR
    if isinstance(exc, AssetNotFoundException):
        status_code = status.HTTP_404_NOT_FOUND
    elif isinstance(exc, CSharpServiceUnavailableException):
        status_code = status.HTTP_503_SERVICE_UNAVAILABLE
    elif isinstance(exc, LLMInferenceException):
        status_code = status.HTTP_502_BAD_GATEWAY

    response_payload = APIResponseEnvelope(
        success=False,
        message=exc.message,
        errors=[exc.message],
        correlation_id=correlation_id,
    )
    return JSONResponse(status_code=status_code, content=response_payload.model_dump(mode="json"))


@app.exception_handler(RequestValidationError)
async def handle_validation_exception(request: Request, exc: RequestValidationError):
    correlation_id = getattr(request.state, "correlation_id", None)
    error_messages = [f"{err['loc']}: {err['msg']}" for err in exc.errors()]

    response_payload = APIResponseEnvelope(
        success=False,
        message="Request payload validation failed.",
        errors=error_messages,
        correlation_id=correlation_id,
    )
    return JSONResponse(
        status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
        content=response_payload.model_dump(mode="json"),
    )


@app.exception_handler(Exception)
async def handle_unhandled_exception(request: Request, exc: Exception):
    correlation_id = getattr(request.state, "correlation_id", None)
    logger.exception(f"Unhandled exception encountered: {str(exc)}")

    response_payload = APIResponseEnvelope(
        success=False,
        message="An unexpected internal server error occurred.",
        errors=[str(exc)] if settings.DEBUG else ["Internal server error."],
        correlation_id=correlation_id,
    )
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content=response_payload.model_dump(mode="json"),
    )


@app.get("/health", tags=["Infrastructure"], response_model=APIResponseEnvelope[dict])
async def health_check():
    db_health = await _check_database()
    vector_health = await check_vector_db_health()
    is_healthy = db_health["status"] == "connected"

    return APIResponseEnvelope(
        success=is_healthy,
        message=(
            "Service is healthy and operational."
            if is_healthy
            else "Service degraded: database disconnected."
        ),
        data={
            "app_name": settings.APP_NAME,
            "environment": settings.ENVIRONMENT,
            "status": "UP" if is_healthy else "DEGRADED",
            "database": db_health,
            "vector_database": vector_health,
        },
    )


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=settings.PORT,
        reload=settings.DEBUG,
    )