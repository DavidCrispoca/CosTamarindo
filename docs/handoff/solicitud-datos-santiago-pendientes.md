# Solicitud pendiente a Santiago — cierre real de backend, DB y Frontend

> **Para:** Santiago y su agente.
> **Fecha:** 2026-11-24.
> **Base verificada:** `main` en `a18d7ab`; `Yoann` ya quedó alineada con `main`.
> **Respuesta previa:** [`solicitud-datos-santiago.md`](solicitud-datos-santiago.md) §7.
> **Objetivo:** completar **solo lo que todavía no se puede resolver en este repositorio**:
> Supabase/JWT, migración `004`, imagen del backend, nodo Frontend y evidencias reales.
>
> **Regla de oro:** no enviar claves, tokens, contraseñas, cadenas de conexión ni `.env` por Git,
> chat, issues, PRs o capturas. Usar un gestor de secretos o cargar los valores directamente en el
> nodo. En este documento solo se piden **estados, nombres, rutas y salidas sin secretos**.

---

## 1. Veredicto de la respuesta del 2026-11-24

La respuesta es **parcial**: sirve para cerrar la parte local del frontend, pero **no desbloquea
F7 ni el backend real**. No se recibieron claves, migraciones aplicadas, backend desplegado, URL
pública ni acceso al nodo de Santiago.

| Área | ¿Respondida? | Qué falta |
| :--- | :--- | :--- |
| Build WebGL local, UI y PlayMode | ✅ sí | Nada nuevo; ya está documentado en §7 y en `evidencias/F7/13` |
| Contrato anónimo + claim | 🟡 solo por código | Confirmación explícita de Santiago |
| Supabase DEV, claves y pooler | ⛔ no | Proyecto, variables y entrega segura |
| Migración `004_anonymous_claim.sql` | ⛔ no | Aplicación, backup y `check_db.py` |
| Imagen `costamarindo-backend:latest` | ⛔ no | Reconstrucción y despliegue |
| Backend real y `/api/health` | ⛔ no | Contenedor arriba y salida verificable |
| Nodo Frontend: URL, acceso y publicación | ⛔ no | Dominio/IP, acceso o procedimiento, commit publicado |
| Evidencias reales de backend/DB | ⛔ no | Salidas de E2E, migraciones y health sin secretos |

**No hace falta volver a enviar** el build local, las capturas `14`/`15`, los 15/15 PlayMode, los
64 tests de backend ni el resultado de `gitleaks`: ya están registrados.

---

## 2. Bloqueantes P0

### 2.1 Supabase DEV y secretos

Necesitamos una de estas dos opciones:

**Opción A — cargar los valores en el nodo del backend** y confirmar únicamente que quedaron
definidos. No compartir los valores.

**Opción B — entregarlos por el gestor de secretos del proyecto.** Crear una entrada
`CosTamarindo DEV` y dar acceso al equipo; no pegar valores en el repositorio ni en el chat.

Variables requeridas:

| Variable | Confirmación necesaria |
| :--- | :--- |
| `SUPABASE_URL` | Proyecto DEV, región y URL base |
| `SUPABASE_PROJECT_REF` | Referencia del proyecto DEV |
| `SUPABASE_PUBLISHABLE_KEY` | Clave pública `sb_publishable_…` |
| `SUPABASE_SECRET_KEY` | Clave secreta `sb_secret_…`, solo en backend |
| `SUPABASE_DB_URL` | Pooler de **sesión, puerto 5432**, no 6543 |
| `BACKEND_JWT_SECRET` | 32+ caracteres; puede generarlo el equipo |
| `PIN_PEPPER` | 16+ caracteres; puede generarlo el equipo |
| `SESSION_MASTER_KEY` | 16+ caracteres; puede generarlo el equipo |

También confirmar:

- [ ] El proyecto es **DEV**, no PROD.
- [ ] El usuario del pooler puede ejecutar migraciones.
- [ ] El registro de Supabase Auth acepta el correo sintético
      `@players.costamarindo.local` con `email_confirm=true`.
- [ ] `SUPABASE_SECRET_KEY` es la clave nueva `sb_secret_…` o la legacy `service_role`.
- [ ] El nodo tiene salida a `sg-ai:11434` por la red interna.
- [ ] `8000` no está publicado; solo Nginx accede al backend.

### 2.2 Migración `004` y base de datos

Antes de aplicar `004`, confirmar que existe un backup o snapshot de Supabase DEV.

Si Santiago ejecuta las migraciones, devolver la salida **redactada** de:

```powershell
git rev-parse HEAD
backend\.venv\Scripts\python.exe backend\scripts\migrate.py --status
backend\.venv\Scripts\python.exe backend\scripts\migrate.py
backend\.venv\Scripts\python.exe backend\scripts\check_db.py
```

Si prefiere no ejecutarlas, darnos acceso temporal de solo lectura/administrado y lo hacemos
nosotros.

Confirmar explícitamente:

- [ ] `001_schema.sql`, `002_rls.sql`, `003_harden_surface.sql` y `004_anonymous_claim.sql`
      aparecen aplicadas.
