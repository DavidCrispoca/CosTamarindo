# F7 · Respuesta local a la solicitud de Santiago

> **Fecha:** 2026-11-24 · **Commit verificado:** `a376e07` (`main`, coincidente con `origin/main`)
> **Alcance:** lo que este repositorio puede comprobar **sin credenciales y sin acceso al nodo de Santiago**.
> **Secretos:** ninguno. No se recibió, generó ni pegó ningún valor real de Supabase/JWT;
> no se versionó `.env`, token, certificado ni cadena de conexión.

---

## 1. Lo que sí se pudo responder desde este repositorio

| Ítem de la solicitud | Resultado | Evidencia |
| :--- | :--- | :--- |
| Rama de integración | `main` en `a376e07`, alineada con `origin/main`; los commits de `Yoann` ya están integrados | `git log --oneline -5`, `git status -sb` |
| Build WebGL del frontend | Release regenerado desde `main`: **17 archivos / 16,5 MB**, `Build Finished, Result: Success.` | `-logFile …/webgl-build-final.log` (fuera del repo) |
| Medición real en navegador | `unityReady: true`, **1,2 s** de carga, canvas **960×600**, `transferMB: 16,5`, `httpErrors: []`, `consoleErrors: []` durante la carga | `node tools/dev-server/check.mjs http://localhost:8080/` |
| Flujo `Jugar → Game` | Verificado con CDP: la escena cambia y anima tras pulsar «Jugar» | `14-webgl-bocadillo-translucido.png` |
| Fin de partida y textos | Se forzó `GameManager.EndMatch` por CDP; panel sin solapes y estado de red en tono neutro | `15-webgl-game-over-legible.png` |
| Tests del juego | **15/15** PlayMode en verde con el código final | `-runTests -testPlatform PlayMode`, `playtests-final.xml` (fuera del repo) |
| Seguridad del repositorio | `gitleaks` sobre 84 commits y sobre el árbol: **0 hallazgos**; el hook pre-commit volvió a escanear cada commit | `SECURITY.md`, `docs/seguridad/politica-secretos.md` |
| Bocadillo de la Palenquera | Fondo con alpha **0,55** (antes 0,94); nombre, frase e historial en franjas separadas | `Assets/Scripts/Core/Theme.cs`, `UI/NpcDialogueController.cs` |
| Texto al terminar sin backend | Antes: «No se pudo conectar con el servidor.» en rojo. Ahora: «Sin conexión: puntaje guardado en este dispositivo.» en tono neutro; los errores 4xx/5xx reales siguen mostrándose | `Assets/Scripts/UI/OverlayController.cs` |

Detalle de las franjas del panel de fin de partida, en unidades de la escena (tarjeta de
760×700, centro en 0): título `256..328`, puntaje `146..250`, récord `92..144`, desglose
`-30..90`, estado `-102..-38`, reclamo `-275..-105` y botón `-345..-285`. No se traslapan.
El desglose se limita a tres líneas más un resumen «… y N tipos más».

### 1.1 Sobre los 404 de `/api` en local

El servidor de desarrollo (`tools/dev-server/server.mjs`) **no despliega el backend**, así que
`POST /api/session/start` y `POST /api/dialog` devuelven 404 al terminar la partida. En DevTools
aparecen dos errores de red esperados; ya no se muestran como error rojo dentro del juego:
la partida termina en modo offline y el récord queda en `PlayerPrefs`. Para eliminar esos 404
hay que publicar el backend real y el proxy `/api/` en el nodo de Santiago.

---

## 2. Lo que sigue dependiendo de Santiago

No se puede simular ni inventar. Sin cambios en este repositorio:

- [ ] Variables de Supabase/JWT cargadas por canal seguro en el nodo (§1.1).
- [ ] Confirmación de proyecto DEV, región y host del pooler.
- [ ] `001`…`004_anonymous_claim.sql` aplicadas y `check_db.py` en verde (§1.2).
- [ ] Imagen `costamarindo-backend:latest` reconstruida y desplegada (§1.3).
- [ ] Acceso o procedimiento de publicación en el nodo Frontend (§2.1–§2.3).
- [ ] Validación real de leaderboard, claim y diálogo en la URL definitiva (§2.4).
- [ ] Aceptación del contrato anónimo y del PIN de 4–6 dígitos (§3).

---

## 3. Contrato confirmado por código (pendiente de aceptación de Santiago)

| Punto | Estado en `main` |
| :--- | :--- |
| Flujo anónimo | ✅ se juega sin login; nombre + PIN solo al reclamar top 5 |
| `/api/session/start` y `/api/score` | ✅ sin token de jugador |
| `/api/dialog` | ✅ token opcional (`AuthStub.IsBackendToken`) |
| `/api/claim` | ✅ nombre + PIN de 4–6 dígitos |
| Escena `Login` | ✅ queda por compatibilidad, encadenada a `Game` |
| Ramas | ✅ `Yoann` al día con `main`; `main` es la rama de integración |

---

## 4. Cómo reproducir la medición

```powershell
node tools/dev-server/server.mjs 8080
node tools/dev-server/check.mjs http://localhost:8080/

& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -projectPath unity -runTests -testPlatform PlayMode `
  -testResults "$env:TEMP\playtests.xml" -logFile "$env:TEMP\playtests.log"

& "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath unity `
  -executeMethod CosTamarindo.EditorTools.BuildScript.BuildWebGL `
  -logFile "$env:TEMP\webgl-build.log"
```

> Nginx/TLS no se pudo verificar en esta pasada: Docker estaba apagado. La verificación fue
> con el servidor de desarrollo, que reproduce `Content-Encoding: gzip`, `application/wasm` y
> la CSP por hash del contenedor.
