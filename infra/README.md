# infra/ — tarjeta de operación

Diseño completo de nodos, redes y responsabilidades: [`../docs/arquitectura.md`](../docs/arquitectura.md).

> **Estado:** `Sg-Frontend` corriendo con el build WebGL por HTTPS (`nginx:1.29-alpine`).
> Docker Desktop 4.97 instalado. Faltan `backend`, `ai` y `security` (F6b).
>
> Sin `backend/` todavía, `/api/` devuelve 502 a propósito: Nginx arranca igual.

## Comandos

```bash
# build del juego (necesario antes de levantar el frontend)
powershell -File infra/scripts/build_webgl.ps1     # Windows
bash infra/scripts/build_webgl.sh                  # WSL / Git Bash

# certificado local (una vez por máquina; confía la CA sin admin)
powershell -File infra/scripts/tls.ps1

# entorno
docker compose -f infra/docker-compose.yml up -d --wait
docker compose -f infra/docker-compose.yml ps
docker compose -f infra/docker-compose.yml logs -f frontend
docker compose -f infra/docker-compose.yml down       # conserva volúmenes
docker compose -f infra/docker-compose.yml down -v    # borra volúmenes

# desde infra/ los comandos son más cortos
cd infra && docker compose up -d

# tras recompilar el juego: Nginx lee la CSP al arrancar, hay que recargarlo
docker exec sg-frontend nginx -s reload
```

El HTTPS escucha en **443**; el HTTP plano en **8081** porque el 8080 del host lo ocupa
Unity MCP (`mcp-for-unity`). El contenedor sigue escuchando en 8080 por dentro.

## Reglas

1. **Un solo puerto público: 443.** `expose:` para lo interno, `ports:` solo con justificación.
2. El build va montado en **solo lectura**: para actualizar el juego se reconstruye, no se copia
   dentro del contenedor.
3. Nada de secretos en `infra/`: todo pasa por `../.env` (`env_file`).
4. `infra/tls/` no se versiona: se regenera con `tls.ps1` (mkcert).
5. La CSP del build se regenera tras cada build (`csp.conf`). Si se edita a mano, se pierde.

## Orden rápido si algo falla

| Síntoma | Revisar |
| :--- | :--- |
| Pantalla de carga infinita | `Content-Encoding`/MIME de `Build/*.gz`; consola del navegador |
| `NET::ERR_CERT_AUTHORITY_INVALID` | ejecutar `infra/scripts/tls.ps1` y reiniciar el navegador |
| `juego.local` no resuelve | agregar `127.0.0.1 juego.local` a `hosts` (admin; suele estar en solo lectura) |
| `/api/` devuelve 502 | `docker compose logs backend`; `depends_on` y healthcheck |
| 403/500 en el juego por CSP | regenerar `infra/nginx/snippets/csp.conf` |
| `sg-ai` responde lento | `docker compose logs ai`; modelo descargado; RAM del contenedor |
