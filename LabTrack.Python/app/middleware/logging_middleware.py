import time
import uuid
import logging
from fastapi import Request
from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint
from starlette.responses import Response

logger = logging.getLogger("LabTrack.Python.Middleware")

class LoggingAndTracingMiddleware(BaseHTTPMiddleware):
    """
    ASGI Middleware that assigns a correlation ID to incoming requests,
    logs duration and HTTP metadata, and passes the correlation header downstream.
    """

    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        start_time = time.perf_counter()

        # Extract existing correlation ID from C# Gateway request or generate a new UUID
        correlation_id = request.headers.get("X-Correlation-ID", str(uuid.uuid4()))
        request.state.correlation_id = correlation_id

        # Proceed with processing request downstream
        try:
            response = await call_next(request)
        except Exception as exc:
            duration_ms = (time.perf_counter() - start_time) * 1000.0
            logger.error(
                f"Request Failed: {request.method} {request.url.path} | "
                f"CorrelationId: {correlation_id} | Duration: {duration_ms:.2f}ms | Error: {str(exc)}",
                exc_info=True
            )
            raise exc

        duration_ms = (time.perf_counter() - start_time) * 1000.0

        # Inject correlation header into HTTP response
        response.headers["X-Correlation-ID"] = correlation_id
        response.headers["X-Process-Time-Ms"] = f"{duration_ms:.2f}"

        logger.info(
            f"Request Completed: {request.method} {request.url.path} | "
            f"Status: {response.status_code} | CorrelationId: {correlation_id} | Duration: {duration_ms:.2f}ms"
        )

        return response