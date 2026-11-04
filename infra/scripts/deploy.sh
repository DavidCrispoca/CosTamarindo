#!/usr/bin/env sh
# Despliegue reproducible desde cero:
#   1. compila el juego (WebGL)     2. regenera la CSP
#   3. verifica el certificado TLS  4. levanta los nodos
#   5. comprueba salud y aislamiento
#
# No descarga el modelo de IA: eso se hace una vez con ./pull-model.sh.
set -eu

cd "$(dirname "$0")/.."

echo "── 1/5 Compilando WebGL ──────────────────────────────────────"
if command -v powershell >/dev/null 2>&1; then
  powershell -NoProfile -File scripts/build_webgl.ps1
else
  sh scripts/build_webgl.sh
fi

echo ""
echo "── 2/5 Certificado TLS local ─────────────────────────────────"
if [ ! -f tls/juego.local.pem ]; then
  if command -v powershell >/dev/null 2>&1; then
    powershell -NoProfile -File scripts/tls.ps1
  else
    echo "ERROR: falta tls/juego.local.pem. Ejecutar scripts/tls.ps1 (mkcert)." >&2
    exit 1
  fi
else
  echo "ya existe tls/juego.local.pem"
fi

echo ""
echo "── 3/5 Levantando nodos ──────────────────────────────────────"
docker compose -f docker-compose.yml up -d --wait

echo ""
echo "── 4/5 Salud ─────────────────────────────────────────────────"
docker compose -f docker-compose.yml ps

echo ""
echo "── 5/5 Aislamiento (puertos internos no publicados) ──────────"
for p in 8000 11434; do
  if docker compose -f docker-compose.yml port frontend "$p" >/dev/null 2>&1; then
    echo "FALLO: el puerto $p está publicado en frontend" >&2
  else
    echo "ok: puerto $p no publicado"
  fi
done

echo ""
echo "Listo: https://juego.local"
