#!/usr/bin/env sh
# Levanta el entorno de 5 nodos (F6).
#
#   ./up.sh              -> juego + IA (dev, con 8081)
#   ./up.sh security     -> + Wazuh
#   ./up.sh backend      -> + API (requiere backend/Dockerfile)
#   ENV=prod ./up.sh     -> sin override: solo se publica 443
#
# Siempre desde infra/: las rutas del compose son relativas a este directorio.
set -eu

cd "$(dirname "$0")/.."

PROFILES=""
for p in "$@"; do
  PROFILES="$PROFILES --profile $p"
done

FILES="-f docker-compose.yml"
if [ "${ENV:-dev}" != "prod" ] && [ -f docker-compose.override.yml ]; then
  FILES="$FILES -f docker-compose.override.yml"
fi

# shellcheck disable=SC2086
docker compose $FILES $PROFILES up -d --wait

echo ""
echo "Estado:"
docker compose $FILES ps
echo ""
echo "Juego: https://juego.local   (requiere '127.0.0.1 juego.local' en hosts)"
