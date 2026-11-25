# Solicitud de datos y accesos — Santiago (Backend/Supabase + Frontend)

> **Para:** Santiago y su agente.
> **Fecha:** 2026-11-23 · **Actualizado:** 2026-11-25 (cierre verificado en el nodo de Santiago; §8).
> **Rama verificada:** `Yoann` ya está integrada en `main`; último commit local `a18d7ab`
> (árbol limpio; 64 tests de backend, 15/15 tests PlayMode).
> **Objetivo:** desbloquear la validación real de F7/Supabase y usar el nodo Frontend para publicar
> y probar el build de `main`.
> **Resultado:** F7 validado contra Supabase real y backend real en `https://juego.local`;
> detalle, comandos y salidas sin secretos en §8.
>
> **Regla de oro:** no enviar claves, tokens, contraseñas, cadenas de conexión ni archivos `.env`
> por Git, chat, capturas o documentos. Usar un gestor de secretos o configurarlos directamente en
> el nodo. En este documento solo se piden **nombres, estados y salidas sin secretos**.
>
> **2026-11-24:** la respuesta de §7 es **parcial**. Lo que sigue faltando para cerrar F7 está en
> [`solicitud-datos-santiago-pendientes.md`](solicitud-datos-santiago-pendientes.md).

---

## 0. Contexto: qué ya está listo y qué falta

Ya está listo:

- `sg-ai` healthy con `qwen2.5:3b`, sin puerto publicado, en la red interna `costamarindo-ai`.
- `POST /api/dialog` con guardrails, timeout y fallback; validado con Ollama real en simulación.
- `main` incluye el flujo anónimo de sesión/puntaje, `/api/claim` y la migración
  `004_anonymous_claim.sql`.
- `main` incluye el NPC y el panel de reclamo en `Game.unity`.
- `pytest backend`: **64/64**; compilación Unity y build WebGL de `main` exitosos en local.
- Simulación E2E con repositorio en memoria: health, sesión, score, claim, leaderboard y diálogo
  funcionaron; **no** valida Supabase, RLS ni migraciones.

Estado al 2026-11-25 (detalle y salidas en §8):

1. ✅ Claves y datos de Supabase/JWT confirmados y cargados en el nodo; MCP de Supabase operativo.
2. ✅ `004_anonymous_claim.sql` aplicada y verificada con `check_db.py` y con el MCP.
3. ✅ Imagen `costamarindo-backend:latest` reconstruida desde `main`; `sg-backend` healthy.
4. ✅ Build de `main` servido por `sg-frontend` en `https://juego.local` y validado con backend real.
   Queda únicamente la publicación a Internet (dominio/VPS), si se quiere acceso fuera del equipo.
5. ✅ Evidencias F7 reales generadas y documentadas en §8.

---

## 1. Datos de Supabase y backend

### 1.1 Variables que necesita el backend

Pedimos confirmar si ya existen y, si existen, entregarlas por canal seguro:

| Variable | Para qué | Origen / nota |
| :--- | :--- | :--- |
| `SUPABASE_URL` | URL del proyecto DEV | Dashboard de Supabase. Debe ser `https://…supabase.co` |
| `SUPABASE_PROJECT_REF` | Referencia del proyecto | Dashboard; sirve para la separación DEV/PROD |
| `SUPABASE_PUBLISHABLE_KEY` | Cliente público de Auth | Clave `sb_publishable_…` |
| `SUPABASE_SECRET_KEY` | Administración de Auth desde el backend | Clave `sb_secret_…`; **solo en el servidor** |
| `SUPABASE_DB_URL` | Conexión SQL del backend y migraciones | Pooler de **sesión, puerto 5432**; no usar 6543 |
| `BACKEND_JWT_SECRET` | Firma del JWT propio | 32+ caracteres aleatorios |
| `PIN_PEPPER` | Deriva la contraseña real de GoTrue desde el PIN | 16+ caracteres aleatorios |
| `SESSION_MASTER_KEY` | Deriva la clave HMAC de cada sesión | 16+ caracteres aleatorios |
| `DEV_PROJECT_REFS` | Bloquea PROD apuntando a DEV | Opcional pero recomendado |

