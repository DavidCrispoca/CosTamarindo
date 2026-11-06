from __future__ import annotations

from fastapi import APIRouter, Depends

from ..deps import CurrentUser, current_user, get_score_service
from ..models import SessionStartResponse
from ..services.score_service import ScoreService

router = APIRouter(prefix="/api/session", tags=["session"])


@router.post("/start", response_model=SessionStartResponse)
async def start_session(
    user: CurrentUser = Depends(current_user),
    service: ScoreService = Depends(get_score_service),
) -> SessionStartResponse:
    return await service.start_session(user.user_id)
