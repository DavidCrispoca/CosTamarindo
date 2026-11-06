from __future__ import annotations

from fastapi.testclient import TestClient


def test_register_returns_token_pair(client: TestClient):
    response = client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    assert response.status_code == 201
    body = response.json()
    assert body["username"] == "ana"
    assert body["access_token"]
    assert body["refresh_token"]
    assert body["expires_in"] > 0


def test_register_rejects_weak_pin(client: TestClient):
    response = client.post("/api/auth/register", json={"username": "ana", "pin": "1234"})
    assert response.status_code == 422
    assert response.json()["code"] == "weak_pin"


def test_register_rejects_duplicate_username_case_insensitive(client: TestClient):
    client.post("/api/auth/register", json={"username": "Ana", "pin": "4821"})
    response = client.post("/api/auth/register", json={"username": "ana", "pin": "7788"})
    assert response.status_code == 409
    assert response.json()["code"] == "username_taken"


def test_register_rejects_invalid_username(client: TestClient):
    response = client.post("/api/auth/register", json={"username": "a b", "pin": "4821"})
    assert response.status_code == 422
    assert response.json()["code"] == "invalid_request"


def test_login_with_correct_pin(client: TestClient):
    client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    response = client.post("/api/auth/login", json={"username": "ana", "pin": "4821"})
    assert response.status_code == 200
    assert response.json()["access_token"]


def test_login_with_wrong_pin_fails(client: TestClient):
    client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    response = client.post("/api/auth/login", json={"username": "ana", "pin": "9073"})
    assert response.status_code == 401
    assert response.json()["code"] == "invalid_credentials"


def test_five_failures_lock_the_account(client: TestClient):
    client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    for _ in range(5):
        response = client.post("/api/auth/login", json={"username": "ana", "pin": "9073"})
        assert response.status_code in (401, 423)

    blocked = client.post("/api/auth/login", json={"username": "ana", "pin": "4821"})
    assert blocked.status_code == 423
    assert blocked.json()["code"] == "account_locked"
    assert blocked.headers.get("Retry-After") is None or blocked.headers["Retry-After"] == "60"


def test_successful_login_resets_failure_counter(client: TestClient):
    client.post("/api/auth/register", json={"username": "ana", "pin": "4821"})
    for _ in range(4):
        client.post("/api/auth/login", json={"username": "ana", "pin": "9073"})

    ok = client.post("/api/auth/login", json={"username": "ana", "pin": "4821"})
    assert ok.status_code == 200

    for _ in range(4):
        response = client.post("/api/auth/login", json={"username": "ana", "pin": "9073"})
        assert response.status_code == 401
    still_allowed = client.post("/api/auth/login", json={"username": "ana", "pin": "4821"})
    assert still_allowed.status_code == 200


def test_refresh_rotates_the_token(client: TestClient):
    body = client.post("/api/auth/register", json={"username": "ana", "pin": "4821"}).json()
    response = client.post("/api/auth/refresh", json={"refresh_token": body["refresh_token"]})
    assert response.status_code == 200
    assert response.json()["refresh_token"] != body["refresh_token"]


def test_refresh_with_unknown_token_fails(client: TestClient):
    response = client.post("/api/auth/refresh", json={"refresh_token": "token-invalido-de-prueba"})
    assert response.status_code == 401
    assert response.json()["code"] == "invalid_token"


def test_logout_requires_token(client: TestClient):
    assert client.post("/api/auth/logout").status_code == 401


def test_logout_with_token_succeeds(client: TestClient):
    body = client.post("/api/auth/register", json={"username": "ana", "pin": "4821"}).json()
    response = client.post(
        "/api/auth/logout", headers={"Authorization": f"Bearer {body['access_token']}"}
    )
    assert response.status_code == 204
