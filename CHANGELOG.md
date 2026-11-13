# Changelog

All notable changes to the **LabTrack** Enterprise Laboratory Asset Management Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-10-21

### Added
- **Multi-Tenant Database Architecture**: Established PostgreSQL 16 eight-schema model (`auth`, `inventory`, `scheduling`, `telemetry`, `maintenance`, `financial`, `ai`, `audit`) featuring Row-Level Security (RLS) policies.
- **Cryptographic Audit Ledger**: Implemented an append-only SHA-256 block-chained forensic ledger (`audit.forensic_ledger`) with sequential integrity verification functions (`audit.fn_verify_forensic_ledger_integrity()`).
- **REST API Suite**: Introduced .NET 8 Web API endpoints for:
  - Dual-token JWT authentication, RBAC policy enforcement, and TOTP/MFA flows (`/api/v1/auth`).
  - Physical asset inventory management and QR code check-in handshakes (`/api/v1/scheduling/qr`).
  - Maintenance work order lifecycle tracking and spare parts allocation (`/api/v1/maintenance`).
  - Workstation telemetry ingestion and switch port mapping (`/api/v1/telemetry`).
- **Real-Time Telemetry Hub**: Configured ASP.NET Core SignalR WebSocket hub (`/hubs/telemetry`) with Nginx reverse proxy upgrade rules and automatic reconnect client hooks.
- **Python AI RAG Engine**: Integrated FastAPI vector search service running `sentence-transformers/all-MiniLM-L6-v2` with `pgvector` storage (`ai.vector_store`) for diagnostic context retrieval.
- **PowerShell Operational Suite**:
  - `backup.ps1` & `restore.ps1`: Schema-aware backup routines with OpenSSL AES-256-CBC encryption.
  - `migrate.ps1` & `reset-database.ps1`: EF Core database migration and development reset tools.
  - `start-backend.ps1` & `stop-backend.ps1`: Orchestration scripts for Docker container stacks and native API processes.
  - `verify.ps1`: Automated schema object count and forensic ledger SHA-256 chain validation.
  - `deploy.ps1`, `health-check.ps1`, and `rollback.ps1`: Zero-downtime production deployment and disaster recovery utilities.
- **Testing & Quality Assurance Framework**:
  - Automated sanity test suites (`test-all.ps1`, `test-api.ps1`).
  - Multithreaded Optimistic Concurrency Control (OCC) conflict test script (`test-concurrency.ps1`).
  - Complete operational guides for Playwright E2E, xUnit integration, k6 load testing, and security auditing.
- **System Documentation**: Comprehensive guides covering deployment (`production.md`), disaster recovery (`backup-restore.md`), security architecture (`security-architecture.md`), and troubleshooting matrices (`common-errors.md`, `database-errors.md`, `signalr-errors.md`, `authentication-errors.md`).

### Security
- Mandated rate-limiting zones in Nginx for login and mutating API routes.
- Enforced zero-trust authorization context checks via PostgreSQL session variable injection (`SET LOCAL app.current_user_id`).
- Distributed idempotency key checks (`X-Idempotency-Key`) stored in `audit.idempotency_locks` to eliminate duplicate transaction risks.