from __future__ import annotations

import uuid

import pytest

from app.security import (
    TokenError,
    canonical_score,
    create_access_token,
    decode_access_token,
    derive_pin_password,
    derive_session_key,
    is_weak_pin,
    sign,
    verify_signature,
)

SECRET = "s" * 40


def test_pin_derivation_is_deterministic_and_user_bound():
    first = derive_pin_password("4821", "ana", "pepper")
    second = derive_pin_password("4821", "ana", "pepper")
    other_user = derive_pin_password("4821", "beto", "pepper")
    other_pepper = derive_pin_password("4821", "ana", "otro-pepper")

    assert first == second
    assert first != other_user
    assert first != other_pepper
    assert len(first) >= 6  # GoTrue exige un minimo de 6 caracteres


def test_session_key_depends_on_session_and_nonce():
    key = derive_session_key("master", "session-1", "nonce-1")
    assert key != derive_session_key("master", "session-1", "nonce-2")
    assert key != derive_session_key("master", "session-2", "nonce-1")
    assert len(key) == 32


def test_signature_roundtrip_and_tampering():
    key = derive_session_key("master", "s1", "n1")
    canonical = canonical_score("s1", 120, 60_000, 8, "n1")
    signature = sign(key, canonical)

    assert verify_signature(key, canonical, signature)
    assert not verify_signature(key, canonical, "0" * 64)
    assert not verify_signature(key, canonical[:-1] + "9", signature)
    assert not verify_signature(key, canonical, "zz")


def test_token_roundtrip():
    user_id = uuid.uuid4()
    token, expires_in = create_access_token(
        secret=SECRET, user_id=user_id, username="ana", env="dev", ttl_minutes=30
    )
    payload = decode_access_token(secret=SECRET, token=token, expected_env="dev")

    assert payload["sub"] == str(user_id)
    assert payload["username"] == "ana"
    assert payload["env"] == "dev"
    assert 1700 < expires_in <= 1800


def test_token_from_other_environment_is_rejected():
    token, _ = create_access_token(
        secret=SECRET, user_id=uuid.uuid4(), username="ana", env="dev", ttl_minutes=5
    )
    with pytest.raises(TokenError):
        decode_access_token(secret=SECRET, token=token, expected_env="prod")


def test_expired_token_is_rejected():
    token, _ = create_access_token(
        secret=SECRET, user_id=uuid.uuid4(), username="ana", env="dev", ttl_minutes=-1
    )
    with pytest.raises(TokenError):
        decode_access_token(secret=SECRET, token=token, expected_env="dev")


def test_token_signed_with_other_secret_is_rejected():
    token, _ = create_access_token(
        secret="o" * 40, user_id=uuid.uuid4(), username="ana", env="dev", ttl_minutes=5
    )
    with pytest.raises(TokenError):
        decode_access_token(secret=SECRET, token=token, expected_env="dev")


@pytest.mark.parametrize("pin", ["0000", "1234", "4321", "9999", "2026"])
def test_weak_pins_are_detected(pin: str):
    assert is_weak_pin(pin)


@pytest.mark.parametrize("pin", ["4821", "9073", "1357", "2718"])
def test_strong_pins_pass(pin: str):
    assert not is_weak_pin(pin)
