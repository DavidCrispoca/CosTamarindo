# Handoff · Frontend

> **División:** Frontend · **Nodo:** `Sg-Frontend`
> **Backlog:** `Plan.md` §4 (`FE-01`…`FE-14`)
> **Contratos:** [`../api.md`](../api.md)
> **Estado:** juego jugable verificado en WebGL y **validado contra el backend real** en
> `https://juego.local` (flujo anónimo + claim + NPC). Queda publicarlo en URL pública y cerrar
> `FE-05`/`FE-06`/`FE-11`.

---

## 1. Lo que ya está hecho

| Pieza | Estado | Dónde |
| :--- | :--- | :--- |
| Bucle jugable completo | ✅ | `unity/Assets/Scripts/` |
| Libro, feedback de captura, SFX | ✅ | `BookView.cs`, `CaptureFeedback.cs`, `ProceduralSfx.cs` |
| Personaje y fondo de Figma | ✅ en escena | `Assets/Art/Characters/`, `.../Backgrounds/` |
| 12 ítems con arte curado | ✅ PNG + `.meta` versionados | `Assets/Art/Items/curated/`, `ItemCatalog.cs` |
| Tokens visuales (17 colores, 5 estilos) | ✅ | `Assets/Scripts/Core/Theme.cs` |
| Tipografías SDF | ✅ | `Assets/Art/Fonts/` |
| Escena de login `/ Jugar` → `Game` | ✅ anónima; el PIN se pide solo al reclamar | `Assets/Scenes/Login.unity`, `UI/LoginController.cs` |
| Build + Nginx + TLS | ✅ build Release 2026-11-24 (16,5 MB) servido en `https://juego.local` y validado contra el backend real (`F7/16`, `F7/17`); regenerar si cambia Unity | `builds/webgl`, `infra/nginx/` |
| Pensamiento del NPC | ✅ runtime anclado al sprite, “escribiendo…”, historial de una línea por entrada, fondo translúcido | `Assets/Scripts/UI/NpcDialogueController.cs`, `Core/Theme.cs` |
| Fin de partida | ✅ textos en franjas separadas; sin backend muestra “Sin conexión…” en tono neutro, no un error rojo | `Assets/Scripts/UI/OverlayController.cs`, `Assets/Editor/CosTamarindoSceneBuilder.cs` |
| 15 pruebas PlayMode | ✅ 15/15 (2026-11-24) | `Assets/Tests/PlayMode/` |

---

## 2. Lo que falta, en orden

### 2.1 El arte de ítems — ✅ resuelto el 2026-11-24

El commit `ee62e8c` había borrado `item_cocada.png`, `item_tamarindo.png`, `item_pescado.png` y
`item_frutas.png`, dejando cuatro GUID muertos en `Game.unity` y 60 PNG nuevos sin integrar.

Cómo quedó cerrado (`FE-01`…`FE-03`, detalle en [`../fases/F1.md`](../fases/F1.md) §5.2):

1. **Catálogo de 12 tipos** (8 buenos, 3 peligrosos, 1 basura) en `ItemCatalog.cs`.
2. Los 12 PNG elegidos viven en `Assets/Art/Items/curated/` con el nombre exacto del `Id`
   (`git mv`, conservando el `.meta`), normalizados a `spriteMode: 1`, PPU 100, máx. 512.
3. `CosTamarindoSceneBuilder.LoadItemSprites` carga **solo** esos 12 y avisa si falta alguno;
   la escena se regeneró y `Game.unity` quedó sin referencias huérfanas.

Los otros 48 PNG siguen en `recolectables/` y `daninos/` como fuente, fuera del build.

### 2.2 Legibilidad del bocadillo y del fin de partida (2026-11-24)

| Ajuste | Antes | Ahora |
| :--- | :--- | :--- |
| Fondo del bocadillo | `Ink` con alpha 0,94 (casi opaco, tapaba los ítems) | `Theme.DialogueBubble` = alpha **0,55**; contorno y puntos también más ligeros |
| Frase e historial | 34/21 px con franjas que podían tocarse | 32/20 px, franjas separadas y una línea por entrada (máx. 52 caracteres) |
| Panel de fin de partida | Título, score, récord, desglose, estado y botón con posiciones que se traslapaban | Seis franjas verticales con hueco dentro de la tarjeta de 760×700; desglose limitado a 3 líneas + “… y N tipos más” |
| Sin conexión al terminar | “No se pudo conectar con el servidor.” en rojo | “Sin conexión: puntaje guardado en este dispositivo.” en tono neutro; sí se sigue mostrando un error si el servidor responde 4xx/5xx |