- [ ] `check_db.py` termina sin fallos.
- [ ] `anon` y `authenticated` no tienen `USAGE` sobre `public`.
- [ ] `scores.user_id` acepta `NULL` y el claim lo actualiza.
- [ ] El leaderboard solo muestra puntajes reclamados.
- [ ] El bloqueo progresivo de login funciona en el proyecto real.

### 2.3 Imagen y backend real

La imagen `costamarindo-backend:latest` sigue siendo anterior a `main`. Necesitamos:

- [ ] Reconstruir `costamarindo-backend:latest` desde el commit actual de `main`
      (`a18d7ab`; el código de juego es el de `a376e07`).
- [ ] Desplegar el contenedor del backend en el nodo correspondiente.
- [ ] Confirmar que `docker compose ps` muestra el backend `Up`/`healthy`.
- [ ] Confirmar `GET /api/health` con `status=ok`, `env` y `database=true`.
- [ ] Confirmar que `8000` y `11434` no están publicados.
- [ ] Entregar un extracto de logs `dialog_ok` / `dialog_blocked` / `dialog_fallback` sin tokens.

### 2.4 Nodo Frontend y publicación

Necesitamos publicar el build en una URL real y validar el flujo completo.

**Si Santiago da acceso:**

- [ ] Host/IP, puerto SSH y método de acceso (llave, VPN, Tailscale o consola).
- [ ] Ruta del repositorio y del artefacto `builds/webgl` en el nodo.
- [ ] Archivo de Compose, servicio/contenedor y si `builds/webgl` es volumen o copia interna.
- [ ] Comando exacto de publicación y recarga de Nginx.

**Si prefiere ejecutar él:**

- [ ] Ejecutar el despliegue del build y devolver la salida.
- [ ] Confirmar URL final, commit desplegado, fecha y tamaño del build.
- [ ] Confirmar que `index.html` incluye el NPC y el panel de reclamo.
- [ ] Confirmar TLS, dominio, CSP/HSTS y proxy `/api/` al backend.
- [ ] Confirmar que el frontend **no** tiene claves de Supabase.
- [ ] Adjuntar captura del NPC y del panel de reclamo en la URL real.
- [ ] Adjuntar errores de consola/red (idealmente ninguno).

No alcanza con `localhost` ni con el servidor de desarrollo: eso ya está documentado y no es una
URL compartible.

---

## 3. Confirmaciones de contrato

Responder en una línea cada punto:

- [ ] El flujo **anónimo** es el oficial: se juega sin login y solo se pide nombre + PIN al
      reclamar el top-5.
- [ ] `/api/session/start` y `/api/score` no requieren token.
- [ ] `/api/dialog` acepta token opcional y funciona sin login.
- [ ] `/api/claim` con PIN de 4–6 dígitos es el único camino de identidad.
- [ ] La escena `Login` queda solo por compatibilidad.
- [ ] `main` es la rama de integración; no hay commits pendientes en otra rama.
- [ ] Santiago acepta ser el responsable de la imagen `latest` y del nodo Frontend, o indica quién
      lo es.

Si alguna respuesta es “no”, decirlo antes de aplicar `004` o publicar el build.

---

## 4. Evidencia mínima que debe devolver Santiago

Sin secretos y sin datos personales. Puede ser un `.md` o `.txt` en `evidencias/F7/`:

```text
Commit de main:
Proyecto DEV / región / pooler:
Variables cargadas en el nodo: sí/no (sin valores)
004 aplicada: sí/no
check_db.py: OK/FALLA (salida redactada)
Imagen latest: commit / fecha / estado
Backend: contenedor / healthy / health HTTP
Frontend: URL / commit / tamaño / fecha
Acceso al nodo: SSH / VPN / ninguno
Contrato anónimo aceptado: sí/no
Evidencias: rutas de archivos y capturas
```

También pedimos ejecutar el E2E actualizado y devolver el resultado:

```powershell
$env:CT_E2E_BASE = "https://<url-real>"
backend\.venv\Scripts\python.exe backend\scripts\e2e_check.py
```

Debe cubrir: health, sesión anónima, score, claim si entra al top-5, leaderboard, diálogo,
inyección y replay. No imprime tokens ni secretos.

---

## 5. Criterio de cierre

F7/backend se consideran cerrados cuando se cumpla todo esto:

- [ ] `.env` cargado en el backend, nunca en Git.
- [ ] `001`–`004` aplicadas y `check_db.py` en verde.
- [ ] `costamarindo-backend:latest` reconstruida y desplegada.
- [ ] `/api/health` responde `200` con `database=true`.
- [ ] `/api/session/start`, `/api/score`, `/api/claim`, `/api/leaderboard` y `/api/dialog`
      funcionan contra Supabase DEV real.
- [ ] El build está publicado en una URL accesible y el `index.html` corresponde al commit
      declarado.
- [ ] NPC y panel de reclamo visibles en la URL real.
- [ ] `8000` y `11434` no están publicados.
- [ ] Evidencias reales agregadas a `evidencias/F7/`.

Con esos datos el equipo puede ejecutar la validación final, generar las evidencias que faltan y
actualizar `Plan.md`, `docs/api.md`, `README.md` y los handoffs para cerrar el proyecto.
