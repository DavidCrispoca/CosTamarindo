#!/usr/bin/env sh
# Descarga el modelo del NPC en el volumen de Sg-AI (una sola vez).
#
#   ./pull-model.sh              -> qwen2.5:3b (decisión D4)
#   ./pull-model.sh llama3.2:3b  -> otro modelo
#
# OJO: la red `costamarindo-ai` es `internal: true`, así que sg-ai NO tiene
# salida a Internet en runtime. Eso es deliberado (A.8.20: si el NPC no puede
# salir, no hay exfiltración posible desde el modelo).
#
# Por eso la descarga se hace conectando el contenedor *temporalmente* a la red
# `bridge`, que sí tiene salida, y desconectándolo al terminar. El volumen
# conserva el modelo, así que esto se ejecuta una vez por entorno.
set -eu

cd "$(dirname "$0")/.."

MODEL="${1:-qwen2.5:3b}"
COMPOSE="docker compose -f docker-compose.yml"

$COMPOSE up -d ai

echo "Conectando sg-ai a la red 'bridge' solo para la descarga..."
docker network connect bridge sg-ai

cleanup() {
  echo "Desconectando sg-ai de 'bridge' (vuelve a quedar aislado)..."
  docker network disconnect bridge sg-ai || true
}
trap cleanup EXIT INT TERM

echo "Descargando $MODEL (~2 GB la primera vez)..."
$COMPOSE exec -T ai ollama pull "$MODEL"

echo ""
echo "Modelos disponibles:"
$COMPOSE exec -T ai ollama list
