Markdown
# LabTrack System Architecture & Technology Overview

System architecture documentation for **LabTrack**, an enterprise-grade Laboratory Asset Management, Telemetry Monitoring, Financial Oversight, and AI-Assisted Maintenance Platform.

---

## 1. High-Level Architecture Topology

LabTrack follows a modular Clean Architecture pattern, containerized using Docker and orchestrated behind an Nginx reverse proxy gateway.

               +---------------------------------------+
               |          Clients / Browsers            |
               +---------------------------------------+
                                   |
                                   v
               +---------------------------------------+
               |          Nginx Gateway (80/443)        |
               |   (SSL, Rate Limiting, CORS, Headers) |
               +---------------------------------------+
                                   |
        +--------------------------+--------------------------+
        |                                                     |
        v                                                     v
+-----------------------+                             +-----------------------+
|  .NET 8 Backend API   |                             |   Python AI Engine    |
|   (Core Logic, Auth,  | <--- Internal REST / HTTP - |  (FastAPI, pgvector,  |
|  SignalR Telemetry)   |                             |   RAG Diagnostics)    |
+-----------------------+                             +-----------------------+
|                                                     |
+--------------------------+--------------------------+
|
v
+---------------------------------------+
|        PostgreSQL 16 Database         |
| (8 Schemas, pgvector, SHA-256 Audit)  |
+---------------------------------------+


---

## 2. Component Breakdown

### 2.1 API Gateway (Nginx)
* **Image:** `nginx:alpine`
* **Port Mapping:** `80:80`
* **Responsibilities:**
  * Request routing (`/api/v1/...` $\rightarrow$ .NET 8 API, `/api/v1/ai/...` $\rightarrow$ Python AI Engine).
  * WebSocket upgrade proxying (`/hubs/telemetry` $\rightarrow$ SignalR hub).
  * Rate limiting (`auth_limit`: 5 req/s, `api_limit`: 20 req/s).
  * Global security headers (`X-Frame-Options`, `X-Content-Type-Options`, CSP).

### 2.2 Core Backend Service (.NET 8 Clean Architecture)
* **Runtime:** ASP.NET Core 8.0
* **Internal Port:** `8080`
* **Architectural Layers:**
  * `LabTrack.Api`: Controllers, Middleware, SignalR Hubs, Swagger/OpenAPI.
  * `LabTrack.Application`: CQRS Handlers, DTOs, FluentValidation, Business Workflows.
  * `LabTrack.Domain`: Entities, Aggregates, Enumerations, Domain Events.
  * `LabTrack.Infrastructure`: EF Core, Dapper Connection Factories, JWT Auth, Audit Logger, Network Scanners.

### 2.3 AI Diagnostic Engine (Python 3.11 / FastAPI)
* **Runtime:** Python 3.11 Slim
* **Internal Port:** `8000`
* **Responsibilities:**
  * Knowledge base vector embeddings generation and pgvector similarity queries.
  * Retrieval-Augmented Generation (RAG) assistant threads for equipment repair manuals.
  * Telemetry load pattern anomaly explanation.

### 2.4 Data Layer (PostgreSQL 16)
* **Engine:** PostgreSQL 16
* **Internal Port:** `5432`
* **Extensions:** `uuid-ossp`, `pgcrypto`, `citext`, `pgvector`
* **Isolation Schemas:** `auth`, `inventory`, `scheduling`, `telemetry`, `maintenance`, `financial`, `ai`, `audit`

---

## 3. Key Architectural Patterns & Guarantees

1. **Row Level Security (RLS):** All database tables strictly enforce RLS policies parameterized by `app.current_user_id` set per HTTP transaction.
2. **Cryptographic Forensic Auditing:** The `audit.forensic_ledger` table enforces continuous SHA-256 hash chaining ($H_n = \text{SHA256}(H_{n-1} \parallel \text{Payload})$) to guarantee zero-tampering records.
3. **Optimistic Concurrency Control (OCC):** Mutable entities implement integer `version` properties auto-incremented by database triggers to prevent write race conditions.
4. **Idempotent API Operations:** Non-idempotent state