# Plan de ejecución — CosTamarindo 2D

> Documento vivo. Cada fase tiene objetivo, pasos con las herramientas concretas, entregables y criterio de terminado (DoD).
> Especificación de origen: [`docs/CosTamarindo.md`](docs/CosTamarindo.md) (ISO/IEC 27001:2022).
> Última actualización: 2026-10-27.

---

## 1. Objetivo final

Un videojuego 2D jugable en el navegador (Unity 6 → WebGL) servido por Nginx, con:

1. **Online con Supabase**: autenticación usuario + PIN, leaderboard global (top 5 + posición propia) y puntajes firmados.
2. **5 nodos de infraestructura** (4 en Docker + Supabase Cloud): `Sg-Frontend`, `Sg-Backend`, `Sg-AI`, `Sg-Security` y `Sg-DB`.
3. **NPC con IA local**: diálogos generados por Ollama, sin APIs externas en tiempo de ejecución.
4. **Objeto de auditoría ISO 27001**: 10 controles del Anexo A implementados, atacados y corregidos, con evidencia reproducible por control.

### 1.1 Definición de terminado (global)

- [ ] Build **Release** WebGL cargado desde Nginx en HTTPS y jugable de principio a fin en un navegador limpio.
- [ ] Registro/login con usuario + PIN → partida → envío de puntaje aceptado → top 5 visible.
- [ ] Puntaje alterado, replay y sesión expirada rechazados y registrados en logs.
- [ ] NPC responde con Ollama; con Ollama caído el juego devuelve respuesta de respaldo y sigue funcionando.
- [ ] `docker compose up` levanta el entorno completo; desde fuera solo el puerto 443 está expuesto.
- [ ] Un archivo de evidencia por cada uno de los 10 controles en `evidencias/`.
- [ ] SoA, informe y README de despliegue en `docs/`.
- [ ] Demo reproducible desde cero y respaldo grabado.

---

## 2. Estado actual verificado (2026-10-27)

| Elemento | Estado | Evidencia |
| :--- | :--- | :--- |
| Unity Hub / Editor | `6000.3.27f1` (Unity 6.3 LTS) | `ProjectSettings/ProjectVersion.txt` |
| Módulo WebGL | Instalado | `.../PlaybackEngines/WebGLSupport` |
| Proyecto Unity | URP 2D, Input System 1.20.1, paquetes 2D completos | `Packages/manifest.json` |
| Contenido Unity | Escena `Game` jugable (bucle completo offline), 10 scripts, 4 sprites, TMP Essentials, fuentes Lilita One / Patrick Hand como SDF | `docs/fases/F2.md` |
| **Unity MCP** | **Conectado** — autoarranque reparado (§ F2 3.1); 36 herramientas | `mcpforunity://editor/state` |
| **Figma MCP** | **Autenticado** — sesión `90118ff1-0eb1-4e3b-8a14-3bd385659ac0` | `figma.whoami` |
| Figma: archivo | `qI8jr7abLu6tIpL0MnL8Ol` — Design System + 8 pantallas completas | `get_metadata` |
| Supabase MCP | Deshabilitado en la config local; se habilitará **solo hacia DEV** | `.codex/config.toml`, `opencode.json` |
| Docker Desktop | **No instalado** | `Get-Command docker` |
| Ollama | **No instalado** | `Get-Command ollama` |
| Máquina | 32 GB RAM, 86 GB libres, WSL2 v2, Python 3.12.10, Node 24.16 | — |
| Remoto Git | `https://github.com/DavidCrispoca/CosTamarindo.git` — `main` publicada y sincronizada | `git ls-remote` |

---

## 3. Decisiones cerradas

