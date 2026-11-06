from __future__ import annotations

import math

from fastapi.testclient import TestClient

from app.security import canonical_score, sign


def _auth(token: str) -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


def _play(client: TestClient, username: str, score: int, duration_ms: int = 60_000) -> None:
    """Registra a un jugador y envia un puntaje firmado valido."""
    client.post("/api/auth/register", json={"username": username, "pin": "4821"})
    login = client.post("/api/auth/login", json={"username": username, "pin": "4821"}).json()
    token = login["access_token"]
    session = client.post("/api/session/start", headers=_auth(token)).json()

    # El techo por captura (25 pts) obliga a que el numero de capturas sea coherente.
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
        headers=_auth(token),
    )
    assert response.status_code == 200, response.text


def test_leaderboard_is_empty_without_scores(client: TestClient):
    response = client.get("/api/leaderboard")
    assert response.status_code == 200
    assert response.json()["entries"] == []


def test_leaderboard_shows_top_five_and_marks_the_player(client: TestClient):
    scores = [("ana", 120), ("beto", 300), ("caro", 240), ("dani", 60), ("eva", 180), ("fabi", 90)]
    for username, score in scores:
        _play(client, username, score)

    login = client.post("/api/auth/login", json={"username": "dani", "pin": "4821"}).json()
    response = client.get("/api/leaderboard", headers=_auth(login["access_token"]))
    assert response.status_code == 200
    entries = response.json()["entries"]

    positions = [(entry["username"], entry["best_score"]) for entry in entries if not entry["is_me"]]
    assert positions == [("beto", 300), ("caro", 240), ("eva", 180), ("ana", 120), ("fabi", 90)]

    mine = [entry for entry in entries if entry["is_me"]]
    assert len(mine) == 1
    assert mine[0]["username"] == "dani"
    assert mine[0]["rank_position"] == 6


def test_leaderboard_without_token_has_no_me_flag(client: TestClient):
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
