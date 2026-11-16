# Arquitectura de infraestructura — CosTamarindo 2D

> **Estado (2026-11-13):** `Sg-Frontend` está *healthy* y escribe logs reales; `Sg-Backend`
> tiene imagen construida y dependencias actualizadas, pero **no arranca** hasta cargar las
> claves de Supabase/JWT en `.env`; `Sg-AI` está **healthy** con `qwen2.5:3b` (1,9 GB) y el
> endpoint de diálogo ya llama a Ollama. `Sg-Security` es la **VM externa** `sg-security` (`172.18.18.220`, Ubuntu
> 24.04 + Wazuh 4.14.10), con el agente **002** activo y `100250` con 36 alertas sobre logs
> reales de Nginx. El FIM guarda estado en `002.db`, pero Wazuh 4.14 no emite la alerta por
> archivo en este all-in-one. La VM anterior `sg-jacome-security` se descartó; el servicio
> `security` del compose queda como respaldo local. Tailscale se difiere hasta tener los
> cinco nodos. TLS local con mkcert y cabeceras verificadas (A.8.24).
> `Sg-DB` es Supabase Cloud (excepción D3) y ya tiene esquema, RLS y backups.
> **Alcance:** cómo se levanta, se opera y se comparte el entorno de 5 nodos.
> **Decisiones de referencia:** `Plan.md` §11.5 (D1–D8).
> **Avance por división y backlog:** `Plan.md` §4–§8.

---

## 1. Idea en una frase

Cinco nodos con una responsabilidad cada uno, cinco redes Docker con el mínimo privilegio
posible, **un solo puerto publicado (443)** y el backend como único componente que habla con
la IA y con la base de datos.

```
                        INTERNET / JUGADOR
                                 |  HTTPS 443 (unico puerto publicado)
 +------------------------------ v --------------------------------+
 | DMZ - red: dmz           [ Sg-Frontend ]                        |
 | nginx: build WebGL + TLS + HSTS/CSP + rate limit                |
 | /api/* ------------------------------+                          |
 +--------------------------------------|--------------------------+
                                        | http://sg-backend:8000
 +--------------------------------------v--------------------------+
 | APP - red: app           [ Sg-Backend ]                          |
 | FastAPI: JWT, HMAC, anti-replay, plausibilidad, saneo de prompts |
 +----------+------------------------------------+-----------------+
            | red: data (salida TLS 443)         | red: ai
 +----------v-----------------+      +-----------v-----------------+
 | [ Sg-DB ] Supabase Cloud    |      | [ Sg-AI ] Ollama :11434     |
 | Postgres + Auth + RLS       |      | qwen2.5:3b, 1 paralelo      |
 +----------+-----------------+      +-----------+-----------------+
            | logs                             | logs
 +----------v----------------------------------v-------------------+
 | MGMT - red: mgmt        logs de frontend y backend              |
 | SIEM - red: siem        [ sg-wazuh-agent ]  ── Tailscale ──┐    |
 +------------------------------------------------------------|----+
                                                              v
                              VM sg-security (Sg-Security)
                              Wazuh manager + indexer + dashboard 443
                              (solo tailnet; reglas 100200-100250)
```

Regla de oro: **el jugador solo ve `Sg-Frontend`**. Todo lo demás es inalcanzable desde fuera.

Cada nodo tiene una **división dueña** con su propio backlog en `Plan.md`:

| Nodo | División | Sección del plan |
| :--- | :--- | :--- |
| `Sg-Frontend` | Frontend | `Plan.md` §4 |
| `Sg-Backend` | Backend | `Plan.md` §5 |
| `Sg-DB` | DB | `Plan.md` §6 |
| `Sg-Security` | Security | `Plan.md` §7 |
| `Sg-AI` | IA | `Plan.md` §8 |

---

## 2. Los cinco nodos

