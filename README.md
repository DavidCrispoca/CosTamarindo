# CosTamarindo 2D

Videojuego *catcher* 2D desarrollado en **Unity 6 (WebGL)** como objeto de auditoría de
**ISO/IEC 27001:2022**. Incluye autenticación y leaderboard online con **Supabase**,
infraestructura de 5 nodos con **Docker** y diálogos de NPC generados por **Ollama** local.

- [`docs/handoff/`](docs/handoff/README.md) — puesta en marcha y handoff por división
- Plan de ejecución por divisiones: [`Plan.md`](Plan.md) — **fuente de verdad del avance**
- Especificación ISO 27001: [`docs/CosTamarindo.md`](docs/CosTamarindo.md)
- Arquitectura de los 5 nodos Docker: [`docs/arquitectura.md`](docs/arquitectura.md)
- Operación de infraestructura: [`infra/README.md`](infra/README.md)
- Bitácora por fase: [`docs/fases/`](docs/fases/)

---

## Estado

El proyecto está partido en **cinco divisiones**, una por nodo. Cada una tiene su propio
backlog y criterio de terminado en [`Plan.md`](Plan.md) §4–§8.

| División | Nodo | Estado real (2026-11-23) | Documento |
| :--- | :--- | :--- | :--- |
| **Frontend** | `Sg-Frontend` | 🟡 Nginx/TLS *healthy*; build de `main` generado; arte versionado; falta publicarlo en el nodo de Santiago | [`Plan.md` §4](Plan.md) |
| **Backend** | `Sg-Backend` | 🚧 API y 64/64 tests; imagen `main-test` construida; `latest` pendiente; no arranca sin claves | [`Plan.md` §5](Plan.md) |
| **DB** | `Sg-DB` | 🟡 Supabase DEV con RLS y backup; falta aplicar `004_anonymous_claim.sql` | [`Plan.md` §6](Plan.md) |
| **Security** | `Sg-Security` | 🟡 A.8.25 completo; A.8.15/A.8.20/A.8.24 parciales; SoA de 93 controles | [`evidencias/`](evidencias/) |
| **IA** | `Sg-AI` | ✅ Ollama healthy, `qwen2.5:3b`, endpoint y pensamiento probados; simulación E2E con Ollama real | [`Plan.md` §8](Plan.md) |

### Qué funciona hoy

- **Nginx / `Sg-Frontend`** sirve en <https://juego.local> con TLS de mkcert, CSP por hash
  y HSTS; el build WebGL ya está generado y la raíz responde `200`.
- **Bucle jugable offline** implementado: menú → partida → 3 vidas → game over → reintento,
  con Libro de colección, feedback de captura y SFX sintetizado. **15/15** tests PlayMode
  verdes (última corrida 2026-11-03).
- **API y tests**: 64/64 tests del backend; la imagen `main-test` se construyó desde `main`, pero
  `costamarindo-backend:latest` sigue pendiente y el contenedor no arranca hasta cargar las claves
  de Supabase/JWT y aplicar `004_anonymous_claim.sql`.
- **Base de datos Supabase DEV** con esquema versionado, RLS sin acceso de cliente y
  restauración verificada.
- **`Sg-AI`**: Ollama healthy, `qwen2.5:3b` (1,9 GB), sin puerto publicado, prompt versionado,
  guardrails y fallback; `POST /api/dialog` ya genera texto real. Evidencias en
  [`evidencias/F7/`](evidencias/F7/).
- **`Sg-Security`**: VM Wazuh con agente **002** activo, `100250` con 36 alertas y
  evidencias parciales de A.8.15/A.8.20/A.8.24; SoA de 93 controles en [`docs/soa.md`](docs/soa.md).
- **Gitleaks y CI**: A.8.25 con hook probado; workflow de Gitleaks + tests + `pip-audit` en
  [`.github/workflows/security.yml`](.github/workflows/security.yml).

### Qué falta para cumplir el objetivo

1. **Frontend**: publicar el build de `main` en el nodo de Santiago y validar NPC + panel de
   reclamo; completar UI de Figma y clientes de red.
2. **Backend/DB**: cargar las claves de Supabase/JWT, aplicar `004_anonymous_claim.sql` y validar
   el arranque completo, el claim y RLS.
3. **IA**: validar con el backend real; el nodo, el prompt, el build WebGL, la captura del
   pensamiento anclado al sprite, el endpoint y la compilación C# ya están listos.
4. **Security**: cerrar A.8.15/A.8.20/A.8.24 y los 6 controles restantes con ataque y evidencia.
5. **Entrega**: build final, E2E de navegador, informe, runbook y demo.

> El detalle de cada pendiente, con su código (`FE-01`, `BE-03`, `DB-02`, `SEC-04`, `IA-02`…) y
> su criterio de terminado, está en [`Plan.md`](Plan.md).

### Empezar a trabajar

Cada división tiene su documento de traspaso en [`docs/handoff/`](docs/handoff/README.md):
puesta en marcha, contratos, recetas de ataque, punto de integración de la IA y qué no tocar.

---

## Estructura

```
.
├── Plan.md              # divisiones, backlog, fases y criterios de terminado
├── docs/                # especificación ISO, arquitectura, SoA, bitácora de fases
├── unity/               # proyecto Unity 6 (Assets, Packages, ProjectSettings)
├── backend/             # API FastAPI + IA del NPC (F5, F7)
├── infra/               # Docker Compose, Nginx, Wazuh, scripts
├── assets/figma/        # fuente del arte (los PNG pesados solo en unity/Assets/Art)
└── evidencias/          # una carpeta por control ISO auditado
```

---

## Requisitos

| Herramienta | Versión mínima |
| :--- | :--- |
| Unity Hub + Unity | 6000.3.27f1 (Unity 6.3 LTS) con módulo WebGL |
| Python | 3.12 |
| Docker Desktop (WSL2) | última estable |
| Ollama | última estable |
| Node.js | 20+ |
| Git | 2.40+ |

---

## Arranque rápido

```bash
# 1. Variables de entorno (nunca commitear el .env real)
cp .env.example .env

# 2. Abrir el proyecto Unity
#    Unity Hub → Add project → unity/

# 3. Compilar el juego y servirlo con Nginx en Docker
powershell -File infra/scripts/build_webgl.ps1
powershell -File infra/scripts/tls.ps1            # una vez: certificado de juego.local
powershell -File infra/scripts/up.ps1 backend     # frontend + IA + API

# 4. Backend fuera de Docker (opcional, para iterar)
#    cd backend && .venv\Scripts\python -m uvicorn app.main:app --reload --port 8000
```

El juego queda en **`https://juego.local`** (una vez agregado `127.0.0.1 juego.local` al
archivo `hosts`). El HTTP plano escucha en **8081** porque el 8080 del host lo ocupa Unity MCP.

---

## Convenciones

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) — `tipo(alcance): descripción`.
- **Ramas:** `feat/`, `fix/`, `sec/`, `docs/`; `main` solo por pull request.
- **Secretos:** solo en `.env` local; el repositorio guarda únicamente `.env.example`.
- **Escenas Unity:** serialización en texto forzada; trabajo por prefabs para evitar conflictos.

---

## Licencia y uso

Proyecto académico — Universidad Militar Nueva Granada, Seguridad Informática.
Assets gráficos: ver [`assets/figma/README.md`](assets/figma/README.md).
