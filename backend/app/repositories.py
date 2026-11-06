"""Acceso a datos. Todo el SQL vive aqui, parametrizado y sin concatenacion.

Las tablas no son accesibles para anon ni authenticated (ver 002/003): este
modulo corre con el rol service_role del pooler.
"""

from __future__ import annotations

import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Protocol

from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker


@dataclass(frozen=True, slots=True)
class Profile:
    id: uuid.UUID
    username: str


@dataclass(frozen=True, slots=True)
class GameSession:
    id: uuid.UUID
    user_id: uuid.UUID
    nonce: str
    created_at: datetime
    expires_at: datetime
    used_at: datetime | None


@dataclass(frozen=True, slots=True)
class LeaderboardEntry:
    rank_position: int
    username: str
    best_score: int
    is_me: bool


class Repository(Protocol):
    """Contrato que consumen los servicios. Permite sustituirlo en pruebas."""

    async def create_profile(self, user_id: uuid.UUID, username: str) -> None: ...
    async def get_profile_by_username(self, username: str) -> Profile | None: ...
    async def get_profile(self, user_id: uuid.UUID) -> Profile | None: ...

    async def create_session(
        self, user_id: uuid.UUID, nonce: str, ttl_minutes: int
    ) -> GameSession: ...
    async def get_session(self, session_id: uuid.UUID) -> GameSession | None: ...
    async def consume_session(self, session_id: uuid.UUID) -> bool: ...

    async def insert_score(
        self,
        *,
        user_id: uuid.UUID,
        session_id: uuid.UUID,
        score: int,
        duration_ms: int,
        catches: int,
    ) -> int: ...
    async def best_score(self, user_id: uuid.UUID) -> int | None: ...
    async def leaderboard(self, user_id: uuid.UUID | None, limit: int = 5) -> list[LeaderboardEntry]: ...

    async def locked_until(self, username_lower: str) -> datetime | None: ...
    async def register_login_failure(
        self, username_lower: str, max_attempts: int, lock_seconds: int
    ) -> datetime | None: ...
    async def clear_login_failures(self, username_lower: str) -> None: ...

    async def ping(self) -> None: ...


