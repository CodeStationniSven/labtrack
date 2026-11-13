import pytest
from httpx import AsyncClient
from app.main import app


@pytest.mark.asyncio
async def test_health_check_endpoint():
    """
    Tests the health check endpoint to ensure it returns 200 OK 
    and proper service metadata.
    """
    async with AsyncClient(app=app, base_url="http://test") as client:
        response = await client.get("/health")

    assert response.status_code == 200
    
    payload = response.json()
    assert payload["success"] is True
    assert payload["message"] == "Service is healthy and operational."
    assert payload["data"]["app_name"] == "LabTrack.Python"
    assert payload["data"]["status"] == "UP"


@pytest.mark.asyncio
async def test_non_existent_route_404():
    """
    Tests requesting an undefined endpoint returns a standard 404 response.
    """
    async with AsyncClient(app=app, base_url="http://test") as client:
        response = await client.get("/api/invalid-route-xyz")

    assert response.status_code == 404