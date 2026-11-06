# F5 · Backend FastAPI y puntaje firmado — evidencia

> **División:** Backend · **Nodo:** `Sg-Backend`
> **Fecha:** 2026-11-06
> **Estado:** ✅ API, pruebas, imagen y despliegue en Compose verificados
> **Pruebas:** 43/43 (`pytest`) · 3 corridas E2E consecutivas en verde

---

## 1. Qué se implementó

| Endpoint | Función |
| :--- | :--- |
| `GET /api/health` | estado y conexión a la base (503 si está caída) |
| `POST /api/auth/register` | alta con usuario + PIN, perfil y tokens |
| `POST /api/auth/login` | login con bloqueo progresivo |
| `POST /api/auth/refresh` | rotación del refresh token |
| `POST /api/auth/logout` | cierre de sesión auditado |
| `POST /api/session/start` | nonce, clave HMAC derivada y TTL |
| `POST /api/score` | validación autoritativa del puntaje |
| `GET /api/leaderboard` | top 5 y posición propia |
| `POST /api/dialog` | contrato, saneo y respaldo del NPC (sin Ollama todavía) |

Contratos completos en [`docs/api.md`](../../docs/api.md).

### 1.1 Decisiones de diseño defendibles

| Decisión | Por qué |
| :--- | :--- |
| El cliente **no** habla con Supabase Auth | así el bloqueo progresivo se aplica de verdad y ninguna clave de Supabase viaja al navegador |
| El backend emite **su propio JWT** (HS256, 30 min) | vida corta, claim `env`, validación local y sin depender del modelo de claves de GoTrue |
| El PIN se **deriva** con `PIN_PEPPER` | GoTrue exige 6 caracteres y un PIN de 4 dígitos no debe guardarse como credencial directa; sin el pimiento, los hashes no se atacan con 10.000 combinaciones |
| La clave HMAC **no se guarda** | se deriva de `(SESSION_MASTER_KEY, session_id, nonce)`; un volcado de la base no permite firmar puntajes |
| Conexión por el **pooler en modo sesión** (5432) | el modo transacción no soporta sentencias preparadas y provocaba errores intermitentes |
| `session_id UNIQUE` en `scores` | el «un envío por sesión» queda garantizado también por la base |

---

## 2. Pruebas automatizadas

```
$ cd backend && python -m pytest
43 passed in 0.56s
```

Cobertura por archivo:

| Archivo | Qué verifica |
| :--- | :--- |
| `tests/test_security.py` | derivación del PIN, clave de sesión, firma, JWT (entorno, expiración, secreto), PIN débiles |
| `tests/test_auth_flow.py` | registro, duplicados, PIN débil, login, 5 fallos → bloqueo, reset del contador, refresh rotado, logout |
| `tests/test_score_flow.py` | sesión, puntaje válido, firma alterada, replay, nonce, expirada, implausible, sesión ajena, sin token, límites de esquema, récord |
| `tests/test_leaderboard.py` | top 5 + posición propia, sin token, salud |

Las pruebas no tocan red ni base: se sustituyen el repositorio y el proveedor de
identidad. La verificación real se hace con `scripts/e2e_check.py` y
`scripts/check_db.py`.

---

## 3. Prueba de extremo a extremo (real, contra Supabase)

```
$ CT_E2E_BASE=https://juego.local python backend/scripts/e2e_check.py
1. health
  OK   health responde {'status': 'ok', 'env': 'dev', 'database': True}
2. registro
  OK   registro 201
3. login y refresh
  OK   login 200
  OK   refresh rota el token
4. bloqueo tras 5 fallos
  OK   5 fallos registrados [401, 401, 401, 401, 423]
  OK   cuenta bloqueada account_locked
5. sesion y puntaje valido
  OK   sesion creada
  OK   puntaje aceptado {'ok': True, 'score': 180, 'best_score': 180, 'rank_position': 1}
6. ataques
  OK   firma alterada rechazada {'code': 'session_used'}
  OK   score alterado -> bad_signature
  OK   puntaje implausible -> implausible_score
  OK   sesion sin token -> 401
7. replay exacto
  OK   primer envio aceptado
  OK   replay -> session_used
8. leaderboard
  OK   leaderboard 200
  OK   mi posicion marcada
9. limites de entrada
  OK   inyeccion bloqueada
  OK   texto largo rechazado 422
10. limpieza
  usuario de prueba eliminado

E2E en verde.
```

