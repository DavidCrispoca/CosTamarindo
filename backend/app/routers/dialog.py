from __future__ import annotations

from fastapi import APIRouter, Depends, Request

from ..deps import CurrentUser, current_user
from ..models import DialogRequest, DialogResponse
from ..services.dialog_service import DialogGuard, build_reply

router = APIRouter(prefix="/api", tags=["dialog"])


def get_guard(request: Request) -> DialogGuard:
    guard = getattr(request.app.state, "dialog_guard", None)
    if guard is None:
        guard = DialogGuard()
        request.app.state.dialog_guard = guard
    return guard


@router.post("/dialog", response_model=DialogResponse)
async def dialog(
    payload: DialogRequest,
    user: CurrentUser = Depends(current_user),
    guard: DialogGuard = Depends(get_guard),
) -> DialogResponse:
    reply, blocked = build_reply(
        user_id=str(user.user_id), text=payload.text, context="defecto", guard=guard
    )
    return DialogResponse(reply=reply, blocked=blocked)