| ID | Decisión | Justificación |
| :--- | :--- | :--- |
| D1 | El cliente **nunca escribe** en la base de datos. Solo el backend con `service_role` | Hallazgo H3 del documento |
| D2 | Backend en **FastAPI (Python)** | Validación con Pydantic + integración simple con Ollama |
| D3 | **Supabase Cloud (free)** para Auth/Postgres; `Sg-DB` se documenta como excepción externa | Ahorra 6–8 GB de RAM y tiempo; riesgo de pausa mitigado con keep-alive + `pg_dump` |
| D4 | Modelo **`qwen2.5:3b`** cuantizado en Ollama | Mejor español que llama3.2:3b a costo similar; cabe en CPU |
| D5 | TLS local con **mkcert** (`juego.local`) | Sin dominio público ni costo |
| D6 | Sesiones de juego en tabla **`game_sessions`** de Postgres, no en memoria | Sobrevive reinicios; permite auditoría |
| D7 | **Figma manda** en arte y narrativa; la capa ISO vive en infraestructura y documentación | El arte ya está terminado y es del equipo |
| D8 | Login **usuario + PIN** (correo sintético + PIN como contraseña) con rate limit y bloqueo progresivo | Coincide con el diseño y genera evidencia de A.8.5 |

---

## 4. Arquitectura objetivo

```
                       INTERNET / JUGADOR
                                │ HTTPS 443
┌───────────────────────────────▼────────────────────────────────┐
│ DMZ · red: dmz          Sg-Frontend (contenedor)               │
│ Nginx: build WebGL + TLS + HSTS/CSP + rate limit               │
│ /api/* ──────────────────────────────┐                         │
└──────────────────────────────────────┼─────────────────────────┘
                                       │ http://sg-backend:8000
┌──────────────────────────────────────▼─────────────────────────┐
│ APP · red: app          Sg-Backend (contenedor)                │
│ FastAPI: JWT, HMAC, anti-replay, plausibilidad, saneo de prompts│
└───────┬───────────────────────────────────────┬────────────────┘
        │ red: data (salida TLS)                │ red: ai
┌───────▼─────────────────────┐   ┌─────────────▼────────────────┐
│ Sg-DB · Supabase Cloud      │   │ Sg-AI (contenedor)           │
│ Postgres + Auth + RLS       │   │ Ollama :11434 interno        │
└───────┬─────────────────────┘   └─────────────┬────────────────┘
        │ logs                                  │ logs
┌───────▼───────────────────────────────────────▼────────────────┐
│ MGMT · red: mgmt        Sg-Security (perfil opcional)          │
│ Wazuh manager + indexer + dashboard · SonarQube bajo demanda   │
└────────────────────────────────────────────────────────────────┘
```

| Nodo | Implementación | Puerto | Control ISO |
| :--- | :--- | :--- | :--- |
| `Sg-Frontend` | Nginx (contenedor) | 443 público | A.8.19, A.8.20, A.8.24 |
| `Sg-Backend` | FastAPI + Uvicorn (contenedor) | 8000 interno | A.8.24, A.8.28, A.8.5, A.8.15 |
| `Sg-AI` | Ollama (contenedor) | 11434 interno | A.8.20, A.8.28 |
| `Sg-Security` | Wazuh + SonarQube (perfil `security`) | interno | A.8.15, A.8.25 |
| `Sg-DB` | Supabase Cloud | 443 salida | A.8.5, A.8.28, A.8.13 |

Diseño de red: 5 redes Docker (`dmz`, `app`, `data`, `ai`, `mgmt`) con `internal: true` donde aplica. El backend es el único que conoce `sg-ai`; Nginx es el único expuesto.

---

## 5. Stack

