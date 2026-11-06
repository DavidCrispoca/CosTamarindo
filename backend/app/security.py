"""Criptografia y derivacion de material sensible (A.8.24, A.8.5).

Dos ideas sostienen este modulo:

1. El PIN nunca llega a Supabase Auth en claro. Se deriva con un pimiento
   (PIN_PEPPER) que solo vive en el servidor. Si la base se filtra, los hashes
   de GoTrue no se pueden atacar por fuerza bruta de 10.000 combinaciones sin
   conocer el pimiento.

2. La clave HMAC de la sesion no se guarda. Se deriva de
   (SESSION_MASTER_KEY, session_id, nonce). Una copia de la base no permite
   firmar un puntaje.
"""

from __future__ import annotations

import base64
import hashlib
import hmac
import secrets
import uuid
from datetime import datetime, timedelta, timezone
from typing import Any

import jwt

TOKEN_ALGORITHM = "HS256"
TOKEN_ISSUER = "costamarindo"
TOKEN_AUDIENCE = "costamarindo-api"


def _b64url(raw: bytes) -> str:
    return base64.urlsafe_b64encode(raw).rstrip(b"=").decode("ascii")


def derive_pin_password(pin: str, username: str, pepper: str) -> str:
    """Convierte el PIN de 4 digitos en la credencial que guarda GoTrue."""
    message = f"{username.strip().lower()}:{pin}".encode()
    digest = hmac.new(pepper.encode(), message, hashlib.sha256).digest()
    return _b64url(digest)


def derive_session_key(master_key: str, session_id: str, nonce: str) -> bytes:
    """Clave HMAC de una sesion concreta, derivada y no almacenada."""
    message = f"{session_id}:{nonce}".encode()
    return hmac.new(master_key.encode(), message, hashlib.sha256).digest()


def canonical_score(session_id: str, score: int, duration_ms: int, catches: int, nonce: str) -> str:
    """Cadena canonica firmada. El cliente y el servidor deben construirla igual."""
    return f"{session_id}|{score}|{duration_ms}|{catches}|{nonce}"


def sign(session_key: bytes, canonical: str) -> str:
    return hmac.new(session_key, canonical.encode(), hashlib.sha256).hexdigest()


def verify_signature(session_key: bytes, canonical: str, signature: str) -> bool:
    if not isinstance(signature, str) or len(signature) != 64:
        return False
    return hmac.compare_digest(sign(session_key, canonical), signature.lower())


def new_nonce() -> str:
    return secrets.token_urlsafe(18)


# PIN de 4 digitos es de baja entropia por diseño (10.000 combinaciones), asi que
# la defensa real es el bloqueo progresivo. Aun asi se rechazan los valores que
# un atacante prueba primero; el coste es cero y quita el caso trivial.
WEAK_PINS: frozenset[str] = frozenset(
    {
        "0000", "1111", "2222", "3333", "4444", "5555", "6666", "7777", "8888", "9999",
        "1234", "4321", "0123", "3210", "1212", "2121", "1122", "6969", "2000", "2001",
        "2024", "2025", "2026", "1010", "0101", "1990", "1991", "2580", "0852", "1470",
    }
)


def is_weak_pin(pin: str) -> bool:
    if pin in WEAK_PINS:
        return True
    digits = [int(char) for char in pin]
    ascending = all(digits[i] + 1 == digits[i + 1] for i in range(len(digits) - 1))
    descending = all(digits[i] - 1 == digits[i + 1] for i in range(len(digits) - 1))
    return ascending or descending


def create_access_token(
    *,
    secret: str,
    user_id: uuid.UUID | str,
    username: str,
    env: str,
    ttl_minutes: int,
) -> tuple[str, int]:
    now = datetime.now(timezone.utc)
    expires = now + timedelta(minutes=ttl_minutes)
    payload: dict[str, Any] = {
        "iss": TOKEN_ISSUER,
        "aud": TOKEN_AUDIENCE,
        "sub": str(user_id),
        "username": username,
        "env": env,
        "iat": int(now.timestamp()),
        "exp": int(expires.timestamp()),
        "jti": secrets.token_hex(8),
    }
    token = jwt.encode(payload, secret, algorithm=TOKEN_ALGORITHM)
    return token, int((expires - now).total_seconds())


class TokenError(Exception):
    """Token ausente, mal firmado, expirado o de otro entorno."""


def decode_access_token(*, secret: str, token: str, expected_env: str) -> dict[str, Any]:
    try:
        payload = jwt.decode(
            token,
            secret,
            algorithms=[TOKEN_ALGORITHM],
            audience=TOKEN_AUDIENCE,
            issuer=TOKEN_ISSUER,
            options={"require": ["exp", "iat", "sub", "aud", "iss"]},
        )
    except jwt.PyJWTError as exc:
        raise TokenError("invalid_token") from exc

    if payload.get("env") != expected_env:
        # A.8.31: un token de DEV no sirve contra PROD ni al reves.
        raise TokenError("wrong_environment")
    return payload
