# Plan de ejecución — CosTamarindo 2D

> **Documento vivo y fuente de verdad del avance.** Todo lo que diga este archivo manda
> sobre `Proceso.md` (bitácora interna, no versionada) y sobre cualquier nota de fase.
> **Última verificación contra el repositorio:** 2026-11-06.
> **Especificación de origen:** [`docs/CosTamarindo.md`](docs/CosTamarindo.md) (ISO/IEC 27001:2022).
>
> **Cómo se usa este documento.** El proyecto está partido en **cinco divisiones** que
> corresponden uno a uno con los cinco nodos de la arquitectura: **Frontend**, **Backend**,
> **DB**, **Security** e **IA**. Cada tarea tiene un código (`FE-01`, `BE-01`, `DB-01`,
> `SEC-01`, `IA-01`), un criterio de terminado y una division dueña. Si una tarea toca dos
> divisiones, se parte en dos y cada una se cierra por separado.

---

## 1. Objetivo final

Un videojuego 2D jugable en el navegador (Unity 6 → WebGL) servido por Nginx, con:

1. **Online con Supabase**: autenticación usuario + PIN, leaderboard global (top 5 + posición
   propia) y puntajes firmados que el servidor valida.
2. **5 nodos de infraestructura** (4 en Docker + Supabase Cloud): `Sg-Frontend`, `Sg-Backend`,
   `Sg-AI`, `Sg-Security` y `Sg-DB`.
3. **NPC con IA local**: diálogos generados por Ollama, sin APIs externas en tiempo de ejecución.
4. **Objeto de auditoría ISO 27001**: 10 controles del Anexo A implementados, atacados y
   corregidos, con evidencia reproducible por control.

### 1.1 Definición de terminado (global)

- [ ] Build **Release** WebGL cargado desde Nginx en HTTPS y jugable de principio a fin en un
      navegador limpio.
- [ ] Registro/login con usuario + PIN → partida → envío de puntaje aceptado → top 5 visible.
- [ ] Puntaje alterado, replay y sesión expirada rechazados y registrados en logs.
- [ ] NPC responde con Ollama; con Ollama caído el juego devuelve respuesta de respaldo y sigue
      funcionando.
- [ ] `docker compose up` levanta el entorno completo; desde fuera solo el puerto 443 está expuesto.
- [ ] Un archivo de evidencia por cada uno de los 10 controles en `evidencias/`.
- [ ] SoA, informe y README de despliegue en `docs/`.
- [ ] Demo reproducible desde cero y respaldo grabado.

---

## 2. Las cinco divisiones

| División | Nodo | Qué posee | Rutas que puede tocar | Fases en las que participa |
| :--- | :--- | :--- | :--- | :--- |
| **Frontend** | `Sg-Frontend` | Juego Unity, arte, UI, build WebGL, Nginx (estático + TLS + proxy) | `unity/`, `assets/figma/`, `builds/` (artefacto), `infra/nginx/` | F1, F2, F3, parte de F4/F5/F7, F9 |
| **Backend** | `Sg-Backend` | API FastAPI: sesión, puntaje firmado, leaderboard, proxy de diálogo, logs JSON | `backend/`, `docs/api.md` | F5, parte de F4/F7/F8 |
| **DB** | `Sg-DB` | Supabase Cloud: esquema, RLS, Auth, backups, plausibilidad de datos | `backend/sql/`, documentación de esquema, scripts de backup | F4, parte de F5/F8/F9 |
| **Security** | `Sg-Security` | Los 10 controles ISO, Wazuh, Gitleaks, SonarQube, hardening, evidencias, ataque y corrección | `evidencias/`, `docs/soa.md`, `infra/wazuh/`, `tools/git-hooks/`, `.gitleaks.toml`, CI | F0, F6 (aislamiento), F8, parte de F3/F9 |
| **IA** | `Sg-AI` | Ollama, modelo, system prompt, guardrails, fallback de diálogo | `infra/scripts/pull-model.sh`, parte de `backend/` (`app/ai/`) | F7, parte de F8 |

**Regla de oro:** cada división tiene un nodo, un directorio dueño y una lista de criterios de
terminado. Nada se da por hecho porque «funciona en el editor»: se verifica en el nodo real.

---

## 3. Estado real verificado (2026-11-06)

### 3.1 Estado por elemento

