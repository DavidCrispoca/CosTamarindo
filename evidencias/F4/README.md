# F4 · Supabase: esquema, RLS y backups — evidencia

> **División:** DB · **Nodo:** `Sg-DB`
> **Fecha:** 2026-11-06
> **Estado:** ✅ esquema, RLS, leaderboard, constraints y backup/restauración verificados
> **Proyecto:** `CosTamarindo` · ref `jukyiwsteuzwrmzlgfme` · `us-east-1` · Postgres 17.11 · `ACTIVE_HEALTHY`

---

## 1. Punto de partida

El proyecto se creó vacío: `public` sin tablas, vistas ni funciones, cero usuarios en
`auth.users`, cero políticas RLS y cero buckets. Verificado con `information_schema` y
`pg_class` antes de aplicar nada.

---

## 2. Esquema aplicado

Archivos versionados en `backend/sql/`, aplicados con `backend/scripts/migrate.py` (no a
mano desde el panel). El historial vive en `private.schema_migrations`, fuera de la
superficie de datos.

| Migración | Contenido |
| :--- | :--- |
| `001_schema.sql` | `profiles`, `game_sessions`, `scores`, `login_attempts`, vista `leaderboard`, función `get_leaderboard` |
| `002_rls.sql` | RLS en las cuatro tablas, revocación de privilegios, sin políticas (deny by default) |
| `003_harden_surface.sql` | `REVOKE USAGE ON SCHEMA public FROM PUBLIC`; registro de migraciones a `private` |

```
$ python backend/scripts/migrate.py --status
001_schema.sql           aplicada 2026-11-06 23:04
002_rls.sql              aplicada 2026-11-06 23:04
003_harden_surface.sql   aplicada 2026-11-06 23:04
```

### 2.1 Tablas

| Tabla | Para qué | Nota de diseño |
| :--- | :--- | :--- |
| `profiles` | alias visible en el leaderboard | único por `lower(username)`; formato `^[A-Za-z0-9_]{3,16}$` |
| `game_sessions` | nonce, emisión y consumo de la partida | **no guarda la clave HMAC**: se deriva de `SESSION_MASTER_KEY` |
| `scores` | puntajes aceptados | `session_id` es `UNIQUE`: una sesión, un puntaje |
| `login_attempts` | bloqueo progresivo de login | persiste entre reinicios (A.8.5) |

### 2.2 Constraints verificados

```
game_sessions: game_sessions_expiry, game_sessions_nonce_format, game_sessions_user_id_fkey
login_attempts: login_attempts_failed_nonneg, login_attempts_username_len
profiles: profiles_id_fkey, profiles_username_format
scores: scores_catches_range, scores_duration_range, scores_session_id_fkey,
        scores_session_unique, scores_user_id_fkey, scores_value_range
```

---

## 3. RLS y superficie de datos (A.8.28, A.8.5)

Postura: **no hay acceso de cliente**. No existe ninguna política porque no hay lectura
ni escritura legítima de `anon` ni de `authenticated`; el leaderboard sale por la API.

```
$ python backend/scripts/check_db.py
== Tablas de public ==
  game_sessions      kind=r rls=True
  login_attempts     kind=r rls=True
  profiles           kind=r rls=True
  scores             kind=r rls=True
  leaderboard        kind=v
== Politicas RLS ==
  ninguna (deny by default: es lo esperado)
== Privilegios de anon/authenticated ==
  sin privilegios de tabla (correcto)
== USAGE sobre el schema public ==
  anon: usage=False
  authenticated: usage=False
  service_role: usage=True
== Funcion del leaderboard ==
  get_leaderboard(p_user_id uuid, p_limit integer)
Todo en orden.
```

Prueba de extremo a extremo del mismo control, con la clave publicable:

```
GET https://<ref>.supabase.co/rest/v1/profiles?select=username&limit=1
apikey: sb_publishable_...
-> HTTP 401 (sin cuerpo: PostgREST no puede ni enumerar el schema)
```

### 3.1 Security Advisor

