# CosTamarindo 2D

🌐 **Jugar ahora:** <https://costamarindo.onrender.com>

Videojuego *catcher* 2D desarrollado en **Unity 6 (WebGL)** como objeto de auditoría de
**ISO/IEC 27001:2022**. Incluye autenticación y leaderboard online con **Supabase**,
infraestructura de 5 nodos con **Docker** y diálogos de NPC generados por **Ollama** local.

En partida, **Esc** o **P** abre la pausa: allí se ajustan y silencian la música y los
efectos por separado. La preferencia se guarda para la siguiente partida.

- [`docs/handoff/`](docs/handoff/README.md) — puesta en marcha y handoff por división
- Plan de ejecución por divisiones: [`Plan.md`](Plan.md)
- Especificación ISO 27001: [`docs/CosTamarindo.md`](docs/CosTamarindo.md)
- Arquitectura de los 5 nodos Docker: [`docs/arquitectura.md`](docs/arquitectura.md)
- Operación de infraestructura: [`infra/README.md`](infra/README.md)
- Seguridad y política de secretos: [`SECURITY.md`](SECURITY.md) ·
  [`docs/seguridad/politica-secretos.md`](docs/seguridad/politica-secretos.md)
- Bitácora por fase: [`docs/fases/`](docs/fases/)

---

## Estructura

```
.
├── Plan.md              # divisiones, backlog, fases y criterios de terminado
├── docs/                # especificación ISO, arquitectura, SoA, bitácora de fases
├── unity/               # proyecto Unity 6 (Assets, Packages, ProjectSettings)
├── backend/             # API FastAPI + IA del NPC
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

El juego local queda en **`https://juego.local`** (una vez agregado `127.0.0.1 juego.local` al
archivo `hosts`).

---

## Convenciones

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) — `tipo(alcance): descripción`.
- **Ramas:** `feat/`, `fix/`, `sec/`, `docs/`; `main` solo por pull request.
- **Secretos:** solo en `.env` local; el repositorio guarda únicamente `.env.example`.
  **Prohibido subir claves o datos sensibles** (ver [`SECURITY.md`](SECURITY.md)).
- **Escenas Unity:** serialización en texto forzada; trabajo por prefabs para evitar conflictos.

---

## Licencia y uso

Proyecto académico — Universidad Militar Nueva Granada, Seguridad Informática.
Assets gráficos: ver [`assets/figma/README.md`](assets/figma/README.md).
