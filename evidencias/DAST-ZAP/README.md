# DAST — OWASP ZAP — antes/después

> **Herramienta:** OWASP ZAP (Docker `zaproxy/zap-stable:latest`)
> **Fecha:** 2026-11-25
> **Objetivo:** `https://costamarindo.onrender.com/`
> **Commit antes:** `352916f` · **Commit después:** `019b9e7`

---

## Escaneos

| Escaneo | Antes | Después |
| :--- | :--- | :--- |
| Baseline del sitio (pasivo + spider) | 0 FAIL, **8 WARN** | 0 FAIL, **5 WARN** |
| API (OpenAPI + `-O`, activo) | 0 FAIL, **4 WARN** | 0 FAIL, **1 WARN** |

---

## Correcciones aplicadas (etapa 6)

- `deploy/render/nginx.conf` → `server_tokens off;`, `~*^/$ "no-cache"`, `try_files $uri $uri/ =404;`
- `infra/nginx/snippets/security-headers.conf` → HSTS, CORP `same-origin`, COEP `credentialless`

Resultado: H-01 (HSTS), H-02 (versión nginx), H-03 (CORP) y H-04 (COEP) pasaron de WARN a **PASS**; H-06 (SPA 200) ahora responde **404** en rutas desconocidas.

Quedan como informativos/bajos: cache-control en `/`, comentarios del build (Unity) y CSP sin fallback. Detalle completo en `../hallazgos-finales.md`.

---

## Archivos

| Archivo | Contenido |
| :--- | :--- |
| `zap-baseline-antes.*` / `zap-baseline-despues.*` | baseline del sitio (html/md/xml/json) |
| `zap-api-antes.*` / `zap-api-despues.*` | escaneo activo de la API |
| `openapi.json` | OpenAPI usado para el escaneo de API |
| `zap-baseline-*.log` / `zap-api-*.log` | salida de consola |
| `zap.yaml` | plan del Automation Framework |
