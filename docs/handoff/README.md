# Handoff · CosTamarindo 2D

> **Para quién:** las personas que toman Frontend, Security e IA.
> **Estado del proyecto:** 2026-11-23 (merge de `main` en `85a5205`).
> **Fuente de verdad del avance:** [`../../Plan.md`](../../Plan.md).
> **Contratos de la API:** [`../api.md`](../api.md).

Este paquete explica **qué hay hecho, qué falta, cómo se levanta y qué reglas no se tocan**,
división por división. No reemplaza a `Plan.md`: lo hace accionable.

| Documento | Para qué |
| :--- | :--- |
| [`frontend.md`](frontend.md) | integrar Unity con la API, reparar el arte y cerrar `FE-01`…`FE-14` |
| [`security.md`](security.md) | atacar y evidenciar los 10 controles (`SEC-01`…`SEC-14`) |
| [`ia.md`](ia.md) | conectar Ollama al NPC sin romper el puntaje (`IA-01`…`IA-07`) |
| [`solicitud-datos-santiago.md`](solicitud-datos-santiago.md) | **pedir a Santiago** claves, migraciones, evidencias y datos/accesos del nodo Frontend |
| [`handoff-santiago-backend.md`](handoff-santiago-backend.md) | contexto histórico de F7/backend; el contrato vigente es anónimo + claim |
| [`handoff-alejandra-frontend.md`](handoff-alejandra-frontend.md) | integrar el NPC, regenerar WebGL y validar el bocadillo en su rama |
| [`evidencias/_plantilla/README.md`](../../evidencias/_plantilla/README.md) | formato obligatorio de una carpeta de evidencia |

---

## 1. Estado real en una tabla

| División | Nodo | Estado | Qué falta |
| :--- | :--- | :--- | :--- |
| **Frontend** | `Sg-Frontend` | 🟡 Nginx/TLS healthy; build de `main` generado; arte versionado; falta publicarlo en el nodo de Santiago | publicar y validar NPC + panel de reclamo |
| **Backend** | `Sg-Backend` | 🚧 API y **64/64 tests**; imagen `main-test` construida; `latest` pendiente | cargar Supabase/JWT, migrar `004` y validar arranque |
| **DB** | `Sg-DB` | 🟡 esquema 001–003 y backup probados; `004_anonymous_claim.sql` pendiente en DEV | aplicar `004` y validar claim/RLS |
| **Security** | `Sg-Security` | 🟡 A.8.25 completo; A.8.15/A.8.20/A.8.24 parciales; SoA de 93 | cerrar ataques y controles restantes |
| **IA** | `Sg-AI` | ✅ Ollama healthy, `qwen2.5:3b`, endpoint y pensamiento probados; simulación E2E con Ollama real | validar con backend real/Supabase |

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

Resultado esperado: `sg-frontend` en `healthy` y `{"status":"ok","env":"dev","database":true}`.
`sg-ai` y `sg-backend` requieren, respectivamente, descargar el modelo y cargar las claves
de Supabase/JWT; sin eso quedan detenidos o no arrancan. El build WebGL ya está generado; regenerarlo si se cambia Unity.

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
