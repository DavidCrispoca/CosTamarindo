# SEC-12 — Auditoría de dependencias y SonarQube — evidencia parcial

> **Tarea:** SEC-12 — SonarQube (backend + C#) con Quality Gate y `pip-audit`/`npm audit`
> **Fecha de ejecución:** 2026-11-13
> **Estado:** **Parcial** — auditoría de dependencias cerrada; SonarQube diferido.
> **Riesgo asociado:** R-08 (dependencia vulnerable o comprometida).

---

## 1. Qué se buscaba

Detectar dependencias conocidas como vulnerables y evitar que una versión insegura llegue a
la imagen del backend. El alcance real de esta pasada fue el backend Python; el frontend Unity
no tiene un `package.json` de producción y el C# se audita con el análisis de SonarQube, que
queda pendiente.

---

## 2. Antes

`pip-audit` sobre `backend/requirements.txt` encontró **38 vulnerabilidades conocidas en 3
paquetes**:

| Paquete | Versión | Problema |
| :--- | :--- | :--- |
| `PyJWT` | 2.10.1 | Múltiples avisos, incluido manejo de JWK y claves vacías |
| `pytest` | 8.3.4 | Aviso de la versión de desarrollo |
| `starlette` | 0.41.3 | Avisos de denegación de servicio y manejo de rangos |

Además, `pytest` y `pytest-asyncio` estaban en `requirements.txt`, por lo que se instalaban en
la imagen final del backend. Eso no es necesario para ejecutar la API y aumenta la superficie.

Salida literal: [`01-pip-audit-antes.txt`](01-pip-audit-antes.txt).

---

## 3. Corrección aplicada

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| `backend/requirements.txt` | `fastapi==0.162.7`, `uvicorn[standard]==0.54.0`, `pydantic==2.14.3`, `PyJWT==2.15.1`, `SQLAlchemy[asyncio]==2.1.8`, `asyncpg==0.32.0` | Eliminar avisos y mantener compatibilidad |
| `backend/requirements.txt` | Se quitaron `pytest` y `pytest-asyncio` | No deben estar en la imagen de ejecución |
| `backend/requirements-dev.txt` | `pytest==9.2.0`, `pytest-asyncio==1.5.0` | Pruebas actualizadas sin afectar runtime |
| `backend/app/errors.py` | `HTTP_422_UNPROCESSABLE_CONTENT` | Constante vigente de Starlette |

La actualización de FastAPI arrastra `starlette==1.10.0`, que ya no tiene los avisos de la
versión 0.41.3. No se cambió la lógica de negocio.

---

## 4. Verificación

### 4.1 Tests

```powershell
cd backend
uv run --python 3.12 --with-requirements requirements-dev.txt pytest -q
```

Resultado: **43 passed** en la fecha de esta evidencia. Tras F7 la suite es de **53 tests**;
la salida original se conserva en [`03-tests.txt`](03-tests.txt).

### 4.2 Auditoría después

```powershell
uvx pip-audit -r backend/requirements.txt --progress-spinner off
```

Resultado: **`No known vulnerabilities found`**. Salida en
[`02-pip-audit-despues.txt`](02-pip-audit-despues.txt).

### 4.3 Imagen

`backend/Dockerfile` instala solo `requirements.txt`; al mover `pytest` a
`requirements-dev.txt`, la imagen final deja de incluir herramientas de prueba. La imagen se
reconstruye con:

```powershell
docker compose --env-file .env -f infra/docker-compose.yml --profile backend build backend
```

---

## 5. SonarQube: por qué queda pendiente

SonarQube no se desplegó en esta pasada. Motivos:

1. Requiere un servidor adicional, volumen, token y configuración de Quality Gate.
2. En ese momento el backend todavía no arrancaba sin las claves de Supabase/JWT (ya arranca
   desde el 2026-11-25); el análisis estático puede ejecutarse, pero el valor real llega
   cuando el código de negocio esté completo.
3. El bloque independiente priorizó lo que no depende de terceros: plantilla, SoA, TLS,
   `nmap`, Wazuh y auditoría de dependencias.

**Lo que sí queda cubierto:** `pip-audit` en CI y local, Gitleaks en CI y tests del backend.
**Lo que falta para cerrar SEC-12:** levantar SonarQube, analizar backend y C#, definir
Quality Gate y adjuntar el reporte.

---

## 6. Limitaciones y riesgo residual

- La auditoría de dependencias no reemplaza un análisis estático de código (SonarQube).
- `pip-audit` consulta avisos publicados; no detecta una dependencia comprometida sin aviso.
- El frontend Unity no tiene un `npm audit` aplicable; su cadena se revisa con el build y
  `strings` (SEC-07).
- La imagen del backend debe reconstruirse para que el cambio de dependencias llegue al
  contenedor.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-pip-audit-antes.txt`](01-pip-audit-antes.txt) | 38 vulnerabilidades antes de la corrección |
| [`02-pip-audit-despues.txt`](02-pip-audit-despues.txt) | Sin vulnerabilidades conocidas después |
| [`03-tests.txt`](03-tests.txt) | 43 tests pasaban con las versiones nuevas (hoy 53 tras F7) |

---

## 8. Checklist de cierre

- [x] Auditoría de dependencias antes/después.
- [x] `pytest` fuera de la imagen de ejecución.
- [x] Tests del backend en verde.
- [x] `pip-audit` sin avisos en runtime.
- [ ] SonarQube desplegado y Quality Gate definido.
- [ ] Análisis de C# y del frontend.
