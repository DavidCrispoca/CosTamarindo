"""Dependencias de FastAPI: settings, repositorio, servicios y usuario actual."""

from __future__ import annotations

import uuid
from dataclasses import dataclass

from fastapi import Depends, Request

from .config import Settings
from .errors import InvalidToken
from .logging_setup import user_id_var
from .providers.supabase_auth import SupabaseAuth
from .repositories import Repository
from .security import TokenError, decode_access_token
from .services.auth_service import AuthService
from .services.score_service import ScoreService


@dataclass(frozen=True, slots=True)
class CurrentUser:
    user_id: uuid.UUID
    username: str


def get_request_settings(request: Request) -> Settings:
    """Settings de la instancia que atiende la peticion.

    No usa el singleton global: en pruebas se construye la app con settings
    explicitos y sin .env, y el singleton intentaria leer el archivo.
    """
    return request.app.state.settings


def get_repository(request: Request) -> Repository:
    return request.app.state.repository


def get_auth_provider(request: Request) -> SupabaseAuth:
    return request.app.state.auth_provider


def get_auth_service(
    repo: Repository = Depends(get_repository),
    provider: SupabaseAuth = Depends(get_auth_provider),
    settings: Settings = Depends(get_request_settings),
) -> AuthService:
    return AuthService(repo, provider, settings)


def get_score_service(
    repo: Repository = Depends(get_repository),
    settings: Settings = Depends(get_request_settings),
) -> ScoreService:
    return ScoreService(repo, settings)


def _extract_bearer(request: Request) -> str | None:
    header = request.headers.get("Authorization")
    if not header or not header.lower().startswith("bearer "):
        return None
    token = header[7:].strip()
    return token or None


def _decode(token: str, settings: Settings) -> CurrentUser:
    try:
        payload = decode_access_token(
            secret=settings.backend_jwt_secret, token=token, expected_env=settings.env
        )
    except TokenError as exc:
        raise InvalidToken() from exc
    user_id_var.set(str(payload["sub"]))
    return CurrentUser(user_id=uuid.UUID(payload["sub"]), username=payload.get("username", ""))


def current_user(
    request: Request, settings: Settings = Depends(get_request_settings)
) -> CurrentUser:
    token = _extract_bearer(request)
    if token is None:
        raise InvalidToken()
    return _decode(token, settings)


def optional_user(
    request: Request, settings: Settings = Depends(get_request_settings)
) -> CurrentUser | None:
    token = _extract_bearer(request)
    if token is None:
        return None
    try:
        return _decode(token, settings)
    except InvalidToken:
        return None
