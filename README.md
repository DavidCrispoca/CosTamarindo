# CosTamarindo 2D

Videojuego *catcher* 2D desarrollado en **Unity 6 (WebGL)** como objeto de auditoría de
**ISO/IEC 27001:2022**. Incluye autenticación y leaderboard online con **Supabase**,
infraestructura de 5 nodos con **Docker** y diálogos de NPC generados por **Ollama** local.

- Plan de ejecución por divisiones: [`Plan.md`](Plan.md) — **fuente de verdad del avance**
- Especificación ISO 27001: [`docs/CosTamarindo.md`](docs/CosTamarindo.md)
- Arquitectura de los 5 nodos Docker: [`docs/arquitectura.md`](docs/arquitectura.md)
- Operación de infraestructura: [`infra/README.md`](infra/README.md)
- Bitácora por fase: [`docs/fases/`](docs/fases/)

---

## Estado

El proyecto está partido en **cinco divisiones**, una por nodo. Cada una tiene su propio
backlog y criterio de terminado en [`Plan.md`](Plan.md) §4–§8.

| División | Nodo | Estado real (2026-11-06) | Documento |
| :--- | :--- | :--- | :--- |
| **Frontend** | `Sg-Frontend` | 🚧 juego jugable offline; arte de ítems sin cablear; build de 16,2 MB desactualizado | [`Plan.md` §4](Plan.md) |
| **Backend** | `Sg-Backend` | ✅ API completa y desplegada (43/43 tests) | [`Plan.md` §5](Plan.md) |
| **DB** | `Sg-DB` | ✅ Supabase DEV con RLS y backup probado | [`Plan.md` §6](Plan.md) |
| **Security** | `Sg-Security` | 🚧 1 de 10 controles: A.8.25 (Gitleaks) | [`evidencias/A.8.25/`](evidencias/A.8.25/README.md) |
| **IA** | `Sg-AI` | 🚧 nodo y modelo `qwen2.5:3b` operativos; sin endpoint ni guardrails | [`Plan.md` §8](Plan.md) |

### Qué funciona hoy

- **El juego corre** en <https://juego.local> servido por `Sg-Frontend` con TLS de mkcert,
  CSP por hash y HSTS.
- **Bucle jugable completo offline**: menú → partida → 3 vidas → game over → reintento, con
  Libro de colección, feedback de captura y SFX sintetizado. **15/15** tests PlayMode verdes.
- **API online completa** en `https://juego.local/api/`: registro y login con PIN, bloqueo
  progresivo, sesión de partida, puntaje firmado con HMAC y leaderboard con posición propia.
  **43/43** tests y cuatro ataques de puntaje rechazados con motivo.
- **Base de datos Supabase DEV** con esquema versionado, RLS sin acceso de cliente y
  restauración verificada.
- **`Sg-AI`** está aislado en la red `ai` con el modelo `qwen2.5:3b` descargado.
- **Gitleaks** implementado como control A.8.25, con hook de pre-commit probado.

### Qué falta para cumplir el objetivo

1. **Frontend**: arte de ítems cableado, UI de Figma, clientes de red y login obligatorio.
2. **IA**: prompt, guardrails y fallback del NPC.
3. **Security**: los 9 controles restantes con ataque y evidencia.
4. **Entrega**: build final, E2E de navegador, SoA, runbook y demo.

> El detalle de cada pendiente, con su código (`FE-01`, `BE-03`, `DB-02`, `SEC-04`, `IA-02`…) y
> su criterio de terminado, está en [`Plan.md`](Plan.md).

---

## Estructura

```
.
├── Plan.md              # divisiones, backlog, fases y criterios de terminado
├── docs/                # especificación ISO, arquitectura, SoA, bitácora de fases
├── unity/               # proyecto Unity 6 (Assets, Packages, ProjectSettings)
├── backend/             # API FastAPI (vacío: F5)
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
