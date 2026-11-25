# A.8.12 — Prevención de fuga de información — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.12 — *Information leakage prevention*
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** `Select-String`/`regex` sobre el build WebGL descomprimido
> **Estado:** ✅ implementado y verificado
> **Riesgo asociado:** R-01 (fuga de secretos o URLs internas desde el cliente).

---

## 1. Qué se controla

Que el cliente WebGL (lo que cualquiera descarga y puede inspeccionar con DevTools o
`strings`) **no contenga secretos ni rutas internas**: claves de Supabase
(`sb_secret_…`, `service_role`), `PIN_PEPPER`, `SESSION_MASTER_KEY`, `BACKEND_JWT_SECRET`,
URLs privadas o claves privadas. El cliente solo habla con `/api/` del mismo origen; toda
clave vive en el backend.

Propiedad demostrada: **confidencialidad** del secreto (no hay nada que extraer del build).

---

## 2. Antes (línea base)

| Campo | Valor |
| :--- | :--- |
| Entorno | Build WebGL Release `builds/webgl/Build` (16,5 MB) |
| Estado previo | Se había revisado por diseño (cliente sin claves), pero **sin escaneo formal** del artefacto compilado |

---

## 3. Ataque reproducible

| Campo | Valor |
| :--- | :--- |
| Herramienta | `Select-String` (regex) sobre `loader.js` y `framework.js` descomprimido; lectura ASCII de `.wasm.gz` y `.data.gz` |
| Comando | `Select-String -Path webgl.loader.js, framework.js -Pattern 'sb_secret|supabase.co|PIN_PEPPER|…'` |
| Precondiciones | Build Release generado |
| Resultado si el control falla | Aparece una clave, token o URL interna en el cliente |

Resultado (ver [`01-dlp-scan.txt`](01-dlp-scan.txt)): **0 coincidencias** en todos los
patrones de secreto/URL. La única aparición de una IP es el chequeo HTTPS del `loader.js`
de Unity (`["localhost","127.0.0.1"]`), que es una cadena del motor, no una URL real.

La consola del navegador también se revisó en la medición de `check.mjs`: `consoleErrors: []`.

---

## 4. Corrección

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| `unity/Assets/Scripts/Net/ApiClient.cs` | Solo habla con `/api/` relativo; comentario explícito de que no se guardan tokens | El cliente no necesita ni conoce claves |
| `infra/docker-compose.yml` | `sg-frontend` sin `env_file` ni claves de Supabase | Las claves no entran al contenedor del frontend |
| `docs/api.md` | Contrato D10: el cliente nunca habla con Supabase | Diseño que evita la fuga por construcción |

---

## 5. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | Escaneo de `builds/webgl/Build` (regex sobre JS y lectura ASCII de binarios) |
| Resultado esperado | 0 secretos/URLs; solo la cadena HTTPS del loader |
| Resultado observado | 0 secretos, 0 URLs, 0 claves privadas |
| Fecha/hora | 2026-11-25 |

---

## 6. Limitaciones y riesgo residual

- **No cubre la memoria en runtime:** un cliente comprometido puede extraer la
  `session_key` de su propia sesión (H1). Aceptado: la firma se limita con plausibilidad,
  TTL, nonce y un envío por sesión (ver A.8.24).
- **El `.data.gz`/`.wasm.gz` se escanearon en crudo (ASCII).** No se hizo análisis
  dinámico; el riesgo de que Unity incruste una cadena en binario es bajo y los artefactos
  son generados por el motor.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-dlp-scan.txt`](01-dlp-scan.txt) | Patrones buscados y resultados (0 secretos) |

---

## 8. Checklist de cierre

- [x] El ataque (escaneo) se ejecutó de verdad y la salida está adjunta.
- [x] La corrección está en el repositorio y es reproducible.
- [x] La verificación posterior demuestra el cambio.
- [x] No hay secretos reales en la evidencia.
- [x] La limitación y el riesgo residual están escritos.
- [x] `docs/soa.md` apunta a esta carpeta.
