#!/bin/sh
# Arranque para Render: uvicorn (API) en segundo plano + nginx en primer plano.
set -e

PORT="${PORT:-10000}"

# Sustituye el puerto en la config de nginx.
sed -i "s/__PORT__/${PORT}/g" /etc/nginx/nginx.conf

# API en segundo plano. Ollama no existe en Render; /api/dialog usa el banco
# de respaldo automáticamente, así que el juego funciona igual.
uvicorn app.main:app \
  --host 127.0.0.1 --port 8000 \
  --proxy-headers --forwarded-allow-ips '*' --no-server-header &

# Nginx en primer plano (Render le manda el tráfico al puerto $PORT).
exec nginx -g 'daemon off;'
