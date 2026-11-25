# F7 · E2E real contra backend + Supabase (sin secretos)

> **Fecha:** 2026-11-25 · **Commit base:** `e2de1e5`
> **Base usada:** `CT_E2E_BASE=http://127.0.0.1:8081` (Nginx de dev)
> **Script:** [`backend/scripts/e2e_check.py`](../../backend/scripts/e2e_check.py)
> **Resultado:** `EXIT=0`, «E2E en verde.»
> **Secretos:** ninguno. `session_key` y `nonce` se redactan; no se imprimen tokens ni claves.

## 1. Objetivo

Validar el contrato completo (flujo anónimo + reclamo top-5 + ataques) contra el **backend
real y Supabase DEV**, no contra el repositorio en memoria de la simulación histórica (F7/10).

## 2. Pasos y resultados

| # | Paso | Resultado real |
| :--- | :--- | :--- |
| 1 | health | `200`, `{'status': 'ok', 'env': 'dev', 'database': True}` |
| 2 | registro (compatibilidad) | `201` |
| 3 | login y refresh | `200` y rotación correcta del token |
| 4 | bloqueo tras 5 fallos | `[401, 401, 401, 401, 423]`; `account_locked` |
| 5 | sesión y puntaje anónimos | sesión creada; `score=720` aceptado con `in_top5: True` |
| 6 | reclamo top-5 | `ok: True`, `rank_position: 1` |
| 7 | ataques | `session_used`, `bad_signature` e `implausible_score` rechazados |
| 8 | replay exacto | primer envío aceptado; el segundo devuelve `session_used` |
| 9 | leaderboard | `200`; solo la entrada reclamada; sin `is_me` para el jugador anónimo |
| 10 | límites de entrada | inyección devuelta con `blocked: True`; texto largo rechazado con `422` |
| 11 | limpieza | usuario de prueba eliminado al final |

## 3. Extracto de la salida (redactado)

```text
5. sesion y puntaje anonimos
  OK   sesion anonima creada {'session_id': '<uuid>', 'nonce': '<redacted>',
       'session_key': '<redacted>', 'expires_in': 900, 'best_score': 0}
  OK   puntaje anonimo aceptado {'ok': True, 'score': 720, 'best_score': 0,
       'rank_position': None, 'in_top5': True}
6. reclamo top-5
  OK   reclamo aceptado {'ok': True, 'username': '<e2e>', 'best_score': 720, 'rank_position': 1}
7. ataques
  OK   replay de sesion usada -> session_used
  OK   score alterado -> bad_signature
  OK   puntaje implausible -> implausible_score
8. replay exacto
  OK   primer envio aceptado
  OK   replay -> session_used
9. leaderboard
  OK   leaderboard 200
  OK   reclamo visible en leaderboard
  OK   leaderboard anonimo sin is_me
10. limites de entrada
  OK   inyeccion bloqueada {'blocked': True}
  OK   texto largo rechazado 422
11. limpieza
  usuario de prueba eliminado

E2E en verde.
```

## 4. Qué demuestra

- El flujo **anónimo** (jugar sin login) y el **reclamo con nombre + PIN** funcionan contra
  Supabase DEV, no solo en memoria.
- El backend rechaza los cuatro ataques de puntaje con motivo registrado.
- El leaderboard solo muestra puntajes reclamados y no marca `is_me` para un jugador anónimo.
- La limpieza elimina el usuario de prueba; no quedan credenciales ni tokens en la salida.

## 5. Límites

- Se ejecutó contra `https://juego.local` en este equipo (mkcert + `hosts`), no contra una URL pública.
- Deja un puntaje anónimo de prueba en `scores` (sin perfil); es dato de DEV y no contiene datos personales.
