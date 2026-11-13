<#
.SYNOPSIS
    Muestra cabeceras HTTP y versión TLS para la evidencia A.8.24.

.DESCRIPTION
    Usa curl.exe de Windows. `--ssl-no-revoke` evita el error de revocación de Schannel
    con la CA local de mkcert. No usa -k: si el certificado no es confiable, el comando
    debe fallar y eso también es evidencia.

.EXAMPLE
    powershell -File evidencias/ataques/tls-headers.ps1 -Url https://juego.local/healthz

.EXAMPLE
    powershell -File evidencias/ataques/tls-headers.ps1 -Url https://juego.local/api/health -OutFile evidencias/A.8.24/01-headers.txt
#>
[CmdletBinding()]
param(
    [string]$Url = 'https://juego.local/healthz',
    [string]$OutFile
)

$ErrorActionPreference = 'Continue'

$curl = Get-Command curl.exe -ErrorAction SilentlyContinue
if (-not $curl) {
    throw 'curl.exe no encontrado. En Windows 10/11 viene con el sistema.'
}

$headers = & $curl.Source --ssl-no-revoke -s -D - -o NUL $Url 2>&1
$status = $LASTEXITCODE

$output = @(
    "# TLS/headers - evidencia A.8.24"
    "# fecha: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK')"
    "# comando: curl --ssl-no-revoke -D - -o NUL $Url"
    "# exit_code: $status"
    ''
) + $headers

$output | ForEach-Object { $_ }

if ($OutFile) {
    $output | Set-Content -Path $OutFile -Encoding utf8
    Write-Host "Salida guardada en $OutFile"
}