| Nodo | Tecnología | Qué hace | Redes | Puerto | Persistencia | Límite RAM | Control ISO |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Sg-Frontend** | `nginx:1.29-alpine` | Sirve el build WebGL, termina TLS, aplica CSP/HSTS, rate limit y proxy `/api/` | `dmz`, `app` | **443** (público), 8081 (solo dev local) | build montado `../builds/webgl` (ro), certs (ro), logs | 128 MB | A.8.19, A.8.20, A.8.24 |
| **Sg-Backend** | Python 3.12 + FastAPI/Uvicorn (imagen propia, multi-stage, usuario `app`) | Valida JWT, firma HMAC, nonce, TTL y plausibilidad; único que escribe en la BD (`service_role`); único que llama a la IA | `app`, `data`, `ai`, `mgmt` | 8000 (**solo interno**) | ninguna: estado en Postgres | 512 MB | A.8.24, A.8.28, A.8.5, A.8.15 |
| **Sg-AI** | `ollama/ollama` | NPC: diálogos en español con `qwen2.5:3b` | `ai` (**`internal: true`**) | 11434 (**solo interno**) | volumen `costamarindo-ai-models` (~2,5 GB) | 4 GB | A.8.20, A.8.28 |
| **Sg-Security** | VM Ubuntu 24.04 + Wazuh 4.14.10 (manager/indexer/dashboard) + SonarQube bajo demanda | SIEM: recibe logs, reglas 100200–100250, FIM del webroot | `siem` (solo el agente) | 443 dashboard (**solo Default Switch/tailnet**), 1514/1515 (agentes) | volúmenes de la VM | fuera del host del juego | A.8.15, A.8.25 |
| **Sg-DB** | **Supabase Cloud** (Postgres 15 + GoTrue) | Identidad, leaderboard, `game_sessions` | — (salida 443 desde `data`) | 443 salida | gestionada por Supabase + `pg_dump` diario | — | A.8.5, A.8.28, A.8.13 |

`Sg-DB` **no es un contenedor**: es una excepción documentada (decisión D3). Ahorra 6–8 GB de
RAM y elimina el riesgo de perder la base con el portátil. Se mitiga con keep-alive, RLS y
`pg_dump` programado.

---

## 3. Redes Docker

| Red | `internal` | Nodos conectados | Para qué | Salida a Internet |
| :--- | :--- | :--- | :--- | :--- |
| `dmz` | no | frontend | entrada pública | sí (443 publicado) |
| `app` | no | frontend, backend | frontend → backend | no |
| `data` | no | backend | backend → Supabase (TLS 443) | **sí, solo backend** |
| `ai` | **sí** | backend, ai | backend → Ollama | no |
| `mgmt` | **sí** | backend, frontend, `wazuh-agent` | logs entre contenedores | no |
| `siem` | no | `wazuh-agent` | agente → VM `Sg-Security` (1514/1515/443) | **sí, solo hacia la VM/tailnet** |

Consecuencias que se verifican en el DoD de F6:

- `docker compose port sg-frontend 443` funciona; `docker compose port sg-backend 8000` **no
  devuelve nada**.
- Desde otro contenedor de `app` no se alcanza `sg-ai:11434` (no está en esa red).
- Desde el host, `nmap -Pn -p 8000,11434 localhost` sale `closed|filtered`.
- `Sg-AI` no puede salir a Internet: el modelo se descarga en el build del entorno, no en runtime.

---

## 4. Estructura de archivos

```
infra/
├── docker-compose.yml          # los 5 servicios, redes, volúmenes, perfiles
├── .env.example                # copia de ../.env.example (referencia para compose)
├── nginx/
│   ├── nginx.conf              # http: logs JSON, rate limit, gzip caliente
│   ├── conf.d/costamarindo.conf# 8080 interno (8081 en dev) + 443 TLS, .gz, proxy /api/
│   └── snippets/
│       ├── security-headers.conf
│       └── csp.conf            # GENERADO por tools/dev-server/csp.mjs
├── tls/                        # mkcert (NO se versiona)
├── scripts/
│   ├── up.sh / down.sh / logs.sh / deploy.sh
│   ├── tls.ps1                 # mkcert → infra/tls/juego.local*.pem
│   └── build_webgl.sh|.ps1
└── wazuh/                      # Sg-Security (F8)
    ├── rules/costamarindo_rules.xml
    ├── agent-group/game.conf
    └── README.md
```