Los tres secretos propios (`BACKEND_JWT_SECRET`, `PIN_PEPPER`, `SESSION_MASTER_KEY`) podemos
generarlos nosotros si Santiago prefiere no compartirlos. En ese caso, se configuran directamente
en el `.env` del backend y solo se confirma que quedaron cargados.

> **Respuesta 2026-11-25 (§8):** las nueve variables están definidas en el `.env` del nodo y se
> usaron sin copiar sus valores en el repositorio ni en este documento. Proyecto de trabajo
> `CosTamarindo` (`jukyiwsteuzwrmzlgfme`) en región `us-east-1`, usado como DEV; pooler en modo
> sesión (5432), clave pública `sb_publishable_…` y clave administrativa `sb_secret_…`, ambas
> activas. `GET /auth/v1/health` → 200. No se creó ni tocó ningún proyecto PROD.

También pedimos confirmar:

- Región del proyecto y host del pooler.
- Si el proyecto es **DEV**, **staging** o **PROD**. No queremos ejecutar pruebas destructivas en
  PROD.
- Si la clave `SUPABASE_SECRET_KEY` es la nueva `sb_secret_…` o la legacy `service_role`.
- Si el usuario de base de datos del pooler tiene permisos para ejecutar las migraciones.
- Si el proyecto tiene activado el registro público de Supabase Auth. El backend crea usuarios
  con `email_confirm=true` y correo sintético `@players.costamarindo.local`; necesitamos saber si
  GoTrue rechaza ese dominio o exige confirmación por correo.

> **Respuesta 2026-11-25:** región `us-east-1` y pooler en modo sesión (5432); proyecto **DEV**;
> clave administrativa moderna `sb_secret_…`; la conexión del `.env` permitió consultar las
> migraciones sin errores; y el E2E creó y borró usuarios sintéticos sin necesitar confirmación por
> correo. Detalle en §8.2.

### 1.2 Migraciones y estado de la base

Pedimos alguna de estas dos opciones:

**Opción A — Santiago ejecuta y devuelve la salida (sin secretos):**

```powershell
git rev-parse HEAD
backend\.venv\Scripts\python.exe backend\scripts\migrate.py --status
backend\.venv\Scripts\python.exe backend\scripts\migrate.py
backend\.venv\Scripts\python.exe backend\scripts\check_db.py
```

**Opción B — nos da acceso temporal de solo lectura/administrado** para ejecutar las migraciones
y `check_db.py`.

Confirmado el 2026-11-25 (ver §8.2):

- [x] `001_schema.sql`, `002_rls.sql`, `003_harden_surface.sql` y `004_anonymous_claim.sql`
      aparecen aplicadas (la última, el 2026-11-16).
- [x] `check_db.py` termina sin fallos (salida 0).
- [x] `anon` y `authenticated` no tienen `USAGE` sobre `public` (D13) ni privilegios de tabla.
- [x] `scores.user_id` y `game_sessions.user_id` aceptan `NULL`; el `claim` actualiza el puntaje.
- [x] El leaderboard solo muestra puntajes reclamados (comprobado con el E2E real).
- [x] `login_attempts` y el bloqueo progresivo funcionan en el proyecto real (423 tras 5 fallos).
- [x] No se reaplicó `004`, por lo que no se requirió un backup nuevo; la migración ya estaba
      aplicada antes de este cierre.

No se pegaron contraseñas, cadenas de conexión ni claves; solo estados y códigos de salida.

### 1.3 Imagen y despliegue del backend

La imagen `costamarindo-backend:latest` local es anterior a `main` y no incluye
`app/providers/ollama.py`. La imagen `costamarindo-backend:main-test` sí se construyó desde
`main`.

Confirmado el 2026-11-25 (ver §8.2):

