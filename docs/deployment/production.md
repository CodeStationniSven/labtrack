LabTrack Production Deployment & Hardening Guide
This document defines the enterprise production deployment standards, server hardening guidelines, TLS termination procedures, secrets management protocols, and scaling strategies for deploying LabTrack in live institutional environments.

1. Production Infrastructure & Server Prerequisites
Production deployments require dedicated virtual private servers or bare-metal nodes running hardened Linux distributions (e.g., Ubuntu 22.04 LTS / Debian 12 Bookworm).

Minimum Compute Specifications:

CPU: 8 Dedicated vCPUs

RAM: 32 GB ECC RAM (to accommodate PostgreSQL buffer pools and Python embedding memory)

Storage: 500 GB NVMe SSD with RAID-1 mirroring (for database volumes and log persistence)

Firewall Configuration (UFW):

Bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp proto tcp comment 'SSH Admin Access'
sudo ufw allow 80/tcp proto tcp comment 'HTTP Traffic'
sudo ufw allow 443/tcp proto tcp comment 'HTTPS Traffic'
sudo ufw enable
2. Environment Configuration & Secret Management
Production secrets (database credentials, JWT HMAC keys, encryption salts) must never be committed to source control. They must be injected via secure environment files (.env.production) or enterprise secret stores (e.g., HashiCorp Vault, AWS Secrets Manager).

Production .env.production Template
Ini, TOML
# =============================================================================
# LabTrack Production Environment Configuration
# =============================================================================

ASPNETCORE_ENVIRONMENT=Production
PORT=8080

# PostgreSQL Production Credentials
POSTGRES_DB=labtrack_db
POSTGRES_USER=labtrack_admin
POSTGRES_PASSWORD=__SECURE_GENERATED_DB_PASSWORD__
POSTGRES_HOST=postgres
POSTGRES_PORT=5432

# Cryptographic JWT Signing Key (Minimum 256 bits / 32 bytes)
JWT_SECRET_KEY=__SECURE_RANDOM_JWT_SECRET_KEY_MIN_32_BYTES__
JWT_ISSUER=https://api.labtrack.io
JWT_AUDIENCE=https://app.labtrack.io

# AI Engine Integration
AI_SERVICE_URL=http://ai:8000
BACKUP_BACKEND_ENCRYPTION_KEY=__SECURE_BACKUP_AES_PASSPHRASE__
3. SSL/TLS Termination & Nginx Security Hardening
In production, Nginx acts as the secure TLS termination point, enforcing modern cipher suites and redirecting all unencrypted HTTP traffic to HTTPS.

Production Nginx Server Block (backend/docker/nginx/production.conf)
Nginx
# Rate limiting zones
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=20r/s;
limit_req_zone $binary_remote_addr zone=auth_limit:10m rate=5r/s;

upstream labtrack_api {
    server api:8080 max_fails=3 fail_timeout=10s;
    keepalive 32;
}

upstream labtrack_ai {
    server ai:8000 max_fails=3 fail_timeout=10s;
    keepalive 16;
}

# HTTP to HTTPS Redirection
server {
    listen 80;
    listen [::]:80;
    server_name api.labtrack.io app.labtrack.io;
    return 301 https://$host$request_uri;
}

# HTTPS Secure Production Server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name api.labtrack.io;

    # SSL Certificates (Let's Encrypt / Enterprise PKI)
    ssl_certificate /etc/letsencrypt/live/api.labtrack.io/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.labtrack.io/privkey.pem;

    # Modern SSL Protocols & Ciphers
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1d;

    # HSTS Header
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;

    # Global Client Security Headers
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';" always;

    client_max_body_size 50M;

    # Healthcheck bypass
    location /health {
        proxy_pass http://labtrack_api/health;
        access_log off;
    }

    # Auth Rate Limiting
    location /api/v1/auth/ {
        limit_req zone=auth_limit burst=10 nodelay;
        proxy_pass http://labtrack_api;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Main Core API Proxy
    location /api/ {
        limit_req zone=api_limit burst=30 nodelay;
        proxy_pass http://labtrack_api;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # AI Diagnostic Service Proxy
    location /api/v1/ai/ {
        proxy_pass http://labtrack_ai/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 120s;
    }

    # Live Telemetry WebSocket Hubs
    location /hubs/ {
        proxy_pass http://labtrack_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
    }
}
4. Database Production Hardening & Tuning
PostgreSQL 16 must be tuned for high concurrent transaction throughput and strict security enforcement.

Key postgresql.conf Production Parameters
Ini, TOML
# Resource Usage
shared_buffers = 8GB                  # 25% of total system RAM
effective_cache_size = 24GB           # 75% of total system RAM
work_mem = 64MB                       # Dedicated memory for sort/hash ops
maintenance_work_mem = 2GB            # Memory for VACUUM, CREATE INDEX

# Write-Ahead Log (WAL)
wal_level = replica
checkpoint_completion_target = 0.9    # Spread checkpoint I/O load
max_wal_size = 16GB
min_wal_size = 2GB

# Security & Connections
max_connections = 200
ssl = on
ssl_cert_file = '/var/lib/postgresql/certs/server.crt'
ssl_key_file = '/var/lib/postgresql/certs/server.key'
5. Observability, Logging, & Health Probes
Structured JSON Logging: Nginx and backend services log incoming requests and exceptions in structured JSON format, ingested by centralized log collectors (e.g., Fluentbit / Grafana Loki).

Automated Health Probes: Docker container healthchecks run every 30 seconds against /health endpoints on both the .NET API and Python AI service. Unhealthy containers are automatically recycled by the container runtime.

6. Zero-Downtime Deployment & Maintenance
Deployments follow a rolling update strategy to ensure continuous availability for lab equipment monitoring and active researcher sessions:

Bash
# Pull latest container images without disrupting running services
docker compose pull

# Perform rolling container replacement
docker compose up --detach --no-deps --build api ai nginx

# Run schema and ledger integrity verification after deployment
docker exec -t labtrack-postgres-1 psql -U postgres -d labtrack_db -c "SELECT * FROM public.fn_verify_labtrack_schema_deployment();"
docker exec -t labtrack-postgres-1 psql -U postgres -d l