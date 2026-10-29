<#
.SYNOPSIS
    Genera el certificado TLS local del juego con mkcert.

.DESCRIPTION
    Crea `infra/tls/<dominio>.pem` y su clave, válidos para el dominio y para
    localhost. Confía la CA de mkcert en el almacén del usuario actual, así que
    no hace falta ejecutar nada como administrador.

    El único paso que sí pide admin es agregar el dominio al archivo `hosts`.

.EXAMPLE
    powershell -File infra/scripts/tls.ps1
#>
[CmdletBinding()]
param(
    [string]$Domain = 'juego.local'
)

$ErrorActionPreference = 'Stop'

$root   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tlsDir = Join-Path $root 'infra\tls'

function Resolve-Mkcert {
    $cmd = Get-Command mkcert -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $winget = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
    $exe = Get-ChildItem $winget -Filter 'mkcert.exe' -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($exe) { return $exe.FullName }

    throw 'mkcert no encontrado. Instálalo con: winget install FiloSottile.mkcert'
}

$mkcert = Resolve-Mkcert
New-Item -ItemType Directory -Force -Path $tlsDir | Out-Null

& $mkcert `
    -cert-file (Join-Path $tlsDir "$Domain.pem") `
    -key-file  (Join-Path $tlsDir "$Domain-key.pem") `
    $Domain localhost 127.0.0.1 ::1
if ($LASTEXITCODE -ne 0) { throw "mkcert falló con código $LASTEXITCODE" }

# La CA se confía por usuario: Chrome/Edge leen ese almacén y no hace falta UAC.
$caroot = (& $mkcert -CAROOT).Trim()
$trusted = certutil -user -store Root 2>$null | Select-String -SimpleMatch 'mkcert'
if (-not $trusted) {
    certutil -user -addstore -f Root (Join-Path $caroot 'rootCA.pem') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo confiar la CA de mkcert en el almacén del usuario' }
    Write-Host 'CA de mkcert agregada al almacén del usuario.'
}

Write-Host "Certificado listo: $tlsDir\$Domain.pem"
Write-Host ''
Write-Host 'Último paso (una sola vez, requiere admin): agrega el dominio a hosts.'
Write-Host "  127.0.0.1  $Domain"
Write-Host '  PowerShell admin: Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1  juego.local"'