- [x] Santiago reconstruyó `costamarindo-backend:latest` desde `main` en este nodo.
- [x] El backend real corre aquí, en el mismo host del juego (`sg-backend`).
- [x] Compose: `infra/docker-compose.yml` + `infra/docker-compose.override.yml`; servicio
      `backend`, contenedor `sg-backend`.
- [x] Tiene salida a `sg-ai:11434` por `costamarindo-ai` (`dialog_ok` con Ollama real).
- [x] `8000` y `11434` siguen sin publicarse; Nginx publica `443` y, en dev, `8081`.
- [x] El `.env` está cargado en el contenedor (`/api/health` → `database: true`).

---

## 2. Datos del nodo Frontend

Ahora que Santiago tiene el nodo Frontend, necesitamos saber cómo publicar y validar el build de
`main`.

### 2.1 Identificación y acceso

- [x] Nodo local `Sg-Frontend`: contenedor `sg-frontend` (Nginx 1.29-alpine) en Docker Desktop
      sobre Windows 11 Pro, en el equipo de Santiago.
- [x] URL de trabajo verificada: `https://juego.local` (200 con TLS local). **Dominio público
      pendiente:** no se creó VPS ni se asignó dominio en este cierre (§8.3).
- [x] No requiere IP/hostname ni puerto SSH: la administración es local con `docker compose`.
- [x] No se necesitó Tailscale/VPN ni auth key para esta validación local.
- [x] Acceso local por Docker en la sesión de Santiago; no se compartieron contraseñas.
- [x] Santiago ejecutó el despliegue aquí y dejó la salida documentada en §8.

### 2.2 Stack y publicación

- [x] Windows 11 Pro; Docker 29.9.2; Docker Compose v5.6.1; Nginx 1.29-alpine.
- [x] Repositorio `C:\Users\USUARIO\Desktop\CosTamarindo`; artefacto `builds/webgl`.
- [x] Servicio `frontend`, contenedor `sg-frontend`.
- [x] `builds/webgl` está montado como volumen de solo lectura en `/usr/share/nginx/html`.
- [x] Comando usado: `powershell -File infra/scripts/up.ps1 backend` (equivalente a
      `docker compose --profile backend up -d --wait` con ambos archivos y el `.env`).
- [x] `infra/nginx/snippets/csp.conf` sigue vigente: el SHA-256 del script inline de
      `index.html` coincide con la directiva; no hizo falta regenerarlo ni recargar Nginx.
- [x] No hay CI/CD en el nodo: el despliegue es manual.
- [x] Build servido desde `main` en `a18d7ab` (2026-11-24).
- [x] El build de `main` incluye el NPC y el panel de reclamo (evidencias `F7/14` y `F7/15`
      y medición real en §8.2).

### 2.3 Red, TLS y seguridad

- [x] `juego.local` con certificado mkcert confiado por el navegador del equipo; `curl
      --ssl-no-revoke https://juego.local/` → 200. Dominio público aún no asignado (§8.3).
- [x] La CA raíz de mkcert se confía en el almacén del usuario con `infra/scripts/tls.ps1`;
      en otro equipo basta ejecutar ese script y tener la entrada `hosts`.
- [x] Nginx proxya `/api/` a `sg-backend:8000`, con CSP por hash, HSTS y `limit_req`
      (`infra/nginx/conf.d/costamarindo.conf`).
- [x] Publicados: `443` y `8081` (solo dev). `8000` y `11434` figuran como `expose`, sin
      publicar (comprobado en `docker compose ps`).
- [x] `sg-frontend` no tiene `env_file` ni claves de Supabase: el navegador solo habla con
      `/api/` del mismo origen.
- [x] Logs accesibles con `docker logs sg-frontend` y en el volumen
      `costamarindo-frontend-logs`; en las evidencias se recortan y no se pegan tokens.

### 2.4 Validación hecha en ese nodo

- [x] Abrir `https://juego.local/` y confirmar `200`, también por HTTPS.
- [x] Pensamiento del NPC verificado en el build de `main` (evidencias `F7/14` y `F7/15`).
- [x] Panel de fin de partida/reclamo verificado en el mismo build; con backend real el reclamo
      devolvió `rank_position: 1` en el E2E (§8.2). El playtest humano queda en `FE-11` (§8.3).
