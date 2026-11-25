# Contexto completo · CosTamarindo 2D

> **Qué es este documento:** una foto completa del proyecto para que cualquier persona (o agente)
> pueda retomarlo sin leer todo el repositorio. Resume qué es el juego, cómo está armado, qué está
> verificado, qué evidencias existen y **qué falta o está en curso**.
>
> **Fecha:** 2026-11-25 · **Rama:** `main` · **Último commit de código:** `e2de1e5`
> **Fuentes de verdad:** [`Plan.md`](Plan.md) (plan y decisiones), [`docs/api.md`](docs/api.md)
> (contratos), [`README.md`](README.md) (arranque).
> **Regla de oro:** ningún secreto, token, cadena de conexión ni valor de `.env` se escribe en
> documentos, capturas o commits.

---

## 1. Qué es el proyecto

**CosTamarindo 2D** es un juego web (Unity → WebGL) con temática del Pacífico colombiano. El
jugador atrapa ítems que caen (buenos, peligrosos y basura) durante 120 segundos con 3 vidas; una
NPC (la Palenquera) comenta la partida con un modelo de lenguaje local. Al terminar, el puntaje se
envía al servidor; si entra al **top 5 global**, el jugador puede reclamarlo con **nombre + PIN**.

El proyecto es también un ejercicio de **seguridad e ISO/IEC 27001:2022**: cada control se cierra
con «antes vulnerable → ataque reproducible → corrección → verificación», con evidencias.

**Flujo de una partida:**

```
Login (anónimo) → Game (jugar) → fin de partida
   → POST /api/session/start        (crea sesión anónima)
   → POST /api/score                (firma HMAC, valida plausibilidad)
   → si in_top5: POST /api/claim    (nombre + PIN)
   → GET  /api/leaderboard          (top 5)
   → POST /api/dialog               (NPC, cosmético, no afecta puntaje)
```

---

## 2. Arquitectura

Cinco nodos (cuatro contenedores Docker + Supabase Cloud como excepción documentada):

| Nodo | Qué hace | Dónde corre |
| :--- | :--- | :--- |
| `Sg-Frontend` | Sirve el build WebGL y proxifica `/api/` con TLS, CSP y HSTS | Contenedor Nginx `sg-frontend` |
| `Sg-Backend` | API FastAPI: auth, sesión, puntaje firmado, claim, leaderboard, diálogo | Contenedor `sg-backend` |
| `Sg-DB` | Identidad (GoTrue), Postgres, RLS, leaderboard, sesiones | Supabase Cloud DEV (excepción D3) |
| `Sg-AI` | Modelo `qwen2.5:3b` para el NPC | Contenedor Ollama `sg-ai` |
| `Sg-Security` | Wazuh, Gitleaks, TLS, cabeceras, ataques y evidencias ISO | VM + agente Wazuh |

**Redes Docker:** `costamarindo-dmz`, `costamarindo-app`, `costamarindo-data`,
`costamarindo-mgmt` y `costamarindo-ai` (interna). Solo `sg-frontend` publica `443` (y `8081` en
dev). `sg-backend:8000` y `sg-ai:11434` no se publican.

**Principio clave:** el cliente **nunca** habla con Supabase. Solo habla con `/api/` del mismo
origen; el backend es el único que conoce la clave secreta, la base y la IA (D10).

---

## 3. Stack y versiones

| Pieza | Versión |
| :--- | :--- |
| Unity | `6000.3.27f1` (Unity 6.3 LTS) con módulo WebGL; URP 2D 17.3.0 |
| Backend | Python 3.12, FastAPI, Pydantic; 64 tests |
| Base de datos | Supabase Cloud DEV, PostgreSQL 17.11, GoTrue `v2.198.0` |
| IA | Ollama + `qwen2.5:3b` (1,9 GB) |
| Frontend servidor | Nginx `1.29-alpine` |
| Contenedores | Docker `29.9.2`, Compose `v5.6.1` |
| TLS local | mkcert (`juego.local`) |
| SO de desarrollo | Windows 11 Pro `10.0.26300.0` |

---

## 4. Estado verificado por nodo (2026-11-25)

### 4.1 `Sg-DB` — ✅ operativo

- Proyecto DEV `CosTamarindo`, región `us-east-1`, PostgreSQL 17.11.
- Migraciones aplicadas: `001_schema.sql`, `002_rls.sql`, `003_harden_surface.sql` (2026-11-06) y
  `004_anonymous_claim.sql` (2026-11-16).