| Capa | Tecnología | Versión |
| :--- | :--- | :--- |
| Motor | Unity 6.3 LTS | `6000.3.27f1` |
| Render | URP 2D + Input System + UGUI/TMP | 17.3.0 / 1.20.1 |
| Fuentes | Lilita One + Patrick Hand (Google Fonts, OFL) | SDF generadas en Unity |
| Frontend web | Nginx | 1.27-alpine |
| Backend | FastAPI + Uvicorn + Pydantic + httpx + PyJWT | Python 3.12 |
| DB/Auth | Supabase Cloud (Postgres 15, GoTrue) | plan free |
| LLM | Ollama + `qwen2.5:3b` (Q4_K_M) | última estable |
| Contenedores | Docker Desktop + Compose v2 (WSL2) | última estable |
| TLS | mkcert | última |
| SIEM | Wazuh 4.x | contenedor |
| SAST | SonarQube Community | contenedor / CI |
| Secretos | Gitleaks | pre-commit |
| DAST | OWASP ZAP + Burp Suite Community + Nmap | local |
| E2E web | Playwright | `npx` |

---

## 6. Estructura del repositorio

```
CosTamarindo/
├── Plan.md                     # este documento
├── README.md                   # arranque rápido y mapa del repo
├── .gitignore                  # Unity + agentes + secretos + artefactos
├── .env.example                # variables sin valores reales
├── docs/
│   ├── CosTamarindo.md         # especificación ISO 27001
│   ├── arquitectura.md         # diagramas y decisiones (se crea en F6)
│   └── soa.md                  # Declaración de Aplicabilidad (F8)
├── unity/                      # proyecto Unity 6 (Assets, Packages, ProjectSettings)
├── backend/                    # FastAPI
│   ├── app/                    # routers, modelos, seguridad, logging
│   ├── tests/
│   ├── requirements.txt
│   └── Dockerfile
├── infra/
│   ├── docker-compose.yml
│   ├── nginx/                  # nginx.conf, headers, sitios
│   ├── wazuh/                  # reglas y decoders
│   ├── sonar/                  # configuración del scanner
│   └── scripts/                # deploy, hashes, backups, ataques
├── assets/
│   └── figma/                  # exportaciones fuente (SVG/PNG)
└── evidencias/
    ├── A.8.24/ … A.8.19/       # captura + log + comando por control
    └── ataques/                # scripts reproducibles
```

### 6.1 Qué **no** se sube

`.opencode/`, `opencode.json*`, `.codex/`, `.claude/`, `.cursor/`, `AGENTS.md`, `CLAUDE.md`, skills, configuraciones de MCP, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds, binarios, `.env`, certificados, `node_modules/`, `__pycache__/`, dumps de base de datos. Todo está en `.gitignore`.

---

## 7. Convenciones de trabajo

### 7.1 Ramas

| Rama | Uso |
| :--- | :--- |
| `main` | Siempre desplegable. Protegida: solo PR. |
| `feat/<tema>` | Funcionalidad nueva (`feat/game-loop`) |
| `fix/<tema>` | Corrección de bug |
| `sec/<control>` | Implementación o corrección de un control ISO (`sec/a8-24-hmac`) |
| `docs/<tema>` | Documentación y evidencias |

### 7.2 Commits — Conventional Commits

Formato: `tipo(alcance): descripción imperativa en presente`

| Tipo | Cuándo |
| :--- | :--- |
| `feat` | Funcionalidad visible |
| `fix` | Corrección de defecto |
| `sec` | Cambio motivado por un control ISO |
| `refactor` | Reestructuración sin cambio de comportamiento |
| `test` | Pruebas |
| `docs` | Documentación y evidencias |
| `chore` | Repo, dependencias, tooling |
| `ci` | Pipeline |

Alcances: `unity`, `backend`, `infra`, `db`, `art`, `docs`, `ci`.

Ejemplos:
- `feat(unity): agrega bucle de captura con tres vidas`
- `sec(backend): valida firma HMAC y un solo envío por sesión`
- `chore(repo): inicializa estructura y gitignore`

Reglas: un commit = un cambio lógico; no mezclar arte con backend; nunca commitear secretos; el cuerpo explica **por qué**, no qué.

### 7.3 Flujo

`rama` → commits → PR → revisión → merge a `main` → despliegue. Nada directo a `main` después del primer commit.

### 7.4 Secretos

