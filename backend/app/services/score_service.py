"""Emision de sesiones y validacion autoritativa del puntaje (A.8.24, A.8.9).

El login deja de ser requisito para jugar: la sesion y el puntaje son anonimos.
Si el puntaje entra al top 5 global, el cliente reclama el nombre con
`/api/claim`.

Orden de comprobaciones en `/api/score`. El orden importa para la evidencia:
cada ataque debe producir un motivo distinto y registrable.

    1. la sesion existe                -> bad_session
    2. la sesion no se uso             -> session_used
    3. la sesion no expiro             -> session_expired
    4. el nonce coincide               -> bad_nonce
    5. la firma HMAC es valida         -> bad_signature
    6. el puntaje es plausible         -> implausible_score
    7. consumo atomico + insercion     -> session_used si pierde la carrera
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone

from ..config import Settings
from ..errors import ScoreRejected
from ..logging_setup import audit
from ..models import ClaimResponse, ScoreRequest, ScoreResponse, SessionStartResponse
from ..repositories import Repository
from ..security import (
    canonical_score,
    derive_session_key,
    new_nonce,
    verify_signature,
)

TOP_N = 5


class ScoreService:
    def __init__(self, repo: Repository, settings: Settings) -> None:
        self._repo = repo
        self._settings = settings

    async def start_session(self) -> SessionStartResponse:
        nonce = new_nonce()
        record = await self._repo.create_session(None, nonce, self._settings.session_ttl_minutes)
        session_key = derive_session_key(
            self._settings.session_master_key, str(record.id), record.nonce
        )
        audit("session_start", session_id=str(record.id))
        return SessionStartResponse(
            session_id=record.id,
            nonce=record.nonce,
            session_key=session_key.hex(),
            expires_in=self._settings.session_ttl_minutes * 60,
            best_score=0,
        )

    async def submit(
        self, *, payload: ScoreRequest, client_ip: str = "-"
    ) -> ScoreResponse:
        session = await self._repo.get_session(payload.session_id)
        if session is None:
            self._reject(None, "bad_session", client_ip)
        if session.used_at is not None:
            self._reject(None, "session_used", client_ip)
        if session.expires_at <= datetime.now(timezone.utc):
            self._reject(None, "session_expired", client_ip)
        if not _constant_equals(session.nonce, payload.nonce):
            self._reject(None, "bad_nonce", client_ip)

        session_key = derive_session_key(
            self._settings.session_master_key, str(session.id), session.nonce
        )
        canonical = canonical_score(
            str(session.id), payload.score, payload.duration_ms, payload.catches, payload.nonce
        )
        if not verify_signature(session_key, canonical, payload.signature):
            self._reject(None, "bad_signature", client_ip)

        reason = self._implausibility(payload)
        if reason:
            self._reject(None, reason, client_ip)

        if not await self._repo.consume_session(session.id):
            self._reject(None, "session_used", client_ip)

        await self._repo.insert_score(
            user_id=None,
            session_id=session.id,
            score=payload.score,
            duration_ms=payload.duration_ms,
            catches=payload.catches,
        )
        in_top5 = await self._repo.is_top5(payload.score)

        audit(
            "score_accepted",
            session_id=str(session.id),
            score=payload.score,
            duration_ms=payload.duration_ms,
            catches=payload.catches,
            in_top5=in_top5,
            ip=client_ip,
        )
        return ScoreResponse(
            score=payload.score, best_score=0, rank_position=None, in_top5=in_top5
        )

    async def assert_claimable(self, *, session_id: uuid.UUID) -> None:
        """Valida sin efectos secundarios que el puntaje se puede reclamar.

        Se llama ANTES de resolver la identidad, para que un reclamo invalido
        (puntaje fuera del top 5 o ya reclamado) no cree un perfil de nombre.
        """
        stored = await self._repo.get_score_by_session(session_id)
        if stored is None or stored.user_id is not None:
            raise ScoreRejected("bad_session")
        if not await self._repo.is_top5(stored.score):
            raise ScoreRejected("not_top5")

    async def claim(
        self, *, session_id: uuid.UUID, user_id: uuid.UUID, username: str
    ) -> ClaimResponse:
        stored = await self._repo.get_score_by_session(session_id)
        if not await self._repo.claim_score(session_id, user_id):
            raise ScoreRejected("bad_session")

        best = await self._repo.best_score(user_id) or (stored.score if stored else 0)
        rank = await self._rank_of(user_id)

        audit(
            "score_claimed",
            user_id=str(user_id),
            username=username,
            score=stored.score if stored else None,
            session_id=str(session_id),
        )
        return ClaimResponse(username=username, best_score=best, rank_position=rank)

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

    def _reject(self, user_id: uuid.UUID | None, reason: str, client_ip: str) -> None:
        audit(
            "score_rejected",
            user_id=str(user_id) if user_id else None,
            reason=reason,
            ip=client_ip,
        )
        raise ScoreRejected(reason)

    async def _rank_of(self, user_id: uuid.UUID) -> int | None:
        entries = await self._repo.leaderboard(user_id, TOP_N)
        for entry in entries:
            if entry.is_me:
                return entry.rank_position
        return None


def _constant_equals(left: str, right: str) -> bool:
    import hmac

    return hmac.compare_digest(left.encode(), right.encode())