`docker-compose.yml` se ejecuta **desde `infra/`**: las rutas del compose son relativas a ese
directorio (`../builds/webgl`, `../backend`).

---

## 5. Servicios del compose

### 5.1 `frontend`

```yaml
frontend:
  image: nginx:1.29-alpine
  ports:
    - "443:443"
    - "8081:8080"   # dev: el 8080 del host lo ocupa Unity MCP
  volumes:
    - ../builds/webgl:/usr/share/nginx/html:ro
    - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    - ./nginx/conf.d:/etc/nginx/conf.d:ro
    - ./nginx/snippets:/etc/nginx/snippets:ro
    - ./tls:/etc/nginx/certs:ro
    - frontend-logs:/var/log/nginx
  networks: [dmz, app, mgmt]
  healthcheck: wget -qO- http://localhost:8080/healthz
  restart: unless-stopped
  mem_limit: 128m
```

Puertos: en `dev` se publica `443:443` y `8081:8080`; en `prod`, **solo** `443:443`. La
separación dev/prod con `docker-compose.override.yml` todavía no existe (`Plan.md` → `BE-11`).
El `docker-compose.yml` actual declara los servicios `frontend`, `backend`, `ai`, `security` y
`wazuh-agent`, las seis redes y los volúmenes, con `mem_limit` y healthcheck. `backend`,
`security` y `wazuh-agent` viven detrás de perfiles porque dependen de F5 y F8.

> Desviación documentada (2026-11-10): `Sg-Security` ya no es el servicio `security` del compose,
> sino la VM externa `sg-security` (reconstruida el 2026-11-10; la anterior
> `sg-jacome-security` quedó inaccesible). El agente `wazuh-agent` se conecta a la VM por la red
> `siem` (Default Switch mientras no exista la tailnet del juego). `security` queda como respaldo
> local y **no debe encenderse a la vez** que la VM.

### 5.2 `backend`

```yaml
backend:
  build: { context: ../backend, dockerfile: Dockerfile }
  env_file: ../.env
  environment:
    LOG_DIR: /var/log/costamarindo   # copia JSON para el agente Wazuh
  volumes: [backend-logs:/var/log/costamarindo]
  expose: ["8000"]              # nunca `ports:`
  networks: [app, data, ai, mgmt]
  depends_on:
    ai: { condition: service_healthy }
  healthcheck: curl -fsS http://localhost:8000/api/health
  restart: unless-stopped
  mem_limit: 512m
```

Imagen multi-stage: etapa `builder` instala dependencias en un venv, etapa final `python:3.12-slim`
con usuario sin privilegios (`app`, UID 10001), `PYTHONDONTWRITEBYTECODE`, sin `pip` cache ni
compiladores. Variables sensibles solo por `env_file`; nunca en la imagen.

### 5.3 `ai`

```yaml
ai:
  image: ollama/ollama:latest
  expose: ["11434"]
  environment:
    OLLAMA_NUM_PARALLEL: "1"
    OLLAMA_MAX_LOADED_MODELS: "1"
  volumes: [sg-ai-models:/root/.ollama]
  networks: [ai]
  healthcheck: ollama list
  restart: unless-stopped
  mem_limit: 4g
```

El modelo se descarga una vez con `infra/scripts/pull-model.sh`
(`docker compose exec ai ollama pull qwen2.5:3b`). La red `ai` es `internal: true`.

### 5.4 `Sg-Security` — VM externa + agente (perfil `security`)

El nodo real es la VM **`sg-security`** (Ubuntu 24.04, Wazuh 4.14.10: manager, indexer y
dashboard). No es un contenedor y no está en la ruta de la partida: si se cae, el juego sigue.
El dashboard se abre por la tailnet o por túnel SSH; la VM **no publica 1514/1515/443 a Internet**.