- [x] Llamadas a `/api/session/start`, `/api/score`, `/api/claim` y `/api/dialog` ejecutadas
      contra el Nginx real; no hay llamadas directas a Supabase desde el navegador (sin `env_file`
      en `sg-frontend` y contrato D10).
- [x] El NPC no altera puntaje, vidas ni estado de la partida (`/api/score` no llama a Ollama;
      contrato IA y 64/64 tests de backend).
- [x] Captura y salida de consola/red guardadas sin secretos (temporales fuera del repo;
      resumen en §8.2).

---

## 3. Decisiones de contrato que deben confirmarse

`main` cambió el contrato respecto al handoff anterior. Necesitamos que Santiago confirme:

- [x] El flujo **anónimo** es el oficial: se juega sin login y solo se pide nombre + PIN si el
      puntaje entra al top-5 (ejecutado en el E2E real).
- [x] `/api/session/start` y `/api/score` no requieren token; `/api/dialog` acepta token
      opcional.
- [x] `/api/claim` con PIN de 4–6 dígitos es el único camino de identidad del jugador.
- [x] La escena `Login` sigue existiendo solo por compatibilidad, encadenada a `Game`.
- [x] `main` es la rama de integración (`a18d7ab`) y `Yoann` quedó integrada en ella.
- [x] Solo se publicó `main`; las ramas `origin/Yoann` y `origin/aleja` siguen disponibles y no
      se mezclaron en este cierre.

El contrato quedó confirmado por código y por el flujo real ejecutado; no se cambió después de
publicar el build ni de aplicar `004`.

---

## 4. Evidencia que debe devolver Santiago

Entregado el 2026-11-25 en §8, sin secretos ni datos personales:

- [x] `git rev-parse HEAD` del backend y del frontend (`a18d7ab`, un solo repo).
- [x] `docker compose ps` (nombres, estados y puertos, sin variables).
- [x] Salida de `migrate.py --status` y resultado de `check_db.py`.
- [x] `GET /api/health` con `status`, `env` y `database`.
- [x] Códigos/resultados de `/api/session/start`, `/api/score`, `/api/claim`,
      `/api/leaderboard` y `/api/dialog`.
- [x] Extracto de logs `dialog_ok` y `dialog_blocked`, sin tokens.
- [x] Frontend: URL final, commit, medición en navegador y referencia a las capturas del NPC y
      del panel de reclamo ya versionadas en `evidencias/F7/`.
- [x] Confirmación de que `8000` y `11434` no están publicados.

El formato pedido (fecha, commit, comandos y salidas recortadas) se cumplió dentro de este mismo
documento, en §8, para que quede junto a la solicitud que responde.

---

## 5. Cómo entregar los secretos

Elegir una sola vía:

1. **Gestor de secretos compartido** (1Password, Bitwarden, vault del proyecto): crear una entrada
   “CosTamarindo DEV” con las variables y dar acceso al equipo.
2. **Configuración directa en el nodo**: Santiago carga el `.env` y solo confirma qué variables
   quedaron definidas; no las comparte.
3. **Archivo cifrado** fuera del repo, con contraseña enviada por otro canal.

No usar: Git, issues, PRs, chat, capturas, documentos compartidos sin cifrar ni `.env` en el
repositorio. Si alguna clave ya se compartió por error, rotarla antes de continuar.

---

## 6. Checklist de respuesta

Respondido el 2026-11-25 (detalle en §8):

- [x] Variables de Supabase/JWT cargadas en el nodo (sin compartir valores).
- [x] Confirmación de proyecto DEV y región/pooler.
- [x] `004_anonymous_claim.sql` aplicada y `check_db.py` en verde.
- [x] Responsable y estado de la imagen `costamarindo-backend:latest`.
- [x] Acceso o procedimiento de despliegue del nodo Frontend.
- [x] URL de trabajo, commit publicado y estado del build.
- [x] Confirmación del contrato anónimo/claim.
- [x] Evidencias de backend y frontend.

