# CosTamarindo 2D

Videojuego *catcher* 2D desarrollado en **Unity 6 (WebGL)** como objeto de auditoría de
**ISO/IEC 27001:2022**. Incluye autenticación y leaderboard online con **Supabase**,
infraestructura de 5 nodos con **Docker** y diálogos de NPC generados por **Ollama** local.

- Especificación ISO 27001: [`docs/CosTamarindo.md`](docs/CosTamarindo.md)
- Plan de ejecución por fases: [`Plan.md`](Plan.md)
- Arquitectura de los 5 nodos Docker: [`docs/arquitectura.md`](docs/arquitectura.md)
- Operación de infraestructura: [`infra/README.md`](infra/README.md)
- Bitácora por fase: [`docs/fases/`](docs/fases/)

---

## Estado

| Fase | Descripción | Estado | Documento |
| :--- | :--- | :--- | :--- |
| F0 | Repositorio y baseline | ✅ cerrada | [`docs/fases/F0.md`](docs/fases/F0.md) |
| F1 | Assets Figma → Unity | 🚧 personaje + fondo en juego · faltan ítems y UI | [`docs/fases/F1.md`](docs/fases/F1.md) |
| F2 | Vertical slice jugable | 🚧 jugable (sin red) | [`docs/fases/F2.md`](docs/fases/F2.md) |
| F3 | Build WebGL + Nginx | ✅ cerrada (16,1 MB, 2,9 s, 0 errores) | [`docs/fases/F3.md`](docs/fases/F3.md) |
| F4 | Supabase: Auth y leaderboard | ⬜ pendiente |
| F5 | Backend FastAPI + puntaje firmado | ⬜ pendiente |
| F6 | Infraestructura Docker (5 nodos) | 🚧 `Sg-Frontend` arriba · faltan backend, IA y seguridad |
| F7 | NPC con Ollama | ⬜ pendiente |
| F8 | Controles ISO, ataques y evidencias | ⬜ pendiente |
| F9 | Despliegue final y entrega | ⬜ pendiente |

---

## Estructura

```
.
├── Plan.md              # fases, herramientas y criterios de terminado
├── docs/                # especificación ISO, arquitectura, SoA, bitácora de fases
├── unity/               # proyecto Unity 6 (Assets, Packages, ProjectSettings)
├── backend/             # API FastAPI (sesiones, puntaje, diálogo)
├── infra/               # Docker Compose, Nginx, Wazuh, scripts
├── assets/figma/        # fuente del arte (los PNG pesados solo en unity/Assets/Art)
└── evidencias/          # una carpeta por control ISO auditado
```

---

## Requisitos

| Herramienta | Versión mínima |
| :--- | :--- |
| Unity Hub + Unity | 6000.3 LTS (módulo WebGL) |
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
docker compose -f infra/docker-compose.yml up -d --wait

# 4. Backend (F5, todavía no existe backend/)
#    cd backend && uvicorn app.main:app --reload --port 8000
```

El juego queda en **`https://juego.local`** (una vez agregado `127.0.0.1 juego.local` al
archivo `hosts`). El HTTP plano escucha en **8081** porque el 8080 del host lo ocupa Unity
MCP. La infraestructura de 5 nodos se completa en F6.

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
