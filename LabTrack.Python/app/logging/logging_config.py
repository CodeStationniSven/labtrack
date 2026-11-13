import logging
import sys
import os
from typing import Dict, Any, Optional
from app.logging import JSONFormatter, setup_logging


def configure_app_logging(
    log_level: Optional[str] = None,
    service_name: str = "LabTrack.Python"
) -> logging.Logger:
    """
    Initializes and returns a configured logger instance with structured JSON formatting
    and environment-based log severity levels.
    """
    effective_level = log_level or os.getenv("LOG_LEVEL", "INFO")
    
    # Configure root logger with JSON stream handler
    setup_logging(log_level=effective_level)
    
    logger = logging.getLogger(service_name)
    logger.setLevel(getattr(logging, effective_level.upper(), logging.INFO))
    
    logger.info(
        f"Logging system initialized for service '{service_name}' at level '{effective_level}'."
    )
    
    return logger


def get_logger_with_context(name: str, context: Dict[str, Any]) -> logging.LoggerAdapter:
    """
    Returns a LoggerAdapter that automatically injects structured contextual key-value pairs
    (e.g., correlation_id, asset_id, user_id) into output log streams.
    """
    base_logger = logging.getLogger(name)
    return logging.LoggerAdapter(base_logger, extra={"extra_fields": context})