class SqlRepository:
    """Implementacion sobre Postgres (Supabase)."""

    def __init__(self, session_factory: async_sessionmaker[AsyncSession]) -> None:
        self._session_factory = session_factory

    # -- profiles ---------------------------------------------------------
    async def create_profile(self, user_id: uuid.UUID, username: str) -> None:
        async with self._session_factory() as session, session.begin():
            await session.execute(
                text(
                    "insert into public.profiles (id, username) values (:id, :username) "
                    "on conflict (id) do nothing"
                ),
                {"id": str(user_id), "username": username},
            )

    async def get_profile_by_username(self, username: str) -> Profile | None:
        async with self._session_factory() as session:
            row = (
                await session.execute(
                    text(
                        "select id, username from public.profiles "
                        "where lower(username) = lower(:username)"
                    ),
                    {"username": username},
                )
            ).first()
        return Profile(id=row.id, username=row.username) if row else None

    async def get_profile(self, user_id: uuid.UUID) -> Profile | None:
        async with self._session_factory() as session:
            row = (
                await session.execute(
                    text("select id, username from public.profiles where id = :id"),
                    {"id": str(user_id)},
                )
            ).first()
        return Profile(id=row.id, username=row.username) if row else None

    # -- game_sessions ----------------------------------------------------
    async def create_session(
        self, user_id: uuid.UUID, nonce: str, ttl_minutes: int
    ) -> GameSession:
        async with self._session_factory() as session, session.begin():
            row = (
                await session.execute(
                    text(
                        "insert into public.game_sessions (user_id, nonce, expires_at) "
                        "values (:user_id, :nonce, now() + make_interval(mins => :ttl)) "
                        "returning id, user_id, nonce, created_at, expires_at, used_at"
                    ),
                    {"user_id": str(user_id), "nonce": nonce, "ttl": ttl_minutes},
                )
            ).one()
        return GameSession(
            id=row.id,
            user_id=row.user_id,
            nonce=row.nonce,
            created_at=row.created_at,
            expires_at=row.expires_at,
            used_at=row.used_at,
        )

    async def get_session(self, session_id: uuid.UUID) -> GameSession | None:
        async with self._session_factory() as session:
            row = (
                await session.execute(
                    text(
                        "select id, user_id, nonce, created_at, expires_at, used_at "
                        "from public.game_sessions where id = :id"
                    ),
                    {"id": str(session_id)},
                )
            ).first()
        if not row:
            return None
        return GameSession(
            id=row.id,
            user_id=row.user_id,
            nonce=row.nonce,
            created_at=row.created_at,
            expires_at=row.expires_at,
            used_at=row.used_at,
        )

    async def consume_session(self, session_id: uuid.UUID) -> bool:
        """Marca la sesion como usada de forma atomica.

        El `where used_at is null` es lo que garantiza un solo envio por sesion
        incluso con dos peticiones simultaneas.
        """
        async with self._session_factory() as session, session.begin():
            result = await session.execute(
                text(
                    "update public.game_sessions set used_at = now() "
                    "where id = :id and used_at is null returning id"
                ),
                {"id": str(session_id)},
            )
            return result.first() is not None

    # -- scores -----------------------------------------------------------
    async def insert_score(
        self,
        *,
        user_id: uuid.UUID,
        session_id: uuid.UUID,
        score: int,
        duration_ms: int,
        catches: int,
    ) -> int:
        async with self._session_factory() as session, session.begin():
            row = (
                await session.execute(
                    text(
                        "insert into public.scores (user_id, session_id, score, duration_ms, catches) "
                        "values (:user_id, :session_id, :score, :duration_ms, :catches) "
                        "returning id"
                    ),
                    {
                        "user_id": str(user_id),
                        "session_id": str(session_id),
                        "score": score,
                        "duration_ms": duration_ms,
                        "catches": catches,
                    },
                )
            ).one()
        return int(row.id)

    async def best_score(self, user_id: uuid.UUID) -> int | None:
        async with self._session_factory() as session:
            return await session.scalar(
                text("select max(score) from public.scores where user_id = :id"),
                {"id": str(user_id)},
            )

    async def leaderboard(
        self, user_id: uuid.UUID | None, limit: int = 5
    ) -> list[LeaderboardEntry]:
        async with self._session_factory() as session:
            rows = (
                await session.execute(
                    text(
                        "select rank_position, username, best_score, is_me "
                        "from public.get_leaderboard(cast(:user_id as uuid), :limit)"
                    ),
                    {"user_id": str(user_id) if user_id else None, "limit": limit},
                )
            ).all()
        return [
            LeaderboardEntry(
                rank_position=int(row.rank_position),
                username=row.username,
                best_score=int(row.best_score),
                is_me=bool(row.is_me),
            )
            for row in rows
        ]

    # -- login_attempts ---------------------------------------------------
    async def locked_until(self, username_lower: str) -> datetime | None:
        async with self._session_factory() as session, session.begin():
            row = (
                await session.execute(
                    text(
                        "select failed_count, locked_until from public.login_attempts "
                        "where username_lower = :u for update"
                    ),
                    {"u": username_lower},
                )
            ).first()
            if not row:
                return None
            locked = row.locked_until
            if locked and locked <= datetime.now(timezone.utc):
                # El bloqueo vencio: se reinicia el contador de forma perezosa.
                await session.execute(
                    text(
                        "update public.login_attempts set failed_count = 0, locked_until = null "
                        "where username_lower = :u"
                    ),
                    {"u": username_lower},
                )
                return None
            return locked

    async def register_login_failure(
        self, username_lower: str, max_attempts: int, lock_seconds: int
    ) -> datetime | None:
        async with self._session_factory() as session, session.begin():
            await session.execute(
                text(
                    "insert into public.login_attempts (username_lower) values (:u) "
                    "on conflict (username_lower) do nothing"
                ),
                {"u": username_lower},
            )
            row = (
                await session.execute(
                    text(
                        "select failed_count from public.login_attempts "
                        "where username_lower = :u for update"
                    ),
                    {"u": username_lower},
                )
            ).one()
            failures = int(row.failed_count) + 1
            lock_until = None
            if failures >= max_attempts:
                lock_until = datetime.now(timezone.utc) + timedelta(seconds=lock_seconds)
            await session.execute(
                text(
                    "update public.login_attempts "
                    "set failed_count = :count, locked_until = :lock, last_attempt_at = now() "
                    "where username_lower = :u"
                ),
                {"count": failures, "lock": lock_until, "u": username_lower},
            )
            return lock_until

    async def clear_login_failures(self, username_lower: str) -> None:
        async with self._session_factory() as session, session.begin():
            await session.execute(
                text("delete from public.login_attempts where username_lower = :u"),
                {"u": username_lower},
            )

    async def ping(self) -> None:
        async with self._session_factory() as session:
            await session.execute(text("select 1"))