En esta sesión la VM está en **`172.18.18.220`**, el Default Switch en `172.18.16.1/20` y
UFW permite `22/443/1514/1515` desde `172.18.16.0/20` y `172.19.224.0/20`; `55000/9200`
escuchan en localhost. El agente es el **ID 002** (`sg-game-host`, grupo `game`). El frontend
escribe `access.log` real y `100250` ya disparó 36 veces con una ráfaga `429`; el FIM detecta
el cambio y lo guarda en `002.db`, pero Wazuh 4.14 no emite la alerta por archivo en este
all-in-one.
`backend.log` queda pendiente hasta que el backend tenga las claves de Supabase/JWT.

Mientras la tailnet del juego no exista, la VM se alcanza por el Default Switch. Cuando estén
los cinco nodos se crea la tailnet, se saca la VM de la tailnet vieja y se cambia
`WAZUH_MANAGER` a su IP de Tailscale.

El agente del host del juego:

```yaml
wazuh-agent:
  profiles: ["security"]
  image: wazuh/wazuh-agent:4.14.10
  environment:
    WAZUH_MANAGER: ${WAZUH_MANAGER:-127.0.0.1}
    WAZUH_MANAGER_SERVER: ${WAZUH_MANAGER:-127.0.0.1}
    WAZUH_AGENT_NAME: sg-game-host
    WAZUH_AGENT_GROUP: game
    WAZUH_REGISTRATION_PASSWORD: ${WAZUH_REGISTRATION_PASSWORD:-}
  volumes:
    - ../builds/webgl:/fim/webgl:ro
    - frontend-logs:/var/log/nginx:ro
    - backend-logs:/var/log/costamarindo:ro
  networks: [mgmt, siem]
  mem_limit: 256m
```

El servicio `security` del compose (mismo Wazuh 4.14.10) queda como **respaldo local** en el perfil
`security-local`: `docker compose --profile security-local up -d security`. No encenderlo a la vez
que la VM.

### 5.5 `Sg-DB`

No hay servicio. El backend apunta a `SUPABASE_URL` y `SUPABASE_DB_URL` del `.env`. La salida
TLS se hace por la red `data`.

---

## 6. Flujo del equipo

### 6.1 Qué hace cada quien

| División | Toca | No toca |
| :--- | :--- | :--- |
| **Frontend** | `unity/`, `assets/figma/`, `builds/` (generado), `infra/nginx/` | `backend/`, `backend/sql/`, `infra/wazuh/` |
| **Backend** | `backend/app/`, `backend/tests/`, `backend/Dockerfile`, `docs/api.md` | `unity/`, `backend/sql/`, `infra/nginx/` |
| **DB** | `backend/sql/`, esquema y políticas en Supabase DEV | lógica de juego y de negocio |
| **Security** | `evidencias/`, `docs/soa.md`, `infra/wazuh/`, Gitleaks, TLS, CI | lógica de juego y de negocio |
| **IA** | prompt y guardrails en `backend/app/ai/`, `infra/scripts/pull-model.sh` | `unity/`, esquema de datos |
| **Todos** | leer `docs/arquitectura.md`, correr `compose up` | commitear `.env`, certs, `builds/` |

### 6.2 Ciclo diario (dev)

```bash
# 1. build del juego (solo si cambió Unity)
powershell -File infra/scripts/build_webgl.ps1

# 2. entorno completo
cd infra && docker compose up -d --build

# 3. jugar
#    https://juego.local   (requiere hosts + mkcert)

# 4. logs y estado
docker compose ps
docker compose logs -f backend frontend

# 5. bajar
docker compose down          # conserva volúmenes (modelos, wazuh)
docker compose down -v       # borra también los volúmenes: solo si hay que rehacer
```

