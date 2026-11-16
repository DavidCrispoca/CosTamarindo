from __future__ import annotations

import math

from fastapi.testclient import TestClient

from app.security import canonical_score, sign


def _play(client: TestClient, username: str, score: int, duration_ms: int = 60_000) -> None:
    """Juega anonimo y reclama el nombre con un PIN valido."""
    session = client.post("/api/session/start").json()

    catches = max(8, math.ceil(score / 25))
    canonical = canonical_score(session["session_id"], score, duration_ms, catches, session["nonce"])
    key = bytes.fromhex(session["session_key"])
    response = client.post(
        "/api/score",
        json={
            "session_id": session["session_id"],
            "score": score,
            "duration_ms": duration_ms,
            "catches": catches,
            "nonce": session["nonce"],
            "signature": sign(key, canonical),
        },
    )
    assert response.status_code == 200, response.text

    claim = client.post(
        "/api/claim",
        json={"session_id": session["session_id"], "username": username, "pin": "4821"},
    )
    assert claim.status_code == 200, claim.text


def test_leaderboard_is_empty_without_scores(client: TestClient):
    response = client.get("/api/leaderboard")
    assert response.status_code == 200
    assert response.json()["entries"] == []


def test_leaderboard_shows_top_five_claimed_names(client: TestClient):
    scores = [("ana", 120), ("beto", 300), ("caro", 240), ("dani", 60), ("eva", 180), ("fabi", 90)]
    for username, score in scores:
        _play(client, username, score)

    response = client.get("/api/leaderboard")
    assert response.status_code == 200
    entries = response.json()["entries"]

    positions = [(entry["username"], entry["best_score"]) for entry in entries]
    assert positions == [("beto", 300), ("caro", 240), ("eva", 180), ("ana", 120), ("fabi", 90)]


def test_leaderboard_excludes_anonymous_scores(client: TestClient):
    # Un puntaje anonimo (sin reclamar) no aparece en la tabla.
    session = client.post("/api/session/start").json()
    score = 999
    duration_ms = 90_000
    catches = 40
    canonical = canonical_score(session["session_id"], score, duration_ms, catches, session["nonce"])
    key = bytes.fromhex(session["session_key"])
    resp = client.post(
        "/api/score",
        json={
            "session_id": session["session_id"],
            "score": score,
            "duration_ms": duration_ms,
            "catches": catches,
            "nonce": session["nonce"],
            "signature": sign(key, canonical),
        },
    )
    assert resp.status_code == 200, resp.text

    response = client.get("/api/leaderboard")
    assert response.json()["entries"] == []


def test_leaderboard_has_no_me_flag_without_token(client: TestClient):
    _play(client, "ana", 120)
    response = client.get("/api/leaderboard")
    entries = response.json()["entries"]
    assert entries[0]["username"] == "ana"
    assert entries[0]["is_me"] is False


def test_health_reports_database_status(client: TestClient):
    response = client.get("/api/health")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"
    assert response.json()["database"] is True
