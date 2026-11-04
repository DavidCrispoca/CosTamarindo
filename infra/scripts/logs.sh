#!/usr/bin/env sh
# Logs de un nodo (por defecto frontend), en vivo.
#
#   ./logs.sh            -> sg-frontend
#   ./logs.sh ai         -> sg-ai
#   ./logs.sh backend    -> sg-backend
set -eu

cd "$(dirname "$0")/.."

SERVICE="${1:-frontend}"

exec docker compose -f docker-compose.yml logs -f --tail=100 "$SERVICE"