Los textos se regeneran con `CosTamarindo.EditorTools.BuildScript.RebuildScenes`; no hace falta
editar `Game.unity` a mano. El puntaje sigue guardándose en `PlayerPrefs` (`GameManager`).

### 2.3 Integración con la API (`FE-07`) — ya desbloqueada

El contrato vigente es **anónimo**: se juega sin login y solo se pide nombre + PIN si el
puntaje entra al top 5. Detalle completo en [`../api.md`](../api.md) §2–§3. Flujo:

```
POST /api/session/start          -> session_id, nonce, session_key, best_score  (sin token)
   (jugar)
POST /api/score                  -> firma HMAC del resultado; responde in_top5  (sin token)
GET  /api/leaderboard            -> top 5
POST /api/claim                  -> nombre + PIN                                 (si in_top5)
POST /api/dialog                 -> texto de la Palenquera                       (token opcional)
```

`/api/auth/register`, `/login`, `/refresh` y `/logout` siguen disponibles por compatibilidad,
pero **no** están en el camino del jugador.

#### Reglas de integración

| Regla | Detalle |
| :--- | :--- |
| Base URL | `https://juego.local/api` en local (mismo origen: no hay CORS); en producción, la URL pública con `/api/` proxificado por Nginx |
| Token | si se usa, **solo en memoria**. Nunca `PlayerPrefs` ni IndexedDB (A.8.9) |
| `session_key` | solo en memoria durante la partida; no se persiste ni se envía al servidor |
| Contenido | `Content-Type: application/json`, `UnityWebRequest` |
| Errores | leer `code` del cuerpo; no mostrar detalles internos al jugador |
| Timeout | 10 s para sesión/score, 10 s para claim, 25 s para diálogo |
| Sin backend | no es un error de juego: el récord se guarda en `PlayerPrefs` y se muestra «Sin conexión: puntaje guardado en este dispositivo.» |

#### Firma del puntaje

Cadena canónica, exactamente:

```
{session_id}|{score}|{duration_ms}|{catches}|{nonce}
```

y `signature = hex(HMAC-SHA256(session_key, cadena))`, con `session_key` interpretada como
**bytes desde su hex** (64 caracteres → 32 bytes).

```csharp
static string Sign(string sessionKeyHex, string canonical)
{
    using var hmac = new HMACSHA256(Convert.FromHexString(sessionKeyHex));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
    return Convert.ToHexString(hash).ToLowerInvariant();
}
```

`duration_ms` es la duración real de la partida, `catches` el número de ítems atrapados
(buenos + peligros + basura). Los techos del servidor son:

- `score ≤ segundos × 12`
- `score ≤ catches × 25`
- `catches ≤ segundos × 3`

Si el cliente los supera, el servidor rechaza con `implausible_score`. **No inventar valores
para “cuadrar”**: si un playtest real los supera, se ajusta el servidor con evidencia.

#### Errores que el cliente debe manejar

| `code` | Qué mostrar |
| :--- | :--- |
| `bad_session` / `bad_nonce` / `bad_signature` | no reenviar; registrar en consola |
| `session_used` / `session_expired` | no reenviar; mostrar el resultado local |
| `implausible_score` | registrar en consola y no reintentar |
| `bad_session` / `not_top5` en `/api/claim` | no se puede reclamar ese puntaje; cerrar el panel |
| `invalid_credentials` / `account_locked` | en `/api/claim`: PIN incorrecto o cuenta bloqueada; respetar `Retry-After` |
| `weak_pin` / `username_taken` | “Elegí otro PIN” / “Ese usuario ya existe” |
| `database_unavailable` / `upstream_error` | “Servicio no disponible, intentá de nuevo” |
| red caída / HTTP 404 en local | estado offline neutral; el puntaje ya quedó en el dispositivo |

#### Dónde engancharlo

| Archivo | Cambio |
| :--- | :--- |
| `Assets/Scripts/Auth/AuthStub.cs` | endpoints de login/registro solo por compatibilidad; el flujo del jugador ya no los usa |
| `Assets/Scripts/Core/GameManager.cs` | ✅ expone `score`, `durationMs` y `catches` al terminar la partida |
| `Assets/Scripts/Net/ApiClient.cs` | ✅ cliente HTTP de session/score/claim/leaderboard/dialog; token opcional en memoria |
| `Assets/Scripts/Net/ScoreSigner.cs` | ✅ firma HMAC (arriba) |
| `OverlayController` | ✅ envío automático al terminar, panel top-5/claim, estado de error y modo offline neutral |

### 2.4 Resto del backlog