| Elemento | Estado real | Evidencia / ruta |
| :--- | :--- | :--- |
| Unity Hub / Editor | `6000.3.27f1` (Unity 6.3 LTS), módulo WebGL | `unity/ProjectSettings/ProjectVersion.txt` |
| Proyecto Unity | URP 2D 17.3.0, Input System 1.20.1, TMP, asmdefs separados | `unity/Packages/manifest.json` |
| Escenas | `Game.unity` (índice 0), `Login.unity` (índice 1), `SampleScene` (índice 2, sobra) | `unity/ProjectSettings/EditorBuildSettings.asset` |
| Gameplay offline | Completo: 3 vidas, 120 s, curva de dificultad, pausa, game over, Libro, feedback de captura, SFX sintetizado | `unity/Assets/Scripts/`, [`evidencias/F2/`](evidencias/F2/README.md) |
| Tests PlayMode | **15/15** verdes (última corrida 2026-11-03; no re-ejecutados el 2026-11-06) | `unity/Assets/Tests/PlayMode/` |
| Autenticación | **Stub local** (`AuthStub`), no Supabase. Bloqueo 5 fallos → 60 s, token en memoria | `unity/Assets/Scripts/Auth/AuthStub.cs` |
| Arte de personaje y fondo | Palenquera y `bg_playa.png` importados y en la escena | `unity/Assets/Art/Characters/`, `.../Backgrounds/` |
| Arte de ítems | ⚠️ **Regresión real**: los 4 sprites originales se borraron y la escena quedó con 4 referencias rotas. Hay 60 PNG nuevos sin cablear | ver §3.2 |
| UI de Figma | 0 componentes exportados; `unity/Assets/Art/UI/` está **vacío** | — |
| Build WebGL | 16,2 MB en `builds/webgl` (generado 2026-10-28, **con el arte anterior**) | `builds/` (no versionado) |
| Nginx / Sg-Frontend | Operativo: 443 + 8081, TLS mkcert, CSP por hash, HSTS, proxy `/api/` | `infra/nginx/`, [`docs/fases/F3.md`](docs/fases/F3.md) |
| Docker | Compose de 5 nodos declarado y validado. `sg-frontend` y `sg-ai` *healthy*; `backend` y `security` tras perfiles | `infra/docker-compose.yml`, [`evidencias/F6/`](evidencias/F6/README.md) |
| Modelo de IA | `qwen2.5:3b` (1,9 GB) descargado en el volumen `costamarindo-ai-models`; `sg-ai` aislado en red `ai` | [`evidencias/F6/`](evidencias/F6/README.md) §4 |
| Backend | **No existe**: `backend/` está vacío | — |
| Base de datos | **No existe**: sin proyecto Supabase DEV, sin SQL versionado | — |
| Logs / SIEM | Sin Wazuh activo ni reglas; solo se declara en el compose | `infra/docker-compose.yml` |
| Gitleaks (A.8.25) | ✅ Implementado, hook instalado y probado | [`evidencias/A.8.25/`](evidencias/A.8.25/README.md) |
| Otros 9 controles ISO | ⬜ Sin implementar ni evidenciar | — |
| CI | **No existe** (`.github/` ausente) | — |
| SoA / informe / `docs/api.md` | **No existen** | — |
| Remoto Git | `main` publicada; árbol de trabajo **sucio** (§3.2) | `git status` |

### 3.2 Hallazgos que corrigen la documentación anterior

Estos puntos estaban mal documentados o directamente no estaban documentados. Quedan aquí
porque afectan a la planificación:

1. **Los 4 sprites de ítems con arte real ya no existen.** El commit `ee62e8c`
   (*feat(assets): agregar items recolectables y dañinos*) **borró**
   `item_cocada.png`, `item_tamarindo.png`, `item_pescado.png` y `item_frutas.png`, y dejó en su
   lugar 30 PNG en `Assets/Art/Items/recolectables/` y 30 en `Assets/Art/Items/daninos/`.
   `Game.unity` conserva las cuatro referencias a los GUID borrados, así que **hoy los ítems se
   dibujan con el relleno procedural**. La documentación seguía diciendo «4/8 ítems con arte».
2. **Los 60 PNG nuevos no están integrados.** Sus `.meta` están en disco pero **sin versionar**
   (un clon limpio los reimportaría con ajustes por defecto, no como Sprite). Además se
   importaron con `spriteMode: 2` (**Multiple**) y `maxTextureSize: 2048`, no con los ajustes de
   `FigmaArtImporter` (Single, PPU 100, 512).
3. **Los nombres no coinciden con el catálogo.** `ItemSpawner` resuelve el sprite por
   `sprite.name == def.Id` (por ejemplo `item_cocada`), pero los archivos nuevos se llaman
   `01_cocada_redonda`, `05_limonada_de_coco`, `01_erizo_de_mar`… Sin renombrar o sin cambiar
   el catálogo, ningún ítem nuevo se empareja.
4. **El árbol de trabajo está sucio en `main`**: `unity/ProjectSettings/ProjectSettings.asset`
   modificado (se vació `preloadedAssets`, que contenía `InputSystem_Actions.inputactions`) y
   los 60 `.meta` nuevos sin seguimiento. Ese cambio de `preloadedAssets` **no está verificado**
   y puede afectar a los mapas de acción en runtime: hay que decidir si se conserva o se revierte
   antes de commitear.
