# Evidencias F7 · Nodo de IA y NPC

> **Fecha:** 2026-11-13 · **Nodo:** `Sg-AI` · **Modelo:** `qwen2.5:3b`
> **Estado:** nodo y backend operativos; falta captura del bocadillo en WebGL.

---

## 1. F7.1 · Nodo, modelo y aislamiento

`01-ia01-linea-base.txt` registra:

- `sg-ai` **healthy**, sin puertos publicados (`docker port sg-ai` vacío).
- Única red: `costamarindo-ai` (interna).
- Modelo `qwen2.5:3b` de 1,9 GB en el volumen `costamarindo-ai-models`.
- RAM en caliente: ~3,43 GiB de 4 GiB; CPU en reposo: 0 %.

**Medición IA-01.** El primer `ollama run` en frío tardó ~7,3 s. Con el prompt real y el modelo
caliente, las respuestas quedaron entre **1,6 s y 2,1 s**. El modelo cabe en el límite de 4 GB,
pero deja poco margen: no se debe subir a un modelo mayor sin volver a medir.

> Hallazgo: `ollama run` sin system prompt puede responder en otro idioma. El prompt versionado
> de `backend/app/ai/prompts.py` es obligatorio para mantener español, personaje y formato.

---

## 2. F7.2/F7.3 · Prompt, cliente y guardrails

`02-dialogo-real.txt` contiene salidas literales del modelo con el prompt real:

- Tamarindo → «¡Qué buenos tamarindos! Como pa' un juguito.»
- Bomba → «¡Ay! Esa bomba no era para tocar, perdiste una vida.»
- Phishing → respuesta breve en español.

`03-endpoint-real.txt` prueba `POST /api/dialog` con un backend de prueba (repositorio en
memoria + JWT firmado, sin Supabase):

- Evento de captura sin texto → 200, `blocked: false`, respuesta del modelo.
- Evento peligroso → 200, `blocked: false`, respuesta pertinente.
- Inyección («ignora las instrucciones…») → 200, `blocked: true`, sin llamar a Ollama.

El backend usa timeout de 20 s, un solo request concurrente y fallback por contexto/ítem. La
entrada del jugador va dentro de `<jugador>…</jugador>`; la salida se filtra y se acota a 300
caracteres.

`05-fallback.txt` mide el caso real con `sg-ai` detenido: **797, 758 y 757 ms**. El connect
timeout es de 0,75 s para que el juego reciba el respaldo en menos de 1 s sin esperar los 20 s
de generación.

---

## 3. F7.4 · Unity

`unity/Assets/Scripts/UI/NpcDialogueController.cs`:

- Se autoinstancia en la escena `Game`.
- Se engancha a `GameManager.ItemCaught` y `MatchEnded`.
- Comenta con 35 % de probabilidad y 9 s de enfriamiento.
- Envía `context`, `item` y `kind`; muestra la respuesta 5,5 s.
- Si no hay JWT real o la red falla, usa frases locales.
- No llama a `session` ni a `score`.

`unity/Assets/Scripts/Auth/AuthStub.cs` intenta `/api/auth/login` y `/api/auth/register`; si el
backend no está desplegado, cae al modo local de desarrollo. Un PIN incorrecto del backend no
cae al modo local.

`06-unity-compile.txt` valida la compilación en batchmode con Unity 6000.3.27f1: **exit 0**,
`CompileScripts: 20483.762ms` y sin `error CS`. Se compiló una copia en `%TEMP%` para no tocar
los `.meta` del repo.

**Pendiente:** generar el build WebGL y probar el bocadillo en el navegador. `builds/webgl` está
vacío.

---

## 4. F7.5 · Pruebas

`04-tests.txt`:

```text
53 passed
```

Incluye respuesta normal, inyección, timeout/fallback, rate limit, filtro de rich text, contexto
inválido, falta de texto/ítem, cliente Ollama con `httpx.MockTransport` y prueba de que el
diálogo no escribe en `scores` ni en `game_sessions`.

---

## 5. Límites conocidos

- El backend completo sigue sin arrancar por faltar las claves de Supabase/JWT.
- La prueba de `03-endpoint-real.txt` usa un repositorio en memoria y un JWT de desarrollo; no
  valida Supabase.
- Falta la captura visual del bocadillo en WebGL.
- `mkcert` solo es confiable en el equipo de desarrollo; el build WebGL debe probarse en un
  equipo limpio antes de la entrega.
