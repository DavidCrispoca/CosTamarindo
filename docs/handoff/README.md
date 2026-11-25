# Handoff · CosTamarindo 2D

> **Para quién:** las personas que toman Frontend, Security e IA.
> **Estado del proyecto:** 2026-11-25 (`main`; último commit de código `e2de1e5`, más este cierre
> documental). `sg-backend`, `sg-db` y `sg-frontend` están operativos y validados en el equipo.
> **Fuente de verdad del avance:** [`../../Plan.md`](../../Plan.md) §3 y [`../../contexto.md`](../../contexto.md).
> **Contratos de la API:** [`../api.md`](../api.md).

Este paquete explica **qué hay hecho, qué falta, cómo se levanta y qué reglas no se tocan**,
división por división. No reemplaza a `Plan.md`: lo hace accionable.

| Documento | Para qué |
| :--- | :--- |
| [`contexto.md`](../../contexto.md) | **contexto completo del proyecto**: qué es, arquitectura, estado por nodo, evidencias y todo lo pendiente/en curso |
| [`frontend.md`](frontend.md) | estado del cliente Unity/WebGL, contrato anónimo y backlog `FE-01`…`FE-14` |
| [`security.md`](security.md) | atacar y evidenciar los 10 controles (`SEC-01`…`SEC-14`) |
| [`ia.md`](ia.md) | conectar Ollama al NPC sin romper el puntaje (`IA-01`…`IA-07`) |
| [`solicitud-datos-santiago.md`](solicitud-datos-santiago.md) | **pedir a Santiago** claves, migraciones y acceso al nodo; §7 registra lo ya entregado en local |
| [`solicitud-datos-santiago-pendientes.md`](solicitud-datos-santiago-pendientes.md) | **seguimiento 2026-11-24:** solo lo que falta tras la respuesta parcial (backend/DB y nodo Frontend) |
| [`solicitud-aprobacion-backend-local.md`](solicitud-aprobacion-backend-local.md) | **aprobación 2026-11-25:** levantar backend local contra DEV y entregar secretos por canal seguro |
| [`respuesta-aprobacion-backend-local.md`](respuesta-aprobacion-backend-local.md) | **respuesta de Santiago (2026-11-25):** aprobación y entrega por vault |
| [`handoff-santiago-backend.md`](handoff-santiago-backend.md) | contexto histórico de F7/backend; el contrato vigente es anónimo + claim |
| [`handoff-alejandra-frontend.md`](handoff-alejandra-frontend.md) | contexto histórico de integración del NPC; el estado vigente está en `frontend.md` |
| [`evidencias/_plantilla/README.md`](../../evidencias/_plantilla/README.md) | formato obligatorio de una carpeta de evidencia |

---

## 1. Estado real en una tabla

| División | Nodo | Estado | Qué falta |
| :--- | :--- | :--- | :--- |
| **Frontend** | `Sg-Frontend` | ✅ build Release del 2026-11-24 (16,5 MB, 1,5 s, 0 errores de app) servido en `https://juego.local` y validado contra el backend real; 12 ítems curados, UI legible y API cliente implementada; `builds/` no se versiona | publicar el build en URL pública; `FE-05`/`FE-06` (bloqueados por `FIGMA_TOKEN`) y `FE-11` (playtest humano) |
| **Backend** | `Sg-Backend` | ✅ API y **64/64 tests**; imagen `costamarindo-backend:latest` reconstruida desde `main`; `sg-backend` healthy; E2E real en verde (`EXIT=0`) | nada bloqueante; ver evidencias `F7/16` y `F7/17` |
| **DB** | `Sg-DB` | ✅ esquema 001–004 aplicado y verificado con `check_db.py` (`EXIT=0`); RLS sin acceso de cliente; backup 2026-11-06 probado | proyecto PROD al desplegar (DB-08) |
| **Security** | `Sg-Security` | 🟡 A.8.25, SEC-01, SEC-02, SEC-06 (parcial), SEC-08 (parcial), SEC-12, SEC-13 y SEC-14 cerrados; SoA de 93. **`backend.log` ya se escribe** en `costamarindo-backend-logs` | ataques reales de los 10 controles (ya desbloqueados por el backend real), alerta FIM end-to-end y prueba de aislamiento |
| **IA** | `Sg-AI` | ✅ Ollama healthy, `qwen2.5:3b`, endpoint y pensamiento probados; **validado contra el backend real/Supabase** (`dialog_ok`, `F7/16` y `F7/17`) | nada bloqueante |

