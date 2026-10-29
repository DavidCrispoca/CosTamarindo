# infra/ — tarjeta de operación

Diseño completo de nodos, redes y responsabilidades: [`../docs/arquitectura.md`](../docs/arquitectura.md).

> **Estado:** configuración de Nginx lista; el `docker-compose.yml` llega en **F6**.
> Hoy Docker Desktop no está instalado en la máquina de desarrollo.

## Comandos

```bash
# build del juego (necesario antes de levantar frontend)
powershell -File infra/scripts/build_webgl.ps1     # Windows
bash infra/scripts/build_webgl.sh                  # WSL / Git Bash

# certificado local (una vez por máquina)
bash infra/scripts/tls.sh

# entorno
cd infra
docker compose up -d --build
docker compose ps
docker compose logs -f backend
docker compose down          # conserva volúmenes
docker compose down -v       # borra volúmenes (modelos de Ollama, wazuh)

# con SIEM (F8)
docker compose --profile security up -d
```

## Reglas

1. **Un solo puerto público: 443.** `expose:` para lo interno, `ports:` solo con justificación.
2. El build va montado en **solo lectura**: para actualizar el juego se reconstruye, no se copia
   dentro del contenedor.
3. Nada de secretos en `infra/`: todo pasa por `../.env` (`env_file`).
4. `infra/certs/` no se versiona: se regenera con `tls.sh`.
5. La CSP del build se regenera tras cada build (`csp.conf`). Si se edita a mano, se pierde.

## Orden rápido si algo falla

| Síntoma | Revisar |
| :--- | :--- |
| Pantalla de carga infinita | `Content-Encoding`/MIME de `Build/*.gz`; consola del navegador |
| `NET::ERR_CERT_AUTHORITY_INVALID` | `mkcert -install` y reiniciar el navegador |
| `/api/` devuelve 502 | `docker compose logs backend`; `depends_on` y healthcheck |
| 403/500 en el juego por CSP | regenerar `infra/nginx/snippets/csp.conf` |
| `sg-ai` responde lento | `docker compose logs ai`; modelo descargado; RAM del contenedor |
