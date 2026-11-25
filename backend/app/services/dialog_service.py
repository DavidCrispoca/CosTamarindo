"""Diálogo del NPC: contrato, guardrails y llamada a Ollama.

El NPC es cosmético. Puede comentar una captura, una vida perdida o una
pregunta del jugador, pero nunca decide vidas, puntaje ni estado de partida.
Si Ollama no está disponible, responde el banco de respaldo local.
"""

from __future__ import annotations

import random
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

FALLBACKS: dict[str, list[str]] = {
    "inicio": [
        "¡Bienvenida a la playa! Recoge lo que caiga y esquiva lo que pincha.",
        "¡A pescar, mi amor! Las frutas caen solitas, pero los erizos no avisan.",
        "Bienvenido a CosTamarindo. Ojo con lo que brilla, que no todo se come.",
        "La marea está buena hoy. Abre los ojos y llena la canasta.",
    ],
    "captura": [
        "Buena pesca. Cada cosa que atrapas cuenta, pero no todo lo que brilla se come.",
        "¡Esa sí fue buena! Sigue así y el Libro se te llena solito.",
        "Captura bien cogida. La cosecha de hoy viene sabrosa.",
        "Buen ojo, mi amor. La costa premia al que no se distrae.",
    ],
    "peligro": [
        "Eso pica. Tres vidas tienes, no las gastes en el mismo erizo.",
        "¡Cuidado con lo que pincha! Que las vidas no vuelven.",
        "Ese bicho no se toca. Esquiva y respira, que hay más fruta.",
        "Ojo, que eso muerde. Mejor lo dejas pasar.",
    ],
    "game_over": [
        "Se acabó la pesca por hoy. Revisa tu Libro y vuelve a intentarlo.",
        "Partida terminada. La playa te espera pa' la revancha.",
        "Se acabó la ronda. Cuenta lo recogido y vuelve cuando quieras.",
        "Hasta aquí llegó la faena. El mar siempre da otra oportunidad.",
    ],
    "libro": [
        "El Libro guarda lo que ya conoces. Lo que falta aparece como silueta.",
        "Abre tu Libro y mira la cosecha. Lo que no conoces está en sombra.",
        "El Libro es tu memoria de la playa. Cada fruta nueva se anota solita.",
        "Revisa tu Libro: ahí vive todo lo que has descubierto.",
    ],
    "bloqueado": [
        "Esa pregunta no está en mi auditoría. ¿Hablamos de contraseñas?",
        "Eso no va por aquí, mi amor. Mejor hablamos de la pesca.",
        "No te sigo con esa. Pregúntame de la playa y te cuento.",
        "Con eso no me metas, que soy de la costa, no de oficina.",
    ],
    "defecto": [
        "Estoy revisando los registros. Intenta de nuevo en un momento.",
        "Dame un momento, que estoy contando la cosecha.",
        "No te oí bien. Vuelve a intentar y te respondo.",
        "El viento se llevó tu pregunta. Repítela despacio.",
    ],
}

# Respaldos por ítem. Se usan solo si Ollama no responde; el texto generado por
# la IA tiene prioridad. Las claves van en minúsculas y sin tildes.
ITEM_FALLBACKS: dict[str, list[str]] = {
    "tamarindo": [
        "¡Qué buenos tamarindos! Como pa' un juguito.",
        "Tamarindo dulce, de esos que alegran la tarde.",
        "Buen tamarindo cayó. Ese es pa' chuparlo despacio.",
        "Tamarindo ácido, perfecto pa' despertar el ánimo.",
    ],
    "cocada": [
        "Esa cocada sí endulza la partida, mi amor.",
        "¡Cocada! Dulce de coco pa' seguir con ganas.",
        "Cocada fresca, se siente el coco desde aquí.",
        "Cocada recién hecha, no hay antojo que le gane.",
    ],
    "pescado": [
        "Buen pescado, pero ojo con las espinas.",
        "Pescado del bueno, fresco como la marea.",
        "Ese pescado va pa' la olla, mi amor.",
        "Pescado plateado, regalo del mar pa' la mesa.",
    ],
    "frutas": [
        "Frutas frescas, buena cosecha la de hoy.",
        "Frutas variadas, la canasta se va llenando.",
        "Frescas y maduras, así es la fruta que cae.",
        "Frutas de colores, el paraíso costeño en la mano.",
    ],
    "limonada de coco": [
        "Limonada de coco, refresco seguro para seguir jugando.",
        "Limonada de coco bien fría, pa' la sed de la partida.",
        "Esa limonada de coco es el premio del día.",
        "Limonada de coco, dulzura que baja el calor.",
    ],
    "coco verde": [
        "Coco verde, agua fresca pa' la faena.",
        "Ese coco verde trae el refresco de la costa.",
        "Coco verde partido, puro líquido de la mañana.",
    ],
    "arepa de huevo": [
        "¡Arepa de huevo! Desayuno de campeones costeños.",
        "Arepa de huevo caliente, no hay mejor compañía.",
        "Arepa de huevo dorada, la gloria de la mañana.",
    ],
    "raspao": [
        "¡Raspao! Hielo y color pa' refrescar la partida.",
        "Raspao de colores, dulce como el viento de la costa.",
        "Raspao bien cargado, pa' aguantar el sol.",
    ],
    "cangrejo": [
        "¡Ay! Ese cangrejo anda de malas pulgas.",
        "Cangrejo bravo, mejor lejos de las manos.",
        "Cangrejo de lado, no se deja agarrar ni de broma.",
    ],
    "erizo": [
        "¡Ay! Ese erizo no era para comer.",
        "Erizo puyudo, ese no se toca ni por el premio.",
        "Erizo de púas, más vale esquiva que recoge.",
    ],
    "medusa": [
        "Esa medusa te dejó sin vida. ¡A esquivar la próxima!",
        "Medusa en el agua, mejor no estirar la mano.",
        "Medusa transparente, traicionera como ella sola.",
    ],
    "basura": [
        "Eso no se come, eso contamina. Mejor recoge fruta.",
        "Basura al agua no, mi amor. A la caneca.",
        "Eso es basura, déjala caer y sigue con lo bueno.",
    ],
    "bomba": [
        "¡No puede ser! Esa bomba te dañó la cosecha de hoy.",
        "¡Bomba! Eso sí que no se recoge.",
        "Bomba en la playa, corre antes de que truene.",
    ],
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
        return random.choice(ITEM_FALLBACKS[key])
    pool = FALLBACKS.get(context or "defecto", FALLBACKS["defecto"])
    return random.choice(pool)


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
