# API de CosTamarindo

> **División:** Backend · **Nodo:** `Sg-Backend`
> **Versión:** 1.0.0 · **Base:** `https://juego.local/api` (mismo origen que el juego)
> **Estado:** implementada y verificada contra Supabase DEV el 2026-11-06.

El cliente Unity **nunca** habla con Supabase. Habla solo con esta API, que es la
única que conoce la clave secreta, la base y la IA.

---

## 1. Convenciones

| Tema | Regla |
| :--- | :--- |
| Formato | JSON, `Content-Type: application/json`, UTF-8 |
| Autenticación | `Authorization: Bearer <jwt>` emitido por el backend (HS256, TTL 30 min) |
| Errores | `{"code": "<estable>", "message": "<corto>"}`; nunca detalles internos |
| Identificador de petición | cabecera de respuesta `X-Request-ID` |
| Rate limit | Nginx en `/api/` + bloqueo por usuario en login + guard en diálogo |
| Tamaño máximo de cuerpo | 8 kB (Nginx) |

El JWT incluye `iss=costamarindo`, `aud=costamarindo-api`, `sub=<user_id>`,
`username`, `env` y `exp`. Un token emitido en `dev` se rechaza en `prod` y al revés
(A.8.31).

---

## 2. Autenticación

### 2.1 `POST /api/auth/register`

Crea la identidad en Supabase Auth y el perfil. La primera vez no hay login previo.

```json
{ "username": "ana", "pin": "4821" }
```

| Código | Cuándo |
| :--- | :--- |
| `201` | Registrado; devuelve `TokenPair` |
| `409` | `username_taken` |
| `422` | `invalid_request` (formato) o `weak_pin` (PIN predecible) |

Reglas: usuario `^[A-Za-z0-9_]{3,16}$` único sin distinguir mayúsculas; PIN de 4
dígitos, no secuencial ni repetido; se rechazan los PIN más usados.

### 2.2 `POST /api/auth/login`

```json
{ "username": "ana", "pin": "4821" }
```

| Código | Cuándo |
| :--- | :--- |
| `200` | `TokenPair` |
| `401` | `invalid_credentials` |
| `423` | `account_locked`, con `Retry-After` en segundos |

Cinco fallos consecutivos bloquean la cuenta 60 s. El contador vive en
`public.login_attempts`, no en el cliente, y un login correcto lo reinicia.

### 2.3 `POST /api/auth/refresh`

```json
{ "refresh_token": "<token de GoTrue>" }
```

Devuelve un `TokenPair` nuevo. El refresh de GoTrue rota en cada uso; reutilizar uno
viejo falla con `401 invalid_token`.

### 2.4 `POST /api/auth/logout`

Requiere `Bearer`. Responde `204`. El cliente descarta el token; la sesión caduca por
TTL.

### 2.5 `TokenPair`

```json
{
  "access_token": "…",
  "refresh_token": "…",
  "token_type": "bearer",
  "expires_in": 1800,
  "user_id": "uuid",
  "username": "ana"
}
```

---

## 3. Partida y puntaje

### 3.1 `POST /api/session/start`

Requiere `Bearer`.

```json
{
  "session_id": "uuid",
  "nonce": "…",
  "session_key": "hex de 64 caracteres",
  "expires_in": 900,
  "best_score": 180
}
```

`session_key` **no se almacena**: el backend la deriva con `SESSION_MASTER_KEY` a partir
de `(session_id, nonce)`. El cliente la mantiene solo en memoria durante la partida.

### 3.2 `POST /api/score`

Requiere `Bearer`.

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

`signature = hex(HMAC-SHA256(session_key, cadena))`.

| `code` | Significado |
| :--- | :--- |
| `bad_session` | No existe o es de otro jugador |
| `session_used` | Ya se envió un puntaje con esa sesión (incluye replay) |
| `session_expired` | Pasaron los 15 min de TTL |
| `bad_nonce` | El nonce no coincide con el de la sesión |
| `bad_signature` | La firma no valida |
| `implausible_score` | Supera los techos de plausibilidad |

Techos: `score ≤ segundos × 12`, `score ≤ catches × 25`, `catches ≤ segundos × 3`.
Se aplican además los `check` de la tabla.

Respuesta aceptada:

```json
{ "ok": true, "score": 180, "best_score": 180, "rank_position": 1 }
```

### 3.3 `GET /api/leaderboard?limit=5`

Sin token devuelve el top; con token agrega la fila propia marcada.

```json
{
  "entries": [
    { "rank_position": 1, "username": "beto", "best_score": 300, "is_me": false },
    { "rank_position": 6, "username": "dani", "best_score": 60, "is_me": true }
  ]
}
```

---

## 4. Diálogo

### 4.1 `POST /api/dialog`

Requiere `Bearer`.

```json
{ "text": "¿qué es el phishing?" }
```

```json
{ "reply": "…", "blocked": false }
```

- Máximo 200 caracteres y 10 peticiones por minuto y usuario.
- Se eliminan `<` y `>` y los caracteres de control antes de cualquier uso.
- Un patrón de inyección devuelve `blocked: true`, se registra como `dialog_blocked` y
  responde con el texto de respaldo.
- **La llamada a Ollama todavía no está conectada**: esta versión responde del banco de
  respaldos. Es la tarea `IA-02`…`IA-04`. El contrato no cambia cuando se conecte.

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
| 400 | `bad_session`, `session_used`, `session_expired`, `bad_nonce`, `bad_signature`, `implausible_score` | Solo en `/api/score` |
| 401 | `invalid_token`, `invalid_credentials` | |
| 403 | — | No se usa: no hay roles de cliente |
| 409 | `username_taken` | |
| 422 | `invalid_request`, `weak_pin` | |
| 423 | `account_locked` | `Retry-After` |
| 429 | `rate_limited` | `Retry-After` |
| 502 | `upstream_error` | Supabase Auth no disponible |
| 503 | `database_unavailable` | Pooler/base no disponible |
| 500 | `internal_error` | Sin detalles; queda en el log con `X-Request-ID` |

---

## 7. Eventos de auditoría (A.8.15)

Se emiten como JSON a stdout:

`app_start`, `db_ready`, `db_warmup_retry`, `db_warmup_failed`, `register_ok`,
`register_rejected`, `register_failed`, `login_ok`, `login_fail`, `login_blocked`,
`token_refresh`, `logout`, `session_start`, `score_accepted`, `score_rejected`,
`dialog_blocked`, `health_degraded`, `database_error`, `unhandled_error`.

Campos: `ts`, `level`, `event`, `request_id`, `user_id`, y el detalle del evento. La IP
se registra enmascarada (último octeto a cero).
