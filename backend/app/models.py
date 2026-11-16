"""Contratos HTTP. La validacion de forma vive aqui; la de negocio, en servicios."""

from __future__ import annotations

import uuid
from typing import Literal

from pydantic import BaseModel, Field, model_validator

USERNAME_PATTERN = r"^[A-Za-z0-9_]{3,16}$"
PIN_PATTERN = r"^[0-9]{4,6}$"

DialogContext = Literal["inicio", "captura", "peligro", "game_over", "libro", "defecto"]
DialogKind = Literal["good", "danger", "trash"]


class RegisterRequest(BaseModel):
    username: str = Field(min_length=3, max_length=16, pattern=USERNAME_PATTERN)
    pin: str = Field(min_length=4, max_length=6, pattern=PIN_PATTERN)


class LoginRequest(BaseModel):
    username: str = Field(min_length=3, max_length=16, pattern=USERNAME_PATTERN)
    pin: str = Field(min_length=4, max_length=6, pattern=PIN_PATTERN)


class RefreshRequest(BaseModel):
    refresh_token: str = Field(min_length=10, max_length=4096)


class TokenPair(BaseModel):
    access_token: str
    refresh_token: str
    token_type: Literal["bearer"] = "bearer"
    expires_in: int
    user_id: uuid.UUID
    username: str


class SessionStartResponse(BaseModel):
    session_id: uuid.UUID
    nonce: str
    session_key: str = Field(description="Clave HMAC de esta sesion; el cliente no la persiste")
    expires_in: int
    best_score: int


class ScoreRequest(BaseModel):
    session_id: uuid.UUID
    score: int = Field(ge=0, le=100_000)
    duration_ms: int = Field(ge=1000, le=900_000)
    catches: int = Field(ge=0, le=5000)
    nonce: str = Field(min_length=16, max_length=64)
    signature: str = Field(min_length=64, max_length=64, pattern=r"^[a-fA-F0-9]{64}$")


class ScoreResponse(BaseModel):
    ok: bool = True
    score: int
    best_score: int
    rank_position: int | None = None
    in_top5: bool = False


class ClaimRequest(BaseModel):
    session_id: uuid.UUID
    username: str = Field(min_length=3, max_length=16, pattern=USERNAME_PATTERN)
    pin: str = Field(min_length=4, max_length=6, pattern=PIN_PATTERN)


class ClaimResponse(BaseModel):
    ok: bool = True
    username: str
    best_score: int
    rank_position: int | None = None


class LeaderboardEntryModel(BaseModel):
    rank_position: int
    username: str
    best_score: int
    is_me: bool


class LeaderboardResponse(BaseModel):
    entries: list[LeaderboardEntryModel]


class DialogRequest(BaseModel):
    # `text` puede ir vacío cuando Unity envía solo el evento de una captura.
    text: str = Field(default="", max_length=200)
    context: DialogContext = "defecto"
    item: str | None = Field(default=None, max_length=40)
    kind: DialogKind | None = None

    @model_validator(mode="after")
    def _requires_text_or_item(self) -> "DialogRequest":
        if not self.text.strip() and not (self.item and self.item.strip()):
            raise ValueError("text o item requerido")
        return self


class DialogResponse(BaseModel):
    reply: str
    blocked: bool = False


class HealthResponse(BaseModel):
    status: Literal["ok", "degraded"]
    env: str
    database: bool