5. **`com.coplaydev.unity-mcp` está en `Packages/manifest.json` versionado.** Es una dependencia
   Git del agente dentro del proyecto Unity. No la necesita el build y es superficie de ataque
   (hallazgo H9 de la especificación): debe salir antes de la entrega o justificarse por escrito.
6. **Varias fases se describían con estados ya superados**: `docs/arquitectura.md` decía que el
   compose solo tenía `Sg-Frontend`; `docs/fases/F2.md` decía que jugador y fondo eran
   placeholders; `docs/fases/F6.md` y `infra/README.md` no reflejaban la verificación del
   2026-11-03. Todo eso quedó corregido en esta misma pasada.
7. **El build de 16,2 MB está desactualizado** respecto del arte actual: se generó antes del
   borrado de los sprites. Cualquier medición de peso debe repetirse tras cerrar `FE-02`.

---

## 4. Frontend · `Sg-Frontend`

**Responsabilidad.** Todo lo que el jugador ve y ejecuta: el juego Unity, su arte, su UI, el
build WebGL y el nodo Nginx que lo sirve por HTTPS y proxifica `/api/`.

**Límites.** No escribe en la base, no firma puntajes en nombre del servidor, no decide reglas
de negocio. El cliente **nunca es fuente de verdad** (A.8.9): solo presenta.

### 4.1 Estado actual

| Pieza | Estado |
| :--- | :--- |
| Bucle jugable offline | ✅ completo |
| Libro de colección, feedback de captura, SFX | ✅ |
| Escena `Login` con PIN y bloqueo progresivo | ✅ (contra stub local) |
| Personaje y fondo de Figma | ✅ en escena |
| Ítems con arte | ⛔ referencias rotas (§3.2.1) |
| Componentes de UI de Figma | ⛔ 0/~30 |
| Tokens (17 colores, 5 estilos) | ⛔ hardcodeados en el `SceneBuilder` |
| Build WebGL | 🚧 16,2 MB pero previo al arte actual |
| Nginx + TLS + CSP | ✅ operativo |
| Leaderboard, bocadillo de diálogo, envío de puntaje | ⛔ no existen en el cliente |

### 4.2 Backlog

| ID | Tarea | Depende de | DoD |
| :--- | :--- | :--- | :--- |
| **FE-01** | Reparar las 4 referencias rotas de `Game.unity`: regenerar la escena con el arte ya normalizado | FE-02 | Play en editor sin `Missing (Sprite)` y sin warnings de referencia |
| **FE-02** | Normalizar importación de los PNG elegidos (`Single`, PPU 100, máx. 512, alfa), renombrarlos a los Id del catálogo y **versionar los `.meta`** | FE-03 | `ItemSpawner` resuelve cada sprite por nombre; sin referencias huérfanas |
| **FE-03** | Curar el catálogo: decidir cuántos tipos de ítem existen (recomendado: 10–12, no 60) y qué sprite representa cada uno | — | Tabla de mapeo tipo → sprite → puntos/peso, aprobada y reflejada en `ItemCatalog` |
| **FE-04** | `Theme.cs` o ScriptableObject con los 17 colores y 5 estilos; quitar colores hardcodeados del `SceneBuilder` | — | Ningún `new Color(...)` de paleta suelto en UI |
| **FE-05** | Kit de UI: `WoodButton`, `IconButton`, `HandInput`, `PinSlot`, `WoodPanel`, `Heart`, `Counter`, `DialogBubble`, `LeaderRow`, `CollectibleCard` | FE-04 | 10 prefabs reutilizables usados por las escenas |
| **FE-06** | Aplicar el kit a HUD, pausa, game over, Libro y Login; reemplazar corazones de LiberationSans por el sprite | FE-05 | HUD sin placeholders tipográficos; consola limpia |
| **FE-07** | Cliente de red: `ApiClient`, `SessionClient`, `ScoreSigner`, `LeaderboardClient`, `DialogueClient` (`UnityWebRequest`, token en memoria) | BE-02…BE-05 | Flujo real contra `https://juego.local/api/` sin secretos en el cliente |
| **FE-08** | Pantalla de leaderboard (top 5 + posición propia) y estados vacío/error/timeout | FE-07 | Se ve el top 5 tras enviar un puntaje |
| **FE-09** | Bocadillo del NPC: “escribiendo…”, historial corto, sin bloquear gameplay, fallback visible | FE-07, IA-03 | El juego no se congela ni con Ollama caído |
| **FE-10** | Hacer `Login` obligatorio (build index 0) y encadenar `Login → Game` | FE-07 | Sin sesión no se juega; logout limpia memoria |
| **FE-11** | Playtest de 10–15 min y ajuste de `spawnInterval` / `fallSpeed` | FE-01 | Curva de dificultad validada por una persona real |
| **FE-12** | Rebuild Release + medición (tamaño, tiempo de carga, errores de consola) | FE-01, FE-07…FE-10 | ≤ 30 MB, < 10 s, 0 errores, 0 warnings |
| **FE-13** | Desactivar `SampleScene` del build y limpiar `preloadedAssets` (decidir si se conserva `InputSystem_Actions`) | — | Build Settings con solo las escenas necesarias; input verificado en WebGL |
| **FE-14** | Quitar `com.coplaydev.unity-mcp` del `manifest.json` versionado | — | El proyecto compila en una máquina sin MCP instalado |

