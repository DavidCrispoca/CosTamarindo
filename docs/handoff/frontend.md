# Handoff · Frontend

> **División:** Frontend · **Nodo:** `Sg-Frontend`
> **Backlog:** `Plan.md` §4 (`FE-01`…`FE-14`)
> **Contratos:** [`../api.md`](../api.md)
> **Estado:** juego jugable offline; API online lista y sin consumir.

---

## 1. Lo que ya está hecho

| Pieza | Estado | Dónde |
| :--- | :--- | :--- |
| Bucle jugable completo | ✅ | `unity/Assets/Scripts/` |
| Libro, feedback de captura, SFX | ✅ | `BookView.cs`, `CaptureFeedback.cs`, `ProceduralSfx.cs` |
| Personaje y fondo de Figma | ✅ en escena | `Assets/Art/Characters/`, `.../Backgrounds/` |
| Tipografías SDF | ✅ | `Assets/Art/Fonts/` |
| Escena de login con bloqueo | ✅ contra stub | `Assets/Scenes/Login.unity`, `Auth/AuthStub.cs` |
| Build + Nginx + TLS | ✅ | `builds/webgl`, `infra/nginx/` |
| 15 pruebas PlayMode | ✅ | `Assets/Tests/PlayMode/` |

---

## 2. Lo que falta, en orden

### 2.1 El arte de ítems está roto (lo primero)

El commit `ee62e8c` borró `item_cocada.png`, `item_tamarindo.png`, `item_pescado.png` y
`item_frutas.png`. `Game.unity` conserva **cuatro GUID que ya no existen**, así que todos los
ítems se dibujan con relleno procedural. Además hay 60 PNG nuevos sin integrar.

| Paso | Tarea | Qué hacer |
| :--- | :--- | :--- |
| 1 | `FE-03` | Decidir el catálogo final. **Recomendado: 10–12 tipos**, no 60: el spawner no gana nada con 60 y el Libro es una grilla 4×2. Aprobar una tabla tipo → sprite → puntos → peso |
| 2 | `FE-02` | Renombrar los PNG elegidos a los `Id` del catálogo (`item_cocada.png`, `item_erizo.png`…) y **versionar los `.meta`**. Extender `Assets/Editor/FigmaArtImporter.cs` para recorrer `Assets/Art/Items`: `Single`, PPU 100, máx. 512, alfa |
| 3 | `FE-01` | Regenerar la escena con **CosTamarindo → Construir escena de juego** y comprobar que no quede `Missing (Sprite)` |

Detalle completo en [`../fases/F1.md`](../fases/F1.md) §5.

> **Por qué este orden:** no se puede renombrar ni regenerar sin haber decidido qué ítems
> existen. El catálogo manda.

### 2.2 Integración con la API (`FE-07`) — ya desbloqueada

El backend está en producción local. Contrato completo en [`../api.md`](../api.md). Flujo:

```
POST /api/auth/register  o  /api/auth/login     -> JWT + refresh
POST /api/session/start                          -> session_id, nonce, session_key
   (jugar)
POST /api/score                                  -> firma HMAC del resultado
GET  /api/leaderboard                            -> top 5 + mi posición
```

#### Reglas de integración

| Regla | Detalle |
| :--- | :--- |
| Base URL | `https://juego.local/api` (mismo origen: no hay CORS en el juego) |
| Token | **solo en memoria**. Nunca `PlayerPrefs` ni IndexedDB (A.8.9) |
| `session_key` | solo en memoria durante la partida; no se persiste |
| Contenido | `Content-Type: application/json`, `UnityWebRequest` |
| Errores | leer `code` del cuerpo; no mostrar detalles internos al jugador |
| Timeout | 10 s para auth, 10 s para score, 25 s para diálogo |

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
| `invalid_credentials` | “Usuario o PIN incorrectos” |
| `account_locked` | “Cuenta bloqueada”, cuenta atrás con `Retry-After` |
| `weak_pin` | “Elegí otro PIN” |
| `username_taken` | “Ese usuario ya existe” |
| `invalid_token` | volver a login |
| `session_used` / `session_expired` | no reenviar; mostrar el resultado local |
| `implausible_score` | registrar en consola y no reintentar |
| `database_unavailable` / `upstream_error` | “Servicio no disponible, intentá de nuevo” |