- `.env` real solo en la máquina y en el servidor; `.env.example` versionado.
- Claves de Supabase **DEV** para desarrollo; **PROD** jamás en el repo.
- Gitleaks en pre-commit y en CI.
- Supabase MCP apuntado solo a DEV y con permisos mínimos.

---

## 8. Fases de ejecución

### F0 · Repositorio y baseline  ·  *(H0)*

**Objetivo:** repo limpio, reproducible y sin ruido, con el proyecto Unity versionado correctamente.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Crear repo en `Desktop/CosTamarindo`, rama `main`, remoto `origin` | `git init`, `git remote add` |
| 2 | `.gitignore` (Unity + agentes + secretos + artefactos) | editor |
| 3 | Mover especificación a `docs/`, crear `README.md` y `.env.example` | `git mv` |
| 4 | Renombrar la carpeta del proyecto Unity a `unity/` con el editor cerrado | `git mv` + Unity Hub |
| 5 | Activar **Force Text** (`EditorSettings.asset`) y **Visible Meta Files** | archivo / MCP |
| 6 | Importar **TMP Essentials** | menú `Window/TextMeshPro/Import TMP Essential Resources` vía `execute_menu_item` |
| 7 | Verificar que `Library/`, `Temp/`, `Logs/`, `UserSettings/` no entran al índice | `git status` |
| 8 | Primer commit y push | `git commit`, `git push -u origin main` |

**Entregables:** repo remoto con `main`, `.gitignore`, `README.md`, `Plan.md`, `docs/`, proyecto Unity versionado.
**DoD:** `git ls-files` no contiene rutas de `Library/`, `.env`, `.codex/`, `opencode.json`, ni archivos `*.csproj`; el remoto coincide con el local.
**Riesgo:** mover el proyecto con Unity abierto → hacerlo en el orden indicado y verificar que Unity reabra sin errores.

---

### F1 · Pipeline Figma → Unity  ·  *(H0/H1)*

**Objetivo:** todos los assets y tokens del diseño dentro de Unity, listos para armar UI.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | `create_session` sobre el archivo `qI8jr7abLu6tIpL0MnL8Ol` | Figma MCP |
| 2 | Inventariar nodos y capas por pantalla | `get_metadata` (node `0:1`) |
| 3 | Exportar componentes individuales: botones, inputs, paneles, corazones, contador, bocadillo, filas, cartas, logo (SVG) | `download_assets` con `nodeId` |
| 4 | Descargar imágenes fuente (fondos, Palenquera, niña alegre/triste) | `download_assets` → `rawImages` |
| 5 | Extraer tokens: 17 colores y 5 estilos de texto | `get_design_context` por nodo de tipografía/color |
| 6 | Descargar `Lilita One` y `Patrick Hand` (OFL) y generar SDF en TMP | Google Fonts + TMP Font Asset Creator |
| 7 | Importar todo por MCP con ajustes de importación (PPU, sprite mode, 9-slice, compresión) | `manage_texture`, `manage_asset` |
| 8 | Crear atlas de sprites y prefabs de UI (`WoodButton`, `IconButton`, `HandInput`, `PinSlot`, `WoodPanel`, `Heart`, `Counter`, `DialogBubble`, `LeaderRow`, `CollectibleCard`) | `manage_prefabs`, `manage_gameobject` |
| 9 | Guardar exportaciones fuente en `assets/figma/` (sin duplicar binarios pesados) | `git add` selectivo |

**Inventario a extraer**

