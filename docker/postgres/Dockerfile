# =============================================================================
# LabTrack PostgreSQL Database Engine Dockerfile
# Custom PostgreSQL 16 image bundled with pgvector & pgcrypto extensions
# =============================================================================

FROM postgres:16-bookworm

# Environment variable defaults
ENV POSTGRES_DB=labtrack_db \
    POSTGRES_USER=postgres \
    POSTGRES_PASSWORD=postgres

# Install compilation toolchain & pgvector extension source dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        build-essential \
        git \
        postgresql-server-dev-16 \
        ca-certificates \
    # Clone and install pgvector extension
    && git clone --branch v0.7.0 https://github.com/pgvector/pgvector.git /tmp/pgvector \
    && cd /tmp/pgvector \
    && make \
    && make install \
    # Cleanup build dependencies to reduce image footprint
    && rm -rf /tmp/pgvector \
    && apt-get remove -y build-essential git postgresql-server-dev-16 \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/*

# Copy startup initialization SQL scripts into entrypoint directory
# Scripts executed in alphabetical sequence on container boot
COPY database_contract.sql /docker-entrypoint-initdb.d/01_database_contract.sql
COPY reference_data.sql /docker-entrypoint-initdb.d/02_reference_data.sql

# Copy views and seeds into initialization buffer
COPY views/ /docker-entrypoint-initdb.d/views/
COPY seeds/ /docker-entrypoint-initdb.d/seeds/

# Healthcheck probe monitoring PostgreSQL service readiness
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=5 \
    CMD pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" || exit 1

EXPOSE 5432