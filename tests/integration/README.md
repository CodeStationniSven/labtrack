LabTrack Integration Testing Guide
This document provides comprehensive instructions for configuring, executing, and maintaining the backend integration test suite for the LabTrack enterprise laboratory management platform.

1. Overview & Test Scope
The LabTrack integration test suite validates the interaction between the .NET 8 Clean Architecture layers (Api, Application, Infrastructure), custom middleware (authentication, idempotency, RLS context injection), and the PostgreSQL 16 database instance.

                  +-----------------------------------+
                  |     xUnit Integration Test        |
                  |     (WebApplicationFactory)       |
                  +-----------------------------------+
                                    |
                                    v  (In-Memory / Testcontainers)
                  +-----------------------------------+
                  |       .NET 8 Backend API          |
                  +-----------------------------------+
                                    |
                                    v
                  +-----------------------------------+
                  |     PostgreSQL 16 Database        |
                  |  (Isolated Test Schema / RLS)     |
                  +-----------------------------------+
Key Integration Test Categories
API Controller & Endpoint Integration: Validates HTTP status codes, routing, request validation via FluentValidation, and standardized JSON error response envelopes.

Database Repository & RLS Context: Verifies that Entity Framework Core and Dapper queries correctly respect PostgreSQL Row-Level Security (RLS) policies when app.current_user_id session variables are injected.

Concurrency & OCC Validation: Tests Optimistic Concurrency Control (OCC) version conflicts during concurrent record updates across domain aggregates.

Middleware & Security Verification: Ensures JWT bearer authentication filters, rate-limiting zones, and distributed idempotency locking (audit.idempotency_locks) operate as specified.

2. Prerequisites & Test Environment
Integration tests utilize Testcontainers for .NET or a dedicated local PostgreSQL test instance to ensure database-level fidelity (including triggers, PL/pgSQL verification functions, and extensions like pgvector).

.NET 8 SDK:

Bash
dotnet --version  # Requires .NET 8.0+
Docker Engine: Required for spinning up ephemeral PostgreSQL containers during test execution if using Testcontainers.

3. Test Suite Directory Structure
backend/tests/integration/
├── Fixtures/
│   ├── LabTrackWebApplicationFactory.cs   # Custom WebApplicationFactory setup
│   └── DatabaseTestFixture.cs             # Container and seed data management
├── Helpers/
│   ├── AuthTokenGenerator.cs              # Test JWT token generation for RBAC roles
│   └── TestDataSeeder.cs                  # Baseline schema and entity seeders
├── Api/
│   ├── AuthControllerIntegrationTests.cs   # Login, refresh, and MFA integration tests
│   └── AssetControllerIntegrationTests.cs  # Inventory, QR check-in, and reservation tests
├── Repositories/
│   ├── UserRepositoryIntegrationTests.cs  # Auth schema repository tests
│   └── LedgerRepositoryIntegrationTests.cs # Forensic ledger SHA-256 chain tests
└── README.md                              # This documentation file
4. Running the Integration Test Suite
4.1 Running via .NET CLI
You can execute the integration tests directly using the .NET CLI:

PowerShell
# Navigate to solution root or integration test project
dotnet test backend/tests/integration/LabTrack.IntegrationTests.csproj --configuration Release --verbosity normal
4.2 Running via Centralized PowerShell Script
The centralized test runner script automatically includes backend integration tests:

PowerShell
# Run full comprehensive test suite including backend integration tests
.\backend\scripts\testing\test-all.ps1
5. Writing a New Integration Test
When adding new domain features, follow the established integration test pattern using WebApplicationFactory:

C#
public class AssetControllerIntegrationTests : IClassFixture<LabTrackWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AssetControllerIntegrationTests(LabTrackWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAssetById_WithValidId_ReturnsOkWithAssetDetails()
    {
        // Arrange
        var assetId = "23000000-0000-0000-0000-000000000001";
        
        // Act
        var response = await _client.GetAsync($"/api/v1/assets/{assetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AssetResponseDto>();
        content.Should().NotBeNull();
        content.Id.Should().Be(assetId);
    }
}