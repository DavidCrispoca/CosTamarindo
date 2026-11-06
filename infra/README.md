# infra/ — tarjeta de operación

Diseño completo de nodos, redes y responsabilidades: [`../docs/arquitectura.md`](../docs/arquitectura.md).
Backlog por división y criterios de terminado: [`../Plan.md`](../Plan.md).

> **Estado (2026-11-06):** los cinco nodos están declarados y validados en Compose.
> `Sg-Frontend`, `Sg-AI` y `Sg-Backend` corren *healthy*; `Sg-Security` vive en un perfil
> porque depende de F8. `Sg-DB` es Supabase Cloud, no un contenedor.
>
> La API ya responde en `https://juego.local/api/` (`/api/health` en 200).

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

## Nodos, perfiles y dueño

| Nodo | División | Perfil | Puerto | Por qué |
| :--- | :--- | :--- | :--- | :--- |
| `frontend` (Sg-Frontend) | Frontend | *(siempre)* | **443**, 8081 | Nginx con el build WebGL y TLS |
| `ai` (Sg-AI) | IA | *(siempre)* | 11434 **interno** | Ollama; el modelo va al volumen `costamarindo-ai-models` |
| `backend` (Sg-Backend) | Backend | `backend` | 8000 **interno** | `backend/Dockerfile`; imagen multi-stage |
| `security` (Sg-Security) | Security | `security` | interno | Wazuh consume ~3 GB; se enciende en F8 |
| — (Sg-DB) | DB | — | 443 salida | Supabase Cloud (decisión D3) |

## Redes

| Red | `internal` | Quién | Para qué |
| :--- | :--- | :--- | :--- |
| `costamarindo-dmz` | no | frontend | entrada pública |
| `costamarindo-app` | **sí** | frontend, backend | frontend → backend |
| `costamarindo-data` | no | backend | backend → Supabase (TLS 443) |
| `costamarindo-ai` | **sí** | backend, ai | backend → Ollama |
| `costamarindo-mgmt` | **sí** | frontend, backend, security | logs al SIEM |

## Scripts disponibles

| Script | Función |
| :--- | :--- |
| `up.ps1` / `up.sh` | Levanta el entorno con los perfiles pedidos (`-Prod` omite el override) |
| `down.sh` | Apaga; `--purge` borra volúmenes (no hay `down.ps1`) |
| `logs.sh` | Logs de un nodo en vivo |
| `deploy.sh` | Build → TLS → up → salud → aislamiento |
| `pull-model.sh` | Descarga el modelo con salida temporal a Internet |
| `tls.ps1` | Certificado de `juego.local` con mkcert |
| `build_webgl.ps1` / `.sh` | Build Release + regeneración de CSP |
| `backup.ps1` / `backup.sh` | `pg_dump` comprimido + SHA256 en `infra/backups/` |
| `restore-test.ps1` | Restauración verificada en un Postgres efímero |
| `keepalive.ps1` | Lectura real para que el plan Free no pause el proyecto |

## Puertos

| Puerto | Quién | Cuándo |
| :--- | :--- | :--- |
| `443` | Nginx | siempre |
| `8081` | Nginx HTTP plano | solo en dev (`docker-compose.override.yml`) |
| `8000` | Backend | **nunca publicado**: solo red `app` |
| `11434` | Ollama | **nunca publicado**: solo red `ai` |

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
| `/api/` devuelve 502 | backend caído o sin perfil: `docker compose --profile backend up -d` y `docker compose logs backend` |
| `/api/health` devuelve 503 | la base no responde: revisar `SUPABASE_DB_URL` (pooler 5432) y el estado del proyecto en Supabase |
| 403/500 en el juego por CSP | regenerar `infra/nginx/snippets/csp.conf` y recargar Nginx |
| `sg-ai` responde lento | `docker compose logs ai`; modelo descargado; RAM del contenedor |