#### Dónde engancharlo

| Archivo | Cambio |
| :--- | :--- |
| `Assets/Scripts/Auth/AuthStub.cs` | reemplazar el cuerpo de `SignIn` por la llamada a `/api/auth/login`. **La UI no se toca** |
| `Assets/Scripts/Core/GameManager.cs` | al terminar la partida, exponer `score`, `durationMs`, `catches` |
| nuevo `Assets/Scripts/Net/ApiClient.cs` | cliente HTTP con token en memoria |
| nuevo `Assets/Scripts/Net/ScoreSigner.cs` | firma HMAC (arriba) |
| `OverlayController` | botón “Enviar puntaje” y estado de error |

### 2.3 Resto del backlog

| Tarea | Qué falta |
| :--- | :--- |
| `FE-04` | `Theme.cs`/ScriptableObject con los 17 colores y 5 estilos; hoy están hardcodeados en `CosTamarindoSceneBuilder` |
| `FE-05` | 10 prefabs de UI (`WoodButton`, `HandInput`, `PinSlot`, `Heart`, `DialogBubble`, `LeaderRow`…) |
| `FE-06` | aplicar el kit a HUD, pausa, game over, Libro y Login; corazones con sprite |
| `FE-08` | pantalla de leaderboard con estados vacío/error/timeout |
| `FE-09` | bocadillo del NPC (depende de `IA-03` para el contenido) |
| `FE-10` | `Login` a build index 0 y encadenado `Login → Game` |
| `FE-11` | playtest de 10–15 min y ajuste de `spawnInterval` / `fallSpeed` |
| `FE-12` | rebuild Release + medición (≤ 30 MB, < 10 s, 0 errores) |
| `FE-13` | sacar `SampleScene` del build y decidir el `preloadedAssets` vaciado |
| `FE-14` | quitar `com.coplaydev.unity-mcp` del `Packages/manifest.json` versionado |

---

## 3. Cómo se verifica el trabajo

```powershell
# tests del juego (batchmode, sin abrir el editor)
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -projectPath unity -runTests -testPlatform PlayMode `
  -testResults "$env:TEMP\playtests.xml" -logFile "$env:TEMP\playtests.log"

# build + CSP
powershell -File infra/scripts/build_webgl.ps1
docker exec sg-frontend nginx -s reload

# medición real
node tools/dev-server/check.mjs https://juego.local/
```

**Criterio de aceptación de `FE-12`:** `unityReady: true`, runtime < 10 s, transferencia
≤ 30 MB, cero errores y cero advertencias de consola.

---

## 4. Trampas que ya costaron tiempo

1. **El 8080 del host es de Unity MCP.** El HTTP plano del contenedor se publica en 8081.
2. **La CSP se lee al arrancar.** Si se regenera `csp.conf`, hay que recargar Nginx o el
   juego se queda cargando.
3. **Unity pide los `.gz` directamente.** No sirve `gzip_static`: hace falta
   `Content-Encoding: gzip` **y** el MIME original (`application/wasm`).
4. **`ItemSpawner` empareja por `sprite.name == ItemDef.Id`.** Un sprite con otro nombre no
   se usa, aunque esté importado.
5. **`spriteMode: 2` es Multiple**, no Single. Los 60 PNG nuevos quedaron así: no se cargan
   como `Sprite` con `LoadAssetAtPath<Sprite>`.
6. **`SampleScene` sigue en Build Settings** (índice 2). No molesta, pero sobra.
7. **El árbol tiene cambios sin commitear** de arte (`*.meta`) y `ProjectSettings.asset`.
   Coordinar antes de tocar, para no pisarse.

---

## 5. Qué NO tocar

- `backend/` (salvo leer `docs/api.md`).
- `infra/nginx/` sin avisar: es superficie de Security.
- `Assets/Scripts/Core/GameManager.cs` en su lógica de reglas: los valores de puntaje y
  vidas son la referencia de los techos de plausibilidad del servidor. Si cambian, avisar.
