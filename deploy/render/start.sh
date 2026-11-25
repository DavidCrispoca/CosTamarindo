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
# Integridad del build (A.8.19). Se activa con VERIFY_INTEGRITY=1 una vez que
# SHA256SUMS (y su firma) esten regenerados sobre el build final.
if [ "${VERIFY_INTEGRITY:-0}" = "1" ]; then
  /verify-integrity.sh
fi

exec nginx -g 'daemon off;'
