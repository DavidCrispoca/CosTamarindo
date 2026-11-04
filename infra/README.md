# infra/ — tarjeta de operación

Diseño completo de nodos, redes y responsabilidades: [`../docs/arquitectura.md`](../docs/arquitectura.md).

> **Estado (2026-11-03):** compose de 5 nodos declarado y validado.
> `Sg-Frontend` y `Sg-AI` arrancan por defecto; `Sg-Backend` y `Sg-Security` viven en
> perfiles porque dependen de `backend/` (F5) y de que haya logs (F8).
>
> Sin `backend/` todavía, `/api/` devuelve 502 a propósito: Nginx arranca igual.

## Arranque

```powershell
# Windows
powershell -File infra/scripts/up.ps1              # juego + IA
powershell -File infra/scripts/up.ps1 security     # + Wazuh
powershell -File infra/scripts/up.ps1 backend      # + API (requiere backend/Dockerfile)

# WSL / Git Bash
sh infra/scripts/up.sh
sh infra/scripts/up.sh security
```

O directamente con Compose (las rutas son relativas a `infra/`):

```bash
cd infra
docker compose up -d --wait
docker compose --profile security up -d
docker compose ps
docker compose logs -f frontend
docker compose down          # conserva volúmenes
docker compose down -v       # borra volúmenes
```

## Nodos y perfiles

| Nodo | Perfil | Puerto | Por qué |
| :--- | :--- | :--- | :--- |
| `frontend` (Sg-Frontend) | *(siempre)* | **443**, 8081 | Nginx con el build WebGL y TLS |
| `ai` (Sg-AI) | *(siempre)* | 11434 **interno** | Ollama; el modelo va al volumen `costamarindo-ai-models` |
| `backend` (Sg-Backend) | `backend` | 8000 **interno** | Necesita `backend/Dockerfile` (F5) |
| `security` (Sg-Security) | `security` | interno | Wazuh consume ~3 GB; se enciende en F8 |

`Sg-DB` no es un contenedor: es Supabase Cloud (decisión D3).

## Redes

| Red | `internal` | Quién | Para qué |
| :--- | :--- | :--- | :--- |
| `costamarindo-dmz` | no | frontend | entrada pública |
| `costamarindo-app` | **sí** | frontend, backend | frontend → backend |
| `costamarindo-data` | no | backend | backend → Supabase (TLS 443) |
| `costamarindo-ai` | **sí** | backend, ai | backend → Ollama |
| `costamarindo-mgmt` | **sí** | frontend, backend, security | logs al SIEM |

## Modelo del NPC

```bash
sh infra/scripts/pull-model.sh            # qwen2.5:3b (decisión D4)
sh infra/scripts/pull-model.sh llama3.2:3b
```

Una sola vez: el volumen conserva el modelo entre reinicios.

## Reglas

1. **Un solo puerto público: 443.** `expose:` para lo interno; `ports:` solo con justificación.
2. El build va montado en **solo lectura**: para actualizar el juego se reconstruye.
3. Nada de secretos en `infra/`: todo pasa por `../.env` (`env_file`).
4. `infra/tls/` no se versiona: se regenera con `tls.ps1` (mkcert).
5. La CSP del build se regenera tras cada build (`csp.conf`) y **se relee al arrancar**:
   sin `docker exec sg-frontend nginx -s reload` el hash viejo bloquea el juego.

## Orden rápido si algo falla

| Síntoma | Revisar |
| :--- | :--- |
| `failed to connect to the docker API` | Docker Desktop no está corriendo (no arranca solo) |
| Pantalla de carga infinita | `Content-Encoding`/MIME de `Build/*.gz`; consola del navegador |
| `NET::ERR_CERT_AUTHORITY_INVALID` | ejecutar `infra/scripts/tls.ps1` y reiniciar el navegador |
| `juego.local` no resuelve | agregar `127.0.0.1 juego.local` a `hosts` |
| `/api/` devuelve 502 | normal sin `backend`; con backend, ver `docker compose logs backend` |
| 403/500 en el juego por CSP | regenerar `infra/nginx/snippets/csp.conf` y recargar Nginx |
| `sg-ai` responde lento | `docker compose logs ai`; modelo descargado; RAM del contenedor |
