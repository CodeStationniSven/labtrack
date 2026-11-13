import httpx
from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
import logging

logger = logging.getLogger(__name__)

class LLMMessage(BaseModel):
    role: str = Field(..., description="Role of the message sender (system, user, assistant)")
    content: str = Field(..., description="Message text content")

class LLMCompletionRequest(BaseModel):
    messages: List[LLMMessage]
    temperature: float = Field(0.2, ge=0.0, le=2.0)
    max_tokens: int = Field(512, ge=1, le=4096)
    model: str = Field("gpt-4o-mini", description="Target model name")

class LLMCompletionResponse(BaseModel):
    content: str
    tokens_used: int
    model: str

class LLMClient:
    def __init__(self, api_key: str, base_url: str = "https://api.openai.com/v1"):
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")
        self.headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

    async def generate_completion(self, request: LLMCompletionRequest) -> LLMCompletionResponse:
        """
        Sends an asynchronous chat completion request to the external LLM provider.
        """
        endpoint = f"{self.base_url}/chat/completions"
        payload = {
            "model": request.model,
            "messages": [msg.model_dump() for msg in request.messages],
            "temperature": request.temperature,
            "max_tokens": request.max_tokens
        }

        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                response = await client.post(endpoint, json=payload, headers=self.headers)
                response.raise_for_status()
                data = response.json()

                content = data["choices"][0]["message"]["content"]
                tokens_used = data.get("usage", {}).get("total_tokens", 0)

                return LLMCompletionResponse(
                    content=content,
                    tokens_used=tokens_used,
                    model=request.model
                )
            except httpx.HTTPStatusError as err:
                logger.error(f"HTTP error occurred during LLM completion: {err.response.text}")
                raise
            except Exception as err:
                logger.error(f"Failed to communicate with LLM inference service: {str(err)}")
                raise