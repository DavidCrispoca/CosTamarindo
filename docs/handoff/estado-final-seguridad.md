# Estado final de seguridad — handoff para Santiago

> **Fecha:** 2026-11-25
> **Autor del handoff:** Yoann
> **Objetivo:** dejar cerrado lo que se podía cerrar y documentar lo que falta para no bloquear la entrega.

---

## 1. Qué quedó hecho en esta sesión

| Ítem | Estado |
| :--- | :--- |
| Examen base Bearer + ZAP (build final en Render) | ✅ hecho (`evidencias/SAST-Bearer/`, `evidencias/DAST-ZAP/`) |
| Documentación de hallazgos | ✅ `evidencias/hallazgos-iniciales.md` |
| Corrección de hallazgos (etapa 6) | ✅ commit `019b9e7` |
| Re-escaneo antes/después | ✅ `evidencias/hallazgos-finales.md` |
| HSTS, CORP, COEP, `server_tokens off`, 404 en rutas | ✅ desplegado y verificado en vivo |
| Verificación de integridad en el despliegue (script) | ✅ `deploy/render/verify-integrity.sh` + cableado en Dockerfile/start.sh |

Resultado: **0 críticos, 0 altos, 0 medios, 0 bajos** pendientes de seguridad en la app; solo quedan informativos (cache-control en `/`, comentarios del build, CSP sin fallback).

---

## 2. Lo que queda para Santiago (pasos 8–9 y cierres restantes)

### A.8.19 — SHA256SUMS (requiere tu clave GPG)

**Hallazgo importante:** el `SHA256SUMS` actual está **desactualizado**. No coincide ni con
`deploy/render/webgl/Build/` ni con el build servido en Render, porque se generó antes de los
últimos cambios del build. El `.asc` sí está firmado, pero sobre esa versión vieja.

Pasos para dejarlo bien:

1. Recompilar el WebGL final (cuando estén todas las funcionalidades).
2. Copiar el build a `deploy/render/webgl/`.
3. Regenerar:
   ```bash
   cd deploy/render/webgl/Build
   sha256sum webgl.data.gz webgl.framework.js.gz webgl.loader.js webgl.wasm.gz > ../../../../evidencias/A.8.19/SHA256SUMS
   ```
4. Re-firmar con tu keyring:
   ```bash
   gpg --armor --detach-sign evidencias/A.8.19/SHA256SUMS
   ```
5. Activar la verificación automática: en Render, setear `VERIFY_INTEGRITY=1`.
   El `start.sh` ya corre `/verify-integrity.sh` cuando esa variable vale `1`.

> La verificación ya está implementada (`deploy/render/verify-integrity.sh`): compara
> `/webgl/Build` contra `SHA256SUMS` con `sha256sum -c` y valida la firma GPG con
> `pubkey.asc`. Solo falta regenerar + re-firmar y encender la variable.

### A.8.24 — cifrado en reposo y gestión de claves (decisión documentada)

- **Cifrado en reposo:** los datos del juego (puntajes, perfiles, sesiones) viven en
  Supabase Cloud, que cifra en reposo (TDE) → **fuera de alcance** (lo gestiona el
  proveedor). El build, los logs y los backups no contienen datos personales ni secretos
  en este alcance académico → **riesgo residual aceptado** (R-05/R-06 ya mitigados con
  TLS + HMAC).
- **Gestión de claves:** los secretos (`SUPABASE_*`, `BACKEND_JWT_SECRET`, `PIN_PEPPER`,
  `SESSION_MASTER_KEY`) viven en `.env` (gitignored) y en el vault; el certificado local en
  `infra/tls/` (gitignored); la clave GPG de firma es local al firmante (pública versionada).
  Rotación/revocación = regenerar en el dashboard de Supabase/Render, actualizar
  `.env`/vault y re-desplegar.

Con esto el checklist de A.8.24 queda cerrado (salvo "repetir en equipo limpio", opcional).

### A.8.15 — FIM end-to-end (limitación conocida)

Wazuh 4.14 all-in-one **guarda** el estado FIM de `/fim/webgl` pero **no emite la alerta
por archivo**. Ya está documentado en `evidencias/A.8.15/README.md` con el riesgo residual
R-04 aceptado y compensado con alertas de logs (`backend.log` real escribiéndose).

### PROD — backup inicial (requiere el secreto de PROD)

Queda pendiente un `pg_dump` del proyecto **`CosTamarindo-prod`** (el DEV ya tuvo backup).
Se hace con la `SUPABASE_DB_URL` de PROD (no está en este equipo). Comando de referencia:

```bash
pg_dump "$SUPABASE_DB_URL" --no-owner --no-privileges -f infra/backups/costamarindo-prod-$(date +%Y%m%dT%H%M%S).dump
```

### SEC-13 — CI (primera ejecución remota)

El workflow `.github/workflows/security.yml` (Gitleaks + pytest + pip-audit + validar Compose)
corre en cada push a `main`. Queda **confirmar en GitHub** que la última ejecución salió en
verde (no hay `gh` CLI en este equipo para comprobarlo desde acá).

---

## 3. Estado de los 10 controles

| Control | Estado |
| :--- | :--- |
| A.8.5 fuerza bruta + JWT | ✅ cerrado (`evidencias/A.8.5/`) |
| A.8.9 PlayerPrefs/IndexedDB | ✅ cerrado (`evidencias/A.8.9/`) |
| A.8.12 consola + strings | ✅ cerrado (`evidencias/A.8.12/`) |
| A.8.15 Wazuh + FIM | ⚠️ parcial (FIM end-to-end: limitación Wazuh 4.14 documentada) |
| A.8.19 SHA256SUMS + GPG | ⚠️ SHA256SUMS generado+firmado pero **desactualizado**; verificación en deploy implementada |
| A.8.20 aislamiento red | ✅ cerrado (`evidencias/A.8.20/`) |
| A.8.24 criptografía | ✅ TLS + HMAC + cabeceras; decisión de cifrado en reposo/claves documentada |
| A.8.28 SQLi/XSS/injection | ✅ cerrado (`evidencias/A.8.28/` + escaneo activo ZAP en verde) |
| A.8.31 DEV vs PROD | ✅ cerrado (`evidencias/A.8.31/`) |
| A.8.25 SDLC + Gitleaks | ✅ cerrado (`evidencias/A.8.25/` + CI) |
