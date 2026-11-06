from __future__ import annotations

from fastapi import APIRouter, Depends, Response, status
from sqlalchemy.exc import SQLAlchemyError

from ..config import Settings
from ..deps import get_repository, get_request_settings
from ..logging_setup import audit
from ..models import HealthResponse
from ..repositories import Repository

router = APIRouter(prefix="/api", tags=["health"])


@router.get("/health", response_model=HealthResponse)
async def health(
    response: Response,
    repo: Repository = Depends(get_repository),
    settings: Settings = Depends(get_request_settings),
) -> HealthResponse:
    database_ok = True
    try:
        await repo.ping()
    except SQLAlchemyError:
        database_ok = False
        audit("health_degraded", reason="database_unreachable")

    if not database_ok:
        # 503 para que Compose y cualquier monitor lo vean como fallo real:
        # un 200 "degraded" esconde la caida.
        response.status_code = status.HTTP_503_SERVICE_UNAVAILABLE

    return HealthResponse(
        status="ok" if database_ok else "degraded",
        env=settings.env,
        database=database_ok,
    )
