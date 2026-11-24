# Handoff — Alejandra (Frontend/Unity) · integrar el NPC y publicar el build WebGL

> **Actualizado 2026-11-23:** `main` ya incluye el NPC y el panel de reclamo en `Game.unity`.
> El flujo vigente es anónimo y `POST /api/dialog` acepta token **opcional**; el NPC funciona sin
> JWT. Este documento sirve como contexto histórico de integración.
>
> **Para el agente de Alejandra.** Contexto operativo para integrar F7 en la rama de Frontend.
> No borrar arte ni tocar escenas/prefabs sin necesidad: el bocadillo se construye en runtime.
>
> **Rama de referencia:** `Yoann` (último commit; incluye los fixes de simulación).
> **Nodo IA:** `sg-ai` healthy con `qwen2.5:3b`; el backend real depende de Santiago.
> **Estado F7:** cerrado en simulación WebGL; falta integrar/validar en la rama de Frontend.

---

## 0. Qué se integró y qué cambió

El commit original `434bdaa` tocaba solo tres archivos de Unity:

```text
unity/Assets/Scripts/Auth/AuthStub.cs
unity/Assets/Scripts/UI/NpcDialogueController.cs
unity/Assets/Scripts/UI/NpcDialogueController.cs.meta
```

La simulación agregó correcciones en `NpcDialogueController.cs`:

- Bootstrap también en `SceneManager.sceneLoaded`: si `Game` se carga después de `Login`, el NPC
  sí se crea.
- `game_over` envía `text: "La partida terminó"` porque el contrato exige `text` o `item`.
- `RequestLineWhenFree` espera si hay una petición en vuelo, para no perder el comentario final.
- El bocadillo se ancla al `SpriteRenderer` del jugador: se dibuja encima de la cabeza, con tres
  puntos de pensamiento, sigue al personaje y se limita a los bordes de la pantalla.
- En `game_over` el cuadro se coloca arriba del panel final, para no tapar el resumen ni el botón.
- El puntaje no cambia.

Qué hace el NPC:

- Se engancha a `GameManager.ItemCaught` y `GameManager.MatchEnded`.
- Comenta con 35 % de probabilidad al atrapar un ítem y con 9 s de enfriamiento.
- **Siempre** pide una frase al terminar la partida (`game_over`).
- Envía `context`, `item` y `kind` a `https://juego.local/api/dialog`.
- Si no hay JWT real o la red falla, muestra frases locales.
- No llama a `session` ni a `score`; el puntaje no se toca.

`AuthStub` intenta primero `/api/auth/login` y `/api/auth/register`; solo cae al modo local si el
backend no responde por red. Un PIN incorrecto del backend **no** cae al modo local.

---

## 1. Integrar los cambios en la rama de Frontend

### Opción recomendada: cherry-pick del último commit de `Yoann`

```powershell
git fetch origin
git checkout <tu-rama-de-frontend>
git cherry-pick <último-commit-de-Yoann>
```

Si hay conflictos en `AuthStub.cs`, **no** reemplazar a ciegas: conservar la lógica local que ya
tenga la rama y agregar la parte “backend-first” de `Yoann`.

Si no hay cambios locales en esos archivos, una alternativa es traer solo esos paths:

```powershell
git checkout origin/Yoann -- unity/Assets/Scripts/Auth/AuthStub.cs
git checkout origin/Yoann -- unity/Assets/Scripts/UI/NpcDialogueController.cs
git checkout origin/Yoann -- unity/Assets/Scripts/UI/NpcDialogueController.cs.meta
```

Reglas:

- **No tocar** los `.meta` de arte que están sin commitear.
- **No editar** `Game.unity` para agregar el NPC: el bootstrap lo hace solo.
- Mantener `ApiBaseUrl = "https://juego.local/api"` y el token solo en memoria.
- Si la rama de Frontend tiene otro flujo de login, probar ambos caminos: backend disponible y
  backend caído.

---

## 2. Compilar y generar el build WebGL

### 2.1 Compilación rápida (opcional)

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath unity `
  -logFile "$env:TEMP\compile-npc.log"

Select-String -Path "$env:TEMP\compile-npc.log" -Pattern "error CS|Compilation failed|Scripts have compiler errors"
```

Criterio: exit 0 y ninguna línea de error.

### 2.2 Build WebGL

La forma oficial:

```powershell
powershell -File infra/scripts/build_webgl.ps1
```

El script:

- genera `builds/webgl` (excluido de git);
- recalcula `infra/nginx/snippets/csp.conf` con el hash del `<script>` inline de Unity.

Si se construye en una copia temporal para no tocar `.meta`/`ProjectSettings.asset`, copiar
después el artefacto a `builds/webgl` y recalcular la CSP:

```powershell
node tools/dev-server/csp.mjs builds\webgl\index.html infra\nginx\snippets\csp.conf
```

### 2.3 Publicar en Nginx

```powershell
docker exec sg-frontend nginx -t
docker exec sg-frontend nginx -s reload
Invoke-WebRequest "https://juego.local/" -UseBasicParsing | Select-Object StatusCode
```

