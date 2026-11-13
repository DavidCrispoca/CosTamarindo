# F7 · Nodo de IA y NPC con Ollama — plan por fases

> **División:** IA · **Nodo:** `Sg-AI`
> **Estado:** 🚧 F7.0–F7.3 cerradas; F7.4 implementada pendiente de prueba en Unity/WebGL;
> F7.5 parcial (tests y evidencias listos)
> **Depende de:** [F6](F6.md) (compose), F5 (backend), F4 (auth)
> **Handoff:** [`../handoff/ia.md`](../handoff/ia.md)
> **Backlog:** [`Plan.md` §8](../../Plan.md) (`IA-01`…`IA-07`)

---

## 1. Objetivo

Dejar el NPC de la Palenquera funcionando de punta a punta:

1. `Sg-AI` levanta Ollama con `qwen2.5:3b`, aislado y sin puerto publicado.
2. El backend llama a Ollama con un prompt versionado, timeout y un solo request concurrente.
3. La entrada y la salida pasan por los guardrails; si Ollama falla, responde el banco de respaldo.
4. Unity muestra la respuesta en un bocadillo dentro del juego.
5. La IA **no** decide vidas, puntaje ni estado de la partida.

---

## 2. Punto de partida

| Pieza | Estado real |
| :--- | :--- |
| Servicio `ai` en Compose | ✅ healthy en `10.83.0.2`, red `costamarindo-ai` interna, `mem_limit: 4g` |
| Volumen `costamarindo-ai-models` | ✅ `qwen2.5:3b` (1,9 GB) descargado |
| `POST /api/dialog` | ✅ contrato, guardrails, rate limit, fallback y llamada real a Ollama |
| Prompt del NPC | ✅ `backend/app/ai/prompts.py` |
| Cliente Ollama en backend | ✅ `backend/app/providers/ollama.py` |
| Bocadillo en Unity | ✅ `NpcDialogueController` (runtime UI); ⬜ falta probarlo en WebGL |
| Pruebas del diálogo | ✅ `backend/tests/test_dialog.py`; 53 tests en verde |
| Evidencia F7 | ✅ `evidencias/F7/01`–`05` y `README.md` |

**Estado 2026-11-13.** `Sg-AI` está operativo y el endpoint ya genera texto real. La prueba de
extremo a extremo se hizo con un backend de prueba (repositorio en memoria + JWT firmado) porque
el backend completo sigue bloqueado por las claves de Supabase/JWT. El bocadillo de Unity está
implementado y usa respaldo local si no hay JWT real; falta compilarlo y verlo en WebGL.

---

## 3. Fases

### F7.0 · Contrato, contexto y línea base

**Objetivo:** cerrar el contrato antes de escribir código.

- Confirmar que `POST /api/dialog` conserva `{ "text": "...", "context": "..." }` y
  `{ "reply": "...", "blocked": false }`.
- Definir los contextos permitidos: `inicio`, `captura`, `peligro`, `game_over`, `libro` y
  `defecto`. `bloqueado` es interno y no lo envía el cliente.
- Decidir que `context` sea opcional y tenga `defecto` por defecto, para no romper clientes viejos.
- Registrar la línea base: configuración de Compose, estado del contenedor y latencia actual del
  fallback.

**Entregable:** contrato acordado y documentado en `docs/api.md` §4.

**Puerta:** el contrato no toca `game_sessions`, `scores` ni el flujo de partida.

---

### F7.1 · Levantar `Sg-AI` y descargar el modelo

**Objetivo:** tener el nodo operativo y medir `IA-01` antes de tocar el prompt.

```powershell
# 1. levantar solo la IA
docker compose --env-file .env -f infra/docker-compose.yml --profile ai up -d ai

# 2. descargar el modelo (conecta bridge temporalmente y lo desconecta al terminar)
bash infra/scripts/pull-model.sh

# 3. comprobar modelo y aislamiento
docker exec sg-ai ollama list
docker port sg-ai
docker inspect -f '{{json .NetworkSettings.Networks}}' sg-ai
```

Medir con un prompt fijo:

```powershell
docker exec -it sg-ai ollama run qwen2.5:3b "Responde en una frase: ¿qué es el phishing?"
docker stats --no-stream sg-ai
```

Registrar latencia en frío y en caliente, CPU y RAM. Si `qwen2.5:3b` no cabe o es demasiado
lento, se documenta y se decide si se cambia de modelo antes de seguir.

