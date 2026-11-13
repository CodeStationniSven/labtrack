LabTrack End-to-End (E2E) Testing Guide
This document provides comprehensive instructions for configuring, executing, and maintaining the End-to-End (E2E) automated test suite for the LabTrack enterprise laboratory management platform.

1. Overview & Test Scope
The LabTrack E2E test suite validates critical user journeys, multi-service integrations, real-time WebSocket telemetry streams, and security boundaries across the entire system architecture:

                  +-----------------------------------+
                  |      Playwright / Test Runner     |
                  +-----------------------------------+
                                    |
                                    v  (HTTP / WSS)
                  +-----------------------------------+
                  |     Nginx Reverse Proxy Gateway   |
                  +-----------------------------------+
                       |                         |
                       v                         v
        +---------------------------+     +---------------------------+
        |   .NET 8 Backend API      |     |    Python AI RAG Engine   |
        +---------------------------+     +---------------------------+
                       |                         |
                       +------------+------------+
                                    |
                                    v
                  +-----------------------------------+
                  |     PostgreSQL 16 Database        |
                  |  (Multi-Schema + RLS + Ledger)    |
                  +-----------------------------------+
Key Test Categories
Authentication & RBAC Flows: Login credential validation, JWT token lifecycle, refresh token rotation, and Role-Based Access Control (Admin, Technician, Researcher, Auditor).

Asset Lifecycle & QR Check-In: Physical asset reservation, QR token scanning handshake, resource locking (RESOURCE_LOCKED), and Optimistic Concurrency Control (OCC).

Real-Time Telemetry & SignalR: WebSocket connection handshake (/hubs/telemetry), live heartbeat ingestion, and Nginx upgrade proxying.

AI RAG Diagnostic Queries: Semantic search queries against vector embeddings stored in ai.vector_store via pgvector.

2. Prerequisites & Environment Setup
Before running the E2E test suite, ensure your local development or test staging environment is fully provisioned:

Node.js & Playwright CLI (for UI/API test runner):

Bash
node --version  # Requires v18+
npm install -g @playwright/test
Docker & Docker Compose (running containerized backend stack):

Bash
docker compose up -d
Database Seed Data: Ensure test accounts (e.g., admin@labtrack.io) and baseline catalog records are loaded via EF Core migrations or seed scripts.

3. Test Suite Directory Structure
backend/tests/e2e/
├── config/
│   └── playwright.config.ts      # E2E test runner configuration
├── fixtures/
│   └── auth.fixture.ts           # Authentication state fixtures
├── specs/
│   ├── auth.spec.ts              # Login, MFA, and token expiration tests
│   ├── asset-workflow.spec.ts    # QR check-in, reservation, and OCC tests
│   ├── telemetry.spec.ts         # SignalR WebSocket connection tests
│   └── ai-diagnostic.spec.ts     # Python vector search and RAG tests
└── README.md                     # This documentation file
4. Running the E2E Test Suite
4.1 Running via PowerShell Operational Script
You can execute the automated test runner through the centralized testing script:

PowerShell
# Run full comprehensive test suite including E2E
.\backend\scripts\testing\test-all.ps1
4.2 Running via Playwright CLI
To run specific E2E test suites interactively or headless:

Bash
# Navigate to E2E test directory
cd backend/tests/e2e

# Run all E2E tests in headless mode
npx playwright test

# Run tests with UI inspector enabled
npx playwright test --ui

# Run a specific specification file
npx playwright test specs/asset-workflow.spec.ts
5. Continuous Integration (CI) Pipeline Integration
In automated CI/CD pipelines (e.g., GitHub Actions), E2E tests execute after container deployment and health verification:

YAML
name: LabTrack E2E Test Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4

      - name: Set up Docker Compose Stack
        run: docker compose up -d

      - name: Wait for API Health
        run: |
          ./backend/scripts/production/health-check.ps1

      - name: Set up Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20

      - name: Install Dependencies
        working-directory: ./backend/tests/e2e
        run: npm ci

      - name: Run Playwright E2E Tests
        working-directory: ./backend/tests/e2e
        run: npx playwright test --reporter=html