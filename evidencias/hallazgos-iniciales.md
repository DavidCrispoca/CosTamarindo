# Hallazgos — examen base (antes de corregir)

> **Fecha:** 2026-11-25
> **Objetivo:** build final desplegado en `https://costamarindo.onrender.com/`
> **Herramientas:** Bearer CLI 2.1.3 (SAST) · OWASP ZAP (DAST)
> **Commit del código:** `352916f` (origin/main)
> **Estado:** línea base **"antes"** — pendiente de corrección (etapa 6)

---

## Resumen ejecutivo

| Severidad | Cantidad |
| :--- | :--- |
| Críticos | 0 |
| Altos | 0 |
| Medios | 1 |
| Bajos | 5 |
| Informativos | 2 |

- **SAST (Bearer):** 88 reglas, **0 fallos**. Solo reportó 3 tipos de dato (PII/Personal Data, 33 ocurrencias) como información.
- **DAST (ZAP):** 0 FAIL. El escaneo activo de la API probó SQLi, XSS, command injection, path traversal, XXE, SSRF, SSTI, etc. y **todas dieron PASS**. Los hallazgos son de **cabeceras de seguridad del despliegue público**, no de lógica de la aplicación.

---

## Tabla de hallazgos

| ID | Hallazgo | Dónde | Severidad | Detalle |
| :--- | :--- | :--- | :--- | :--- |
| H-01 | `Strict-Transport-Security` ausente | Todo el sitio y `/api/*` en Render | **Media** | El Nginx de Render no emite HSTS. El Nginx local sí lo hace; en producción falta. Riesgo de downgrade a HTTP. |
| H-02 | Versión de nginx expuesta | Respuestas del despliegue | Baja | El header `Server` revela la versión (`20020`). Falta `server_tokens off;`. |
| H-03 | `Cross-Origin-Resource-Policy` ausente | `/api/health`, `/api/session/start`, `/api/leaderboard`, `/api/dialog` | Baja | No se restringe quién puede cargar recursos de la API. |
| H-04 | `Cross-Origin-Embedder-Policy` ausente | Sitio | Baja | Faltan COEP/COOP en recursos. |
| H-05 | Cache-control revisable en estáticos | `/`, `/robots.txt`, `/sitemap.xml`, `TemplateData/*` | Baja | Contenido cacheable sin directivas explícitas. |
| H-06 | SPA devuelve 200 para rutas inexistentes | `/` (catch-all → index.html) | Baja | URLs arbitrarias responden 200 HTML en vez de 404 (ZAP `100001`). |
| H-07 | Comentarios sospechosos en HTML | `index.html` del build | Informativo | Comentarios de Unity/CSS; sin datos sensibles. |
| H-08 | CSP sin fallback en alguna directiva | `index.html` | Informativo | `style-src`/`script-src` sin fallback explícito en un caso. |

---

## Detalle por herramienta

### SAST — Bearer (backend)

- Alcance: `backend/` (37 archivos Python).
- Resultado: `SUCCESS` — **88 reglas, 0 fallos**.
- Clasificación de datos: 3 tipos (PII, Personal Data), 33 ocurrencias (informativo).
- **Limitación:** Bearer gratuito no analiza C#; el cliente Unity no está cubierto por SAST (lo cubre el DAST).

### DAST — ZAP

| Escaneo | Objetivo | Resultado |
| :--- | :--- | :--- |
| Baseline (pasivo + spider) | `https://costamarindo.onrender.com/` | 0 FAIL, **8 WARN** |
| API (activo, OpenAPI + `-O`) | 10 endpoints públicos | 0 FAIL, **4 WARN** |

- El escaneo activo inyectó payloads en `/api/auth/register`, `/api/auth/login`, `/api/claim`, `/api/score`, `/api/leaderboard`, `/api/dialog`, etc.
- `/api/leaderboard?limit=5+AND+1=1+--+` → **403 Forbidden** (rechazado; no se explotó SQLi).
- Todas las reglas de inyección (SQLi, XSS, RCE, path traversal, XXE, SSRF, SSTI, Log4Shell…) → **PASS**.

---

## Observación (no es un hallazgo de seguridad)

El artefacto servido por Render **no es byte-igual** al de `deploy/render/webgl/` en `main`:

- Servido: `webgl.data.gz` de 14,1 MB con cabecera `UnityWeb` (datos **sin comprimir**).
- Commit `352916f`: `webgl.data.gz` de 8,08 MB con cabecera `1F 8B` (gzip).

Es decir, el despliegue usa un build con compresión distinta (o compilado aparte). Para A.8.19 (`SHA256SUMS`) habrá que calcular los hashes **del artefacto realmente desplegado**, no del directorio versionado.

---

## Siguiente paso (etapa 6)

Corregir H-01 a H-06 (principalmente `deploy/render/nginx.conf`), repetir ZAP/Bearer y comparar antes/después.
