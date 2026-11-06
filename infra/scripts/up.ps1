# Levanta el entorno de 5 nodos (F6) en Windows.
#
#   powershell -File infra/scripts/up.ps1              -> juego + IA (dev)
#   powershell -File infra/scripts/up.ps1 security     -> + Wazuh
#   powershell -File infra/scripts/up.ps1 backend      -> + API
#   powershell -File infra/scripts/up.ps1 -Prod        -> sin override de dev
#
# Equivalente a up.sh. Resuelve docker.exe aunque no esté en el PATH del
# proceso actual, que es lo que pasa cuando Docker se instaló con el shell
# ya abierto (el PATH nuevo no se hereda).

param(
    [switch] $Prod,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Profiles
)

$ErrorActionPreference = 'Stop'

function Resolve-Docker {
    $cmd = Get-Command docker -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $known = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    if (Test-Path $known) { return $known }

    throw 'No se encontró docker.exe. Abrí Docker Desktop o agregá Docker al PATH.'
}

$docker = Resolve-Docker
$infraDir = Split-Path -Parent $PSScriptRoot
$composePath = Join-Path $infraDir 'docker-compose.yml'
$overridePath = Join-Path $infraDir 'docker-compose.override.yml'

$arguments = @('compose', '-f', $composePath)
if (-not $Prod -and (Test-Path $overridePath)) {
    $arguments += @('-f', $overridePath)
}

if ($Profiles) {
    foreach ($profile in $Profiles) {
        $arguments += @('--profile', $profile)
    }
}

$arguments += @('up', '-d', '--wait')

Write-Host "> $docker $($arguments -join ' ')" -ForegroundColor DarkGray
& $docker @arguments
if ($LASTEXITCODE -ne 0) { throw "docker compose up falló con código $LASTEXITCODE" }

Write-Host ''
& $docker compose -f $composePath ps

Write-Host ''
Write-Host 'Juego: https://juego.local   (requiere "127.0.0.1 juego.local" en hosts)' -ForegroundColor Green
if ($Profiles -contains 'backend') {
    Write-Host 'API:   https://juego.local/api/health' -ForegroundColor Green
}
