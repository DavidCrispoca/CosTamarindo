# A.8.31 — Separación de entornos — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.31 — *Separation of development, test and production environments*
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** `node` (fetch + `crypto`), backend DEV y PROD reales
> **Estado:** ✅ implementado y verificado
> **Riesgo asociado:** R-08 (ejecutar pruebas destructivas en producción o usar credenciales DEV en PROD).

---

## 1. Qué se controla

Que un token emitido en un entorno **no sirva en otro**: un JWT de DEV no puede autenticarse
en PROD ni al revés. Se defiende por dos capas independientes:

1. **Secretos distintos** (`BACKEND_JWT_SECRET` por entorno): un token firmado con la clave de
   DEV no valida en PROD.
2. **Claim `env`** dentro del JWT: `decode_access_token` rechaza `env != expected_env` con
   `wrong_environment` (`backend/app/security.py:134`).

Propiedad demostrada: **autenticidad y aislamiento** entre entornos.

---

## 2. Antes (línea base)

| Campo | Valor |
| :--- | :--- |
| DEV | `https://juego.local` (Supabase `CosTamarindo`, ref `jukyiwsteuzwrmzlgfme`) |
| PROD | `https://costamarindo.onrender.com` (Supabase `CosTamarindo-prod`, `env=prod`) |
| Estado previo | Guard implementado y prueba unitaria `test_token_from_other_environment_is_rejected`; **sin evidencia real entre los dos backends desplegados** |

---

## 3. Ataque reproducible

| Campo | Valor |
| :--- | :--- |
| Herramienta | `evidencias/ataques/iso-a831-env.mjs` |
| Comando | `node evidencias/ataques/iso-a831-env.mjs` |
| Precondiciones | Backend DEV (juego.local) y PROD (costamarindo.onrender.com) operativos |
| Resultado si el control falla | El token DEV se acepta en PROD (200/204) |

Resultado (ver [`01-ataque.txt`](01-ataque.txt)):

| Paso | Resultado |
| :--- | :--- |
| Token DEV (`env=dev`) contra PROD `/api/auth/logout` | `401 invalid_token` |
| Token DEV contra DEV `/api/auth/logout` | `204` (línea base) |
| Token PROD contra PROD `/api/auth/logout` | `204` (descartar que PROD rechace todo) |

---

## 4. Corrección

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| `backend/app/security.py` | `create_access_token` graba el claim `env`; `decode_access_token` exige `env == expected_env` | Rechazo de tokens de otro entorno |
| `backend/app/settings.py` / `factory.py` | `env` desde configuración; `docs_url=None if is_prod` (sin docs/OpenAPI en PROD) | Aislamiento de superficie |
| `deploy/render.yaml` | Secretos `sync: false` y `ENV=prod` en el dashboard de Render | Cada entorno con sus claves |

---

## 5. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | `node evidencias/ataques/iso-a831-env.mjs` |
| Resultado esperado | `401` en PROD con token DEV; `204` en cada entorno con su propio token |
| Resultado observado | `401 invalid_token` en PROD; `204` en DEV y `204` en PROD con token propio |
| Fecha/hora | 2026-11-25 |

> El rechazo en PROD devuelve `invalid_token` (los secretos JWT difieren entre entornos) antes
> de llegar al check de `env`. La rama `wrong_environment` (mismo secreto, distinto `env`) queda
> cubierta por `backend/tests/test_security.py::test_token_from_other_environment_is_rejected`.
> Ambas capas demuestran la separación.

---

## 6. Limitaciones y riesgo residual

- **No se rotaron secretos PROD:** la evidencia prueba el aislamiento con las claves actuales.
- **El PROD real está en Render** (`costamarindo.onrender.com`), no en una infraestructura
  propia; la separación se apoya en variables del dashboard (`sync: false`), no en un VPS
  dedicado.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-ataque.txt`](01-ataque.txt) | Token DEV rechazado en PROD y aceptado en DEV/PROD propio |
| [`../../ataques/iso-a831-env.mjs`](../../ataques/iso-a831-env.mjs) | Script reproducible |

---

## 8. Checklist de cierre

- [x] El ataque se ejecutó de verdad y la salida está adjunta.
- [x] La corrección está en el repositorio y es reproducible.
- [x] La verificación posterior demuestra el cambio.
- [x] No hay secretos reales en la evidencia.
- [x] La limitación y el riesgo residual están escritos.
- [x] `docs/soa.md` apunta a esta carpeta.