Único punto abierto, no bloqueante para el trabajo local: publicación a Internet (dominio/VPS),
recogido en §8.3. Con esto quedó cerrada la validación real de F7/Supabase; la actualización de
`Plan.md`, `docs/api.md` y `README.md` queda fuera de este cierre (solo se commitea este archivo).

---

## 7. Respuesta local — 2026-11-24 (histórica)

> Esta sección quedó **superada por §8**, que cierra la validación con Supabase, backend y
> frontend reales en el nodo de Santiago. Se conserva como registro del estado previo, cuando
> aún no había credenciales ni acceso al nodo.

Sí se encontró este documento (`docs/handoff/solicitud-datos-santiago.md`) y se revisó punto por
punto. **No se inventó ningún dato sensible**: no llegaron claves, URLs privadas ni acceso al nodo,
y nada de eso se pegó en el repositorio.

### 7.1 Lo que ya se pudo entregar desde aquí

| Sección solicitada | Estado | Detalle / evidencia |
| :--- | :--- | :--- |
| §0 Contexto | ✅ actualizado | `Yoann` integrada en `main` (`efe5d0a`); 64/64 tests de backend y 15/15 PlayMode |
| §1 Datos de Supabase/JWT | ⛔ pendiente de Santiago | No hay claves ni proyecto confirmado; no se generaron valores falsos |
| §1.2 Migraciones y `004` | ⛔ pendiente de Santiago | Sin acceso a Supabase; el repositorio solo contiene los `.sql` |
| §1.3 Imagen y despliegue | ⛔ pendiente de Santiago | `costamarindo-backend:latest` sigue pendiente; Docker estaba apagado |
| §2 Nodo Frontend | 🟡 parcial | No hay acceso al nodo; sí se verificó el build en local con el servidor de desarrollo |
| §2.4 Validación en navegador | ✅ en local | `Login → Jugar → Game`, fin de partida, panel legible y estado offline; ver [evidencia F7](../../evidencias/F7/13-respuesta-local-frontend.md) |
| §3 Contrato anónimo/claim | ✅ confirmado por código | Falta la aceptación explícita de Santiago antes de publicar |
| §4 Evidencias | ✅ para frontend | [`/evidencias/F7/13-respuesta-local-frontend.md`](../../evidencias/F7/13-respuesta-local-frontend.md) y capturas `14` y `15`; backend/DB siguen pendientes |
| §5 Entrega de secretos | ✅ sin cambios | No se usó Git, chat ni documentos para secretos; se mantiene la regla de oro |
| §6 Checklist | 🟡 parcial | Marcado lo verificable en local; el resto espera respuesta de Santiago |

Detalles de la respuesta local: **16,5 MB**, `unityReady: true`, **1,2 s**, 0 errores de app,
bocadillo con alpha 0,55 y textos del fin de partida sin solapamiento. Los dos 404 de `/api`
que aparecen en local son esperados: el servidor de desarrollo no despliega backend y el juego
ahora muestra «Sin conexión: puntaje guardado en este dispositivo.» en vez de un error rojo.

### 7.2 Lo que necesitamos que Santiago confirme para cerrar

Las casillas sin marcar de §1–§4 siguen vigentes, en especial: proyecto **DEV**, migración `004`
con `check_db.py` en verde, responsable de `costamarindo-backend:latest`, ruta/comando de
publicación del build y la URL real del juego. Con eso se puede repetir la medición con
Nginx/TLS y generar las evidencias F7 contra el backend real.

### 7.3 Publicación web e ISO/IEC 27001:2022

- **Web:** el build WebGL se compila y mide en local, pero **no hay URL pública**. Para compartir
  rápido alcanza una demo estática (por ejemplo itch.io, sin ranking online); para la versión
  completa hace falta el VPS con Docker + Nginx + HTTPS descrito en §2. El navegador debe hablar
  siempre a `/api/` del mismo origen; los secretos quedan solo en el backend.
