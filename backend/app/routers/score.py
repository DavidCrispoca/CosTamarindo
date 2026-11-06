from __future__ import annotations

from fastapi import APIRouter, Depends, Request

from ..deps import CurrentUser, current_user, get_score_service
from ..logging_setup import mask_ip
from ..models import ScoreRequest, ScoreResponse
from ..services.score_service import ScoreService

router = APIRouter(prefix="/api", tags=["score"])


@router.post("/score", response_model=ScoreResponse)
async def submit_score(
    payload: ScoreRequest,
    request: Request,
    user: CurrentUser = Depends(current_user),
    service: ScoreService = Depends(get_score_service),
) -> ScoreResponse:
    client_ip = mask_ip(request.client.host if request.client else None)
    return await service.submit(user_id=user.user_id, payload=payload, client_ip=client_ip)