**Entregable:** `evidencias/F7/01-ia01-linea-base.txt` con las salidas.

**Puerta:** contenedor `healthy`, modelo presente, sin puerto `11434` publicado y sin la red
`bridge` después de la descarga.

---

### F7.2 · Cliente Ollama y prompt versionado

**Objetivo:** conectar el backend con Ollama sin romper el contrato actual (`IA-02`, `IA-04`).

- ✅ `backend/app/ai/prompts.py` con el system prompt de la Palenquera: español, máximo 2 frases,
  solo juego/seguridad, ejemplos de tono y la entrada delimitada como `<jugador>…</jugador>`.
- ✅ `backend/app/providers/ollama.py` con cliente HTTP async, `connect_timeout=0.75 s`,
  `read_timeout=20 s`, semáforo de 1 request concurrente y `num_predict` acotado.
- ✅ Configuración: `OLLAMA_URL=http://sg-ai:11434`, `OLLAMA_MODEL=qwen2.5:3b`,
  `OLLAMA_TIMEOUT_SECONDS=20`, `OLLAMA_MAX_CONCURRENCY=1`.
- ✅ `build_reply` es asíncrono y `routers/dialog.py` espera la llamada.
- ✅ Ante timeout, error HTTP, red inalcanzable o respuesta vacía, devuelve el fallback por
  contexto/ítem.
- ✅ El servicio `backend` no tiene `depends_on` de `ai`: si Ollama está caído, arranca y usa el
  respaldo. La IA nunca bloquea el juego.

**Entregable:** `/api/dialog` responde con Ollama cuando está disponible y con el banco de
respaldo cuando no lo está.

**Puerta:** una petición normal devuelve texto del modelo; una petición con Ollama apagado
sigue respondiendo sin error 500.

> **Prueba real:** se ejecutó con un backend de prueba (repositorio en memoria + JWT firmado)
> para aislar el diálogo de Supabase. Resultados en `evidencias/F7/03-endpoint-real.txt`.

---

### F7.3 · Guardrails, rate limit y degradación

**Objetivo:** cerrar `IA-03`, `IA-04` y `IA-05`.

- ✅ `sanitize_input` y `filter_output` con límite de entrada y validación del contexto.
- ✅ Inyección detectada antes del prompt; se registra `dialog_blocked` sin guardar el texto.
- ✅ Entrada envuelta en `<jugador>…</jugador>`; nunca se concatena como instrucción.
- ✅ Salida limitada a 300 caracteres y sin `<`/`>` para no romper TextMeshPro.
- ✅ 10 peticiones/minuto por usuario y un solo request concurrente al modelo.
- ✅ `dialog_fallback` con motivo corto (`timeout`, `http_error`, `unreachable`,
  `empty_response`, `disabled`) sin filtrar secretos.
- ✅ Fallback real con `sg-ai` detenido: 797/758/757 ms (`evidencias/F7/05-fallback.txt`).

**Entregable:** guardrails conectados al flujo real y eventos de auditoría.

**Puerta:** inyección bloqueada, salida sin rich text, fallback rápido y backend sin caídas.

---

### F7.4 · Contrato y bocadillo en Unity

**Objetivo:** llevar la IA al juego (`IA-07` + integración de Frontend).

- ✅ `context`, `item` y `kind` opcionales en `DialogRequest`, documentados en `docs/api.md` §4.
- ✅ `AuthStub` intenta `/api/auth/login` y `/api/auth/register`, guarda el JWT solo en memoria y
  cae al modo local si el backend no está desplegado.
- ✅ `unity/Assets/Scripts/UI/NpcDialogueController.cs` crea el bocadillo en runtime y se engancha
  a `GameManager.ItemCaught` y `MatchEnded`.
- ✅ Envía `context` `captura`, `peligro` o `game_over`, más `item` y `kind`.
- ✅ Comenta de vez en cuando (35 % de probabilidad, 9 s de enfriamiento), muestra la respuesta
  durante 5,5 s y usa frases locales si no hay JWT o la red falla.
- ✅ No llama a `session` ni a `score`; el puntaje no se toca.

**Entregable:** el jugador ve comentarios de la Palenquera al atrapar ítems y al terminar la
partida, sin bloquear el gameplay.

**Puerta:** funciona en WebGL, el contexto cambia la respuesta y el puntaje no se altera.
**Estado:** código implementado; falta compilar y probar en WebGL. `builds/webgl` sigue vacío.

