from __future__ import annotations

from fastapi import APIRouter, Depends, Response, status

from ..deps import CurrentUser, current_user, get_auth_service
from ..models import LoginRequest, RefreshRequest, RegisterRequest, TokenPair
from ..services.auth_service import AuthService

router = APIRouter(prefix="/api/auth", tags=["auth"])


@router.post("/register", response_model=TokenPair, status_code=status.HTTP_201_CREATED)
async def register(
    payload: RegisterRequest,
    service: AuthService = Depends(get_auth_service),
) -> TokenPair:
    return await service.register(username=payload.username, pin=payload.pin)


@router.post("/login", response_model=TokenPair)
async def login(
    payload: LoginRequest,
    service: AuthService = Depends(get_auth_service),
) -> TokenPair:
    return await service.login(username=payload.username, pin=payload.pin)


@router.post("/refresh", response_model=TokenPair)
async def refresh(
    payload: RefreshRequest,
    service: AuthService = Depends(get_auth_service),
) -> TokenPair:
    return await service.refresh(refresh_token=payload.refresh_token)


@router.post("/logout", status_code=status.HTTP_204_NO_CONTENT)
async def logout(
    user: CurrentUser = Depends(current_user),
    service: AuthService = Depends(get_auth_service),
) -> Response:
    await service.logout(username=user.username)
    return Response(status_code=status.HTTP_204_NO_CONTENT)
