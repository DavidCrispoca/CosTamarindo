# F7 · Nodo de IA y NPC con Ollama — plan por fases

> **División:** IA · **Nodo:** `Sg-AI`
> **Estado:** ⬜ plan definido; nodo todavía no iniciado
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
| Servicio `ai` en Compose | ✅ definido con perfil `ai`, red interna y `mem_limit: 4g`; ⬜ no iniciado |
| Volumen `costamarindo-ai-models` | ⬜ vacío (4 KB) |
| `POST /api/dialog` | ✅ contrato, saneo, rate limit y fallback; ⬜ no llama a Ollama |
| Prompt del NPC | ⬜ no existe |
| Cliente Ollama en backend | ⬜ no existe |
| Bocadillo en Unity | ⬜ no existe |
| Pruebas del diálogo | ⬜ no existen |
| Evidencia F7 | ⬜ no existe |

**Bloqueo conocido:** el backend no arranca completo hasta tener las claves de Supabase/JWT en
`.env`. La IA se puede levantar y probar por separado; la prueba real de `/api/dialog` queda
para cuando esas claves estén disponibles.

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

- Crear `backend/app/ai/prompts.py` con el system prompt fijo de la Palenquera:
  español, máximo 3 frases, solo seguridad, sin datos personales y con la entrada delimitada
  como `<jugador>…</jugador>`.
- Crear `backend/app/providers/ollama.py` con un cliente HTTP async, timeout de 20 s,
  semáforo de 1 request concurrente y `num_predict` acotado.
- Añadir configuración: `OLLAMA_URL=http://ai:11434`, `OLLAMA_MODEL=qwen2.5:3b`,
  `OLLAMA_TIMEOUT_SECONDS=20`.
- Hacer `build_reply` asíncrono y esperar la llamada desde `routers/dialog.py`.
- Ante timeout, error HTTP o modelo ausente, devolver el fallback por contexto.

**Entregable:** `/api/dialog` responde con Ollama cuando está disponible y con el banco de
respaldo cuando no lo está.

**Puerta:** una petición normal devuelve texto del modelo; una petición con Ollama apagado
sigue respondiendo sin error 500.

> **Bloqueo:** la prueba real necesita las claves de Supabase/JWT del backend. Mientras falten,
> se implementa y se prueba con mock; el smoke test real queda pendiente.

---

### F7.3 · Guardrails, rate limit y degradación

**Objetivo:** cerrar `IA-03`, `IA-04` y `IA-05`.

- Mantener `sanitize_input` y `filter_output`; añadir límite de entrada y validación del contexto.
- Detectar inyección antes de construir el prompt y registrar `dialog_blocked` sin guardar el
  texto del jugador.
- Envolver la entrada con delimitadores explícitos; nunca concatenarla como instrucción.
- Limitar la salida a 300 caracteres y eliminar `<`/`>` para no romper TextMeshPro.
- Mantener 10 peticiones/minuto por usuario y un solo request concurrente al modelo.
- Registrar `dialog_fallback` con un motivo corto (`timeout`, `http_error`, `model_missing`,
  `rate_limited`) sin filtrar secretos.
- Verificar que el fallback responde en menos de 1 s.

**Entregable:** guardrails conectados al flujo real y eventos de auditoría.

**Puerta:** inyección bloqueada, salida sin rich text, fallback rápido y backend sin caídas.

---

### F7.4 · Contrato y bocadillo en Unity

**Objetivo:** llevar la IA al juego (`IA-07` + integración de Frontend).

- Añadir `context` opcional a `DialogRequest` y documentarlo en `docs/api.md`.
- Crear `unity/Assets/Scripts/UI/DialogClient.cs` para llamar a `/api/dialog` con el JWT.
- Crear el controlador del bocadillo (`DialogBubble.cs` o `NpcDialogController.cs`) y conectarlo
  al `GameManager`/`HudController`.
- Enviar el contexto según el estado: inicio, captura, peligro, game over o libro.
- Mostrar la respuesta, cerrar el bocadillo y usar el fallback local si la red falla.
- No llamar a `session` ni a `score` desde el diálogo; no modificar el puntaje.

**Entregable:** el jugador escribe una pregunta, ve la respuesta en el bocadillo y puede seguir
jugando.

**Puerta:** funciona en WebGL, el contexto cambia la respuesta y el puntaje no se altera.

---

### F7.5 · Pruebas, evidencia y cierre

**Objetivo:** demostrar `IA-06` y cerrar F7.

- Crear `backend/tests/test_dialog.py` con:
  - respuesta normal con Ollama simulado;
  - inyección bloqueada y evento `dialog_blocked`;
  - timeout y Ollama apagado;
  - rate limit;
  - filtro de salida con `<size=500>`;
  - contexto inválido o ausente;
  - prueba de que el diálogo no escribe en `game_sessions` ni en `scores`.
- Ejecutar la suite completa y el smoke test real cuando el backend tenga claves.
- Guardar en `evidencias/F7/` las salidas literales, la latencia y la captura del bocadillo.
- Actualizar `Plan.md` §8, `docs/handoff/ia.md`, `README.md` y el estado de F7.

**Entregable:** `evidencias/F7/README.md` con el antes/después y los criterios cumplidos.

**Puerta:** los 7 ítems del backlog (`IA-01`…`IA-07`) tienen evidencia y no queda ningún
puerto de IA expuesto.

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

- [ ] `docker exec sg-ai ollama list` muestra `qwen2.5:3b`.
- [ ] `sg-ai` no publica `11434` y conserva la red `ai` como `internal`.
- [ ] `/api/dialog` responde con el modelo cuando está disponible.
- [ ] Inyección bloqueada con `blocked: true` y `dialog_blocked`.
- [ ] Ollama apagado devuelve fallback en menos de 1 s.
- [ ] La salida no contiene `<`/`>` y mide 300 caracteres o menos.
- [ ] Unity muestra el bocadillo y el puntaje no cambia.
- [ ] Tests del backend en verde y evidencia en `evidencias/F7/`.
- [ ] Cero APIs externas en runtime.

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
