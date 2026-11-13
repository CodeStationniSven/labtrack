"""
LabTrack External Service Clients Package Initialization.

Contains asynchronous API client abstractions for interacting with external services,
including the C# ASP.NET Core backend API, telemetry aggregators, and LLM inference engines.
"""

from app.clients.csharp_api import CSharpApiClient

__all__ = ["CSharpApiClient"]