| Grupo | Nodos / nombres |
| :--- | :--- |
| Tokens | paleta completa (17), Tipografía (5 estilos) |
| Botones | `Button/Wood` (3 tipos × 3 estados), `Button/Icon` (Pausa, Libro, Cerrar, Volver, Trofeo × 2) |
| Formularios | `Input/Hand` texto y PIN (default/focus/error), `PIN/Slot` (3) |
| Contenedores | `Panel/WoodSign` S/M/L, `Book/Spread`, `FX/Paper Grain` |
| HUD | `HUD/Heart` (full/empty/lost), `HUD/Counter` |
| Diálogo | `Dialog/Bubble` (Palenquera) |
| Ítems | `Item/Falling`: cocada, tamarindo, limonada coco, pescado, frutas, erizo, medusa, basura |
| Colección | `Card/Collectible` (unlocked/locked/new) |
| Leaderboard | `Row/Leaderboard` (top/default/me) |
| Escenarios | 4 fondos de playa, viñeta de inicio, `Logo` vectorial |
| Personajes | Palenquera HD, niña alegre, niña triste |

**Entregables:** `unity/Assets/Art/` con sprites correctamente importados, atlas, fuentes SDF y prefabs de UI.
**DoD:** cada componente del inventario existe como prefab o sprite; los fondos pesan ≤ 1.5 MB; el proyecto compila sin errores en consola.

---

### F2 · Vertical slice jugable  ·  *(H1)*

**Objetivo:** el juego se puede jugar de principio a fin sin red.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Escenas `Boot`, `Login`, `Game`, `Libro` (Pausa, GameOver, Leaderboard y Bocadillo como overlays) | `manage_scene` |
| 2 | `GameConfig` y `ItemDefinition` como ScriptableObjects (puntos, `isDanger`, sprite, probabilidad) | `manage_scriptable_object` |
| 3 | `PlayerController`: movimiento horizontal con Input System, límites de pantalla | `manage_script`, `manage_components` |
| 4 | `Spawner`: curva de dificultad, intervalo y velocidad crecientes, pooling | `manage_script` |
| 5 | `CatchZone`: detecta captura, suma puntos o resta vida, feedback visual/sonido | `manage_script` |
| 6 | `GameManager`: 3 vidas, puntaje, tiempo, récord local (`PlayerPrefs` solo para preferencias), estados | `manage_script` |
| 7 | HUD: corazones, contador, pausa; `PauseMenu`, `GameOverPanel` con contadores por ítem | `manage_ui` |
| 8 | `Libro`: grilla de coleccionables con desbloqueo persistido local | `manage_ui` |
| 9 | `DialogueBubble`: cola de frases, indicador "escribiendo…", sin bloquear gameplay | `manage_ui` |
| 10 | Tests PlayMode mínimos (captura suma, peligro resta vida, game over a 0 vidas) | `run_tests` |
| 11 | Revisar consola y errores de compilación | `read_console` |

**Entregables:** escena jugable con arte real, prefabs, scripts y tests.
**DoD:** partida completa de 2–3 min en el editor; 3 vidas; puntaje y colección persistentes; 0 errores en consola; tests verdes.

---

### F3 · Build WebGL + Nginx  ·  *(H1)*

**Objetivo:** el juego corre en el navegador servido por Nginx, desde la primera semana.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Player Settings: WebGL, compresión Brotli, `Strip Engine Code`, sin Development Build | `manage_build(property=…)` |
| 2 | Build Release a `builds/webgl` | `manage_build(target=webgl)` |
| 3 | Contenedor Nginx con el build montado y compresión correcta (`.br`, `.wasm`, `Content-Type`) | `infra/nginx` |
| 4 | Probar carga, errores de consola del navegador y tamaño total | navegador + Playwright |
| 5 | Ajustar si hay pantalla de carga infinita (`Content-Encoding`, MIME, CSP) | Nginx |
| 6 | Script `infra/scripts/build_webgl.sh` reproducible | shell |

**Entregables:** build servido en `http://localhost:8080`, script de build.
**DoD:** carga en < 10 s en local, jugable con teclado y ratón/táctil, 0 errores de consola, tamaño ≤ 30 MB.

---

### F4 · Supabase: Auth, esquema y leaderboard  ·  *(H2)*

