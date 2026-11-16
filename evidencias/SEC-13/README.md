# SEC-13 — CI de seguridad en GitHub Actions — evidencia

> **Tarea:** SEC-13 — CI en GitHub Actions: Gitleaks + tests + (opcional) Sonar
> **Fecha de ejecución:** 2026-11-13
> **Estado:** **Implementado (local/config)** — el workflow está versionado; la ejecución
> remota se confirma cuando el commit llegue a GitHub.
> **Riesgo asociado:** R-01 (secreto commiteado), R-08 (dependencia vulnerable).

---

## 1. Qué se controla

Que cada push o pull request a `main` pase por tres barreras automáticas:

1. **Gitleaks:** detecta secretos en el historial y en el diff.
2. **Tests del backend:** 43 pruebas con `pytest` en la fecha de la evidencia; hoy son 53 tras F7.
3. **Auditoría de dependencias:** `pip-audit` sobre `backend/requirements.txt`.
4. **Validación de Compose:** `docker compose config --quiet` con `.env.example`.

El workflow no publica el juego ni despliega la VM; solo valida el repositorio.

---

## 2. Implementación

Archivo: [`.github/workflows/security.yml`](../../.github/workflows/security.yml).

| Job | Paso | Qué hace |
| :--- | :--- | :--- |
| `secrets` | `gitleaks/gitleaks-action@v2` | Escanea secretos con historial completo |
| `backend` | `pytest -q` | Ejecuta los tests del backend |
| `backend` | `pip-audit -r requirements.txt` | Falla si hay vulnerabilidades conocidas |
| `backend` | `docker compose config --quiet` | Valida sintaxis y variables del Compose |

Disparadores: `push` a `main`, `pull_request` y `workflow_dispatch`.

---

## 3. Verificación local

No se puede ejecutar GitHub Actions en local, pero se replican los pasos:

```powershell
# 1. Tests
cd backend
uv run --python 3.12 --with-requirements requirements-dev.txt pytest -q

# 2. Auditoría
uvx pip-audit -r backend/requirements.txt --progress-spinner off

# 3. Compose
Copy-Item .env.example .env -Force
docker compose -f infra/docker-compose.yml config --quiet
```

Resultados de esta pasada:

| Paso | Resultado |
| :--- | :--- |
| `pytest` | 43 passed en la fecha; hoy 53 tras F7 |
| `pip-audit` | `No known vulnerabilities found` |
| `docker compose config` | Sin errores (verificado antes de la pasada) |

Los artefactos de tests y auditoría están en
[`../SEC-12/03-tests.txt`](../SEC-12/03-tests.txt) y
[`../SEC-12/02-pip-audit-despues.txt`](../SEC-12/02-pip-audit-despues.txt).

---

## 4. Limitaciones

- **Gitleaks no está instalado en el host actual.** El workflow lo ejecuta en GitHub; para
  probarlo en local hace falta instalar la CLI o usar Docker.
- **SonarQube no está en el workflow.** Se dejó fuera para no romper el CI sin servidor y
  token. Cuando SEC-12 cierre SonarQube, se agrega un job opcional con `SONAR_TOKEN`.
- **El workflow no se ejecutó en GitHub todavía.** La evidencia es la configuración y la
  réplica local de los pasos.
- **El frontend Unity no se compila en CI.** Requiere licencia y runner con Unity; queda para
  F9 o para un runner dedicado.

---

## 5. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`.github/workflows/security.yml`](../../.github/workflows/security.yml) | Workflow versionado |
| [`../SEC-12/03-tests.txt`](../SEC-12/03-tests.txt) | Tests que ejecuta el job |
| [`../SEC-12/02-pip-audit-despues.txt`](../SEC-12/02-pip-audit-despues.txt) | Auditoría que ejecuta el job |

---

## 6. Checklist de cierre

- [x] Workflow con Gitleaks, tests y `pip-audit`.
- [x] Validación de Compose.
- [x] Pasos replicados en local.
- [ ] Primera ejecución verde en GitHub Actions.
- [ ] Job opcional de SonarQube cuando exista el servidor.
