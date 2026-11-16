"""Diálogo del NPC: contrato, guardrails y llamada a Ollama.

El NPC es cosmético. Puede comentar una captura, una vida perdida o una
pregunta del jugador, pero nunca decide vidas, puntaje ni estado de partida.
Si Ollama no está disponible, responde el banco de respaldo local.
"""

from __future__ import annotations

import re
import time
from collections import defaultdict, deque

from ..ai.prompts import build_prompt
from ..errors import DomainError
from ..logging_setup import audit
from ..providers.ollama import OllamaClient, OllamaError

INJECTION_PATTERNS = re.compile(
    r"(ignore\s+(all|previous)|system\s*prompt|you\s+are\s+now|"
    r"olvida\s+(las\s+)?instrucciones|ignora\s+(todo|lo\s+anterior)|"
    r"act[uú]a\s+como|revela\s+(tus\s+)?reglas|jailbreak)",
    re.IGNORECASE,
)

CONTROL_CHARS = re.compile(r"[\x00-\x1f\x7f]")
WHITESPACE = re.compile(r"\s+")
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

# Respaldos por ítem. Se usan solo si Ollama no responde; el texto generado por
# la IA tiene prioridad. Las claves van en minúsculas y sin tildes.
ITEM_FALLBACKS: dict[str, str] = {
    "tamarindo": "¡Qué buenos tamarindos! Como pa' un juguito.",
    "cocada": "Esa cocada sí endulza la partida, mi amor.",
    "pescado": "Buen pescado, pero ojo con las espinas.",
    "frutas": "Frutas frescas, buena cosecha la de hoy.",
    "limonada de coco": "Limonada de coco, refresco seguro para seguir jugando.",
    "erizo": "¡Ay! Ese erizo no era para comer.",
    "medusa": "Esa medusa te dejó sin vida. ¡A esquivar la próxima!",
    "basura": "Eso no se come, eso contamina. Mejor recoge fruta.",
    "bomba": "¡No puede ser! Esa bomba te dañó la cosecha de hoy.",
}


class RateLimited(DomainError):
    code = "rate_limited"
    status_code = 429
    message = "Demasiadas peticiones"


class DialogGuard:
    """Rate limit en memoria y saneo de texto.

    El límite en memoria es por proceso. Con una sola instancia del backend es
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
    """Devuelve el texto limpio y si se detectó intento de inyección."""
    cleaned = CONTROL_CHARS.sub(" ", text or "")
    cleaned = cleaned.replace("<", "").replace(">", "")
    cleaned = WHITESPACE.sub(" ", cleaned).strip()
    if INJECTION_PATTERNS.search(cleaned):
        return cleaned, True
    return cleaned, False


def filter_output(text: str) -> str:
    """TextMeshPro interpreta rich text: se eliminan etiquetas y se acota."""
    cleaned = text.replace("<", "").replace(">", "")
    cleaned = CONTROL_CHARS.sub(" ", cleaned)
    cleaned = WHITESPACE.sub(" ", cleaned)
    return cleaned[:MAX_REPLY_LENGTH].strip()


def reply_for(context: str | None, item: str | None = None) -> str:
    key = (item or "").strip().lower()
    if key in ITEM_FALLBACKS:
        return ITEM_FALLBACKS[key]
    return FALLBACKS.get(context or "defecto", FALLBACKS["defecto"])


async def build_reply(
    *,
    user_id: str,
    text: str,
    context: str | None,
    item: str | None,
    kind: str | None,
    guard: DialogGuard,
    ollama: OllamaClient | None,
) -> tuple[str, bool]:
    """Genera la respuesta o devuelve el respaldo. Nunca lanza por un fallo de IA."""
    guard.check(user_id)

    cleaned, injected = sanitize_input(text)
    cleaned_item, item_injected = sanitize_input(item or "")
    if injected or item_injected:
        audit("dialog_blocked", user_id=user_id, length=len(cleaned))
        return reply_for("bloqueado"), True

    # Un evento de captura puede venir sin texto: el ítem es el mensaje.
    if not cleaned and not cleaned_item:
        return reply_for(context), False

    if ollama is None:
        audit("dialog_fallback", reason="disabled", context=context)
        return reply_for(context, cleaned_item), False

    prompt = build_prompt(
        text=cleaned,
        context=context or "defecto",
        item=cleaned_item or None,
        kind=kind,
    )
    started = time.perf_counter()
    try:
        raw = await ollama.generate(prompt)
    except OllamaError as exc:
        audit("dialog_fallback", reason=exc.reason, context=context)
        return reply_for(context, cleaned_item), False
    except Exception:  # noqa: BLE001 - la IA nunca debe tumbar el juego
        audit("dialog_fallback", reason="unexpected", context=context)
        return reply_for(context, cleaned_item), False

    reply = filter_output(raw)
    if not reply:
        audit("dialog_fallback", reason="empty_response", context=context)
        return reply_for(context, cleaned_item), False

    audit(
        "dialog_ok",
        context=context,
        item=cleaned_item or None,
        latency_ms=round((time.perf_counter() - started) * 1000),
        chars=len(reply),
    )
    return reply, False
