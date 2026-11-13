LabTrack Local Development & Environment Setup GuideThis document outlines the developer environment setup, prerequisites, local container orchestration, build workflows, and testing guidelines for contributing to the LabTrack platform.1. Prerequisites & Toolchain RequirementsDevelopers must ensure the following tooling is installed on their host machine before initializing the local stack:Docker & Docker Compose: Version 24.0+ with container buildx support..NET 8 SDK: ASP.NET Core 8.0 SDK (dotnet --version $\ge$ 8.0.200).Python 3.11+: For running and testing the AI diagnostic microservice locally.Node.js 20+ / pnpm: (Optional) For frontend dashboard development.Git: With support for long paths and LF line endings.2. Local Environment ConfigurationClone the repository and set up your local environment configuration files:Bashgit clone https://github.com/labtrack/labtrack.git
cd labtrack

# Copy environment templates
cp .env.example .env
Sample .env ConfigurationIni, TOMLPOSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=labtrack_db
POSTGRES_HOST=postgres
POSTGRES_PORT=5432

JWT_SECRET_KEY=super-secret-jwt-signing-key-at-least-32-bytes-long
JWT_ISSUER=https://api.labtrack.io
JWT_AUDIENCE=https://app.labtrack.io

AI_SERVICE_URL=http://ai:8000
ASPNETCORE_ENVIRONMENT=Development
3. Running the Stack with Docker ComposeYou can spin up the complete multi-container architecture (PostgreSQL with pgvector, .NET API, Python AI Engine, and Nginx Gateway) using Docker Compose:Bash# Build and start all services in detached mode
docker compose up --build -d

# View container logs across all services
docker compose logs -f

# Stop and tear down container instances and volumes
docker compose down -v
4. Local Development Without Containers (Hybrid Mode)For rapid iteration on backend C# code or Python AI scripts, you can run services natively while keeping supporting infrastructure in Docker.4.1 Start Supporting Infrastructure OnlyBashdocker compose up -d postgres
4.2 Run the .NET 8 Backend API NativelyBashcd src/LabTrack.Api
dotnet restore
dotnet run --environment Development
API Endpoint: http://localhost:5000 (Direct) / https://localhost:5001 (HTTPS)Swagger UI: Available at http://localhost:5000/swagger during local development.4.3 Run the Python AI Engine NativelyBashcd backend/python
python -m venv venv
source venv/bin/activate  # On Windows use: venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
5. Running Tests & Code Quality Checks5.1 C# Backend Unit & Integration TestsBashdotnet test LabTrack.sln --configuration Release --verbosity normal
5.2 Python AI Unit TestsBashcd backend/python
pytest tests/ -v --cov=app
5.3 Database Schema Integrity VerificationAfter initializing your local database, run the verification functions via psql or your database client:SQLSELECT * FROM public.fn_verify_labtrack_schema_deployment();
SELECT * FROM audit.fn_verify_forensic_ledger_integrity(