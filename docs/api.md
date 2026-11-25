# API de CosTamarindo

> **División:** Backend · **Nodo:** `Sg-Backend`
> **Versión:** 2.0.0 · **Base:** `https://juego.local/api` (mismo origen que el juego)
> **Estado:** flujo anónimo + reclamo top-5 implementado y **verificado contra Supabase DEV** con la
> migración `004_anonymous_claim.sql` aplicada (E2E real en `evidencias/F7/17-e2e-real.md`).

El cliente Unity **nunca** habla con Supabase. Habla solo con esta API, que es la
única que conoce la clave secreta, la base y la IA.

---

## 1. Convenciones

| Tema | Regla |
| :--- | :--- |
| Formato | JSON, `Content-Type: application/json`, UTF-8 |
| Juego/puntaje | **Anónimo**: `/api/session/start` y `/api/score` no requieren token |
| Autenticación (legado/admin) | `Authorization: Bearer <jwt>` emitido por el backend (HS256, TTL 30 min) para endpoints que lo requieran (p.ej. logout/refresh si se usan) |
| Errores | `{"code": "<estable>", "message": "<corto>"}`; nunca detalles internos |
| Identificador de petición | cabecera de respuesta `X-Request-ID` |
| Rate limit | Nginx en `/api/` + bloqueo progresivo en login/claim + guard en diálogo |
| Tamaño máximo de cuerpo | 8 kB (Nginx) |

El JWT incluye `iss=costamarindo`, `aud=costamarindo-api`, `sub=<user_id>`,
`username`, `env` y `exp`. Un token emitido en `dev` se rechaza en `prod` y al revés
(A.8.31).

---

## 2. Identidad y reclamo (top 5)

No hay pantalla de login antes de jugar. El juego es **anónimo**. Solo si el puntaje
entra al **top 5 global** de puntajes reclamados, el cliente ofrece reclamarlo con
un **nombre público + PIN**:

- **Nombre** (`^[A-Za-z0-9_]{3,16}$`, sin distinguir mayúsculas): público, único
  globalmente y visible para todos. No existen dos nombres iguales.
- **PIN** (`^[0-9]{4,6}$`): privado. Protege el nombre: volver a usar ese nombre
  (para reclamar otro puntaje) exige el mismo PIN. Se rechazan PIN predecibles
  (repetidos, secuenciales, comunes).
- Si el nombre es nuevo se crea con ese PIN; si ya existe, el PIN se verifica con
  bloqueo progresivo (5 fallos consecutivos → 60 s, `Retry-After`). El contador vive
  en `public.login_attempts`.

> Los endpoints `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh` y
> `/api/auth/logout` siguen disponibles internamente/compatibilidad, pero el flujo de
> juego no los requiere: la identidad se resuelve en `/api/claim`. Reglas de formato:
> usuario `^[A-Za-z0-9_]{3,16}$`; PIN 4–6 dígitos, no predecible.

---

## 3. Partida, puntaje y reclamo

### 3.1 `POST /api/session/start`

**Sin autenticación.** Crea una sesión anónima.

```json
{
  "session_id": "uuid",
  "nonce": "…",
  "session_key": "hex de 64 caracteres",
  "expires_in": 900,
  "best_score": 0
}
```

`session_key` **no se almacena**: el backend la deriva con `SESSION_MASTER_KEY` a partir
de `(session_id, nonce)`. El cliente la mantiene solo en memoria durante la partida.

### 3.2 `POST /api/score`

**Sin autenticación.** El puntaje se guarda de forma anónima (sin nombre).

```json
{
  "session_id": "uuid",
  "score": 180,
  "duration_ms": 60000,
  "catches": 10,
  "nonce": "<el de la sesión>",
  "signature": "<hmac-sha256 hex>"
}
```

Cadena canónica, exactamente:

```
session_id|score|duration_ms|catches|nonce
```

`signature = hex(HMAC-SHA256(bytes.fromhex(session_key), cadena))`.

| `code` | Significado |
| :--- | :--- |
| `bad_session` | Sesión inexistente |
| `session_used` | Ya se envió un puntaje con esa sesión (incluye replay) |
| `session_expired` | Pasaron los 15 min de TTL |
| `bad_nonce` | El nonce no coincide con el de la sesión |
| `bad_signature` | La firma no valida |
| `implausible_score` | Supera los techos de plausibilidad |

Techos: `score ≤ segundos × 12`, `score ≤ catches × 25`, `catches ≤ segundos × 3`.
Se aplican además los `check` de la tabla.

Respuesta aceptada:

```json
{ "ok": true, "score": 180, "best_score": 0, "rank_position": null, "in_top5": true }
```

`in_top5` indica si el puntaje entra en el top 5 global de puntajes **reclamados** en
ese momento. Si es `true`, el cliente debe ofrecer la pantalla de nombre+PIN y llamar
a `/api/claim`; si es `false`, no se muestra login/claim y el puntaje no queda
asociado a ningún nombre.

### 3.3 `POST /api/claim`

Reclama el puntaje anónimo de una sesión con nombre + PIN. Solo procede si el puntaje
sigue en el top 5 y la sesión no fue reclamada.

```json
{ "session_id": "uuid", "username": "ana", "pin": "482150" }
```