Esperado: `200`, no `403`. Si sigue `403`, `builds/webgl/index.html` no existe o Nginx no ve el
volumen. Si el juego queda cargando, revisar que la CSP se haya regenerado y Nginx recargado.

---

## 3. Validar el bocadillo

### Build oficial actual

El build oficial arranca en `Game` (FE-10 pendiente), así que **no pasa por Login**. Sin JWT
real, el NPC usa el fallback local; sirve para validar la UI, no la IA real.

### Prueba con JWT real

Para validar Ollama desde Unity hay dos opciones:

1. Generar un build temporal con `Login` como escena inicial, iniciar sesión y luego restaurar el
   build oficial.
2. Implementar FE-10 (`Login` obligatorio → `Game`) en la rama de Frontend y probar allí.

Pasos:

1. Abrir `https://juego.local/`.
2. Registrarse o iniciar sesión (con backend real o temporal).
3. Jugar hasta que termine la partida. El `game_over` siempre dispara el bocadillo.
4. También probar una captura de ítem; con 35 % de probabilidad y 9 s de enfriamiento puede
   tardar varias capturas.
5. Verificar:
   - el pensamiento “La Palenquera” aparece **encima de la cabeza**, con tres puntos que bajan
     hacia el sprite, sin tapar al personaje;
   - el cuadro sigue al jugador si se mueve y no se sale de la pantalla;
   - el texto se muestra 5,5 s y se desvanece;
   - en `game_over` el cuadro queda arriba del panel, sin tapar el resumen;
   - el puntaje y las vidas no cambian por el bocadillo;
   - en DevTools → Network, `POST /api/dialog` responde 200 con `reply` si hay JWT real;
   - en DevTools → Console, no aparecen errores nuevos del NPC.

Si no hay JWT real, no habrá request a `/api/dialog`: eso es correcto, se usa el fallback local.
Para distinguir la IA real del fallback, revisar el log `dialog_ok` del backend con `latency_ms`
y el `context`/`item` de la llamada.

**Nota de la simulación:** en el build temporal con `Login` primero apareció un
`ArgumentOutOfRangeException` en la consola durante la transición. No se reprodujo en el build
oficial que arranca en `Game`. Revisarlo si FE-10 se implementa; puede ser un problema previo de
la transición de escena, no del NPC.

---

## 4. Evidencia que debe devolver

Guardar en `evidencias/F7/`:

- `13-webgl-bocadillo-alejandra.png`: captura del pensamiento visible en `https://juego.local/`.
- `14-webgl-alejandra.md`:
  - fecha, navegador y equipo;
  - rama y commit usados;
  - si se usó backend real o fallback local;
  - pasos para reproducir;
  - confirmación de que el puntaje no cambió;
  - errores de consola (idealmente ninguno).

No commitear `builds/` (está ignorado) ni los `.meta` de arte si no son parte de la rama.

---

## 5. Criterios de aceptación

- [ ] Los archivos de Unity están integrados y compilan sin `error CS`.
- [ ] `builds/webgl/index.html` existe y `https://juego.local/` responde 200.
- [ ] La CSP se regeneró y Nginx se recargó.
- [ ] El bocadillo aparece en `Game` sin editar la escena.
- [ ] El bocadillo no bloquea el gameplay y no altera puntaje/vidas.
- [ ] Con backend real, el texto viene de Ollama; sin backend, se ve el fallback local.
- [ ] `game_over` muestra el comentario final.
- [ ] Hay captura y evidencia en `evidencias/F7/`.

---

## 6. Qué NO tocar

- `backend/` (solo leer `docs/api.md` si hace falta).
- `infra/nginx/` sin avisar: es superficie de Security.
- La lógica de puntaje/vidas de `GameManager`: los techos de plausibilidad del servidor dependen
  de esos valores. Si cambian, avisar a Backend.
- Los 60 `.meta` de arte que están sin commitear.
- `Game.unity` y los prefabs: el NPC se construye en runtime a propósito.

---

## 7. Problemas esperables

| Síntoma | Causa probable | Acción |
| :--- | :--- | :--- |
| `https://juego.local/` 403 | `builds/webgl` vacío | generar build y copiar artefacto |
| Juego en carga infinita | CSP vieja | regenerar `csp.conf` y `nginx -s reload` |
| No aparece el bocadillo | escena no se llama `Game` o no se cargó el script | revisar `sceneLoaded` y consola |
| Aparece texto local siempre | no hay JWT real o `/api/dialog` falla | revisar login y Network |
| `dialog` 401 | token ausente/expirado | volver a login; no reutilizar tokens viejos |
| `dialog` 422 en `game_over` | cliente viejo con `text` vacío | integrar el fix de `NpcDialogueController` |
| `dialog` 429 | rate limit 10/min | esperar o usar otro usuario |
| `ArgumentOutOfRangeException` al pasar de Login a Game | posible bug previo de la transición | revisar si FE-10 lo reproduce; no es del NPC |

Si el build se hace desde una copia temporal, recordar que el artefacto final debe corresponder
a la rama de Frontend integrada, no a una copia con cambios locales.
