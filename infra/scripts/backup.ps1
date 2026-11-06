# Respaldo logico de la base de datos (A.8.13) en Windows.
#
#   powershell -File infra/scripts/backup.ps1
#
# Genera infra/backups/costamarindo-<fecha>.dump y su SHA256.
# Usa la imagen oficial de Postgres: no hace falta tener pg_dump instalado.
# La contrasena viaja por variable de entorno del cliente Docker, no por la
# linea de comandos (visible en el listado de procesos).

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$envFile = Join-Path $repoRoot '.env'
if (-not (Test-Path $envFile)) { throw "Falta .env en $repoRoot" }

$vars = @{}
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^([A-Z_]+)=(.*)$') { $vars[$Matches[1]] = $Matches[2] }
}

$dbUrl = $vars['SUPABASE_DB_URL']
if (-not $dbUrl) { throw 'Falta SUPABASE_DB_URL en .env' }

$uri = [System.Uri]$dbUrl
$user = $uri.UserInfo.Split(':')[0]
$password = $uri.UserInfo.Split(':')[1]
$dbName = $uri.AbsolutePath.Trim('/')

$backupDir = Join-Path $repoRoot 'infra/backups'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

$stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$name = "costamarindo-$stamp.dump"
$mount = "$backupDir`:/backup"

$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (-not $docker) {
    $candidate = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    if (Test-Path $candidate) { $docker = $candidate } else { throw 'docker.exe no disponible' }
}

# -e PGPASSWORD sin valor: Docker copia la variable del cliente.
$env:PGPASSWORD = $password
try {
    & $docker run --rm -e PGPASSWORD -v $mount postgres:17 `
        pg_dump -h $uri.Host -p $uri.Port -U $user -d $dbName `
        --format=custom --no-owner --schema=public --schema=private `
        --file="/backup/$name"
    if ($LASTEXITCODE -ne 0) { throw "pg_dump fallo con codigo $LASTEXITCODE" }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

$out = Join-Path $backupDir $name
$hash = (Get-FileHash $out -Algorithm SHA256).Hash.ToLower()
"$hash  $name" | Set-Content "$out.sha256" -Encoding ascii

Write-Host "Respaldo: $out"
Write-Output "$hash  $name"
