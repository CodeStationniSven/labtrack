# =============================================================================
# LabTrack Backend API Dockerfile
# Multi-stage build for C# .NET 8 Clean Architecture Microservice
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Base Runtime Image
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

# Install icu-data for full globalization support
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# -----------------------------------------------------------------------------
# Stage 2: SDK & Solution Restore
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project structure for Docker layer caching
COPY ["LabTrack.sln", "./"]
COPY ["src/LabTrack.Api/LabTrack.Api.csproj", "src/LabTrack.Api/"]
COPY ["src/LabTrack.Application/LabTrack.Application.csproj", "src/LabTrack.Application/"]
COPY ["src/LabTrack.Domain/LabTrack.Domain.csproj", "src/LabTrack.Domain/"]
COPY ["src/LabTrack.Infrastructure/LabTrack.Infrastructure.csproj", "src/LabTrack.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/LabTrack.Api/LabTrack.Api.csproj"

# Copy remaining source code
COPY . .

# Compile application binaries
WORKDIR "/src/src/LabTrack.Api"
RUN dotnet build "LabTrack.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -----------------------------------------------------------------------------
# Stage 3: Publish App Artifacts
# -----------------------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LabTrack.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 4: Final Production Output
# -----------------------------------------------------------------------------
FROM base AS final
WORKDIR /app

# Switch to standard container user context
USER app

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LabTrack.Api.dll"]