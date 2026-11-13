LabTrack Containerization & Docker Architecture GuideThis document defines the containerization strategy, Docker Compose orchestration, volume persistence, and multi-service networking model for the LabTrack Laboratory Asset Management Platform.1. Containerization Architecture OverviewLabTrack is fully containerized using Docker and orchestrated via Docker Compose. The stack runs four core services isolated across internal bridge networks with strict volume persistence and healthcheck probes.                   +---------------------------------------+
                   |          Nginx Reverse Proxy          |
                   |      (Port 80/443 -> Gateway)         |
                   +---------------------------------------+
                       |                             |
     (HTTP /api/v1)    |                             |  (HTTP /api/v1/ai)
                       v                             v
+-----------------------------+               +-----------------------------+
|     .NET 8 Backend API      |               |      Python AI Engine       |
|    (Port 8080 / Internal)   |               |    (Port 8000 / Internal)   |
+-----------------------------+               +-----------------------------+
                       \                             /
                        \                           /
                         +-------------------------+
                                      |
                     (PostgreSQL Protocol / Port 5432)
                                      v
                   +---------------------------------------+
                   |        PostgreSQL 16 Database         |
                   |     (pgvector + Persistent Volume)    |
                   +---------------------------------------+
2. Docker Compose Specification (docker-compose.yml)The production-ready orchestration file managing all microservices, networks, and persistent data volumes:YAMLversion: '3.8'

networks:
  labtrack_internal:
    driver: bridge

volumes:
  postgres_data:
    driver: local
  ai_model_cache:
    driver: local

services:
  # =========================================================================
  # 1. PostgreSQL 16 Database with pgvector Extension
  # =========================================================================
  postgres:
    build:
      context: ./backend/docker/postgres
      dockerfile: Dockerfile
    image: labtrack/postgres:16-vector
    container_name: labtrack-postgres-1
    restart: unless-stopped
    environment:
      POSTGRES_DB: labtrack_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - labtrack_internal
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d labtrack_db"]
      interval: 10s
      timeout: 5s
      retries: 5

  # =========================================================================
  # 2. .NET 8 Clean Architecture Backend API
  # =========================================================================
  api:
    build:
      context: .
      dockerfile: backend/docker/api/Dockerfile
    image: labtrack/api:latest
    container_name: labtrack-api-1
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=labtrack_db;Username=postgres;Password=postgres
      - AiServiceUrl=http://ai:8000
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - labtrack_internal
    healthcheck:
      test: ["CMD", "wget", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      retries: 3

  # =========================================================================
  # 3. Python 3.11 AI Diagnostic & RAG Engine
  # =========================================================================
  ai:
    build:
      context: ./backend/python
      dockerfile: Dockerfile
    image: labtrack/ai-engine:latest
    container_name: labtrack-ai-1
    restart: unless-stopped
    environment:
      - DATABASE_URL=postgresql://postgres:postgres@postgres:5432/labtrack_db
      - PORT=8000
    ports:
      - "8000:8000"
    volumes:
      - ai_model_cache:/app/models
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - labtrack_internal
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 5s
      retries: 3

  # =========================================================================
  # 4. Nginx Reverse Proxy & API Gateway
  # =========================================================================
  nginx:
    image: nginx:alpine
    container_name: labtrack-nginx-1
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./backend/docker/nginx/default.conf:/etc/nginx/conf.d/default.conf:ro
      - ./backend/docker/nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - api
      - ai
    networks:
      - labtrack_internal
    healthcheck:
      test: ["CMD", "nginx", "-t"]
      interval: 30s
      timeout: 5s
      retries: 3
3. Persistent Volumes & Data SecurityTo prevent accidental data loss during container teardowns or image rebuilds, LabTrack utilizes managed Docker volumes:Volume NameTarget Path in ContainerPurposepostgres_data/var/lib/postgresql/dataStores relational tables across all 8 schemas, WAL logs, indices, and pgvector vector embeddings.ai_model_cache/app/modelsCaches transformer models and tokenizer weights locally to avoid redundant downloads on container restart.4. Operational Container CommandsCommon administrative commands for managing the LabTrack containerized stack:Bash# Build and start all services in the background
docker compose up --build -d

# Check live health status of all containers
docker compose ps

# View unified log output across all services
docker compose logs -f --tail=100

# Restart a single microservice without affecting others
docker compose restart api

# Stop all services and preserve persistent volumes
docker compose stop

# Tear down containers, networks, and remove persistent volumes (Warning: destructive)
docker compose down -v