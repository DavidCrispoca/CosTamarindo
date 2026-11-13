"""Cliente mínimo para Ollama.

El backend es el único que habla con `Sg-AI`; el navegador nunca ve el puerto
11434. La red `ai` es interna, así que este cliente solo funciona desde un
contenedor conectado a esa red.

El cliente no reintenta: un timeout o un error se traducen a `OllamaError` y el
servicio de diálogo responde con el banco de respaldo. El juego nunca se bloquea
porque la IA esté lenta o apagada.
"""

from __future__ import annotations

import asyncio

import httpx


class OllamaError(Exception):
    """Fallo controlado de la IA. `reason` es corto y seguro para el log."""

    def __init__(self, reason: str) -> None:
        super().__init__(reason)
        self.reason = reason


class OllamaClient:
    def __init__(
        self,
        *,
        base_url: str,
        model: str,
        timeout_seconds: int = 20,
        connect_timeout_seconds: float = 0.75,
        max_concurrency: int = 1,
        num_predict: int = 120,
        temperature: float = 0.8,
        transport: httpx.AsyncBaseTransport | None = None,
    ) -> None:
        self._client = httpx.AsyncClient(
            base_url=base_url.rstrip("/"),
            # El connect timeout es corto: si el contenedor está apagado, el
            # juego debe recibir el fallback en menos de 1 s. La lectura sí
            # puede esperar los 20 s de la generación.
            timeout=httpx.Timeout(timeout_seconds, connect=connect_timeout_seconds),
            transport=transport,
        )
        self._semaphore = asyncio.Semaphore(max(1, max_concurrency))
        self.model = model
        self.num_predict = num_predict
        self.temperature = temperature

    async def generate(self, prompt: str) -> str:
        """Devuelve solo el texto del modelo; nunca el JSON completo."""
        async with self._semaphore:
            try:
                response = await self._client.post(
                    "/api/generate",
                    json={
                        "model": self.model,
                        "prompt": prompt,
                        "stream": False,
                        "keep_alive": "10m",
                        "options": {
                            "temperature": self.temperature,
                            "top_p": 0.9,
                            "num_predict": self.num_predict,
                        },
                    },
                )
                response.raise_for_status()
            except httpx.TimeoutException as exc:
                raise OllamaError("timeout") from exc
            except httpx.HTTPStatusError as exc:
                raise OllamaError("http_error") from exc
            except httpx.RequestError as exc:
                raise OllamaError("unreachable") from exc

        try:
            payload = response.json()
        except ValueError as exc:
            raise OllamaError("bad_response") from exc

        text = payload.get("response") if isinstance(payload, dict) else None
        if not isinstance(text, str) or not text.strip():
            raise OllamaError("empty_response")
        return text.strip()

    async def aclose(self) -> None:
        await self._client.aclose()
