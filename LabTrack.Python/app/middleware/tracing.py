import time
import uuid
import logging
from typing import Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

logger = logging.getLogger("labtrack.tracing")


class TracingMiddleware(BaseHTTPMiddleware):
    """
    Adds a trace ID to every request, exposes it in the response header,
    and logs basic timing info. Useful for correlating logs between the
    Python AI service and the C# backend.
    """

    TRACE_HEADER = "X-Trace-Id"

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        trace_id = request.headers.get(self.TRACE_HEADER) or str(uuid.uuid4())
        request.state.trace_id = trace_id

        start = time.perf_counter()
        try:
            response = await call_next(request)
        except Exception:
            elapsed_ms = (time.perf_counter() - start) * 1000
            logger.exception(
                "request_failed trace_id=%s method=%s path=%s elapsed_ms=%.2f",
                trace_id,
                request.method,
                request.url.path,
                elapsed_ms,
            )
            raise

        elapsed_ms = (time.perf_counter() - start) * 1000
        response.headers[self.TRACE_HEADER] = trace_id

        logger.info(
            "request trace_id=%s method=%s path=%s status=%s elapsed_ms=%.2f",
            trace_id,
            request.method,
            request.url.path,
            response.status_code,
            elapsed_ms,
        )

        return response