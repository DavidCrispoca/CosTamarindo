# Hallazgos — después de corregir (antes/después)

> **Fecha:** 2026-11-25
> **Objetivo:** `https://costamarindo.onrender.com/`
> **Commit de la corrección:** `019b9e7` (`fix(deploy): cabeceras HSTS/CORP/COEP, server_tokens off y 404…`)
> **Estado:** etapa 6 — correcciones aplicadas y re-verificadas

---

## Resumen antes/después

| Herramienta | Antes | Después |
| :--- | :--- | :--- |
| SAST — Bearer (backend) | 0 fallos (88 reglas) | **0 fallos** (sin cambios: no se tocó Python) |
| DAST — ZAP baseline (sitio) | 0 FAIL, **8 WARN** | 0 FAIL, **5 WARN** |
| DAST — ZAP API (activa) | 0 FAIL, **4 WARN** | 0 FAIL, **1 WARN** |

---

## Qué se corrigió (hallazgo → resultado)

| ID | Hallazgo | Antes | Después | Cambio |
| :--- | :--- | :--- | :--- | :--- |
| H-01 | `Strict-Transport-Security` ausente | WARN `10035` (sitio + API) | **PASS** | `add_header Strict-Transport-Security` en `security-headers.conf` |
| H-02 | Versión de nginx expuesta | WARN `20020` | **PASS** | `server_tokens off;` en `deploy/render/nginx.conf` |
| H-03 | `Cross-Origin-Resource-Policy` ausente | WARN `90004` (API) | **PASS** | `add_header Cross-Origin-Resource-Policy "same-origin"` |
| H-04 | `Cross-Origin-Embedder-Policy` ausente | WARN `90004` (sitio) | **PASS** | `add_header Cross-Origin-Embedder-Policy "credentialless"` |
| H-05 | Cache-control revisable | WARN `10015` x3 | **x1** (solo `/`) | `~*^/$ "no-cache"` + SPA ya no devuelve 200 |
| H-06 | SPA devolvía 200 en rutas inexistentes | `200` en `/robots.txt`, `/sitemap.xml`, rutas al azar | **404** | `try_files $uri $uri/ =404;` |

---

## Qué queda (aceptado / informativo)

| ID | Regla | Severidad | Justificación |
| :--- | :--- | :--- | :--- |
| H-05 residual | `10015` en `/` | Baja | `index.html` usa `no-cache` (correcto); ZAP sugiere `no-store`. Aceptado. |
| H-06 residual | `100001` Unexpected Content-Type (18) | Informativa | Son respuestas **correctas** 404/301/403/421 a peticiones malformadas del fuzzer, no una vulnerabilidad. |
| H-07 | `10027` Suspicious Comments | Informativa | Comentarios del build de Unity/CSS, sin datos sensibles. |
| H-08 | `10055` CSP sin fallback | Informativa | El CSP se regenera con el build; no se toca a mano. |

Las reglas de **inyección** (SQLi, XSS, RCE, path traversal, XXE, SSRF, SSTI, Log4Shell…) siguieron en **PASS** en ambos escaneos.

---

## Archivos de evidencia

- `evidencias/SAST-Bearer/` → `bearer-antes.*` y `bearer-despues.*`
- `evidencias/DAST-ZAP/` → `zap-baseline-antes.*` / `zap-baseline-despues.*` y `zap-api-antes.*` / `zap-api-despues.*`
- `evidencias/hallazgos-iniciales.md` (antes) y este archivo (después)
