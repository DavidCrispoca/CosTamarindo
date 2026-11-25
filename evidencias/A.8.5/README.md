# A.8.5 — Autenticación — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.5 — *Authentication information*
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** `node` (fetch + `crypto`), backend FastAPI + Supabase Auth reales
> **Estado:** ✅ implementado y verificado
> **Riesgo asociado:** R-02 (acceso no autorizado por suplantación de identidad).

---

## 1. Qué se controla

Que la identidad del jugador se autentique de forma robusta y que los intentos de
suplantación fallen de forma controlada:

- El PIN se valida en el servidor (nunca en el cliente) y se rechazan PIN débiles.
- Los nombres de usuario son únicos.
- El bloqueo progresivo (5 fallos → 60 s) resiste fuerza bruta y persiste en
  `public.login_attempts`.
- El JWT propio se firma y se verifica: un token manipulado se rechaza.

Propiedades demostradas: **autenticidad** (nadie firma un JWT ajeno) y **disponibilidad
controlada** (el bloqueo cede con `Retry-After`).

> Nota: el flujo de juego es anónimo y la identidad se resuelve en `/api/claim`. Los
> endpoints `/api/auth/*` se conservan por compatibilidad y son el camino de login del
> panel de reclamo; el bloqueo de `login_attempts` es el mismo que protege `/api/claim`.

---

## 2. Antes (línea base)

| Campo | Valor |
| :--- | :--- |
| Entorno | `sg-backend` (Docker, FastAPI) + Supabase Auth DEV |
| Endpoint | `https://juego.local/api/auth/login` |
| Estado previo | Autenticación implementada (código y 64 tests), pero **sin ataque real ejecutado** contra el backend desplegado |

---

## 3. Ataque reproducible

| Campo | Valor |
| :--- | :--- |
| Herramienta | `evidencias/ataques/iso-api-ataques.mjs` |
| Comando | `node evidencias/ataques/iso-api-ataques.mjs https://juego.local` |
| Precondiciones | `sg-backend` healthy y `.env` con las claves de Supabase/JWT |
| Resultado si el control falla | Login con PIN incorrecto sin límite; JWT manipulado aceptado |

Ataques ejecutados (salida literal en [`01-ataque.txt`](01-ataque.txt)):

1. **PIN débil** `0000` → `422 weak_pin`.
2. **Usuario duplicado** → `409 username_taken`.
3. **Fuerza bruta**: 5 intentos con PIN incorrecto → `401,401,401,401,423`; el sexto
   (PIN correcto) → `423 account_locked` con `Retry-After: 60`.
4. **JWT manipulado** (se cambia `sub` sin volver a firmar) contra `/api/auth/logout` →
   `401 invalid_token`; el token válido → `204`.

El backend registra cada evento en JSON estructurado con `request_id` (ver
[`02-audit-backend.txt`](02-audit-backend.txt)): `register_ok`, `login_ok`, `login_fail`
(con `locked` progresando a `true`), `login_blocked` y `logout`.

---

## 4. Corrección

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| `backend/app/services/auth_service.py` | Validación de PIN (`is_weak_pin`), bloqueo progresivo 5/60 s persistido en `login_attempts` | Rechaza PIN predecibles y frena fuerza bruta |
| `backend/app/security.py` | `decode_access_token` verifica firma HS256, `exp` y `env` | Un JWT alterado no valida |
| `backend/app/errors.py` | `InvalidCredentials` (401), `AccountLocked` (423), `WeakPin`, `UsernameTaken` | Códigos estables que el cliente mapea |
| `backend/app/routers/auth.py` | `/logout` exige `current_user` | Superficie protegida para demostrar la verificación del JWT |

---

## 5. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | `node evidencias/ataques/iso-api-ataques.mjs https://juego.local` |
| Resultado esperado | `weak_pin`, `username_taken`, `invalid_token`, `account_locked` + `Retry-After: 60` |
| Resultado observado | 8/8 comprobaciones OK (ver `01-ataque.txt`), exit 0 |
| Fecha/hora | 2026-11-25 |

---

## 6. Limitaciones y riesgo residual

- **PIN de 4–6 dígitos** de baja entropía: mitigado con bloqueo 5/60 s, rechazo de PIN
  débiles y derivación con `PIN_PEPPER`; `password_hibp_enabled` está desactivado a
  propósito (HIBP rechazaría PIN comunes y el control real es el bloqueo).
- **JWT estatal por TTL:** un token robado sigue siendo válido hasta su expiración
  (30 min); el logout no mantiene una denylist. Riesgo residual aceptado para el alcance.
- La evidencia cubre el flujo de compatibilidad `/api/auth/*`, no el bloqueo de
  `/api/claim` contra un PROD con tráfico real (pendiente de A.8.31/PROD).

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-ataque.txt`](01-ataque.txt) | Ataques reproducibles y respuestas reales |
| [`02-audit-backend.txt`](02-audit-backend.txt) | Registro JSON del backend con `request_id` |

---

## 8. Checklist de cierre

- [x] El ataque se ejecutó de verdad y la salida está adjunta.
- [x] La corrección está en el repositorio y es reproducible.
- [x] La verificación posterior demuestra el cambio.
- [x] No hay secretos reales en la evidencia.
- [x] La limitación y el riesgo residual están escritos.
- [x] `docs/soa.md` apunta a esta carpeta.
