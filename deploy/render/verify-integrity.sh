#!/bin/sh
# Verificación de integridad y autenticidad del build WebGL (A.8.19).
#
# Se invoca desde start.sh cuando VERIFY_INTEGRITY=1. Compara el webroot contra
# el SHA256SUMS firmado y, si hay clave pública y gnupg, valida la firma GPG.
# Aborta (exit != 0) ante cualquier discrepancia para no servir un build alterado.
#
# Prerrequisito: SHA256SUMS (y SHA256SUMS.asc + pubkey.asc) vigentes sobre el
# build desplegado. Hay que regenerarlos y re-firmarlos tras cada release.
set -e

WEBROOT="${WEBROOT:-/webgl}"
INTEGRITY_DIR="${INTEGRITY_DIR:-/integrity}"
BUILD_DIR="${WEBROOT}/Build"

echo "[integrity] Verificando integridad del build WebGL..."

# 1) Integridad: cada archivo de Build/ debe coincidir con su suma SHA-256.
cd "${BUILD_DIR}"
sha256sum -c "${INTEGRITY_DIR}/SHA256SUMS"

# 2) Autenticidad: la firma GPG separada debe ser válida con la clave pública.
if [ -f "${INTEGRITY_DIR}/pubkey.asc" ] && command -v gpg >/dev/null 2>&1; then
    echo "[integrity] Verificando firma GPG..."
    GNUPGHOME="$(mktemp -d)"
    export GNUPGHOME
    gpg --batch --import "${INTEGRITY_DIR}/pubkey.asc" >/dev/null 2>&1
    gpg --batch --verify "${INTEGRITY_DIR}/SHA256SUMS.asc" "${INTEGRITY_DIR}/SHA256SUMS"
    rm -rf "${GNUPGHOME}"
else
    echo "[integrity] AVISO: sin pubkey.asc o sin gnupg; se omitió la firma GPG."
fi

echo "[integrity] OK"
