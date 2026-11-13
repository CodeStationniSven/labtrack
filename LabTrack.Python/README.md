# LABTRACK AI SERVICE

FastAPI-based internal AI service.

## Endpoints
- GET  /health
- GET  /ready
- POST /internal/v1/chat
- POST /internal/v1/analyze/asset
- POST /internal/v1/estimate-repair
- POST /internal/v1/analyze/problem

## Run
```uvicorn app.main:app --reload --port 8000```