**Estado 2026-11-25:** `FE-07`…`FE-10`, `FE-12`…`FE-14` cerrados y validados contra el backend
real. En curso queda la publicación pública. `FE-05`/`FE-06` siguen bloqueados por `FIGMA_TOKEN`
y `FE-11` necesita una persona.

| Tarea | Qué falta |
| :--- | :--- |
| `FE-04` | ✅ `Theme.cs` con los 17 colores y 5 estilos; los constructores y controladores ya no escriben colores de paleta |
| `FE-05` | 10 prefabs de UI (`WoodButton`, `HandInput`, `PinSlot`, `Heart`, `DialogBubble`, `LeaderRow`…) — ⛔ bloqueado por `FIGMA_TOKEN` |
| `FE-06` | aplicar el kit a HUD, pausa, game over, Libro y Login; corazones con sprite — ⛔ depende de `FE-05` |
| `FE-08` | ✅ pantalla de leaderboard (top 5 + reclamo con nombre y PIN) y estados vacío/error/timeout |
| `FE-09` | ✅ “escribiendo…”, historial corto de 2 líneas y fallback visible, sin bloquear gameplay |
| `FE-10` | ✅ `Login` en build index 0 y encadenado `Login → Game` |
| `FE-11` | ⬜ playtest de 10–15 min y ajuste de `spawnInterval` / `fallSpeed` (necesita una persona) |
| `FE-12` | ✅ rebuild Release 2026-11-24: 16,5 MB, 1,2 s, 0 errores de consola |
| `FE-13` | ✅ `SampleScene` deshabilitada y `preloadedAssets` vacío verificado en WebGL |
| `FE-14` | ✅ `com.coplaydev.unity-mcp` fuera de `manifest.json` y del lock |

---

## 3. Cómo se verifica el trabajo

```powershell
# tests del juego (batchmode, sin abrir el editor)
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -projectPath unity -runTests -testPlatform PlayMode `
  -testResults "$env:TEMP\playtests.xml" -logFile "$env:TEMP\playtests.log"

# regenerar Login/Game con los constructores (batch)
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath unity `
  -executeMethod CosTamarindo.EditorTools.BuildScript.RebuildScenes `
  -logFile "$env:TEMP\rebuild-scenes.log"

# build Release + CSP
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath unity `
  -executeMethod CosTamarindo.EditorTools.BuildScript.BuildWebGL `
  -logFile "$env:TEMP\webgl-build.log"

# sin Docker: servidor local que respeta Content-Encoding y CSP
node tools/dev-server/server.mjs 8080

# medición en navegador real (Edge + playwright-core, headless)
node tools/dev-server/check.mjs http://localhost:8080/
node tools/dev-server/check.mjs https://juego.local/
```

**Criterio de aceptación de `FE-12`:** `unityReady: true`, runtime < 10 s, transferencia
≤ 30 MB, cero errores y cero advertencias de consola.

---

## 4. Trampas que ya costaron tiempo

1. **El 8080 del host es de Unity MCP** si lo tenés corriendo (el proyecto ya no lo trae como
   dependencia). `tools/dev-server/server.mjs` acepta otro puerto: `node … 8090`.
2. **La CSP se lee al arrancar.** Si se regenera `csp.conf` (o `index.html` cambia), hay que
   recargar Nginx o el juego se queda cargando.
3. **Unity pide los `.gz` directamente.** No sirve `gzip_static`: hace falta
   `Content-Encoding: gzip` **y** el MIME original (`application/wasm`).
4. **`ItemSpawner` empareja por `sprite.name == ItemDef.Id`.** Un sprite con otro nombre no
   se usa, aunque esté importado.
5. **`spriteMode: 2` es Multiple**, no Single. Los 12 PNG curados ya están en Single; los 48
   restantes siguen en Multiple y por eso no se cargan como `Sprite` (no están en el build).
6. **`SampleScene` ya está deshabilitada** (build index 0 `Login`, 1 `Game`). Si alguien
   regenera las escenas, los constructores vuelven a fijar ese orden.
7. **El build no está versionado.** `builds/` está en `.gitignore`: cada quien lo regenera con
   `BuildScript.BuildWebGL` y lo mide con `check.mjs` antes de publicarlo.

---

## 5. Qué NO tocar

- `backend/` (salvo leer `docs/api.md`).
- `infra/nginx/` sin avisar: es superficie de Security.
- `Assets/Scripts/Core/GameManager.cs` en su lógica de reglas: los valores de puntaje y
  vidas son la referencia de los techos de plausibilidad del servidor. Si cambian, avisar.