**Objetivo:** identidad y persistencia online con RLS correcta.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Crear proyecto **DEV** en Supabase Cloud | dashboard |
| 2 | Esquema `profiles`, `game_sessions`, `scores`, vista `leaderboard` (top 5 + posición) | SQL editor / Supabase MCP |
| 3 | RLS: lectura pública del leaderboard; sin `insert/update/delete` para `anon/authenticated`; `game_sessions` solo `service_role` | SQL |
| 4 | Política de username `^[A-Za-z0-9_]{3,16}$` y PIN de 4 dígitos | SQL + backend |
| 5 | Login en Unity: usuario → `<usuario>@players.costamarindo.local`, PIN como contraseña; token **en memoria** | `ApiClient`, `AuthClient` |
| 6 | Refresh de token antes de expirar; logout limpia memoria | `manage_script` |
| 7 | Rate limit y bloqueo progresivo por intentos (5 fallos → 60 s) | backend + Supabase |
| 8 | Probar Security Advisor: ninguna tabla sin RLS | dashboard |
| 9 | `pg_dump` programado y restauración probada | `pg_dump`/`pg_restore` |

**Entregables:** esquema versionado en `backend/sql/`, Auth funcionando desde Unity, backups.
**DoD:** registro y login desde el juego; `anon` no puede insertar en `scores`; leaderboard lee top 5; 6 intentos fallidos bloquean.

---

### F5 · Backend FastAPI + puntaje firmado  ·  *(H2)*

**Objetivo:** servidor autoritativo del puntaje y del diálogo.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Esqueleto FastAPI (`app/main.py`, routers, settings con Pydantic) | editor + `uvicorn` |
| 2 | `POST /api/session/start`: crea sesión con `nonce` + `session_key`, TTL 15 min, persiste en `game_sessions` | backend |
| 3 | `POST /api/score`: valida JWT, HMAC-SHA256, `nonce`, sesión no usada, expiración, plausibilidad (`12 pts/s`) | backend |
| 4 | Inserción con `service_role`; un solo envío por sesión | backend |
| 5 | `GET /api/leaderboard`: top 5 + posición del jugador | backend |
| 6 | `POST /api/dialog`: longitud, rate limit, saneo, system prompt, llamada a Ollama, filtro de salida, fallback | backend |
| 7 | Logs JSON a stdout (`timestamp`, `event`, `user_id`, `ip`, `request_id`) | backend |
| 8 | Unity: `ApiClient`, `ScoreSigner`, `SessionClient`, `DialogueClient` con `UnityWebRequest` | `manage_script` |
| 9 | Tests: firma válida, firma alterada, replay, expirada, puntaje implausible | `pytest` |
| 10 | Prueba end-to-end en el editor y en WebGL | `run_tests` + navegador |

**Entregables:** backend funcional, tests, contratos documentados en `docs/api.md`.
**DoD:** flujo completo aceptado; los 4 ataques de puntaje rechazados con motivo; `pytest` verde.

---

### F6 · Infraestructura Docker de 5 nodos  ·  *(H1/H2)*

**Objetivo:** entorno reproducible con aislamiento real por zonas.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Instalar Docker Desktop + WSL2 y verificar virtualización | instalador |
| 2 | `infra/docker-compose.yml`: servicios `frontend`, `backend`, `ai`, `security` + redes `dmz/app/data/ai/mgmt` | Compose |
| 3 | `Dockerfile` del backend (multi-stage, usuario no root, sin cache de pip) | Docker |
| 4 | Nginx: TLS mkcert, HSTS, CSP compatible con WebGL (`wasm-unsafe-eval`), X-Frame-Options, rate limit, proxy `/api/` | Nginx |
| 5 | `Sg-AI`: contenedor Ollama, volumen de modelos, bind solo a red `ai`, `OLLAMA_NUM_PARALLEL=1` | Compose |
| 6 | `Sg-Security`: perfil `security` con Wazuh (y SonarQube bajo demanda) | Compose |
| 7 | Healthchecks, `restart: unless-stopped`, límites de memoria | Compose |
| 8 | Script `infra/scripts/up.sh`, `down.sh`, `logs.sh`, `deploy.sh` | shell |
| 9 | Verificar aislamiento con `nmap` desde el host y desde otro contenedor | Nmap |
| 10 | `.env` por entorno (`dev`, `prod`) y `compose` con `--profile` | Compose |