```
$ GET /v1/projects/<ref>/advisors/security
lints: 5
5x rls_enabled_no_policy
  INFO | private.schema_migrations  | RLS habilitado sin políticas
  INFO | public.game_sessions       | RLS habilitado sin políticas
  INFO | public.login_attempts      | RLS habilitado sin políticas
  INFO | public.profiles            | RLS habilitado sin políticas
  INFO | public.scores              | RLS habilitado sin políticas
```

Son cinco avisos **informativos** y describen exactamente el diseño: RLS activo sin
políticas significa denegar por defecto. No hay hallazgos de severidad `ERROR` ni `WARN`.

---

## 4. Leaderboard (DB-04)

Una vista no puede resolver «mi posición» porque no conoce al llamante. Se implementó
`public.get_leaderboard(p_user_id, p_limit)`, que devuelve el top N y, si se pasa el
usuario, también su fila con la posición real.

```sql
select * from public.get_leaderboard(null, 5);
-- 3 filas, is_me = false en todas
```

`backend/tests/test_leaderboard.py` cubre el caso de seis jugadores: top 5 correcto y el
sexto jugador devuelto aparte con `rank_position = 6` y `is_me = true`.

---

## 5. Backup y restauración (DB-06)

### 5.1 Respaldo

```
$ powershell -File infra/scripts/backup.ps1
Respaldo: infra/backups/costamarindo-20261106T232300Z.dump
f5d029b99ee242d55c9a22e4c45dc7fb792146d725a2dde85a0f9bd3eb33281e  costamarindo-20261106T232300Z.dump
```

`--format=custom`, `--no-owner`, esquemas `public` y `private`. La contraseña viaja por
variable de entorno del cliente Docker, no por la línea de comandos.

### 5.2 Restauración en una instancia limpia

`infra/scripts/restore-test.ps1` levanta un `postgres:17` efímero, crea los roles de
Supabase y el stub de `auth.users` que el dump referencia, restaura y verifica:

```
1. Levantando Postgres efimero...
2. Creando roles y stub de auth.users...
3. Restaurando...
4. Verificando...
tablas=4
funciones=1
migraciones=3
rls=4
  OK   tablas de public = 4
  OK   get_leaderboard = 1
  OK   tablas con RLS = 4
Restauracion verificada.
```

El contenedor se elimina al terminar: la prueba no deja estado.

---

## 6. Keep-alive (DB-07)

```
$ powershell -File infra/scripts/keepalive.ps1
keepalive ok 2026-11-06T23:24:40Z filas=0
```

Hace una lectura real por PostgREST con el rol `service_role`. Supabase pausa los
proyectos Free tras ~7 días de baja actividad, así que se programa cada 12 h:

```
schtasks /Create /SC DAILY /MO 1 /TN "CosTamarindo keepalive" /TR ^
  "powershell -NoProfile -File C:\ruta\CosTamarindo\infra\scripts\keepalive.ps1"
```

---

## 7. Separación de entornos (DB-08)

- Solo existe el proyecto **DEV**. La cuenta Free ya tiene dos proyectos (el máximo), así
  que PROD se crea al desplegar y no antes.
- `.env.example` documenta los nombres sin valores; `.env` está en `.gitignore`.
- El backend se niega a arrancar con `ENV=prod` si `SUPABASE_PROJECT_REF` está en
  `DEV_PROJECT_REFS` (`Settings.assert_environment`).
- El JWT emitido lleva el claim `env` y el backend rechaza tokens de otro entorno
  (`decode_access_token`), con prueba en `backend/tests/test_security.py`.

Pendiente al desplegar: crear el proyecto PROD, sus claves y su `pg_dump` inicial.

---

## 8. Artefactos

| Ruta | Qué es |
| :--- | :--- |
| `backend/sql/001_schema.sql` | esquema, constraints, vista y función |
| `backend/sql/002_rls.sql` | RLS y revocaciones |
| `backend/sql/003_harden_surface.sql` | cierre de `USAGE` y registro privado |
| `backend/scripts/migrate.py` | aplicador idempotente con checksum |
| `backend/scripts/check_db.py` | verificación de RLS, privilegios y función |
| `infra/scripts/backup.ps1` / `.sh` | respaldo con hash |
| `infra/scripts/restore-test.ps1` | restauración verificada |
| `infra/scripts/keepalive.ps1` | keep-alive del plan Free |