### 1.1 Pendientes consolidados (revisión 2026-11-25)

Leyenda: ✅ hecho · 🟡 en curso · ⬜ pendiente · ⛔ bloqueado. Todo lo abierto está documentado;
no se inventó estado ni se subió ningún secreto.

| Pendiente | Estado | Responsable / bloqueo | Seguir en |
| :--- | :--- | :--- | :--- |
| Sincronizar la documentación y generar las evidencias F7 reales | 🟡 en curso | Santiago / docs | este README y `F7/16`, `F7/17` |
| Publicar el WebGL en URL pública (dominio/VPS o demo estática) | 🟡 en curso | decisión de dominio/acceso | [`solicitud-datos-santiago.md`](solicitud-datos-santiago.md) §8.3 |
| `FE-05`/`FE-06`: 10 prefabs y kit de Figma | ⛔ bloqueado por `FIGMA_TOKEN` | Frontend | [`frontend.md`](frontend.md) §2.4 |
| `FE-11`: playtest humano de 10–15 min | ⬜ pendiente de una persona | Frontend | [`frontend.md`](frontend.md) §2.4 |
| Ataques ISO reales y evidencias (A.8.5, A.8.9, A.8.12, A.8.19, A.8.24, A.8.28, A.8.31) | 🟡 en curso (backend real arriba y `backend.log` disponible) | Security | [`security.md`](security.md) §5 |
| Proyecto PROD y `pg_dump` inicial (DB-08 / A.8.31) | ⬜ pendiente al desplegar | DB / Security | `Plan.md` §6 |
| Informe final de entrega | ⬜ después del despliegue real | — | [`../../Plan.md`](../../Plan.md) §3 |

### 1.2 Estado del despliegue web

- **Listo:** build WebGL Release generado y medido (`tools/dev-server/check.mjs`), servido por
  `sg-frontend` en `https://juego.local` (Nginx con TLS mkcert, CSP por hash y `/api/` proxificado),
  y validado contra el backend real (`F7/16` y `F7/17`).
- **No listo:** no hay URL pública ni commit desplegado. `juego.local` solo es accesible desde esta PC.
- **Dos rutas:** demo estática (por ejemplo itch.io; sin ranking online) para compartir rápido, o
  VPS con Docker + Nginx + HTTPS + `/api/` para la versión completa. La arquitectura definitiva ya
  está descrita en `solicitud-datos-santiago.md` §2; falta decidir dominio y procedimiento de
  publicación.
- **Regla:** secretos solo en el backend; nunca en Unity/WebGL ni en Git.

---

## 2. Cómo levantar todo desde cero

### 2.1 Requisitos

| Herramienta | Versión |
| :--- | :--- |
| Unity Hub + Unity | `6000.3.27f1` con módulo WebGL |
| Python | 3.12 |
| Docker Desktop (WSL2) | última estable |
| Node.js | 20+ (solo `tools/`) |
| mkcert | última |

### 2.2 Primer arranque