Trabajo sin Docker (backend suelto): `uvicorn app.main:app --reload` dentro de
`backend/.venv` y `node tools/dev-server/server.mjs 8080` para el build. Sirve para iterar UI
sin levantar el entorno.

### 6.3 Ciclo de cambio en un nodo

1. Rama `feat/infra-<nodo>` o `sec/a8-<control>`.
2. Cambio acotado a `infra/` + su documentación.
3. `docker compose up -d --build <servicio>` y **verificar el healthcheck**.
4. Si cambia superficie de red: repetir la prueba de aislamiento (`nmap`, `docker compose port`).
5. Commit `feat(infra): …` / `sec(infra): …`; PR con la evidencia en la descripción.
6. Actualizar `docs/arquitectura.md` §2/§3 **en el mismo PR** si cambió nodos, redes o puertos.

### 6.4 Añadir un nodo nuevo (checklist)

1. Una responsabilidad, un servicio.
2. Red mínima: la más interna que funcione; `internal: true` si no necesita Internet.
3. Sin `ports:` salvo que sea un punto de entrada público, y entonces justificarlo en el PR.
4. Healthcheck + `restart: unless-stopped` + `mem_limit`.
5. Usuario no root e imagen pinneada por versión.
6. Fila nueva en la tabla §2 y nodo en el diagrama §1.

### 6.5 Secretos

- `.env` real solo en la máquina de cada quien y en el servidor; `.env.example` versionado.
- Claves `DEV` para desarrollo, `PROD` solo en el despliegue, nunca en el repo.
- `infra/tls/` fuera de git; cada quien genera su certificado con `infra/scripts/tls.ps1`.
- Gitleaks en pre-commit antes de F8.

---

## 7. Entornos

| Entorno | Compose | `.env` | Puertos publicados | Supabase |
| :--- | :--- | :--- | :--- | :--- |
| `dev` | `docker-compose.yml` + `docker-compose.override.yml` | `ENV=dev` | 443, 8080 | proyecto DEV |
| `prod` | `docker-compose.yml` | `ENV=prod` | 443 | proyecto PROD |

`ENV` decide: nivel de logs, CORS, si se publica 8080 y si se permite documentación interactiva
de FastAPI. La separación es parte del control **A.8.31**.

---

## 8. Verificación del entorno (DoD de F6)

```bash
# 1. todo arriba y sano
docker compose ps

# 2. juego por TLS
curl -sI https://juego.local | findstr /i "HTTP strict-transport content-security"

# 3. API detrás del proxy
curl -s https://juego.local/api/health

# 4. puertos internos cerrados
docker compose port sg-backend 8000      # sin salida = correcto
nmap -Pn -p 8000,11434 localhost         # closed/filtered

# 5. aislamiento entre redes
docker compose exec backend sh -c "wget -qO- http://sg-ai:11434/api/tags"
docker compose exec frontend sh -c "wget -qO- --timeout=2 http://sg-ai:11434/api/tags"  # debe fallar

# 6. IA degradada
docker compose stop ai && curl -s -X POST https://juego.local/api/dialog -d '{"text":"hola"}'
# responde fallback en < 1 s
```

Evidencia de cada punto va a `evidencias/` con fecha y salida literal.

---

## 9. Riesgos y plan B

| Riesgo | Señal | Plan B |
| :--- | :--- | :--- |
| Docker Desktop no arranca | error de engine/WSL | Compose dentro de WSL2 Ubuntu; documentar la limitación |
| Wazuh consume demasiada RAM | > 4 GB con el resto arriba | perfil apagado; solo FIM en F8 |
| `sg-ai` sin modelo | `/api/dialog` siempre en fallback | `pull-model.sh` en el arranque o imagen con modelo precargado |
| TLS no confía el navegador | `NET::ERR_CERT_AUTHORITY_INVALID` | `mkcert -install` + reiniciar el navegador |
| Build WebGL viejo en el contenedor | cambios de Unity no se ven | el build va montado en solo lectura: rebuild y recargar sin reiniciar Nginx |
