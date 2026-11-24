# Solicitud de datos y accesos — Santiago (Backend/Supabase + Frontend)

> **Para:** Santiago y su agente.
> **Fecha:** 2026-11-23.
> **Rama verificada:** `Yoann` ya está en `85a5205` (fast-forward de `origin/main`; 64 tests).
> **Objetivo:** desbloquear la validación real de F7/Supabase y usar el nodo Frontend para publicar
> y probar el build de `main`.
>
> **Regla de oro:** no enviar claves, tokens, contraseñas, cadenas de conexión ni archivos `.env`
> por Git, chat, capturas o documentos. Usar un gestor de secretos o configurarlos directamente en
> el nodo. En este documento solo se piden **nombres, estados y salidas sin secretos**.

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

Falta:

1. Las claves y datos de Supabase/JWT para arrancar `sg-backend`.
2. Aplicar y verificar `004_anonymous_claim.sql` en Supabase DEV.
3. Reconstruir y desplegar la imagen real del backend.
4. Publicar el build de `main` en el nodo Frontend y validar NPC + reclamo en navegador.
5. Generar las evidencias F7 reales y actualizar la documentación.

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

También pedimos confirmar:

- Región del proyecto y host del pooler.
- Si el proyecto es **DEV**, **staging** o **PROD**. No queremos ejecutar pruebas destructivas en
  PROD.
- Si la clave `SUPABASE_SECRET_KEY` es la nueva `sb_secret_…` o la legacy `service_role`.
- Si el usuario de base de datos del pooler tiene permisos para ejecutar las migraciones.
- Si el proyecto tiene activado el registro público de Supabase Auth. El backend crea usuarios
  con `email_confirm=true` y correo sintético `@players.costamarindo.local`; necesitamos saber si
  GoTrue rechaza ese dominio o exige confirmación por correo.

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

Necesitamos confirmar:

- [ ] `001_schema.sql`, `002_rls.sql`, `003_harden_surface.sql` y `004_anonymous_claim.sql`
      aparecen aplicadas.
- [ ] `check_db.py` termina sin fallos.
- [ ] `anon` y `authenticated` no tienen `USAGE` sobre `public` (D13).
- [ ] `scores.user_id` acepta `NULL` para el flujo anónimo y el `claim` lo actualiza.
- [ ] El leaderboard solo muestra puntajes reclamados.
- [ ] `login_attempts` y el bloqueo progresivo funcionan en el proyecto real.
- [ ] Existe un plan de backup antes de aplicar `004` (idealmente un snapshot de Supabase).

No pegar en la respuesta contraseñas, cadenas de conexión ni claves. Basta con el estado y los
códigos de salida.

### 1.3 Imagen y despliegue del backend

La imagen `costamarindo-backend:latest` local es anterior a `main` y no incluye
`app/providers/ollama.py`. La imagen `costamarindo-backend:main-test` sí se construyó desde
`main`.

Pedimos confirmar:

- [ ] Quién es el responsable de reconstruir y desplegar `costamarindo-backend:latest`.
- [ ] Si el backend real corre en el nodo de Santiago, en el nuestro o en otro host.
- [ ] Ruta del `docker-compose` y nombre exacto del servicio/contenedor.
- [ ] Si el backend tiene salida a `sg-ai:11434` por la red `costamarindo-ai`.
- [ ] Si `8000` sigue sin publicarse y Nginx es el único acceso.
- [ ] Si el `.env` del backend ya está cargado en ese host (sin enviar valores).

---

## 2. Datos del nodo Frontend

Ahora que Santiago tiene el nodo Frontend, necesitamos saber cómo publicar y validar el build de
`main`.

### 2.1 Identificación y acceso

- [ ] Nombre del nodo, proveedor y tipo (VM, host físico, contenedor).
- [ ] URL o dominio real del juego (no solo `https://juego.local`).
- [ ] Dirección IP/hostname y puerto SSH, si vamos a administrarlo.
- [ ] ¿Está en Tailscale, VPN o red privada del proyecto? ¿Necesitamos una auth key/ACL?
- [ ] Usuario y método de acceso (llave SSH, agente, consola del proveedor). **No enviar
      contraseñas por chat.**
- [ ] Si prefiere no dar acceso: confirmar que él ejecutará el despliegue y devolverá la salida.

### 2.2 Stack y publicación