```powershell
# 1. Variables de entorno (pedirlas al responsable de DB; nunca se versionan)
Copy-Item .env.example .env

# 2. Dependencias del backend
python -m venv backend/.venv
backend\.venv\Scripts\python.exe -m pip install -r backend\requirements-dev.txt

# 3. Certificado local y hosts (una sola vez por máquina)
powershell -File infra/scripts/tls.ps1
# agregar a C:\Windows\System32\drivers\etc\hosts:  127.0.0.1  juego.local

# 4. Base de datos: aplicar migraciones y verificar
backend\.venv\Scripts\python.exe backend\scripts\migrate.py
backend\.venv\Scripts\python.exe backend\scripts\check_db.py

# 5. Build del juego y entorno completo
powershell -File infra/scripts/build_webgl.ps1
powershell -File infra/scripts/up.ps1 backend

# 6. Comprobar
curl.exe --ssl-no-revoke https://juego.local/api/health
```

Resultado esperado: `sg-frontend`, `sg-backend` y `sg-ai` en `healthy` y
`{"status":"ok","env":"dev","database":true}`. Las variables de Supabase/JWT ya están en `.env` y
el modelo `qwen2.5:3b` ya está descargado; solo hay que cargar `.env` en una máquina nueva. El
build WebGL ya está generado; regenerarlo si se cambia Unity.

### 2.3 Verificación rápida de que todo está sano

```powershell
backend\.venv\Scripts\python.exe -m pytest backend        # 64 passed
backend\.venv\Scripts\python.exe backend\scripts\check_db.py
$env:CT_E2E_BASE = "https://juego.local"
backend\.venv\Scripts\python.exe backend\scripts\e2e_check.py
```

### 2.4 Si algo no arranca

| Síntoma | Causa habitual | Solución |
| :--- | :--- | :--- |
| `docker compose` no conecta | Docker Desktop apagado | abrirlo y esperar el motor |
| `NET::ERR_CERT_AUTHORITY_INVALID` | falta el certificado | `infra/scripts/tls.ps1` y reiniciar el navegador |
| `juego.local` no resuelve | falta la entrada en `hosts` | agregarla (el archivo suele estar en solo lectura) |
| `/api/health` → 502 | backend sin levantar | `up.ps1 backend` o `docker compose --profile backend up -d` |
| `/api/health` → 503 | base inaccesible | revisar `SUPABASE_DB_URL` (pooler **5432**) |
| Pantalla de carga infinita | CSP o `Content-Encoding` | regenerar `csp.conf` y `docker exec sg-frontend nginx -s reload` |

---

## 3. Secretos y datos sensibles

| Elemento | Dónde vive | Regla |
| :--- | :--- | :--- |
| `.env` | solo en cada máquina | **nunca** se versiona; `.gitignore` lo cubre |
| Claves de Supabase | `.env` del responsable de DB | `sb_secret_...` jamás sale del servidor |
| `PIN_PEPPER`, `SESSION_MASTER_KEY`, `BACKEND_JWT_SECRET` | `.env` | si se rotan, hay que reiniciar el backend |
| Certificados `infra/tls/` | local | no se versionan |
| `infra/backups/*.dump` | local | contienen datos; no se versionan |

Para trabajar en Frontend **no hace falta** ninguna clave de Supabase: Unity solo habla con
`/api/`. Para Security, las claves de ataque se piden al responsable de DB y se usan desde
`.env`, nunca en un script versionado.

### 3.1 MCP de Supabase en OpenCode

El MCP remoto (`https://mcp.supabase.com/mcp`) por defecto usa OAuth y abre el navegador para
iniciar sesión con tu cuenta. Para **fijar la conexión a una cuenta concreta**
(`est.david.crispoca@unimilitar.edu.co`) y no depender de la sesión OAuth que OpenCode tenga
guardada, en `opencode.json` del repositorio (ignorado por git):

```json
"supabase": {
  "type": "remote",
  "url": "https://mcp.supabase.com/mcp",
  "oauth": false,
  "headers": { "Authorization": "Bearer {file:C:/Users/USUARIO/.config/opencode/.supabase-mcp-token}" }
}
```