### 4.3 Criterio de terminado de la división

- Partida completa en `https://juego.local` con arte real, HUD definitivo y leaderboard.
- Login obligatorio con usuario + PIN contra Supabase Auth.
- 0 errores y 0 warnings de consola en el build.
- Tests PlayMode verdes (los 15 actuales + los de red/cliente).

---

## 5. Backend · `Sg-Backend`

**Responsabilidad.** Servidor autoritativo del puntaje y único componente que habla con la base
y con la IA. Valida JWT, firma HMAC, nonce, TTL, plausibilidad y sesión de un solo uso. Emite
logs JSON auditables.

**Límites.** No renderiza nada, no guarda estado en memoria (D6), no expone `service_role` ni
documentación interactiva en producción.

### 5.1 Estado actual

**No existe.** `backend/` está vacío. El compose ya lo declara en el perfil `backend` y Nginx ya
proxifica `/api/` (hoy devuelve 502 a propósito).

### 5.2 Backlog

| ID | Tarea | Depende de | DoD |
| :--- | :--- | :--- | :--- |
| **BE-01** | Esqueleto: `app/main.py`, routers, settings Pydantic, `/api/health`, logging JSON a stdout | DB-01 (solo para arrancar con env) | `/api/health` responde 200 a través de Nginx |
| **BE-02** | `POST /api/session/start`: `nonce` + `session_key`, TTL 15 min, persiste en `game_sessions` | BE-01, DB-02 | Sesión creada y visible en la tabla; expira sola |
| **BE-03** | `POST /api/score`: JWT + HMAC-SHA256 + nonce + sesión no usada + no expirada + plausibilidad (12 pts/s) | BE-02, DB-02 | Los 4 ataques se rechazan **con motivo** |
| **BE-04** | Inserción con `service_role`; un solo envío por sesión | BE-03, DB-03 | Reintento de la misma sesión devuelve `session_used` |
| **BE-05** | `GET /api/leaderboard`: top 5 + posición propia | BE-01, DB-04 | Devuelve ambas cosas en una respuesta |
| **BE-06** | `POST /api/dialog`: validación, rate limit, saneo, llamada a Ollama, filtro de salida, fallback | BE-01, IA-02 | Con Ollama apagado responde < 1 s |
| **BE-07** | Rate limit y bloqueo progresivo de login (5 fallos → 60 s) del lado servidor | BE-01, DB-01 | 6.º intento bloqueado y registrado |
| **BE-08** | `Dockerfile` multi-stage, usuario no root, sin caché de pip, `expose: 8000` | BE-01 | `sg-backend` *healthy* con perfil `backend` |
| **BE-09** | `pytest`: firma válida, alterada, replay, expirada, implausible, health | BE-03…BE-06 | Suite verde en CI y local |
| **BE-10** | Contratos en `docs/api.md` (request/response/códigos de error) | BE-05 | Documento publicado y usado por FE-07 |
| **BE-11** | `docker-compose.override.yml` dev (8081) / prod (solo 443) | BE-08 | `ENV=prod` no publica 8081 ni docs |

### 5.3 Criterio de terminado de la división

- Flujo completo aceptado: login → sesión → partida → puntaje → top 5.
- Cuatro ataques de puntaje rechazados con motivo registrado.
- `pytest` verde; `/api/health` sano dentro del contenedor.
- Único componente con permiso de escritura en la base.

---

## 6. DB · `Sg-DB` (Supabase Cloud)

**Responsabilidad.** Identidad (GoTrue), esquema Postgres, políticas RLS, leaderboard y
persistencia de sesiones. Es la fuente de verdad de los datos.

**Límites.** No es un contenedor: es una excepción documentada (decisión D3). El cliente no
escribe nunca (D1). `game_sessions` es invisible para `anon` y `authenticated`.

### 6.1 Estado actual

**No existe.** Sin proyecto DEV, sin SQL versionado, sin backups. La plantilla de esquema está
en [`docs/CosTamarindo.md`](docs/CosTamarindo.md) §8.2 y sirve como punto de partida, no como
verdad final.