- `migrate.py --status` → `EXIT=0`; `check_db.py` → `EXIT=0` («Todo en orden»).
- RLS activa en todas las tablas; **0** políticas y **0** privilegios de tabla para
  `anon`/`authenticated`; `USAGE` sobre `public` = `false` para ambos y `true` para `service_role`
  (D13).
- `get_leaderboard(p_user_id uuid, p_limit integer)` presente.
- `scores.user_id` y `game_sessions.user_id` aceptan `NULL`; índice parcial
  `scores_named_best_idx` para rankear solo puntajes reclamados.
- Backup `infra/backups/costamarindo-20261106T232300Z.dump` con hash y restauración probada.
- Advisories del MCP: `health` limpio; `security` y `performance` solo con avisos **INFO**
  esperados por diseño (RLS sin políticas; dos índices aún sin uso). No requieren acción.
- **Pendiente:** proyecto PROD al desplegar (DB-08 / A.8.31).

### 4.2 `Sg-Backend` — ✅ operativo

- `costamarindo-backend:latest` reconstruida desde `main`; `sg-backend` healthy.
- `GET /api/health` → `{"status":"ok","env":"dev","database":true}`.
- 64/64 tests de backend en verde.
- E2E real en verde (`EXIT=0`): flujo anónimo, reclamo top-5 (`rank_position: 1`), bloqueo
  progresivo (`401`×4 → `423`), ataques rechazados (`session_used`, `bad_signature`,
  `implausible_score`), leaderboard y limpieza.
- `/api/dialog` real por el proxy: `dialog_ok` con Ollama, ~10 s; entrada vacía →
  `invalid_request` + `dialog_blocked` (guardrail esperado).
- El diálogo **no** escribe en `scores` ni `game_sessions` (conteos iguales antes/después).
- `backend.log` ya se escribe en el volumen `costamarindo-backend-logs`.
- **Evidencias:** `evidencias/F7/16-backend-real.txt`, `17-e2e-real.md`.
- **Pendiente:** nada bloqueante en el código.

### 4.3 `Sg-Frontend` — ✅ operativo (falta URL pública)

- Build Release con arte curado: 17 archivos, 16,5 MB.
- Servido en `https://juego.local` (Nginx + TLS mkcert, CSP por hash, HSTS, proxy `/api/`).
- Medición real en navegador: `unityReady: true`, ~1,5 s, canvas 960×600, 0 errores de app.
- 15/15 tests PlayMode.
- Flujo `Login → Game` verificado; NPC y panel de reclamo visibles.
- **Pendiente:**
  - 🟡 Publicar en URL pública (dominio/VPS o demo estática).
  - ⛔ `FE-05`/`FE-06`: kit de 10 prefabs de UI, bloqueado por `FIGMA_TOKEN`.
  - ⬜ `FE-11`: playtest humano de 10–15 min y ajuste de dificultad.

### 4.4 `Sg-AI` — ✅ operativo

- `sg-ai` healthy, `qwen2.5:3b` en el volumen `costamarindo-ai-models`, sin `11434` publicado.
- Prompt versionado, guardrails, timeout, rate limit y fallback (< 1 s con Ollama apagado).
- Bocadillo anclado al sprite del jugador, 「escribiendo…」, historial y fallback visible.
- **Validado contra el backend real/Supabase** (`F7/16` y `F7/17`).
- **Pendiente:** nada bloqueante.

### 4.5 `Sg-Security` — 🟡 en curso

- A.8.25 (Gitleaks), SEC-01, SEC-02, SEC-06 (parcial), SEC-08 (parcial), SEC-12, SEC-13 y SEC-14
  cerrados; SoA de 93 controles.
- Wazuh 4.14 en VM, agente activo, reglas `100200–100250`, logs Nginx reales.
- **Desbloqueado ahora:** el backend real está arriba y `backend.log` existe, así que los ataques
  A.8.5, A.8.24 y A.8.28 ya se pueden ejecutar con `curl`/Burp.
- **Pendiente:** ataques y evidencias de los 10 controles, alerta FIM end-to-end, prueba de
  aislamiento desde otro contenedor y tailnet, y el proyecto PROD para A.8.31.
- **Nota:** el perfil `security` no está levantado en este equipo.

---

## 5. Contrato del juego (vigente)

- **Anónimo por defecto:** se juega sin login. `/api/session/start` y `/api/score` no piden token.
- **Reclamo top-5:** solo si el puntaje entra al top 5, se pide **nombre** (`^[A-Za-z0-9_]{3,16}$`,
  único sin distinguir mayúsculas) + **PIN** (4–6 dígitos, no predecible). `POST /api/claim`.