---

### F7.5 · Pruebas, evidencia y cierre

**Objetivo:** demostrar `IA-06` y cerrar F7.

- ✅ `backend/tests/test_dialog.py` con:
  - respuesta normal con Ollama simulado;
  - inyección bloqueada y evento `dialog_blocked`;
  - timeout y fallback;
  - rate limit;
  - filtro de salida con `<size=500>`;
  - contexto inválido y falta de texto/ítem;
  - prueba de que el diálogo no escribe en `game_sessions` ni en `scores`;
  - cliente Ollama con `httpx.MockTransport`.
- ✅ Suite completa: **53 tests** en verde (`evidencias/F7/04-tests.txt`).
- ✅ Smoke test real con `qwen2.5:3b` (`evidencias/F7/02` y `03`).
- ✅ Fallback real con `sg-ai` detenido: 797/758/757 ms (`evidencias/F7/05-fallback.txt`).
- ⬜ Falta la captura visual del bocadillo en WebGL.
- ✅ `Plan.md` §8, `docs/handoff/ia.md`, `docs/api.md` y este plan actualizados.

**Entregable:** `evidencias/F7/README.md` con el antes/después y los criterios cumplidos.

**Puerta:** los 7 ítems del backlog (`IA-01`…`IA-07`) tienen evidencia y no queda ningún
puerto de IA expuesto. `IA-01`…`IA-06` cerrados; `IA-07` con test; falta evidencia visual.

---

## 4. Matriz de fases

| Fase | Enfoque | Depende de | Evidencia |
| :--- | :--- | :--- | :--- |
| **F7.0** | Contrato y contextos | — | `docs/api.md` §4 |
| **F7.1** | Nodo y modelo | F7.0 | `evidencias/F7/01-*` |
| **F7.2** | Cliente y prompt | F7.1 | salida de `/api/dialog` |
| **F7.3** | Guardrails y fallback | F7.2 | pruebas de inyección/timeout |
| **F7.4** | Unity y bocadillo | F7.0, F7.2 | captura en WebGL |
| **F7.5** | Pruebas y cierre | F7.3, F7.4 | `evidencias/F7/README.md` |

F7.2 y F7.4 pueden avanzar en paralelo después de F7.0/F7.1: el backend usa mock mientras
Unity consume el contrato.

---

## 5. Criterios de aceptación de F7

- [x] `docker exec sg-ai ollama list` muestra `qwen2.5:3b`.
- [x] `sg-ai` no publica `11434` y conserva la red `ai` como `internal`.
- [x] `/api/dialog` responde con el modelo cuando está disponible.
- [x] Inyección bloqueada con `blocked: true` y `dialog_blocked`.
- [x] Ollama apagado devuelve fallback sin error 500 y en < 1 s (797/758/757 ms).
- [x] La salida no contiene `<`/`>` y mide 300 caracteres o menos.
- [ ] Unity muestra el bocadillo y el puntaje no cambia.
- [x] Tests del backend en verde y evidencia en `evidencias/F7/`.
- [x] Cero APIs externas en runtime.

---

## 6. Riesgos y decisiones

| Riesgo | Mitigación |
| :--- | :--- |
| El modelo tarda o no cabe en 4 GB | Medir en F7.1; bajar a un modelo menor o ajustar límites antes de integrar |
| Descarga sin Internet o lenta | `pull-model.sh` deja el volumen persistente; reintentar sin tocar el runtime |
| Backend sin claves de Supabase/JWT | Avanzar con mock; smoke test real cuando `.env` esté completo |
| Inyección o rich text | Guardrails existentes + pruebas explícitas en F7.5 |
| El NPC afecta el juego | Prohibido llamar a `session`/`score`; test dedicado `IA-07` |
| Red interna mal configurada | Verificar `docker port` y `NetworkSettings.Networks` en F7.1 |

---

## 7. Orden recomendado

```text
F7.0  contrato y contextos
  ↓
F7.1  sg-ai + qwen2.5:3b + medición IA-01
  ↓
F7.2  prompt + cliente Ollama + fallback
  ↓
F7.3  guardrails + timeout + rate limit
  ↓
F7.4  Unity: cliente + bocadillo + contexto
  ↓
F7.5  tests + evidencia + cierre
```

La primera acción concreta es **F7.1**: levantar `sg-ai`, descargar el modelo y medir `IA-01`
sin cambiar todavía el prompt ni los guardrails.
