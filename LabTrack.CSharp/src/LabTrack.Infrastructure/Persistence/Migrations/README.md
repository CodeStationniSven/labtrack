Markdown
# Database Migrations Management

This folder contains Entity Framework Core migrations for the `LabTrack` application.

## Prerequisites

Ensure you have the EF Core CLI tool installed globally:

```bash
dotnet tool install --global dotnet-ef
Verify installation:

Bash
dotnet-ef --version
Common EF Core Commands
All commands below should be executed from the solution root folder or directly inside the LabTrack.Infrastructure project directory.

1. Create a New Migration
Generate a new migration whenever you make changes to Domain entities or EF Core Fluent API configurations (IEntityTypeConfiguration<T>):

Bash
dotnet ef migrations add <MigrationName> \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj \
  --output-dir Persistence/Migrations
Example:

Bash
dotnet ef migrations add InitialCreate \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj \
  --output-dir Persistence/Migrations
2. Apply Migrations to Database
Apply all pending migrations to target SQL Server database configured in appsettings.json (DefaultConnection):

Bash
dotnet ef database update \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj
Roll back or migrate to a specific target migration:

Bash
dotnet ef database update <TargetMigrationName> \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj
3. Remove Last Migration
Remove the last unapplied migration if it has not been applied to the database yet:

Bash
dotnet ef migrations remove \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj
Note: If the migration was already applied to the local or remote database, revert it first using dotnet ef database update <PreviousMigrationName> before running migrations remove.

4. Generate Idempotent SQL Scripts
Generate a raw SQL deployment script for CI/CD pipelines or DBA code reviews:

Bash
dotnet ef migrations script \
  --idempotent \
  --project src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj \
  --startup-project src/LabTrack.Api/LabTrack.Api.csproj \
  --output ./script.sql
Best Practices & Guidelines
Naming Conventions: Use descriptive PascalCase names for migrations (e.g., AddUserAuthenticationFields, CreateFinancialExpensesTable).

Review Auto-Generated Code: Always inspect generated .cs migration files before running database update.

Data Integrity: Avoid dropping columns or tables directly in production migrations without pre-migration data preservation strategies.

Environment Configuration: Ensure connection strings in appsettings.Development.json vs. appsettings.Production.json are appropriately secured and isolated.