from __future__ import annotations

from fastapi import APIRouter, Depends, Request

from ..deps import get_auth_service, get_score_service
from ..logging_setup import mask_ip
from ..models import ClaimRequest, ClaimResponse, ScoreRequest, ScoreResponse
from ..services.auth_service import AuthService
from ..services.score_service import ScoreService

router = APIRouter(prefix="/api", tags=["score"])


@router.post("/score", response_model=ScoreResponse)
async def submit_score(
    payload: ScoreRequest,
    request: Request,
    service: ScoreService = Depends(get_score_service),
) -> ScoreResponse:
    client_ip = mask_ip(request.client.host if request.client else None)
    return await service.submit(payload=payload, client_ip=client_ip)


@router.post("/claim", response_model=ClaimResponse)
async def claim_score(
    payload: ClaimRequest,
    auth: AuthService = Depends(get_auth_service),
    service: ScoreService = Depends(get_score_service),
) -> ClaimResponse:
    # Primero se valida que el puntaje sea reclamable (sin crear perfiles),
    # y recien despues se resuelve el nombre + PIN.
    await service.assert_claimable(session_id=payload.session_id)
    profile = await auth.claim_identity(username=payload.username, pin=payload.pin)
    return await service.claim(
        session_id=payload.session_id, user_id=profile.id, username=profile.username
    )
