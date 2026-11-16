from __future__ import annotations

from fastapi import APIRouter, Depends

from ..deps import get_score_service
from ..models import SessionStartResponse
from ..services.score_service import ScoreService

router = APIRouter(prefix="/api/session", tags=["session"])


@router.post("/start", response_model=SessionStartResponse)
async def start_session(
    service: ScoreService = Depends(get_score_service),
) -> SessionStartResponse:
    return await service.start_session()
