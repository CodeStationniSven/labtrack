# =============================================================================
# LabTrack AI Engine Dockerfile
# Python 3.11 Runtime Container for Diagnostic ML & Vector Embeddings
# =============================================================================

FROM python:3.11-slim AS base

# Set environment variables for optimized Python execution in containers
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PORT=8000

WORKDIR /app

# Install system dependencies required for scientific libraries and build tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy dependency specifications first to leverage Docker layer caching
COPY requirements.txt .

# Install Python requirements
RUN pip install --upgrade pip \
    && pip install -r requirements.txt

# Copy Python AI service source code
COPY . .

# Create non-root user for runtime execution
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 8000

# Healthcheck probe monitoring AI service readiness
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

ENTRYPOINT ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]