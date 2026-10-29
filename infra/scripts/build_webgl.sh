#!/usr/bin/env bash
# Build WebGL reproducible (WSL / Git Bash / Linux con Unity instalado).
# Uso:  UNITY_BIN="/ruta/a/Unity" bash infra/scripts/build_webgl.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/c/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe}"
LOG="${TMPDIR:-/tmp}/webgl-build.log"

if [ ! -x "$UNITY_BIN" ] && [ ! -f "$UNITY_BIN" ]; then
  echo "Unity no encontrado en: $UNITY_BIN (define UNITY_BIN)" >&2
  exit 1
fi

"$UNITY_BIN" -batchmode -quit \
  -projectPath "$ROOT/unity" \
  -buildTarget WebGL \
  -executeMethod CosTamarindo.EditorTools.BuildScript.BuildWebGL \
  -logFile "$LOG"

# La CSP depende del hash del <script> inline que genera Unity: se recalcula.
node "$ROOT/tools/dev-server/csp.mjs" \
  "$ROOT/builds/webgl/index.html" \
  "$ROOT/infra/nginx/snippets/csp.conf"

echo "Build OK -> $ROOT/builds/webgl  (log: $LOG)"
