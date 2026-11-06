"""Dobles de prueba y fixtures.

Las pruebas de API no tocan Supabase ni Postgres: sustituyen el repositorio y
el proveedor de identidad. La verificacion contra la base real se hace con
`backend/scripts/check_db.py` y con pruebas manuales de extremo a extremo.
"""

from __future__ import annotations

import uuid
from dataclasses import replace
from datetime import datetime, timedelta, timezone

import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.factory import create_app
from app.providers.supabase_auth import AuthRejected, AuthTokens
from app.repositories import GameSession, LeaderboardEntry, Profile


class InMemoryRepository:
    def __init__(self) -> None:
        self.profiles: dict[uuid.UUID, Profile] = {}
        self.sessions: dict[uuid.UUID, GameSession] = {}
        self.scores: list[dict] = []
        self.attempts: dict[str, dict] = {}

    async def create_profile(self, user_id: uuid.UUID, username: str) -> None:
        self.profiles[user_id] = Profile(id=user_id, username=username)

    async def get_profile_by_username(self, username: str) -> Profile | None:
        for profile in self.profiles.values():
            if profile.username.lower() == username.lower():
                return profile
        return None

    async def get_profile(self, user_id: uuid.UUID) -> Profile | None:
        return self.profiles.get(user_id)

    async def create_session(self, user_id: uuid.UUID, nonce: str, ttl_minutes: int) -> GameSession:
        now = datetime.now(timezone.utc)
        record = GameSession(
            id=uuid.uuid4(),
            user_id=user_id,
            nonce=nonce,
            created_at=now,
            expires_at=now + timedelta(minutes=ttl_minutes),
            used_at=None,
        )
        self.sessions[record.id] = record
        return record

    async def get_session(self, session_id: uuid.UUID) -> GameSession | None:
        return self.sessions.get(session_id)

    async def consume_session(self, session_id: uuid.UUID) -> bool:
        record = self.sessions.get(session_id)
        if record is None or record.used_at is not None:
            return False
        self.sessions[session_id] = replace(record, used_at=datetime.now(timezone.utc))
        return True

    async def insert_score(
        self, *, user_id: uuid.UUID, session_id: uuid.UUID, score: int, duration_ms: int, catches: int
    ) -> int:
        self.scores.append(
            {
                "user_id": user_id,
                "session_id": session_id,
                "score": score,
                "duration_ms": duration_ms,
                "catches": catches,
            }
        )
        return len(self.scores)

    async def best_score(self, user_id: uuid.UUID) -> int | None:
        values = [row["score"] for row in self.scores if row["user_id"] == user_id]
        return max(values) if values else None

    async def leaderboard(self, user_id: uuid.UUID | None, limit: int = 5) -> list[LeaderboardEntry]:
        best: dict[uuid.UUID, int] = {}
        for row in self.scores:
            best[row["user_id"]] = max(best.get(row["user_id"], 0), row["score"])
        ordered = sorted(best.items(), key=lambda item: (-item[1], str(item[0])))
        entries: list[LeaderboardEntry] = []
        for index, (owner, score) in enumerate(ordered, start=1):
            profile = self.profiles.get(owner)
            if profile is None:
                continue
            if index <= limit or owner == user_id:
                entries.append(
                    LeaderboardEntry(
                        rank_position=index,
                        username=profile.username,
                        best_score=score,
                        is_me=owner == user_id,
                    )
                )
        return entries

    async def locked_until(self, username_lower: str) -> datetime | None:
        row = self.attempts.get(username_lower)
        if not row:
            return None
        locked = row["locked_until"]
        if locked and locked <= datetime.now(timezone.utc):
            self.attempts.pop(username_lower, None)
            return None
        return locked

    async def register_login_failure(
        self, username_lower: str, max_attempts: int, lock_seconds: int
    ) -> datetime | None:
        row = self.attempts.setdefault(username_lower, {"failed": 0, "locked_until": None})
        row["failed"] += 1
        if row["failed"] >= max_attempts:
            row["locked_until"] = datetime.now(timezone.utc) + timedelta(seconds=lock_seconds)
        return row["locked_until"]

    async def clear_login_failures(self, username_lower: str) -> None:
        self.attempts.pop(username_lower, None)

    async def ping(self) -> None:
        return None


class FakeAuthProvider:
    def __init__(self) -> None:
        self.users: dict[str, tuple[uuid.UUID, str]] = {}
        self.refresh_tokens: dict[str, uuid.UUID] = {}
        self.deleted: list[uuid.UUID] = []

    async def create_user(self, *, username: str, password: str) -> uuid.UUID:
        if username.lower() in {name.lower() for name in self.users}:
            raise AuthRejected("duplicate")
        user_id = uuid.uuid4()
        self.users[username.lower()] = (user_id, password)
        return user_id

    async def delete_user(self, user_id: uuid.UUID) -> None:
        self.deleted.append(user_id)
        for name, (stored_id, _) in list(self.users.items()):
            if stored_id == user_id:
                del self.users[name]

    async def password_grant(self, *, username: str, password: str) -> AuthTokens | None:
        entry = self.users.get(username.lower())
        if not entry or entry[1] != password:
            return None
        refresh = f"refresh-{uuid.uuid4().hex}"
        self.refresh_tokens[refresh] = entry[0]
        return AuthTokens(
            access_token="gotrue-access", refresh_token=refresh, expires_in=3600, user_id=entry[0]
        )

    async def refresh(self, refresh_token: str) -> AuthTokens | None:
        user_id = self.refresh_tokens.get(refresh_token)
        if user_id is None:
            return None
        rotated = f"refresh-{uuid.uuid4().hex}"
        self.refresh_tokens.pop(refresh_token, None)
        self.refresh_tokens[rotated] = user_id
        return AuthTokens(
            access_token="gotrue-access", refresh_token=rotated, expires_in=3600, user_id=user_id
        )


@pytest.fixture
def settings() -> Settings:
    return Settings(
        _env_file=None,
        env="dev",
        supabase_url="https://test.supabase.co",
        supabase_project_ref="testref",
        supabase_publishable_key="clave-publica-de-prueba",
        supabase_secret_key="clave-secreta-de-prueba",
        supabase_db_url="postgresql://localhost/test",
        backend_jwt_secret="x" * 40,
        pin_pepper="pepper-pepper-pepper",
        session_master_key="master-master-master",
        log_level="WARNING",
    )


@pytest.fixture
def repo() -> InMemoryRepository:
    return InMemoryRepository()


@pytest.fixture
def provider() -> FakeAuthProvider:
    return FakeAuthProvider()


@pytest.fixture
def client(settings: Settings, repo: InMemoryRepository, provider: FakeAuthProvider):
    app = create_app(settings=settings, repository=repo, auth_provider=provider)
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def registered(client: TestClient) -> dict:
    response = client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    assert response.status_code == 201, response.text
    return response.json()