`pin` admite 4 a 6 dígitos. Si el nombre es nuevo se crea; si ya existe se verifica
el PIN (mismo bloqueo progresivo que el login: `401 invalid_credentials`,
`423 account_locked` con `Retry-After`).

| `code` | Significado |
| :--- | :--- |
| `not_top5` | El puntaje ya no está en el top 5 (el ranking cambió) |
| `bad_session` | Sesión/puntaje inexistente o ya reclamado |
| `invalid_credentials` | PIN incorrecto para un nombre existente |
| `account_locked` | Nombre bloqueado temporalmente por intentos fallidos |
| `weak_pin` | PIN predecible (nombre nuevo) |
| `username_taken` | Conflicto de nombre (caso de carrera) |

Respuesta:

```json
{ "ok": true, "username": "ana", "best_score": 180, "rank_position": 1 }
```

### 3.4 `GET /api/leaderboard?limit=5`

Público. Devuelve el top de puntajes **reclamados** (con nombre). No hay token de
jugador en el flujo anónimo, por lo que `is_me` será `false`.

```json
{
  "entries": [
    { "rank_position": 1, "username": "beto", "best_score": 300, "is_me": false }
  ]
}
```

---

## 4. Diálogo

### 4.1 `POST /api/dialog`

Autenticación **opcional** (`Bearer`). Sirve para una pregunta del jugador o para un evento de la partida (funciona también en partidas anónimas). Si hay sesión autenticada se asocia al usuario; si no, el límite de tasa se aplica por cliente.

```json
{ "text": "¿qué es el phishing?", "context": "defecto" }
```

```json
{ "context": "captura", "item": "Tamarindo", "kind": "good" }
```

```json
{ "context": "game_over", "text": "La partida terminó" }
```

```json
{ "reply": "…", "blocked": false }
```

- `text` es opcional si se envía `item`; juntos no pueden superar 200 caracteres de texto
  y 40 de nombre de ítem. En `game_over` no hay ítem, así que el cliente envía un texto de evento.
- `context` admite `inicio`, `captura`, `peligro`, `game_over`, `libro` y `defecto`.
- `kind` admite `good`, `danger` y `trash`; se usa para que el comentario sea pertinente.
- Máximo 10 peticiones por minuto por usuario o cliente anónimo.
- Se eliminan `<` y `>` y los caracteres de control antes de cualquier uso.
- Un patrón de inyección devuelve `blocked: true`, se registra como `dialog_blocked` y
  responde con el texto de respaldo.
- La entrada del jugador se envuelve en `<jugador>…</jugador>` y se trata como dato, nunca
  como instrucción.
- El backend llama a Ollama en `Sg-AI` con `connect_timeout=0.75 s`, `read_timeout=20 s` y un
  solo request concurrente. Si Ollama falla, responde el banco de respaldo por contexto/ítem en
  menos de 1 s y registra `dialog_fallback`.
- La respuesta se filtra (sin `<`/`>`, máximo 300 caracteres) antes de devolverla.
- El NPC es cosmético: este endpoint no toca `game_sessions` ni `scores`.

---

## 5. Salud

### 5.1 `GET /api/health`

Sin autenticación, sin tocar datos del jugador.

```json
{ "status": "ok", "env": "dev", "database": true }
```

Responde `503` con `"status": "degraded"` si la base no responde, para que Compose y
cualquier monitor lo traten como caída real.

---

## 6. Códigos de error completos

| HTTP | `code` | Notas |
| :--- | :--- | :--- |
| 400 | `bad_session`, `session_used`, `session_expired`, `bad_nonce`, `bad_signature`, `implausible_score` | `/api/score` |
| 400 | `not_top5`, `bad_session` | `/api/claim` (puntaje fuera del top 5 o ya reclamado) |
| 401 | `invalid_token`, `invalid_credentials` | `invalid_credentials` también en `/api/claim` para nombre existente |
| 403 | — | No se usa: no hay roles de cliente |
| 409 | `username_taken` | Registro/conflicto de nombre |
| 422 | `invalid_request`, `weak_pin` | Formato o PIN predecible |
| 423 | `account_locked` | `Retry-After` (login/claim) |
| 429 | `rate_limited` | `Retry-After` |
| 502 | `upstream_error` | Supabase Auth no disponible |
| 503 | `database_unavailable` | Pooler/base no disponible |
| 500 | `internal_error` | Sin detalles; queda en el log con `X-Request-ID` |

---

## 7. Eventos de auditoría (A.8.15)

Se emiten como JSON a stdout:

`app_start`, `db_ready`, `db_warmup_retry`, `db_warmup_failed`, `register_ok`,
`register_rejected`, `register_failed`, `login_ok`, `login_fail`, `login_blocked`,
`token_refresh`, `logout`, `session_start`, `score_accepted`, `score_claimed`,
`score_rejected`, `dialog_blocked`, `dialog_fallback`, `dialog_ok`, `health_degraded`,
`database_error`, `unhandled_error`.

Campos: `ts`, `level`, `event`, `request_id`, `user_id`, y el detalle del evento. La IP
se registra enmascarada (último octeto a cero). `score_accepted` incluye `in_top5`.
