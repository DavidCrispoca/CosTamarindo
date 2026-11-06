"""Dialogo del NPC: contrato y defensas de entrada/salida.

Estado actual: devuelve respuestas de respaldo por contexto. La llamada a
Ollama es responsabilidad de la division IA (IA-02 a IA-04) y no se implementa
aqui para no mezclar superficies. El contrato y las defensas ya quedan fijados
para que el frontend pueda integrarse.
"""

from __future__ import annotations

import re
import time
from collections import defaultdict, deque

from ..errors import DomainError
from ..logging_setup import audit

INJECTION_PATTERNS = re.compile(
    r"(ignore\s+(all|previous)|system\s*prompt|you\s+are\s+now|"
    r"olvida\s+(las\s+)?instrucciones|ignora\s+(todo|lo\s+anterior)|"
    r"act[uú]a\s+como|revela\s+(tus\s+)?reglas|jailbreak)",
    re.IGNORECASE,
)

CONTROL_CHARS = re.compile(r"[\x00-\x1f\x7f]")
MAX_REPLY_LENGTH = 300

FALLBACKS: dict[str, str] = {
    "inicio": "¡Bienvenida a la playa! Recoge lo que caiga y esquiva lo que pincha.",
    "captura": "Buena pesca. Cada cosa que atrapas cuenta, pero no todo lo que brilla se come.",
    "peligro": "Eso pica. Tres vidas tienes, no las gastes en el mismo erizo.",
    "game_over": "Se acabó la pesca por hoy. Revisa tu Libro y vuelve a intentarlo.",
    "libro": "El Libro guarda lo que ya conoces. Lo que falta aparece como silueta.",
    "bloqueado": "Esa pregunta no está en mi auditoría. ¿Hablamos de contraseñas?",
    "defecto": "Estoy revisando los registros. Intenta de nuevo en un momento.",
}


class RateLimited(DomainError):
    code = "rate_limited"
    status_code = 429
    message = "Demasiadas peticiones"


class DialogGuard:
    """Rate limit en memoria y saneo de texto.

    El limite en memoria es por proceso. Con una sola instancia del backend es
    suficiente; si se escala horizontalmente debe moverse a la base o a Redis.
    """

    def __init__(self, *, max_requests: int = 10, window_seconds: int = 60) -> None:
        self._max = max_requests
        self._window = window_seconds
        self._hits: dict[str, deque[float]] = defaultdict(deque)

    def check(self, key: str) -> None:
        now = time.monotonic()
        bucket = self._hits[key]
        while bucket and now - bucket[0] > self._window:
            bucket.popleft()
        if len(bucket) >= self._max:
            raise RateLimited()
        bucket.append(now)


def sanitize_input(text: str) -> tuple[str, bool]:
    """Devuelve el texto limpio y si se detecto intento de inyeccion."""
    cleaned = CONTROL_CHARS.sub(" ", text)
    cleaned = cleaned.replace("<", "").replace(">", "").strip()
    if INJECTION_PATTERNS.search(cleaned):
        return cleaned, True
    return cleaned, False


def filter_output(text: str) -> str:
    """TextMeshPro interpreta rich text: se eliminan etiquetas y se acota."""
    cleaned = text.replace("<", "").replace(">", "")
    cleaned = CONTROL_CHARS.sub(" ", cleaned)
    return cleaned[:MAX_REPLY_LENGTH].strip()


def reply_for(context: str | None) -> str:
    return FALLBACKS.get(context or "defecto", FALLBACKS["defecto"])


def build_reply(*, user_id: str, text: str, context: str | None, guard: DialogGuard) -> tuple[str, bool]:
    guard.check(user_id)
    cleaned, injected = sanitize_input(text)
    if injected:
        audit("dialog_blocked", user_id=user_id, length=len(cleaned))
        return reply_for("bloqueado"), True
    # Sin Ollama todavia: respuesta determinista por contexto.
    return reply_for(context), False
