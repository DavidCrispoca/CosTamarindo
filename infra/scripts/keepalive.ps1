# Keep-alive del proyecto Supabase Free (DB-07).
#
#   powershell -File infra/scripts/keepalive.ps1
#
# Supabase pausa los proyectos Free tras ~7 dias de baja actividad en la base.
# Esta llamada hace una lectura real (PostgREST, rol service_role) que cuenta
# como actividad. Pensada para el Programador de tareas de Windows o un cron.
#
# Programar (ejemplo, cada 12 h):
#   schtasks /Create /SC DAILY /MO 1 /TN "CosTamarindo keepalive" /TR ^
#     "powershell -NoProfile -File C:\ruta\CosTamarindo\infra\scripts\keepalive.ps1"

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$envFile = Join-Path $repoRoot '.env'
if (-not (Test-Path $envFile)) { throw "Falta .env en $repoRoot" }

$vars = @{}
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^([A-Z_]+)=(.*)$') { $vars[$Matches[1]] = $Matches[2] }
}

$url = $vars['SUPABASE_URL']
$secret = $vars['SUPABASE_SECRET_KEY']
if (-not $url -or -not $secret) { throw 'Faltan SUPABASE_URL o SUPABASE_SECRET_KEY' }

$response = Invoke-RestMethod -Uri "$url/rest/v1/profiles?select=id&limit=1" -Headers @{
    'apikey'        = $secret
    'Authorization' = "Bearer $secret"
    'User-Agent'    = 'CostamarindoKeepalive/1.0'
} -TimeoutSec 30 -Method Get

Write-Output ("keepalive ok {0:o} filas={1}" -f (Get-Date).ToUniversalTime(), @($response).Count)