- **Bloqueo progresivo:** 5 fallos consecutivos → `60 s` (`423`, `Retry-After`), persistido en
  `login_attempts`.
- **Firma del puntaje:** `hex(HMAC-SHA256(session_key, "{session_id}|{score}|{duration_ms}|{catches}|{nonce}"))`.
  Techos del servidor: `score ≤ s×12`, `score ≤ catches×25`, `catches ≤ s×3`.
- **Sesión de un solo uso:** el replay devuelve `session_used`.
- **Diálogo cosmético:** `/api/dialog` acepta token opcional y **nunca** altera puntaje, vidas ni
  estado de partida.
- **Compatibilidad:** `/api/auth/register|login|refresh|logout` siguen disponibles, pero no están
  en el camino del jugador.

Detalle completo en [`docs/api.md`](docs/api.md).

---

## 6. Decisiones cerradas (no se reabren)

| ID | Decisión |
| :--- | :--- |
| D1 | El cliente nunca escribe en la base; solo el backend con `service_role` |
| D2 | Backend en FastAPI (Python) |
| D3 | Supabase Cloud free para Auth/Postgres; `Sg-DB` es excepción externa documentada |
| D4 | Modelo `qwen2.5:3b` |
| D5 | TLS local con mkcert (`juego.local`) |
| D6 | Sesiones de juego en Postgres (`game_sessions`), no en memoria |
| D7 | Figma manda en arte y narrativa; ISO vive en infraestructura y documentación |
| D8 | Login usuario + PIN con correo sintético, rate limit y bloqueo progresivo |
| D9 | El PIN se deriva con `PIN_PEPPER` antes de GoTrue |
| D10 | El backend emite su propio JWT de 30 min con claim `env`; el cliente no habla con Supabase |
| D11 | Pooler en **modo sesión** (5432); el modo transacción (6543) rompe sentencias preparadas |
| D12 | Se rechazan PIN secuenciales, repetidos y comunes |
| D13 | `anon`/`authenticated` sin acceso ni `USAGE` sobre `public`; el leaderboard solo sale por la API |

Detalle en [`Plan.md`](Plan.md) §11.5.

---

## 7. Mapa de evidencias

| Carpeta | Contenido |
| :--- | :--- |
| `evidencias/F2/` | Vertical slice jugable offline |
| `evidencias/F3/` | Build WebGL + Nginx + CSP |
| `evidencias/F4/` | Supabase: esquema, RLS, leaderboard, backup y restauración |
| `evidencias/F5/` | Backend FastAPI y puntaje firmado |
| `evidencias/F6/` | Infraestructura Docker de 5 nodos |
| `evidencias/F7/` | NPC con Ollama: simulación (`01`–`15`) y **validación real (`16`, `17`)** |
| `evidencias/A.8.*`, `SEC-12`, `SEC-13` | Controles ISO y auditorías |
| `evidencias/_plantilla/` | Formato obligatorio de una carpeta de evidencia |

Evidencias F7 reales de este cierre:
[`16-backend-real.txt`](evidencias/F7/16-backend-real.txt) y
[`17-e2e-real.md`](evidencias/F7/17-e2e-real.md).

---

## 8. Pendientes y trabajo en curso

Leyenda: ✅ hecho · 🟡 en curso · ⬜ pendiente · ⛔ bloqueado.

### 8.1 En curso ahora mismo

| Tema | Estado | Qué falta / nota |
| :--- | :--- | :--- |
| Sincronización documental + evidencias F7 reales | 🟡 en curso | Se cierra con este commit (`contexto.md`, README, Plan, handoffs y `F7/16`–`17`) |
| Publicación pública del juego | 🟡 en curso | Decidir dominio/VPS o demo estática; `juego.local` solo funciona en este equipo |
| Ataques ISO con backend real | 🟡 en curso | Security: backend arriba y `backend.log` disponible desbloquean A.8.5/A.8.24/A.8.28 |

### 8.2 Pendientes