### 6.2 Backlog

| ID | Tarea | Depende de | DoD |
| :--- | :--- | :--- | :--- |
| **DB-01** | Crear proyecto **DEV** en Supabase Cloud (free) y dejar `.env` local con sus claves | — | `.env` completo; nada de claves en el repo |
| **DB-02** | `backend/sql/001_schema.sql`: `profiles`, `game_sessions`, `scores`, índices y constraints | DB-01 | Esquema aplicado y **versionado**; reproducible desde cero |
| **DB-03** | RLS: lectura pública del leaderboard; sin `insert/update/delete` para `anon`/`authenticated`; `game_sessions` solo `service_role` | DB-02 | Security Advisor sin tablas sin RLS; `anon` no puede insertar |
| **DB-04** | Leaderboard: vista/top 5 + cálculo de posición propia (la vista sola no conoce al llamante; la posición la resuelve el backend) | DB-02 | Top 5 y posición coinciden con la consulta directa |
| **DB-05** | Políticas de datos: username `^[A-Za-z0-9_]{3,16}$`, PIN de 4 dígitos, `score` 0…100000, `duration_ms` acotado | DB-02 | Valores inválidos rechazados por constraint **y** por Pydantic |
| **DB-06** | `pg_dump` programado + **restauración probada** en un proyecto limpio | DB-03 | Restauración documentada con salida literal |
| **DB-07** | Keep-alive del proyecto free y política de retención mínima | DB-01 | Proyecto no pausado durante la ventana de demo |
| **DB-08** | Separar DEV/PROD: proyecto y claves distintas | DB-01 | Token DEV rechazado contra PROD (evidencia A.8.31) |

### 6.3 Criterio de terminado de la división

- Registro y login reales desde el juego; `anon` no puede escribir en `scores`.
- Leaderboard con top 5 + posición propia.
- Sexto intento fallido bloqueado (con apoyo de BE-07).
- Backup reciente y restauración probada.

---

## 7. Security · `Sg-Security`

**Responsabilidad.** Implementar, atacar, corregir y **evidenciar** los 10 controles del Anexo A.
Opera Wazuh, Gitleaks, SonarQube, TLS, cabeceras, hashes e integridad del webroot, y verifica el
aislamiento de red de los cinco nodos.

**Límites.** No implementa funcionalidad de juego ni de negocio: la audita. Cada control se cierra
con «antes vulnerable → ataque reproducible → corrección → verificación».

### 7.1 Estado actual

| Control | Estado | Dónde |
| :--- | :--- | :--- |
| **A.8.25** SDLC seguro (Gitleaks) | ✅ | [`evidencias/A.8.25/`](evidencias/A.8.25/README.md) |
| A.8.24 Criptografía | 🚧 solo TLS/headers en Nginx | `infra/nginx/` |
| A.8.20 Redes | 🚧 compose y aislamiento con `docker compose port`; falta `nmap` | [`evidencias/F6/`](evidencias/F6/README.md) |
| A.8.15 Logging | 🚧 logs Nginx en JSON; sin Wazuh ni reglas | `infra/nginx/nginx.conf` |
| Resto (A.8.5, A.8.9, A.8.12, A.8.28, A.8.31, A.8.19) | ⬜ | — |

### 7.2 Backlog

