# backend/ · API de CosTamarindo

> **División:** Backend · **Nodo:** `Sg-Backend`
> **Contratos:** [`../docs/api.md`](../docs/api.md) · **Evidencia:** [`../evidencias/F5/`](../evidencias/F5/README.md)
> **Estado:** ✅ desplegado y verificado (43 pruebas, E2E con 4 ataques)

Servidor autoritativo del puntaje. Es el **único** componente que habla con Supabase y, en
F7, el único que hablará con Ollama.

---

## 1. Arranque local

Requiere Python 3.12 y el `.env` de la raíz (ver `../.env.example`).

```powershell
python -m venv backend/.venv
backend\.venv\Scripts\python.exe -m pip install -r backend\requirements-dev.txt

# desde backend/
$env:PYTHONPATH = (Resolve-Path .).Path
.\.venv\Scripts\python.exe -m uvicorn app.main:app --reload --port 8000
```

El `.env` se lee de la raíz del repositorio (`app/config.py`), no de `backend/`.

La documentación interactiva queda en <http://127.0.0.1:8000/api/docs> y solo en `dev`.

---

## 2. Pruebas

```powershell
cd backend
.\.venv\Scripts\python.exe -m pytest
```

43 pruebas herméticas: no tocan red ni base. Sustituyen el repositorio y el proveedor de
identidad con dobles (`tests/conftest.py`).

Verificación real (necesita `.env` y el backend arriba):

```powershell
$env:CT_E2E_BASE = "http://127.0.0.1:8000"   # o https://juego.local
backend\.venv\Scripts\python.exe backend\scripts\e2e_check.py
```

---

## 3. Base de datos

```powershell
# estado de migraciones
backend\.venv\Scripts\python.exe backend\scripts\migrate.py --status

# aplicar pendientes (idempotente, con checksum)
backend\.venv\Scripts\python.exe backend\scripts\migrate.py

# verificar RLS, privilegios, funcion y constraints
backend\.venv\Scripts\python.exe backend\scripts\check_db.py
```

- Los `.sql` viven en `sql/` y se aplican **en orden**. No editar una migración ya
  aplicada: agregar una nueva.
- El historial está en `private.schema_migrations`.
- Conexión: `SUPABASE_DB_URL` (pooler en **modo sesión**, 5432). El modo transacción (6543)
  no soporta sentencias preparadas y produjo fallos intermitentes.

---

## 4. Estructura

```
backend/
├── app/
│   ├── config.py            # settings Pydantic + guard de entorno (A.8.31)
│   ├── security.py          # derivacion del PIN, HMAC de sesion, JWT
│   ├── repositories.py      # todo el SQL, parametrizado
│   ├── providers/
│   │   └── supabase_auth.py # GoTrue (admin + password grant + refresh)
│   ├── services/
│   │   ├── auth_service.py  # registro, login, refresh, bloqueo
│   │   ├── score_service.py # sesion, firma, plausibilidad
│   │   └── dialog_service.py# contrato del NPC, saneo y respaldo  <-- IA conecta aqui
│   └── routers/             # un router por recurso
├── sql/                     # migraciones (DB)
├── scripts/                 # migrate, check_db, e2e_check
├── tests/
├── Dockerfile               # multi-stage, UID 10001
└── requirements*.txt
```

---

## 5. Reglas que no se rompen

1. **El cliente nunca escribe en la base.** Todo pasa por esta API con `service_role`.
2. **Nada de secretos en el cliente.** Si un valor tiene que llegar a Unity, es público por
   definición: `session_key` de una partida y el JWT de esa sesión, nada más.
3. **Un envío por sesión.** Lo garantizan `UPDATE … WHERE used_at IS NULL` y
   `scores.session_id UNIQUE`.
4. **Errores sin detalle interno.** El código es estable (`bad_signature`, `session_used`…);
   el detalle va al log con `X-Request-ID`.
5. **Logs JSON a stdout.** Nada de `print`. Eventos en `docs/api.md` §7.
6. **El NPC no afecta el puntaje** (`IA-07`). No agregar llamadas a IA en el camino de score.

---

## 6. Estado de las tareas

`Plan.md` §5 tiene el detalle. Cerradas: `BE-01`…`BE-05` y `BE-07`…`BE-11`.
`BE-06` está implementada salvo la llamada a Ollama, que pertenece a `IA-02`…`IA-04`.
