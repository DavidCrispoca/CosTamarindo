"""Errores de dominio y su traduccion a HTTP.

El cliente nunca recibe detalles internos: solo un codigo estable y un mensaje
corto. El detalle completo va al log estructurado con el request_id.
"""

from __future__ import annotations

from fastapi import HTTPException, status


class DomainError(Exception):
    code = "error"
    status_code = status.HTTP_400_BAD_REQUEST
    message = "No se pudo procesar la solicitud"

    def __init__(self, message: str | None = None, **details: object) -> None:
        self.message = message or self.message
        self.details = details
        super().__init__(self.message)


class InvalidCredentials(DomainError):
    code = "invalid_credentials"
    status_code = status.HTTP_401_UNAUTHORIZED
    message = "Usuario o PIN incorrectos"


class AccountLocked(DomainError):
    code = "account_locked"
    status_code = status.HTTP_423_LOCKED
    message = "Cuenta bloqueada temporalmente"


class UsernameTaken(DomainError):
    code = "username_taken"
    status_code = status.HTTP_409_CONFLICT
    message = "Ese usuario ya existe"


class WeakPin(DomainError):
    code = "weak_pin"
    status_code = status.HTTP_422_UNPROCESSABLE_ENTITY
    message = "Ese PIN es demasiado predecible"


class InvalidToken(DomainError):
    code = "invalid_token"
    status_code = status.HTTP_401_UNAUTHORIZED
    message = "Sesion invalida o expirada"


class SessionRejected(DomainError):
    code = "bad_session"
    status_code = status.HTTP_400_BAD_REQUEST
    message = "Sesion de juego invalida"


class ScoreRejected(DomainError):
    code = "score_rejected"
    status_code = status.HTTP_400_BAD_REQUEST
    message = "Puntaje rechazado"

    def __init__(self, reason: str) -> None:
        super().__init__(reason)
        self.reason = reason
        self.code = reason


class UpstreamError(DomainError):
    code = "upstream_error"
    status_code = status.HTTP_502_BAD_GATEWAY
    message = "Servicio de identidad no disponible"


def to_http(exc: DomainError) -> HTTPException:
    return HTTPException(
        status_code=exc.status_code,
        detail={"code": exc.code, "message": exc.message},
    )
