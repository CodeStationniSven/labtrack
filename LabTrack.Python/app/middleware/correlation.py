import uuid
import logging
from typing import Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware

logger = logging.getLogger("labtrack.correlation")


class CorrelationMiddleware(BaseHTTPMiddleware):
    """
    Ensures every request has a correlation ID (X-Correlation-Id).
    Reuses one from the incoming request if present, otherwise generates it.
    Propagates the ID in the response and stores it on request.state
    so downstream handlers/loggers can use it.
    """

    CORRELATION_HEADER = "X-Correlation-Id"

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        correlation_id = request.headers.get(self.CORRELATION_HEADER) or str(uuid.uuid4())
        request.state.correlation_id = correlation_id

        response = await call_next(request)
        response.headers[self.CORRELATION_HEADER] = correlation_id

        return response


# Backward-compatible alias used by app.main
CorrelationIdMiddleware = CorrelationMiddleware