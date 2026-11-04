#!/usr/bin/env sh
# Apaga el entorno.
#
#   ./down.sh          -> conserva volúmenes (modelos de IA, datos de Wazuh)
#   ./down.sh --purge  -> borra también los volúmenes
set -eu

cd "$(dirname "$0")/.."

PURGE=""
if [ "${1:-}" = "--purge" ]; then
  PURGE="-v"
  echo "AVISO: se borrarán los volúmenes (modelos de Ollama y datos de Wazuh)."
fi

# shellcheck disable=SC2086
docker compose -f docker-compose.yml --profile backend --profile security down $PURGE
