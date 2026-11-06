#!/usr/bin/env sh
# Respaldo logico de la base de datos (A.8.13).
#
#   sh infra/scripts/backup.sh
#
# Genera un dump comprimido en infra/backups/ y su SHA256 al lado.
# Usa la imagen oficial de Postgres para no exigir un cliente instalado.
# La contrasena va por variable de entorno del cliente Docker.
set -eu

cd "$(dirname "$0")/../.."

if [ ! -f .env ]; then
  echo "Falta .env en la raiz del repositorio" >&2
  exit 1
fi

# shellcheck disable=SC1091
. ./.env

: "${SUPABASE_DB_URL:?falta SUPABASE_DB_URL en .env}"

mkdir -p infra/backups
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
NAME="costamarindo-$STAMP.dump"

# Descompone la URL para no pasar la contrasena por argumento.
PGPASSWORD=$(printf '%s' "$SUPABASE_DB_URL" | sed -E 's|.*://[^:]+:([^@]+)@.*|\1|')
HOST=$(printf '%s' "$SUPABASE_DB_URL" | sed -E 's|.*@([^:/]+).*|\1|')
PORT=$(printf '%s' "$SUPABASE_DB_URL" | sed -E 's|.*@[^:]+:([0-9]+).*|\1|')
USERNAME=$(printf '%s' "$SUPABASE_DB_URL" | sed -E 's|.*://([^:]+):.*|\1|')
export PGPASSWORD

docker run --rm -e PGPASSWORD \
  -v "$(pwd)/infra/backups:/backup" \
  postgres:17 \
  pg_dump -h "$HOST" -p "$PORT" -U "$USERNAME" -d postgres \
    --format=custom --no-owner --schema=public --schema=private \
    --file="/backup/$NAME"

sha256sum "infra/backups/$NAME" > "infra/backups/$NAME.sha256" 2>/dev/null \
  || shasum -a 256 "infra/backups/$NAME" > "infra/backups/$NAME.sha256"

echo "Respaldo: infra/backups/$NAME"
cat "infra/backups/$NAME.sha256"