| Pendiente | Estado | Bloqueo / responsable |
| :--- | :--- | :--- |
| `FE-05`/`FE-06`: kit de 10 prefabs de UI | ⛔ bloqueado | `FIGMA_TOKEN` / Frontend |
| `FE-11`: playtest humano 10–15 min | ⬜ pendiente | una persona real / Frontend |
| Ataques y evidencias de los 10 controles ISO | 🟡 en curso | Security |
| Alerta FIM end-to-end y aislamiento desde contenedor | ⬜ pendiente | Security |
| Proyecto PROD + `pg_dump` inicial (DB-08 / A.8.31) | ⬜ pendiente | DB / Security, al desplegar |
| Publicación con dominio real y certificado válido | 🟡 en curso | decisión de infraestructura |
| Demo reproducible + video de respaldo | ⬜ pendiente | entrega final |
| Informe final de entrega | ⬜ pendiente | después del despliegue |
| Levantar el perfil `security` en este equipo | ⬜ pendiente | `up.ps1 security` (Wazuh) |

---

## 9. Cómo levantar y validar

```powershell
# 1. Variables (solo en la máquina; nunca versionar)
Copy-Item .env.example .env   # luego cargar valores reales

# 2. Dependencias del backend
python -m venv backend/.venv
backend\.venv\Scripts\python.exe -m pip install -r backend/requirements-dev.txt

# 3. Certificado local y hosts (una vez por máquina)
powershell -File infra/scripts/tls.ps1
# hosts: 127.0.0.1 juego.local

# 4. Base de datos
backend\.venv\Scripts\python.exe backend/scripts/migrate.py
backend\.venv\Scripts\python.exe backend/scripts/check_db.py

# 5. Build + entorno
powershell -File infra/scripts/build_webgl.ps1
powershell -File infra/scripts/up.ps1 backend

# 6. Comprobar
curl.exe --ssl-no-revoke https://juego.local/api/health
```

Verificación completa:

```powershell
backend\.venv\Scripts\python.exe -m pytest backend
backend\.venv\Scripts\python.exe backend/scripts/check_db.py
$env:CT_E2E_BASE = "https://juego.local"
backend\.venv\Scripts\python.exe backend/scripts/e2e_check.py
```

Si Docker falla al arrancar con redes huérfanas, ver `README.md` §2.4 y limpiar contenedores
`sg-*` y redes `costamarindo-*` antes de reintentar.

---

## 10. Reglas de secretos y convivencia

- `.env`, `infra/tls/`, `infra/backups/`, `builds/` y `.venv/` **no** se versionan.
- Nunca pegar claves, tokens, cadenas de conexión ni valores de `.env` en documentos, capturas,
  issues o chat. Solo nombres, estados y códigos de salida.
- El cliente no habla con Supabase; el backend es el único con `service_role`.
- Frontend no toca `backend/`; Backend no toca `unity/`; IA no toca el camino de puntaje;
  Security no implementa funcionalidad.
- Conventional Commits con alcance (`unity`, `backend`, `infra`, `db`, `art`, `docs`, `sec`, `ci`).
- Gitleaks corre en pre-commit y en CI.

---

## 11. Mapa de documentos

| Documento | Para qué |
| :--- | :--- |
| [`README.md`](README.md) | arranque desde cero, secretos, qué se versiona |
| `contexto.md` | **este documento**: contexto completo y pendientes |
| [`docs/handoff/README.md`](docs/handoff/README.md) | puesta en marcha y pendientes consolidados por división |
| [`docs/handoff/frontend.md`](docs/handoff/frontend.md) | Unity/WebGL, contrato anónimo y backlog `FE-01`…`FE-14` |
| [`docs/handoff/ia.md`](docs/handoff/ia.md) | Ollama, guardrails, pruebas y límites del NPC |
| [`docs/handoff/security.md`](docs/handoff/security.md) | recetas de ataque y estado por control ISO |
| [`docs/handoff/security-node.md`](docs/handoff/security-node.md) | provisión y operación de la VM Wazuh |
| [`docs/handoff/solicitud-datos-santiago.md`](docs/handoff/solicitud-datos-santiago.md) | solicitud a Santiago y cierre verificado (§8) |
| [`docs/handoff/handoff-santiago-backend.md`](docs/handoff/handoff-santiago-backend.md) | contexto histórico de backend/F7 |
| [`docs/handoff/handoff-alejandra-frontend.md`](docs/handoff/handoff-alejandra-frontend.md) | contexto histórico de integración del NPC |
| [`Plan.md`](Plan.md) | plan maestro, fases, backlog y decisiones |
| [`docs/api.md`](docs/api.md) | contratos de la API |
| [`docs/arquitectura.md`](docs/arquitectura.md) | arquitectura detallada |
| [`docs/soa.md`](docs/soa.md) | Declaración de Aplicabilidad ISO |
