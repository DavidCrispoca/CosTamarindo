from __future__ import annotations

import uuid
from dataclasses import replace
from datetime import datetime, timedelta, timezone

from fastapi.testclient import TestClient

from app.config import Settings
from app.security import canonical_score, derive_session_key, sign

from .conftest import InMemoryRepository


def _auth(token: str) -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


def _start(client: TestClient, token: str) -> dict:
    response = client.post("/api/session/start", headers=_auth(token))
    assert response.status_code == 200, response.text
    return response.json()


def _payload(session: dict, *, score: int, duration_ms: int, catches: int, nonce: str | None = None):
    nonce_value = nonce if nonce is not None else session["nonce"]
    canonical = canonical_score(session["session_id"], score, duration_ms, catches, nonce_value)
    key = bytes.fromhex(session["session_key"])
    return {
        "session_id": session["session_id"],
        "score": score,
        "duration_ms": duration_ms,
        "catches": catches,
        "nonce": nonce_value,
        "signature": sign(key, canonical),
    }


def test_session_start_returns_key_and_nonce(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    assert session["nonce"]
    assert len(session["session_key"]) == 64
    assert session["best_score"] == 0


def test_valid_score_is_accepted(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))

    assert response.status_code == 200, response.text
    body = response.json()
    assert body["score"] == 120
    assert body["best_score"] == 120
    assert body["rank_position"] == 1


def test_tampered_score_is_rejected(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    payload["score"] = 99_999

    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert response.status_code == 400
    assert response.json()["code"] == "bad_signature"


def test_replay_is_rejected(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)

    first = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert first.status_code == 200

    replay = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert replay.status_code == 400
    assert replay.json()["code"] == "session_used"


def test_wrong_nonce_is_rejected(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(
        session, score=100, duration_ms=60_000, catches=8, nonce="nonce-inventado-123456"
    )
    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert response.status_code == 400
    assert response.json()["code"] == "bad_nonce"


def test_expired_session_is_rejected(
    client: TestClient, registered: dict, repo: InMemoryRepository
):
    session = _start(client, registered["access_token"])
    session_id = uuid.UUID(session["session_id"])
    record = repo.sessions[session_id]
    repo.sessions[session_id] = replace(
        record, expires_at=datetime.now(timezone.utc) - timedelta(minutes=1)
    )

    payload = _payload(session, score=100, duration_ms=60_000, catches=8)
    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert response.status_code == 400
    assert response.json()["code"] == "session_expired"


def test_implausible_score_is_rejected(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    # 10 segundos de partida no pueden producir 5000 puntos.
    payload = _payload(session, score=5000, duration_ms=10_000, catches=400)

    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert response.status_code == 400
    assert response.json()["code"] == "implausible_score"


def test_session_from_another_user_is_rejected(client: TestClient, registered: dict):
    other = client.post("/api/auth/register", json={"username": "beto", "pin": "9073"}).json()
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)

    response = client.post("/api/score", json=payload, headers=_auth(other["access_token"]))
    assert response.status_code == 400
    assert response.json()["code"] == "bad_session"


def test_score_requires_authentication(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)

    response = client.post("/api/score", json=payload)
    assert response.status_code == 401


def test_score_rejects_out_of_range_values_at_schema_level(client: TestClient, registered: dict):
    session = _start(client, registered["access_token"])
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)
    payload["catches"] = -1

    response = client.post("/api/score", json=payload, headers=_auth(registered["access_token"]))
    assert response.status_code == 422
    assert response.json()["code"] == "invalid_request"


def test_best_score_is_tracked_across_matches(client: TestClient, registered: dict):
    first = _start(client, registered["access_token"])
    client.post(
        "/api/score",
        json=_payload(first, score=120, duration_ms=60_000, catches=8),
        headers=_auth(registered["access_token"]),
    )
    second = _start(client, registered["access_token"])
    response = client.post(
        "/api/score",
        json=_payload(second, score=80, duration_ms=60_000, catches=6),
        headers=_auth(registered["access_token"]),
    )
    assert response.status_code == 200
    assert response.json()["best_score"] == 120
