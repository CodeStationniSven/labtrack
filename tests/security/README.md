LabTrack Security & Vulnerability Testing Guide
This document provides comprehensive instructions for configuring, executing, and maintaining the security testing and vulnerability assessment suite for the LabTrack enterprise laboratory management platform.

1. Overview & Security Test Scope
The LabTrack security testing suite validates adherence to zero-trust architecture principles, OWASP Top 10 web application security controls, JWT token validation robustness, PostgreSQL Row-Level Security (RLS) isolation, and cryptographic tamper-evidence across the forensic audit ledger.

                  +-----------------------------------+
                  |   Security Testing Tools / SAST   |
                  |  (OWASP ZAP, Trivy, dotnet audit) |
                  +-----------------------------------+
                                    |
                                    v  (Automated Scans & Fuzzing)
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
                  |  (RLS Enforcement & SHA-256 Audit)|
                  +-----------------------------------+
Key Security Test Categories
Static Application Security Testing (SAST): Automated code analysis detecting code smells, hardcoded secrets, SQL injection vectors, and insecure deserialization.

Dependency & Supply Chain Auditing: Automated vulnerability scanning of NuGet, pip, and npm packages for known Common Vulnerabilities and Exposures (CVEs).

Dynamic Application Security Testing (DAST) & API Fuzzing: Automated black-box and grey-box HTTP scanning against REST endpoints and WebSocket channels.

Row-Level Security (RLS) & Forensic Ledger Audit: Verification that unauthenticated or improperly scoped database sessions cannot bypass multi-tenant isolation or tamper with audit.forensic_ledger.

2. Prerequisites & Security Tooling
Ensure the following security tools are installed on your scanning or CI/CD runner environment:

Trivy / OWASP Dependency-Check (for container & package vulnerability scanning).

dotnet CLI built-in audit: (dotnet list package --vulnerable).

OWASP ZAP CLI (for automated DAST baseline scans).

3. Test Suite Directory Structure
backend/tests/security/
├── scripts/
│   ├── dependency-audit.ps1         # Supply chain vulnerability scanner
│   └── ledger-integrity-check.sql   # Cryptographic tamper verification script
└── README.md                        # This documentation file
4. Executing Security Scans
4.1 Dependency & Supply Chain Vulnerability Audit
To scan all backend .NET packages and Python dependencies for known CVEs:

PowerShell
# Scan .NET NuGet packages for vulnerabilities
dotnet list package --vulnerable --include-transitive

# Scan Python dependencies using pip-audit
pip-audit --requirement backend/python/requirements.txt
4.2 Automated OWASP ZAP Baseline Scan (DAST)
To execute an automated DAST scan against the running LabTrack API gateway:

Bash
# Run OWASP ZAP baseline scan against local API container
docker run --rm -t owasp/zap2docker-stable zap-baseline.py \
    -t http://host.docker.internal:8080/swagger/index.html \
    -r zap-report.html
5. Security Compliance Thresholds
LabTrack enforces strict security gates in CI/CD pipelines:

Vulnerability Severity	Maximum Allowable Count	Action / Response
Critical	0	Immediate build failure; mandatory hotfix prior to deployment.
High	0	Build failure; remediation required within 24 hours.
Medium	<3	Warning flag; scheduled for resolution in the next sprint release.
Low / Informational	Tracked	Monitored during periodic code reviews.