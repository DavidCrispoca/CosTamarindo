"""Emision de sesiones y validacion autoritativa del puntaje (A.8.24, A.8.9).

Orden de comprobaciones en `/api/score`. El orden importa para la evidencia:
cada ataque debe producir un motivo distinto y registrable.

    1. la sesion existe                -> bad_session
    2. la sesion es del jugador        -> bad_session
    3. la sesion no se uso             -> session_used
    4. la sesion no expiro             -> session_expired
    5. el nonce coincide               -> bad_nonce
    6. la firma HMAC es valida         -> bad_signature
    7. el puntaje es plausible         -> implausible_score
    8. consumo atomico + insercion     -> session_used si pierde la carrera
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone

from ..config import Settings
from ..errors import ScoreRejected, SessionRejected
from ..logging_setup import audit, user_id_var
from ..models import ScoreRequest, ScoreResponse, SessionStartResponse
from ..repositories import Repository
from ..security import (
    canonical_score,
    derive_session_key,
    new_nonce,
    verify_signature,
)


class ScoreService:
    def __init__(self, repo: Repository, settings: Settings) -> None:
        self._repo = repo
        self._settings = settings

    async def start_session(self, user_id: uuid.UUID) -> SessionStartResponse:
        nonce = new_nonce()
        record = await self._repo.create_session(user_id, nonce, self._settings.session_ttl_minutes)
        session_key = derive_session_key(
            self._settings.session_master_key, str(record.id), record.nonce
        )
        best = await self._repo.best_score(user_id) or 0
        audit("session_start", user_id=str(user_id), session_id=str(record.id))
        return SessionStartResponse(
            session_id=record.id,
            nonce=record.nonce,
            session_key=session_key.hex(),
            expires_in=self._settings.session_ttl_minutes * 60,
            best_score=best,
        )

    async def submit(
        self, *, user_id: uuid.UUID, payload: ScoreRequest, client_ip: str = "-"
    ) -> ScoreResponse:
        session = await self._repo.get_session(payload.session_id)
        if session is None or session.user_id != user_id:
            self._reject(user_id, "bad_session", client_ip)
        if session.used_at is not None:
            self._reject(user_id, "session_used", client_ip)
        if session.expires_at <= datetime.now(timezone.utc):
            self._reject(user_id, "session_expired", client_ip)
        if not _constant_equals(session.nonce, payload.nonce):
            self._reject(user_id, "bad_nonce", client_ip)

        session_key = derive_session_key(
            self._settings.session_master_key, str(session.id), session.nonce
        )
        canonical = canonical_score(
            str(session.id), payload.score, payload.duration_ms, payload.catches, payload.nonce
        )
        if not verify_signature(session_key, canonical, payload.signature):
            self._reject(user_id, "bad_signature", client_ip)

        reason = self._implausibility(payload)
        if reason:
            self._reject(user_id, reason, client_ip)

        if not await self._repo.consume_session(session.id):
            self._reject(user_id, "session_used", client_ip)

        await self._repo.insert_score(
            user_id=user_id,
            session_id=session.id,
            score=payload.score,
            duration_ms=payload.duration_ms,
            catches=payload.catches,
        )
        best = await self._repo.best_score(user_id) or payload.score
        rank = None
        entries = await self._repo.leaderboard(user_id, 5)
        for entry in entries:
            if entry.is_me:
                rank = entry.rank_position
                break

        audit(
            "score_accepted",
            user_id=str(user_id),
            session_id=str(session.id),
            score=payload.score,
            duration_ms=payload.duration_ms,
            catches=payload.catches,
            ip=client_ip,
        )
        return ScoreResponse(score=payload.score, best_score=best, rank_position=rank)

    def _implausibility(self, payload: ScoreRequest) -> str | None:
        seconds = payload.duration_ms / 1000
        # Techo global: el juego no genera mas de MAX_POINTS_PER_SECOND.
        if payload.score > seconds * self._settings.max_points_per_second:
            return "implausible_score"
        # Techo por captura: el item mas valioso vale 25 puntos.
        if payload.score > payload.catches * 25:
            return "implausible_score"
        # Techo de capturas: el spawner no lanza mas de ~3 items por segundo.
        if payload.catches > seconds * 3:
            return "implausible_score"
        return None

    def _reject(self, user_id: uuid.UUID, reason: str, client_ip: str) -> None:
        audit("score_rejected", user_id=str(user_id), reason=reason, ip=client_ip)
        raise ScoreRejected(reason)

    async def current_best(self, user_id: uuid.UUID) -> int:
        return await self._repo.best_score(user_id) or 0


def _constant_equals(left: str, right: str) -> bool:
    import hmac

    return hmac.compare_digest(left.encode(), right.encode())


async def require_session_owner(repo: Repository, session_id: uuid.UUID, user_id: uuid.UUID) -> None:
    session = await repo.get_session(session_id)
    if session is None or session.user_id != user_id:
        raise SessionRejected()