- **ISO:** la SoA (93 controles) y A.8.25 están cerrados; A.8.15, A.8.20 y A.8.24 son parciales y
  faltan seis controles (A.8.5, A.8.9, A.8.12, A.8.19, A.8.28, A.8.31) con sus evidencias de
  ataque. Parte de ese cierre depende de lo que se pide aquí: sin backend no se pueden atacar
  A.8.5/A.8.24/A.8.28, sin publicación ni hashes no se pueden cerrar A.8.12 y A.8.19, y A.8.31
  necesita un proyecto PROD. Detalle en [`security.md`](security.md) §5 y `Plan.md` §7.

---

## 8. Cierre verificado en el nodo de Santiago — 2026-11-25

Esta sección responde de forma definitiva a §1–§6. Todo se comprobó en el equipo de Santiago con
el `.env` local, el MCP de Supabase y los contenedores reales. **No contiene secretos**: solo
nombres, estados, prefijos de clave, códigos de salida y salidas recortadas. No se modificó ningún
proyecto PROD.

### 8.1 Entorno y commit

| Ítem | Valor verificado |
| :--- | :--- |
| Commit | `a18d7ab3abe59b923f5524d57a5bd4619d84cfdd` (2026-11-24) |
| Rama | `main`, alineada con `origin/main`; árbol limpio antes de este cambio |
| Backend y frontend | Un solo repositorio, mismo `HEAD` |
| Sistema operativo | Windows 11 Pro, build `10.0.26300.0` |
| Docker / Compose | `29.9.2` / `v5.6.1` |
| Resolución local | `127.0.0.1 juego.local` en el archivo `hosts` |
| TLS | mkcert local (`infra/tls/juego.local.pem` y su clave), confiado en el almacén del usuario |

Arranque usado: `powershell -NoProfile -File infra/scripts/up.ps1 backend`, que compone
`infra/docker-compose.yml` + `infra/docker-compose.override.yml` con `--env-file .env` y el perfil
`backend` (`up -d --wait`). Antes hubo que retirar contenedores y redes huérfanos de un intento
previo para que el arranque terminara sin conflictos.

### 8.2 sg-db y sg-backend

**Supabase (MCP y scripts).**

- `get_project_url` → `https://jukyiwsteuzwrmzlgfme.supabase.co`; región `us-east-1`.
- `get_publishable_keys` → una clave `sb_publishable_…` activa y la legacy `anon` activa. La clave
  administrativa `sb_secret_…`, la URL del pooler y los secretos propios se tomaron del `.env`
  sin imprimir sus valores.
- `GET /auth/v1/health` → `200` (GoTrue `v2.198.0`).
- `execute_sql` → PostgreSQL 17.11. Migraciones en `private.schema_migrations`:
  `001_schema.sql` y `002_rls.sql` (2026-11-06), `003_harden_surface.sql` (2026-11-06) y
  `004_anonymous_claim.sql` (2026-11-16T23:45Z).
- `migrate.py --status` → cuatro migraciones como aplicadas, `EXIT=0`. No se reaplicó `004`.
- `check_db.py` → `EXIT=0`: RLS activa en todas las tablas, **0** políticas, **0** privilegios de
  tabla para `anon`/`authenticated`, `USAGE` sobre `public` igual a `false` para ambos y `true`
  para `service_role` (D13), y `get_leaderboard(p_user_id uuid, p_limit integer)` presente.
- `user_id` acepta `NULL` en `scores` y `game_sessions` (migración `004`), con el índice parcial
  `scores_named_best_idx` para rankear solo los puntajes reclamados.