**Entregables:** `docker compose up` funcional, documentación en `docs/arquitectura.md`.
**DoD:** juego y API bajo `https://juego.local`; `:11434` y `:8000` inaccesibles desde fuera de su red; healthchecks en verde.

---

### F7 · NPC con Ollama  ·  *(H3)*

**Objetivo:** diálogo del NPC con IA local, seguro y con degradación elegante.

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Descargar `qwen2.5:3b` dentro del contenedor y medir latencia/CPU | `ollama pull`, `ollama run` |
| 2 | System prompt fijo de la Palenquera (español, ≤ 3 frases, solo seguridad, texto del jugador como dato) | backend |
| 3 | Detección de inyección (patrones + delimitadores `<jugador>…</jugador>`) y filtro de salida (`<`, `>`, longitud) | backend |
| 4 | Timeout 20 s, 1 request concurrente, cola y rate limit por usuario | backend |
| 5 | Banco de respuestas por contexto (inicio, captura, peligro, game over, libro) como fallback | backend |
| 6 | UI: bocadillo con "escribiendo…", historial corto, sin bloquear el juego | `manage_ui` |
| 7 | Pruebas: prompt normal, injection, Ollama apagado, respuesta lenta | `pytest` + curl |
| 8 | Confirmar que la respuesta **no** afecta puntaje ni lógica | revisión + test |

**Entregables:** NPC funcional, tests de inyección y fallback.
**DoD:** respuestas en personaje; inyección bloqueada y registrada como `dialog_blocked`; con Ollama apagado el juego responde en < 1 s.

---

### F8 · Controles ISO, ataques y evidencias  ·  *(H4–H6)*

**Objetivo:** implementar, atacar y corregir los 10 controles, con evidencia por control.

| # | Control | Implementación | Ataque | Evidencia |
| :--- | :--- | :--- | :--- | :--- |
| 1 | A.8.24 Criptografía | HMAC-SHA256, nonce, TTL, TLS 1.2+ | Burp altera `score`; replay | Rechazo + log `bad_signature` / `session_used` |
| 2 | A.8.5 Autenticación | Supabase Auth, PIN, rate limit, bloqueo | Burp Intruder 4 dígitos | 429/bloqueo + alerta |
| 3 | A.8.9 Configuración | Server-authoritative, `PlayerPrefs` solo preferencias | Editar IndexedDB (vidas/puntaje) | Puntaje no cambia |
| 4 | A.8.20 Redes | Redes Docker, firewall, CORS, cabeceras | Nmap, acceso a `:11434` y BD | Puertos cerrados |
| 5 | A.8.12 DLP | Sin logs ni secretos en Release, stripping | Consola del navegador + `strings` del wasm | Sin datos sensibles |
| 6 | A.8.15 Logging | JSON + Wazuh + reglas 100100/100101 | Ataques sin rastro | Alertas correlacionadas |
| 7 | A.8.28 Codificación segura | Pydantic, RLS, escape rich text, prompt blindado | SQLi, XSS `<size=500>`, prompt injection | Neutralizado |
| 8 | A.8.31 Separación de entornos | `.env` dev/prod, proyectos Supabase separados, defines | Token DEV contra PROD | Rechazo |
| 9 | A.8.25 SDLC seguro | Gitleaks, SonarQube, PR, tests | Commit de clave falsa | Gitleaks bloquea |
| 10 | A.8.19 Instalación en producción | SHA256SUMS firmado, verificación, FIM | Reemplazar `.wasm` | Falla hash + alerta FIM |