`"oauth": false` es obligatorio: sin él, OpenCode ignora el header y usa la sesión OAuth
guardada. Además, si el servidor está definido en forma plana (`mcp.supabase`) y también bajo
`mcp.servers.supabase`, la forma plana tiene prioridad; el header debe estar en la que gane.

El token se genera en el dashboard de Supabase (`Account → Access Tokens`) **con la cuenta
correcta iniciada** (los tokens acotados empiezan con `sbp_fc`). OpenCode **no** carga `.env`
para resolver `{env:...}` (solo lee `process.env`), así que el token se guarda en un archivo
fuera del repo (`~/.config/opencode/.supabase-mcp-token`, sin salto de línea final) y se
referencia con `{file:...}`. Verificación: `list_projects` debe devolver `CosTamarindo`
(`jukyiwsteuzwrmzlgfme`, organización *Eunomia*). Si el MCP muestra otros proyectos,
**reiniciar OpenCode**: los encabezados se leen al conectar, no en cada llamada.

---

## 4. Qué está versionado y qué no

**Sí entra al repositorio:** `backend/` completo (código, SQL, pruebas, scripts, Dockerfile),
`infra/` (compose, nginx, scripts), `docs/`, `evidencias/`, arte ligero (`assets/figma/items`,
fuentes), el proyecto Unity (`unity/` sin `Library/`).

**No entra:** `.env`, `infra/tls/`, `infra/backups/`, `builds/`, `.venv/`, `__pycache__/`,
`node_modules/`, `unity/Library/`, `Proceso.md` (bitácora interna).

Un clon limpio + `.env` + `pip install -r requirements-dev.txt` alcanza para correr las 64
pruebas. Si alguna prueba falla en un clon nuevo, es un archivo que falta: avisar antes de
improvisar.

---

## 5. Orden recomendado de trabajo

```
Frontend FE-03 -> FE-02 -> FE-01     (arte sano, no bloquea nada mas)
Frontend FE-07                        (clientes de red; necesita API, ya lista)
IA IA-02 -> IA-03 -> IA-05 -> IA-06    (prompt, guardrails, fallback, pruebas; ✅)
IA bocadillo WebGL                      (✅ probado; integrar en Frontend)
Security SEC-01 -> SEC-02 -> SEC-03…  (plantilla, SoA, ataques)
```

Los ataques de Security **necesitan** que Frontend tenga al menos `FE-07` para los controles
que se demuestran desde el navegador (A.8.9, A.8.12). Los de servidor (A.8.24, A.8.5, A.8.28,
A.8.20, A.8.15, A.8.31) se pueden hacer ya con `curl`.

---

## 6. Reglas de convivencia entre divisiones

1. **Un cambio de contrato se avisa en el mismo PR.** Si cambia `docs/api.md`, se actualiza
   el documento y se avisa a Frontend e IA.
2. **Frontend no toca `backend/`; Backend no toca `unity/`.** La integración se hace contra
   el contrato, no contra el código del otro.
3. **IA no toca el camino de puntaje.** `/api/score` no llama a Ollama, ni lo hará.
4. **Security no implementa funcionalidad:** ataca, documenta y propone la corrección.
5. **Nada de secretos en capturas.** Antes de guardar una evidencia, revisar que no se vea
   una clave, un token o una IP real sin enmascarar.
6. **Conventional Commits** con alcance (`unity`, `backend`, `infra`, `db`, `art`, `docs`,
   `sec`, `ci`) y sin atribución de IA.

---

## 7. Decisiones que no se reabren

Están en `Plan.md` §11.5 (`D1`–`D13`). Las que más afectan a las otras divisiones:

- El cliente **no** habla con Supabase: todo por la API (D10).
- La API usa el pooler en modo sesión, 5432 (D11).
- `anon`/`authenticated` no tienen acceso ni `USAGE` sobre `public` (D13).
- La clave HMAC de sesión no se almacena: se deriva (D13 y `score_service`).
- El PIN se deriva con `PIN_PEPPER` antes de GoTrue (D9).
