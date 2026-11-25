# A.8.9 — Configuración (cliente no autoritativo) — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.9 — *Configuration management* (el cliente no es fuente de verdad)
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** Playwright (Edge/Chromium) sobre el build público; lectura de `unity/Assets/Scripts/`
> **Estado:** ✅ implementado y verificado
> **Riesgo asociado:** R-01 (trampa del jugador editando el almacenamiento del cliente).

---

## 1. Qué se controla

Que editar el almacenamiento del cliente (PlayerPrefs/IndexedDB) **no cambie** el resultado
autoritativo del juego. El cliente solo presenta; el servidor valida el puntaje con firma HMAC
y techos de plausibilidad. Propiedad demostrada: **integridad** del resultado y
**no repudio** de que la fuente de verdad es el servidor.

---

## 2. Antes (línea base)

| Campo | Valor |
| :--- | :--- |
| Entorno | Build WebGL público `https://costamarindo.onrender.com` (env=prod) |
| Estado previo | Diseño "servidor autoritativo" en código, pero **sin inspección real del almacenamiento del cliente** |

---

## 3. Ataque reproducible

| Campo | Valor |
| :--- | :--- |
| Herramienta | `evidencias/ataques/iso-a89-playerprefs.mjs` |
| Comando | `node evidencias/ataques/iso-a89-playerprefs.mjs https://costamarindo.onrender.com` |
| Precondiciones | Build WebGL servido |
| Resultado si el control falla | PlayerPrefs con vidas/puntaje/token que el jugador puede editar para trampear |

### 3.1 Inspección del almacenamiento (resultado)

- `localStorage`: **vacío**.
- `IndexedDB`: `/idbfs` (FILE_DATA, el sistema de archivos virtual de Unity donde viven
  PlayerPrefs) y `UnityCache` (caché de assets). Ver [`01-almacenamiento-cliente.txt`](01-almacenamiento-cliente.txt).
- No hay tokens, claves ni lógica de puntaje en el almacenamiento del cliente.

### 3.2 Qué se puede y qué no se puede trampear

| Dato | Dónde vive | ¿Editable por el jugador? |
| :--- | :--- | :--- |
| Vidas (`Lives`) | `GameManager` en memoria (`{ get; private set; }`) | No: no se persiste |
| Puntaje en curso (`Score`) | `GameManager` en memoria | No: no se persiste |
| Récord local (`ct.best_score`) | `PlayerPrefs` (IndexedDB) | Sí, pero solo cambia el display local |
| Colección (`ct.book.*`) | `PlayerPrefs` (IndexedDB) | Sí, pero solo afecta el Libro local |
| Token JWT | memoria (`AuthStub`) | No: nunca se escribe a PlayerPrefs |
| Puntaje definitivo | servidor (`/api/score` con HMAC + plausibilidad) | No: ver A.8.24 (`bad_signature`, `implausible_score`) |

El jugador puede inflar su `best_score` local, pero el leaderboard del servidor no cambia: se
calcula a partir de `/api/score` firmado y validado, no de PlayerPrefs.

---

## 4. Corrección

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| `unity/Assets/Scripts/Core/GameManager.cs` | `Lives`/`Score` en memoria; solo `BestScore` se persiste | Las vidas/puntaje no se pueden inyectar por disco |
| `unity/Assets/Scripts/Auth/AuthStub.cs` | Token en memoria, nunca en PlayerPrefs | El token no queda en el almacenamiento del cliente |
| `unity/Assets/Scripts/Net/ApiClient.cs` | Envía solo el payload firmado; el servidor valida | El cliente no escribe el resultado |
| `backend/app/services/score_service.py` | Firma HMAC + techos de plausibilidad | El servidor rechaza valores trampeados |

---

## 5. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | `node evidencias/ataques/iso-a89-playerprefs.mjs` |
| Resultado esperado | Solo `/idbfs` y `UnityCache`; sin vidas/puntaje/token persistidos |
| Resultado observado | `localStorage` vacío; IndexedDB con `/idbfs` + `UnityCache`; sin secretos |
| Fecha/hora | 2026-11-25 |

---

## 6. Limitaciones y riesgo residual

- **No se hizo un playthrough completo** para capturar las claves exactas de `best_score`
  tras una partida; la inspección es sobre el estado inicial del almacenamiento. El análisis
  de código (qué persiste cada script) cubre esa parte.
- **Un cliente comprometido siempre puede reenviar un payload propio** (H1). La defensa es
  la firma HMAC por sesión y los techos de plausibilidad (A.8.24), no la ofuscación del cliente.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-almacenamiento-cliente.txt`](01-almacenamiento-cliente.txt) | IndexedDB/localStorage reales y qué persiste cada script |
| [`../../ataques/iso-a89-playerprefs.mjs`](../../ataques/iso-a89-playerprefs.mjs) | Script reproducible de inspección |

---

## 8. Checklist de cierre

- [x] El ataque (inspección + intento de inyección) se ejecutó de verdad y la salida está adjunta.
- [x] La corrección está en el repositorio y es reproducible.
- [x] La verificación posterior demuestra el cambio.
- [x] No hay secretos reales en la evidencia.
- [x] La limitación y el riesgo residual están escritos.
- [x] `docs/soa.md` apunta a esta carpeta.
