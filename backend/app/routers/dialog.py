from __future__ import annotations

from fastapi import APIRouter, Depends, Request

from ..deps import CurrentUser, get_ollama, optional_user
from ..logging_setup import mask_ip
from ..models import DialogRequest, DialogResponse
from ..providers.ollama import OllamaClient
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
    request: Request,
    user: CurrentUser | None = Depends(optional_user),
    guard: DialogGuard = Depends(get_guard),
    ollama: OllamaClient | None = Depends(get_ollama),
) -> DialogResponse:
    client_ip = mask_ip(request.client.host if request.client else None)
    guard_key = str(user.user_id) if user else f"anon:{client_ip}"
    reply, blocked = await build_reply(
        user_id=guard_key,
        text=payload.text,
        context=payload.context,
        item=payload.item,
        kind=payload.kind,
        guard=guard,
        ollama=ollama,
    )
    return DialogResponse(reply=reply, blocked=blocked)
