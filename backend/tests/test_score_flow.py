from __future__ import annotations

import math
import uuid
from dataclasses import replace
from datetime import datetime, timedelta, timezone

from fastapi.testclient import TestClient

from app.security import canonical_score, sign

from .conftest import InMemoryRepository


def _start(client: TestClient) -> dict:
    response = client.post("/api/session/start")
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


def test_session_start_is_anonymous(client: TestClient):
    session = _start(client)
    assert session["nonce"]
    assert len(session["session_key"]) == 64
    assert session["best_score"] == 0


def test_valid_score_is_accepted_anonymously(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    response = client.post("/api/score", json=payload)

    assert response.status_code == 200, response.text
    body = response.json()
    assert body["score"] == 120
    assert body["best_score"] == 0
    assert body["rank_position"] is None
    assert body["in_top5"] is True  # leaderboard vacio: cualquier puntaje califica


def test_score_does_not_require_authentication(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)
    response = client.post("/api/score", json=payload)
    assert response.status_code == 200


def test_tampered_score_is_rejected(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    payload["score"] = 99_999

    response = client.post("/api/score", json=payload)
    assert response.status_code == 400
    assert response.json()["code"] == "bad_signature"


def test_replay_is_rejected(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)

    first = client.post("/api/score", json=payload)
    assert first.status_code == 200

    replay = client.post("/api/score", json=payload)
    assert replay.status_code == 400
    assert replay.json()["code"] == "session_used"


def test_wrong_nonce_is_rejected(client: TestClient):
    session = _start(client)
    payload = _payload(
        session, score=100, duration_ms=60_000, catches=8, nonce="nonce-inventado-123456"
    )
    response = client.post("/api/score", json=payload)
    assert response.status_code == 400
    assert response.json()["code"] == "bad_nonce"


def test_expired_session_is_rejected(client: TestClient, repo: InMemoryRepository):
    session = _start(client)
    session_id = uuid.UUID(session["session_id"])
    record = repo.sessions[session_id]
    repo.sessions[session_id] = replace(
        record, expires_at=datetime.now(timezone.utc) - timedelta(minutes=1)
    )

    payload = _payload(session, score=100, duration_ms=60_000, catches=8)
    response = client.post("/api/score", json=payload)
    assert response.status_code == 400
    assert response.json()["code"] == "session_expired"


def test_implausible_score_is_rejected(client: TestClient):
    session = _start(client)
    # 10 segundos de partida no pueden producir 5000 puntos.
    payload = _payload(session, score=5000, duration_ms=10_000, catches=400)

    response = client.post("/api/score", json=payload)
    assert response.status_code == 400
    assert response.json()["code"] == "implausible_score"


def test_score_rejects_out_of_range_values_at_schema_level(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=100, duration_ms=60_000, catches=8)
    payload["catches"] = -1

    response = client.post("/api/score", json=payload)
    assert response.status_code == 422
    assert response.json()["code"] == "invalid_request"


# --- reclamo de nombre ----------------------------------------------------

def _claim(client: TestClient, session: dict, username: str, pin: str = "4821"):
    return client.post(
        "/api/claim",
        json={"session_id": session["session_id"], "username": username, "pin": pin},
    )


def test_claim_new_name_assigns_score_and_ranks(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    client.post("/api/score", json=payload)

    response = _claim(client, session, "ana")
    assert response.status_code == 200, response.text
    body = response.json()
    assert body["username"] == "ana"
    assert body["best_score"] == 120
    assert body["rank_position"] == 1


def test_claim_accepts_6_digit_pin(client: TestClient):
    session = _start(client)
    payload = _payload(session, score=120, duration_ms=60_000, catches=8)
    client.post("/api/score", json=payload)

    response = _claim(client, session, "ana", pin="482150")
    assert response.status_code == 200, response.text


def test_claim_rejects_not_top5_without_creating_profile(client: TestClient, repo: InMemoryRepository):
    for name, score in [("aaa", 500), ("bbb", 400), ("ccc", 300), ("ddd", 200), ("eee", 100)]:
        s = _start(client)
        catches = max(8, math.ceil(score / 25))
        client.post(
            "/api/score",
            json=_payload(s, score=score, duration_ms=60_000, catches=catches),
        )
        assert _claim(client, s, name).status_code == 200

    before = len(repo.profiles)

    session = _start(client)
    client.post("/api/score", json=_payload(session, score=10, duration_ms=60_000, catches=8))
    response = _claim(client, session, "zzz")
    assert response.status_code == 400
    assert response.json()["code"] == "not_top5"

    # Un reclamo rechazado no debe crear el perfil.
    assert len(repo.profiles) == before


def test_claim_with_wrong_pin_on_existing_name_fails(client: TestClient):
    s = _start(client)
    client.post("/api/score", json=_payload(s, score=120, duration_ms=60_000, catches=8))
    assert _claim(client, s, "ana", pin="4821").status_code == 200

    s2 = _start(client)
    client.post("/api/score", json=_payload(s2, score=140, duration_ms=60_000, catches=8))
    response = _claim(client, s2, "ana", pin="9999")
    assert response.status_code == 401
    assert response.json()["code"] == "invalid_credentials"


def test_claim_cannot_reclaim_same_score(client: TestClient):
    session = _start(client)
    client.post("/api/score", json=_payload(session, score=120, duration_ms=60_000, catches=8))
    assert _claim(client, session, "ana").status_code == 200

    response = _claim(client, session, "beto")
    assert response.status_code == 400
    assert response.json()["code"] == "bad_session"


def test_claim_updates_existing_name_best_score(client: TestClient):
    s = _start(client)
    client.post("/api/score", json=_payload(s, score=120, duration_ms=60_000, catches=8))
    assert _claim(client, s, "ana", pin="4821").status_code == 200

    s2 = _start(client)
    client.post("/api/score", json=_payload(s2, score=180, duration_ms=60_000, catches=8))
    response = _claim(client, s2, "ana", pin="4821")
    assert response.status_code == 200
    assert response.json()["best_score"] == 180