- Advisories del MCP: `health` sin hallazgos. `security` devuelve un aviso **INFO** esperado por
  diseño: RLS activa sin políticas en cinco tablas (cuatro `public` y `private.schema_migrations`),
  coherente con D13 — [documentación del lint](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy).
  `performance` devuelve un aviso **INFO** por dos índices aún no usados en `game_sessions` —
  [documentación del lint](https://supabase.com/docs/guides/database/database-linter?lint=0005_unused_index).
  Ninguno requiere cambio: el backend accede con `service_role` y el volumen de datos es mínimo.

**Contenedores.**

```
NAME          STATUS                       PORTS
sg-ai         Up (healthy)                 11434/tcp
sg-backend    Up (healthy)                 8000/tcp
sg-frontend   Up (healthy)                 0.0.0.0:443->443/tcp, 0.0.0.0:8081->8080/tcp
```

- Imagen `costamarindo-backend:latest` reconstruida desde `main` (la anterior era más antigua).
- `sg-backend` y `sg-ai` tienen sus puertos solo como `expose`; Nginx es el único acceso HTTP.
- `GET /api/health` → `{"status":"ok","env":"dev","database":true}`.

**E2E real** (`CT_E2E_BASE=http://127.0.0.1:8081`, `backend/scripts/e2e_check.py`) → `EXIT=0`,
`E2E en verde.` Sin tokens ni secretos en la salida.

| Paso | Resultado |
| :--- | :--- |
| health | `ok`, `database: true` |
| registro / login / refresh (compatibilidad) | `201` / `200` / rotación correcta |
| bloqueo progresivo | `401` ×4 y `423` al quinto fallo; `account_locked` |
| sesión y puntaje anónimos | sesión creada; puntaje `720` aceptado con `in_top5: true` |
| reclamo top-5 | `ok: true`, `rank_position: 1` |
| ataques | `session_used`, `bad_signature` e `implausible_score` rechazados |
| replay exacto | primer envío aceptado; el segundo devuelve `session_used` |
| leaderboard | `200`; solo la entrada reclamada, sin `is_me` para el jugador anónimo |
| límites de entrada | inyección devuelta con `blocked: true`; texto largo rechazado con `422` |
| limpieza | usuario de prueba eliminado al final |

### 8.3 sg-frontend y diálogo

- `curl --ssl-no-revoke https://juego.local/` → `200`; `/api/health` por el mismo origen → `200`
  con el JSON anterior. El flag `--ssl-no-revoke` se debe a la CA local de mkcert en `curl` de
  Windows; el navegador confía el certificado sin él.
- Cabeceras comprobadas: `Content-Security-Policy` con el hash del script inline, HSTS
  (`max-age=31536000`), `Cache-Control: no-cache` y `Server: nginx`. El SHA-256 calculado sobre el
  script de `builds/webgl/index.html` coincide con la directiva de `infra/nginx/snippets/csp.conf`.
- Medición en navegador real (Edge/Chromium con `tools/dev-server/check.mjs`): `unityReady: true`,
  carga **1,5 s**, canvas **960×600**, **16,5 MB** transferidos, `httpErrors: []` y
  `consoleErrors: []`. Únicamente advertencias de rendimiento de WebGL (`ReadPixels` con
  SwiftShader), sin errores de aplicación.
- Panel del NPC y del fin de partida: capturas ya versionadas en
  [`14-webgl-bocadillo-translucido.png`](../../evidencias/F7/14-webgl-bocadillo-translucido.png) y
  [`15-webgl-game-over-legible.png`](../../evidencias/F7/15-webgl-game-over-legible.png). La
  captura nueva de esta sesión se guardó fuera del repo y aquí solo se resume el resultado.
- `/api/dialog` a través del proxy: `dialog_ok` en los logs del backend, respuesta generada por
  Ollama (`qwen2.5:3b` presente en `sg-ai`, sin puerto publicado). Un cuerpo vacío devuelve
  `invalid_request` y registra `dialog_blocked`; es el guardrail esperado.

**Pendiente, no bloqueante para el trabajo local:** no hay dominio público ni VPS;
`https://juego.local` funciona solo en este equipo. Para acceso externo hay que publicar `443` con
un dominio real y un certificado válido, manteniendo al navegador hablando únicamente con `/api/`
del mismo origen. El playtest humano de FE-11 también queda para esa fase.