| ID | Tarea | Depende de | DoD |
| :--- | :--- | :--- | :--- |
| **SEC-01** | Plantilla de evidencia + `evidencias/ataques/` con scripts reproducibles | — | Plantilla usada por los 10 controles |
| **SEC-02** | `docs/soa.md` (Declaración de Aplicabilidad) + matriz de riesgos | — | 93 controles evaluados: aplica / no aplica / fuera de alcance |
| **SEC-03** | A.8.24: ataque con Burp al `POST /api/score` (alterar score) + replay + verificación de TLS | BE-03 | Rechazo con `bad_signature` / `session_used` y log |
| **SEC-04** | A.8.5: fuerza bruta al login, JWT expirado, `alg: none` | BE-07, DB-01 | 429/bloqueo, 401 documentado |
| **SEC-05** | A.8.9: editar IndexedDB (`PlayerPrefs`) para subir vidas/puntaje | FE-07 | El puntaje no cambia; el servidor ignora el cliente |
| **SEC-06** | A.8.20: `nmap` desde host y desde otro contenedor; puertos cerrados | BE-08 | `:8000` y `:11434` inalcanzables; evidencia literal |
| **SEC-07** | A.8.12: consola del navegador + `strings` del `.wasm`/`.data` | FE-12 | Sin logs, secretos ni URLs internas |
| **SEC-08** | A.8.15: Wazuh activo con reglas 100100/100101 y FIM del webroot | BE-03, SEC-01 | Alerta correlacionada por IP |
| **SEC-09** | A.8.28: SQLi en username, XSS/rich text en leaderboard, prompt injection | BE-06, FE-08 | Entradas neutralizadas; `dialog_blocked` registrado |
| **SEC-10** | A.8.31: token DEV contra PROD; defines de entorno en Unity | DB-08, FE-07 | Rechazo por claves distintas |
| **SEC-11** | A.8.19: `SHA256SUMS` firmado con GPG + verificación en despliegue + FIM | FE-12 | Falla el hash si se reemplaza el `.wasm` |
| **SEC-12** | SonarQube (backend + C#) con Quality Gate y `pip-audit`/`npm audit` | BE-09 | Sin vulnerabilidades críticas sin justificar |
| **SEC-13** | CI en GitHub Actions: Gitleaks + tests + (opcional) Sonar | SEC-11, BE-09 | `main` validada automáticamente |
| **SEC-14** | Quitar el paquete MCP del `manifest.json` y documentar la superficie de agentes | FE-14 | Sin dependencias de agente en el proyecto entregado |

### 7.3 Criterio de terminado de la división

- 10 carpetas en `evidencias/` con ataque + corrección + verificación.
- SoA e informe de hallazgos en `docs/`.
- Un solo puerto público (443); hashes verificados; sin secretos en el historial.
- Wazuh con al menos FIM + 2 reglas funcionando.

---

## 8. IA · `Sg-AI`

**Responsabilidad.** Diálogo del NPC con Ollama local (`qwen2.5:3b`), system prompt fijo,
guardrails contra inyección, control de concurrencia y fallback determinista. El NPC es
**cosmético**: nunca afecta puntaje ni lógica.

**Límites.** Sin salida a Internet (red `ai` es `internal: true`). Sin APIs externas en runtime.
El modelo se descarga por procedimiento explícito, no en caliente.

### 8.1 Estado actual

- `sg-ai` **healthy**, sin puerto publicado, solo en `costamarindo-ai`.
- Modelo `qwen2.5:3b` (1,9 GB) presente en el volumen `costamarindo-ai-models`.
- `infra/scripts/pull-model.sh` implementa la conexión temporal a `bridge` para descargar.
- **No existe** endpoint de diálogo, ni prompt, ni guardrails, ni UI de bocadillo.

### 8.2 Backlog

| ID | Tarea | Depende de | DoD |
| :--- | :--- | :--- | :--- |
| **IA-01** | Medir latencia y CPU reales de `qwen2.5:3b` dentro del contenedor | — | Número documentado; decisión de modelo confirmada o corregida |
| **IA-02** | System prompt fijo de la Palenquera: español, ≤ 3 frases, solo seguridad, entrada delimitada como dato | — | Prompt versionado en `backend/app/ai/` |
| **IA-03** | Guardrails: detección de inyección, delimitadores `<jugador>…</jugador>`, filtro de salida (`<`, `>`, longitud) | IA-02 | Inyección bloqueada y registrada como `dialog_blocked` |
| **IA-04** | Timeout 20 s, 1 request concurrente, cola y rate limit por usuario | BE-06 | Sin caída del backend con carga |
| **IA-05** | Banco de respuestas por contexto (inicio, captura, peligro, game over, libro) | IA-02 | Fallback < 1 s y en personaje |
| **IA-06** | Pruebas: normal, injection, Ollama apagado, respuesta lenta | IA-03, IA-05 | 4 casos documentados con salida literal |
| **IA-07** | Confirmar que la respuesta no altera puntaje ni estado de partida | FE-09 | Test que lo demuestra |

### 8.3 Criterio de terminado de la división

- Respuestas en personaje y en español.
- Inyección bloqueada y auditada.
- Ollama caído → fallback < 1 s y juego jugable.
- Cero llamadas externas en runtime (verificable por red).

---

## 9. Matriz fase → división

| Fase | Contenido | División principal | Divisiones secundarias | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **F0** | Repositorio y baseline | *Transversal* (Security) | Frontend | ✅ cerrada |
| **F1** | Pipeline Figma → Unity (arte y UI) | **Frontend** | — | 🚧 ~25 % real (§3.2) |
| **F2** | Vertical slice jugable | **Frontend** | — | ✅ offline cerrado |
| **F3** | Build WebGL + Nginx | **Frontend** | Security (TLS/CSP) | ✅ cerrada (build a regenerar) |
| **F4** | Supabase: Auth, esquema y leaderboard | **DB** | Backend (rate limit), Frontend (AuthClient) | ⬜ pendiente |
| **F5** | Backend FastAPI + puntaje firmado | **Backend** | DB (service_role), Frontend (clientes) | ⬜ pendiente |
| **F6** | Infraestructura Docker de 5 nodos | *Transversal*: un nodo por división | Security (aislamiento) | 🚧 2 de 4 contenedores arriba |
| **F7** | NPC con Ollama | **IA** | Backend (endpoint), Frontend (bocadillo) | ⬜ pendiente |
| **F8** | Controles ISO, ataques y evidencias | **Security** | Las cuatro restantes | 🚧 1 de 10 controles |
| **F9** | Despliegue final y entrega | *Transversal* | Las cinco | ⬜ pendiente |

**Lectura de la matriz.** Las fases F1, F2 y F3 son **Frontend** y ya están casi cerradas. Lo que
falta de verdad son F4 (DB), F5 (Backend), F7 (IA) y F8 (Security). F6 no es una fase de una sola
división: es la suma de los cinco nodos, y cada división cierra el suyo.

### 9.1 Reparto de F6 por división

| Nodo | División dueña | Estado | Qué falta |
| :--- | :--- | :--- | :--- |
| `Sg-Frontend` | Frontend | ✅ healthy | Rebuild, `override` prod (BE-11) |
| `Sg-AI` | IA | ✅ healthy | Endpoint y guardrails (IA-02…IA-06) |
| `Sg-Backend` | Backend | 🚧 declarado, perfil `backend` | `Dockerfile` y API (BE-08) |
| `Sg-Security` | Security | 🚧 declarado, perfil `security` | Reglas Wazuh y FIM (SEC-08) |
| `Sg-DB` | DB | ⬜ no existe | Proyecto Supabase DEV (DB-01…DB-08) |

---

## 10. Orden de ejecución (ruta crítica)

```
Arte y build (paralelo, no bloquea la ruta crítica):
  FE-03 → FE-02 → FE-01 → FE-11 → FE-12

Ruta crítica:
  DB-01 → DB-02 → DB-03 → DB-04
                └→ BE-01 → BE-02 → BE-03 → BE-04 → BE-05
                                            └→ FE-07 → FE-08 → FE-10
                └→ BE-06 → IA-02 → IA-03 → IA-06 → FE-09

Nodo backend y aislamiento:
  BE-08 → SEC-06 → SEC-08

Cierre:
  Todo lo anterior → SEC-03 … SEC-13 → F9
```

**Prioridad recomendada:** DB → Backend → Frontend (clientes) → IA → Security → entrega.
El arte (FE-01…FE-06) corre en paralelo y **no** bloquea la ruta crítica; cerrarlo antes de FE-12
para no reconstruir el build dos veces.

---

## 11. Convenciones de trabajo

### 11.1 Ramas

| Rama | Uso |
| :--- | :--- |
| `main` | Siempre desplegable. Protegida: solo PR. |
| `feat/<tema>` | Funcionalidad nueva (`feat/game-loop`) |
| `fix/<tema>` | Corrección de defecto |
| `sec/<control>` | Implementación o corrección de un control ISO (`sec/a8-24-hmac`) |
| `docs/<tema>` | Documentación y evidencias |

### 11.2 Commits — Conventional Commits

Formato: `tipo(alcance): descripción imperativa en presente`.

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

Reglas: un commit = un cambio lógico; no mezclar arte con backend ni con seguridad; nunca
commitear secretos; el cuerpo explica **por qué**, no qué.

### 11.3 Flujo

`rama` → commits → PR → revisión → merge a `main` → despliegue. Nada directo a `main` después del
primer commit.

### 11.4 Secretos

- `.env` real solo en la máquina y en el servidor; `.env.example` versionado.
- Claves de Supabase **DEV** para desarrollo; **PROD** jamás en el repo.
- Gitleaks en pre-commit y en CI.
- Supabase MCP apuntado solo a DEV y con permisos mínimos.

### 11.5 Decisiones cerradas (no se reabren)

| ID | Decisión |
| :--- | :--- |
| D1 | El cliente nunca escribe en la base; solo el backend con `service_role` |
| D2 | Backend en FastAPI (Python) |
| D3 | Supabase Cloud free para Auth/Postgres; `Sg-DB` como excepción externa documentada |
| D4 | Modelo `qwen2.5:3b` |
| D5 | TLS local con mkcert (`juego.local`) |
| D6 | Sesiones de juego en Postgres (`game_sessions`), no en memoria |
| D7 | Figma manda en arte y narrativa; la capa ISO vive en infraestructura y documentación |
| D8 | Login usuario + PIN con correo sintético, rate limit y bloqueo progresivo |
| — | Arte por REST con token, no por MCP (cuota del plan Starter agotada) |
| — | SVG es la fuente; el PNG es derivado y se renderiza con `@resvg/resvg-js` |
| — | Compilar y testear por batchmode; MCP solo para escena y APIs en vivo |
| — | Gzip en Nginx, no Brotli (`nginx:alpine` no trae `ngx_brotli`) |

---

## 12. Riesgos y planes B

| Riesgo | Señal temprana | Plan B | División |
| :--- | :--- | :--- | :--- |
| El arte nuevo no se cablea y el build sale con placeholders | Referencias rotas en la escena, catálogo sin coincidencias | Congelar `ItemCatalog` en 8 ítems y mapear solo esos sprites | Frontend |
| Supabase Cloud pausa por inactividad | Proyecto pausado | keep-alive + `pg_dump` diario; self-hosted como alternativa | DB |
| El backend no llega a tiempo | F5 sin cerrar al 60 % del cronograma | Reducir alcance: sesión + score + leaderboard; diálogo a respuestas predefinidas | Backend |
| Ollama lento en CPU | > 20 s por respuesta | Modelo 1B–3B más pequeño, `num_predict` corto, fallback primero | IA |
| Wazuh consume demasiada RAM | > 4 GB con todo arriba | Perfil `security` apagado; solo FIM + 2 reglas | Security |
| Docker Desktop no arranca | error de engine/WSL | Compose en WSL2 directo; documentar la limitación | Security |
| Build WebGL no carga | pantalla infinita | revisar `Content-Encoding`, MIME y CSP; regenerar `csp.conf` | Frontend |
| Conflictos de escena en Git | merges con conflictos YAML | Force Text + prefabs + una persona por escena | Frontend |
| Fase de ataques al final y sin tiempo | F8 sin fecha | Reservar ~30 % del tiempo; atacar cada control al implementarlo | Security |

---

## 13. Orden de recorte si falta tiempo

1. Detalles de arte y efectos (FE-04…FE-06).
2. Controles de Nivel 2 (A.8.22, A.8.8, A.8.29, A.8.13, A.8.17, A.8.32, A.8.2, A.8.3).
3. Entorno staging (quedarse con dev y prod).
4. Diálogo libre → opciones predefinidas con respuesta acotada (IA-05 primero).
5. Reglas avanzadas de Wazuh → solo FIM + 2 reglas.

**No se recorta:** firma y validación de puntaje, validación de entrada, logging a Wazuh,
autenticación, evidencias de ataque y corrección.

---

## 14. Checklist de entrega

- [ ] Build WebGL Release servido por Nginx en HTTPS, con el arte actual.
- [ ] Login usuario + PIN con bloqueo por intentos, contra Supabase real.
- [ ] Leaderboard top 5 + posición propia.
- [ ] Puntaje firmado con HMAC y validado en servidor.
- [ ] NPC con Ollama + fallback.
- [ ] 5 nodos operativos y aislados (Frontend, Backend, DB, Security, IA).
- [ ] 10 controles con evidencia de ataque y corrección.
- [ ] SoA completa.
- [ ] Gitleaks sin hallazgos; sin secretos en el repo; sin dependencias de agente en Unity.
- [ ] `pg_dump` reciente y restauración probada.
- [ ] Demo reproducible + video de respaldo.

---

## 15. Estructura del repositorio

```
CosTamarindo/
├── Plan.md                     # este documento (fuente de verdad)
├── README.md                   # arranque rápido y mapa del repo
├── .gitignore / .gitattributes
├── .env.example                # variables sin valores reales
├── docs/
│   ├── CosTamarindo.md         # especificación ISO 27001
│   ├── arquitectura.md         # nodos, redes, compose y flujo del equipo
│   ├── api.md                  # contratos del backend (F5)
│   ├── soa.md                  # Declaración de Aplicabilidad (F8)
│   └── fases/                  # bitácora por fase (F0, F1, F2, F3, F6)
├── unity/                      # proyecto Unity 6 (Frontend)
├── backend/                    # FastAPI (Backend) — hoy vacío
│   ├── app/                    # routers, modelos, seguridad, logging, ai
│   ├── sql/                    # esquema versionado (DB)
│   └── tests/
├── infra/
│   ├── docker-compose.yml      # los 5 nodos declarados
│   ├── nginx/                  # Sg-Frontend
│   ├── wazuh/                  # Sg-Security (F8)
│   └── scripts/                # up/down/logs/deploy/tls/build/pull-model
├── assets/figma/               # exportaciones fuente (Frontend)
└── evidencias/                 # una carpeta por control ISO (Security)
```

### 15.1 Qué **no** se sube

`.opencode/`, `opencode.json*`, `.codex/`, `.claude/`, `.cursor/`, `AGENTS.md`, `CLAUDE.md`,
skills, configuraciones de MCP, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds, binarios,
`.env`, certificados, `node_modules/`, `__pycache__/`, dumps de base de datos. Todo está en
`.gitignore`.
