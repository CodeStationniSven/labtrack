import time
import logging
from typing import Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

logger = logging.getLogger("labtrack.request")


class LoggingMiddleware(BaseHTTPMiddleware):
    """
    Structured request/response logging with correlation ID and latency.
    Complements CorrelationMiddleware (reads request.state.correlation_id).
    """

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        correlation_id = getattr(request.state, "correlation_id", "-")
        start = time.perf_counter()

        try:
            response = await call_next(request)
        except Exception:
            elapsed_ms = (time.perf_counter() - start) * 1000
            logger.exception(
                "request_failed correlation_id=%s method=%s path=%s elapsed_ms=%.2f",
                correlation_id,
                request.method,
                request.url.path,
                elapsed_ms,
            )
            raise

        elapsed_ms = (time.perf_counter() - start) * 1000
        logger.info(
            "request correlation_id=%s method=%s path=%s status=%s elapsed_ms=%.2f",
            correlation_id,
            request.method,
            request.url.path,
            response.status_code,
            elapsed_ms,
        )

        return response


# Backward-compatible alias used by app.main
RequestLoggingMiddleware = LoggingMiddleware