Se ejecutó **tres veces seguidas** después del ajuste del pooler: las tres en verde y con
**0** `database_error` en los logs del contenedor.

### 3.1 Los cuatro ataques, con su motivo

| Ataque | Respuesta |
| :--- | :--- |
| `score` alterado en tránsito | `400 bad_signature` |
| Reenvío exacto de una petición válida | `400 session_used` |
| Puntaje imposible para la duración | `400 implausible_score` |
| Petición sin token | `401 invalid_token` |

Un quinto caso quedó cubierto por prueba: sesión de otro jugador → `400 bad_session`.

---

## 4. Despliegue en Compose

```
$ docker compose -f docker-compose.yml -f docker-compose.override.yml --profile backend up -d --wait
 Container sg-ai Healthy
 Container sg-frontend Healthy
 Container sg-backend Healthy

$ docker compose ps
sg-ai         Up (healthy)   11434/tcp
sg-backend    Up (healthy)   8000/tcp
sg-frontend   Up (healthy)   0.0.0.0:443->443/tcp, 0.0.0.0:8081->8080/tcp
```

| Comprobación | Resultado |
| :--- | :--- |
| `docker compose port sg-backend 8000` | sin salida: **no publicado** |
| `https://juego.local/api/health` | `200` con `{"status":"ok","env":"dev","database":true}` |
| Cabeceras en `/api/` | `X-Frame-Options: DENY`, CSP, `Strict-Transport-Security` |
| Imagen | multi-stage, usuario `app` (UID 10001), sin compiladores ni caché de pip |
| Healthcheck | `/api/health` real; 45 s de gracia por el arranque del pooler |

---

## 5. Registro de eventos (A.8.15)

Ejemplo real de una petición fallida y una aceptada:

```json
{"ts":"2026-11-06T23:19:14.393Z","level":"WARNING","logger":"costamarindo.http","event":"database_error","request_id":"02c9…","path":"/api/auth/login"}
{"ts":"…","level":"INFO","logger":"costamarindo.audit","event":"score_accepted","request_id":"…","user_id":"…","session_id":"…","score":180,"duration_ms":60000,"catches":10,"ip":"172.18.0.0"}
```

La IP se enmascara (último octeto a cero) para reducir datos personales.

---

## 6. Trampas encontradas

1. **`sb_secret_...` se rechaza si el `User-Agent` parece un navegador.** GoTrue responde
   `Forbidden use of secret API key in browser`. El cliente fija
   `CostamarindoBackend/1.0`.
2. **El pooler en modo transacción rompe las sentencias preparadas.** Se desactivan las
   dos cachés (`statement_cache_size` y `prepared_statement_cache_size`) y, aun así,
   aparecían fallos intermitentes. La solución estable fue usar el pooler en **modo
   sesión** (5432).
3. **Las primeras conexiones desde un contenedor recién creado pueden fallar** mientras
   se asienta el DNS interno. Se agregó calentamiento con reintentos y 45 s de
   `start_period` en el healthcheck.
4. **GoTrue exige contraseñas de 6 caracteres como mínimo**, así que un PIN de 4 no se
   puede usar directamente. De ahí la derivación con `PIN_PEPPER`.
5. **`position` es palabra reservada en Postgres.** La columna de la función se llama
   `rank_position`.
6. **Un `Set-Content -Encoding utf8` de PowerShell agrega BOM** y Postgres falla con
   `syntax error at or near "\ufeff"`. El aplicador lee con `utf-8-sig`.

---

## 7. Artefactos

| Ruta | Qué es |
| :--- | :--- |
| `backend/app/` | aplicación (config, seguridad, repositorios, servicios, routers) |
| `backend/tests/` | 43 pruebas herméticas |
| `backend/Dockerfile` | imagen multi-stage sin privilegios |
| `backend/scripts/migrate.py` | migraciones |
| `backend/scripts/check_db.py` | verificación de RLS y privilegios |
| `backend/scripts/e2e_check.py` | E2E con los cuatro ataques |
| `backend/pytest.ini` | configuración de pruebas |
| `docs/api.md` | contratos |