- [ ] Sistema operativo y versiones de Docker, Docker Compose y Nginx.
- [ ] Ruta del repositorio y del artefacto `builds/webgl` en el nodo.
- [ ] Servicio/contenedor que sirve el juego (`sg-frontend` u otro).
- [ ] Si `builds/webgl` está montado como volumen o copiado dentro de la imagen.
- [ ] Comando exacto para publicar un build nuevo.
- [ ] Si hay que regenerar `infra/nginx/snippets/csp.conf` y recargar Nginx.
- [ ] Si el nodo tiene CI/CD o si el despliegue es manual.
- [ ] Commit y fecha del build que está publicado ahora.
- [ ] Si el `index.html` actual ya incluye el NPC y el panel de reclamo de `main`.

### 2.3 Red, TLS y seguridad

- [ ] Dominio y certificado TLS: emisor, vigencia y si el navegador lo confía.
- [ ] Si se usa mkcert, dónde está la CA raíz y cómo se instala en un equipo limpio.
- [ ] Configuración de Nginx: proxy `/api/` hacia el backend, cabeceras CSP/HSTS y rate limit.
- [ ] Puertos abiertos: idealmente solo 443/SSH; confirmar que `8000` y `11434` no están
      publicados.
- [ ] Si el nodo Frontend tiene alguna clave de Supabase. **No debería tenerla**: el navegador
      solo habla con `/api/`.
- [ ] Si hay logs accesibles y cómo se leen sin exponer datos personales.

### 2.4 Validación que necesitamos hacer en ese nodo

- [ ] Abrir la URL real y confirmar `200`.
- [ ] Jugar una partida y ver el pensamiento del NPC.
- [ ] Terminar la partida y ver el panel de reclamo si el puntaje entra al top-5.
- [ ] Confirmar en DevTools que las llamadas son a `/api/session/start`, `/api/score`,
      `/api/claim` y `/api/dialog`, y que no hay llamadas directas a Supabase.
- [ ] Confirmar que el puntaje, las vidas y el estado de la partida no cambian por el NPC.
- [ ] Guardar captura y salida de consola/red sin secretos.

---

## 3. Decisiones de contrato que deben confirmarse

`main` cambió el contrato respeto al handoff anterior. Necesitamos que Santiago confirme:

- [ ] El flujo **anónimo** es el oficial: se juega sin login y solo se pide nombre + PIN si el
      puntaje entra al top-5.
- [ ] `/api/session/start` y `/api/score` no requieren token; `/api/dialog` acepta token
      opcional.
- [ ] `/api/claim` con PIN de 4–6 dígitos es el único camino de identidad del jugador.
- [ ] La escena `Login` sigue existiendo solo por compatibilidad o se elimina del flujo.
- [ ] `main` es la rama de integración y `Yoann` ya quedó al día con `main`.
- [ ] Si hay commits pendientes en otra rama de Santiago, decirlo antes de publicar.

Si alguna de estas decisiones no es la correcta, decirlo **antes** de aplicar `004` o publicar el
build; cambiar el contrato después obliga a rehacer pruebas y evidencias.

---

## 4. Evidencia que debe devolver Santiago

Sin secretos y sin datos personales:

- `git rev-parse HEAD` del backend y del frontend.
- `docker compose ps` (nombres y estados, sin variables).
- Salida de `migrate.py --status` y `check_db.py`.
- `GET /api/health` con `status`, `env` y `database`.
- Códigos HTTP de `/api/session/start`, `/api/score`, `/api/claim`, `/api/leaderboard` y
  `/api/dialog`.
- Extracto de logs `dialog_ok` / `dialog_blocked` / `dialog_fallback`, sin tokens.
- Para el frontend: URL final, commit desplegado, captura del NPC y del panel de reclamo,
  y errores de consola (idealmente ninguno).
- Confirmación de que `8000` y `11434` no están publicados.

Formato sugerido: un `.md` o `.txt` en `evidencias/F7/` con fecha, commit, comandos y salidas
recortadas. Si se prefiere, Santiago puede dejarlo en una rama y nosotros lo integramos.

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

Santiago puede responder marcando o adjuntando:

- [ ] Variables de Supabase/JWT entregadas por canal seguro (o cargadas en el nodo).
- [ ] Confirmación de proyecto DEV y región/pooler.
- [ ] `004_anonymous_claim.sql` aplicada y `check_db.py` en verde.
- [ ] Responsable y estado de la imagen `costamarindo-backend:latest`.
- [ ] Acceso o procedimiento de despliegue del nodo Frontend.
- [ ] URL real, commit publicado y estado del build.
- [ ] Confirmación del contrato anónimo/claim.
- [ ] Evidencias de backend y frontend.

Con esos datos podemos ejecutar la validación real, generar las evidencias F7 y actualizar
`Plan.md`, `docs/api.md`, `README.md` y los handoffs sin volver a bloquear el trabajo.
