#!/usr/bin/env sh
# Levanta el entorno de 5 nodos (F6).
#
#   ./up.sh              -> juego + IA
#   ./up.sh security     -> + Wazuh
#   ./up.sh backend      -> + API (requiere backend/Dockerfile)
#
# Siempre desde infra/: las rutas del compose son relativas a este directorio.
set -eu

cd "$(dirname "$0")/.."

PROFILES=""
for p in "$@"; do
  PROFILES="$PROFILES --profile $p"
done

# shellcheck disable=SC2086
docker compose -f docker-compose.yml $PROFILES up -d --wait

echo ""
echo "Estado:"
docker compose -f docker-compose.yml ps
echo ""
echo "Juego: https://juego.local   (requiere '127.0.0.1 juego.local' en hosts)"