**Pasos transversales**

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Gitleaks pre-commit + escaneo de historial | `gitleaks` |
| 2 | SonarQube sobre backend y C#; Quality Gate | `sonar-scanner` |
| 3 | Wazuh: agentes, reglas locales, FIM del webroot | Wazuh |
| 4 | Manifiesto `SHA256SUMS` firmado + verificación en despliegue | `sha256sum`, `gpg` |
| 5 | Scripts de ataque reproducibles en `evidencias/ataques/` | Python/curl/Burp/ZAP |
| 6 | SoA y matriz de riesgos | `docs/soa.md` |
| 7 | Plantilla de evidencia: control · fecha · ataque · captura · corrección · verificación | `docs/` |

**Entregables:** 10 carpetas de evidencia, SoA, informe de hallazgos.
**DoD:** cada control tiene "antes" vulnerable y "después" corregido, con log o captura reproducible.

---

### F9 · Despliegue final y entrega  ·  *(H7)*

| # | Paso | Herramienta |
| :--- | :--- | :--- |
| 1 | Build Release final + hashes + verificación | `manage_build`, `deploy.sh` |
| 2 | Backup `pg_dump` y restauración documentada | `pg_dump` |
| 3 | Prueba E2E en navegador limpio (registro → partida → puntaje → top 5 → NPC → libro) | Playwright |
| 4 | README de despliegue y runbook de incidencias | docs |
| 5 | Video de respaldo y datos de prueba | grabación |
| 6 | Informe final + SoA + presentación | docs |

**DoD:** demo reproducible siguiendo el README, sin pasos tácitos.

---

## 9. Orden de recorte si falta tiempo

1. Detalles de arte y efectos.
2. Controles de Nivel 2 (A.8.22, A.8.8, A.8.29, A.8.13, A.8.17, A.8.32, A.8.2, A.8.3).
3. Entorno staging (quedarse con dev y prod).
4. Diálogo libre → opciones predefinidas con respuesta acotada.
5. Reglas avanzadas de Wazuh → solo FIM + 2 reglas.

**No se recorta:** firma y validación de puntaje, validación de entrada, logging a Wazuh, autenticación, evidencias de ataque y corrección.

---

## 10. Riesgos y planes B

| Riesgo | Señal temprana | Plan B |
| :--- | :--- | :--- |
| Docker Desktop no arranca (virtualización/WSL) | error al iniciar el engine | Compose en WSL2 directo o nodos locales sin contenedor, documentando la limitación |
| Supabase Cloud pausa por inactividad | proyecto pausado | keep-alive + `pg_dump` diario; self-hosted como alternativa |
| Build WebGL no carga | pantalla infinita | revisar `Content-Encoding`, MIME y CSP; desactivar Brotli temporalmente |
| Ollama lento en CPU | > 20 s por respuesta | modelo 1B–3B más pequeño, `num_predict` corto, fallback |
| Unity MCP inestable | timeouts | el agente edita scripts directamente y el equipo arma escenas |
| Assets pesados | build > 50 MB | atlas, compresión Crunch, límite 2048 px |
| Conflictos de escena en Git | merges con conflictos YAML | Force Text + prefabs + una persona por escena |
| Tiempo insuficiente | fases sin cerrar | aplicar orden de recorte |

---

## 11. Checklist de entrega

- [ ] Build WebGL Release servido por Nginx en HTTPS.
- [ ] Login usuario + PIN con bloqueo por intentos.
- [ ] Leaderboard top 5 + posición propia.
- [ ] Puntaje firmado con HMAC y validado en servidor.
- [ ] NPC con Ollama + fallback.
- [ ] 5 nodos operativos y aislados.
- [ ] 10 controles con evidencia de ataque y corrección.
- [ ] SoA completa.
- [ ] Gitleaks sin hallazgos; sin secretos en el repo.
- [ ] `pg_dump` reciente y restauración probada.
- [ ] Demo reproducible + video de respaldo.
