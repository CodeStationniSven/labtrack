import httpx
from typing import Any, Optional


class CSharpApiClient:
    """HTTP client wrapper for the LabTrack C# backend."""

    def __init__(
        self,
        base_url: str = "https://localhost:5001",
        timeout: float = 30.0,
        headers: Optional[dict[str, str]] = None,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.headers = headers or {}

    async def _request(
        self,
        method: str,
        path: str,
        params: Optional[dict[str, Any]] = None,
        json: Optional[dict[str, Any]] = None,
    ) -> Any:
        async with httpx.AsyncClient(timeout=self.timeout, headers=self.headers) as client:
            response = await client.request(
                method,
                f"{self.base_url}/{path.lstrip('/')}",
                params=params,
                json=json,
            )
            response.raise_for_status()
            if response.status_code == 204 or not response.content:
                return None
            return response.json()

    async def get(self, path: str, params: Optional[dict[str, Any]] = None) -> Any:
        return await self._request("GET", path, params=params)

    async def post(self, path: str, json: Optional[dict[str, Any]] = None) -> Any:
        return await self._request("POST", path, json=json)

    async def put(self, path: str, json: Optional[dict[str, Any]] = None) -> Any:
        return await self._request("PUT", path, json=json)

    async def delete(self, path: str) -> Any:
        return await self._request("DELETE", path)