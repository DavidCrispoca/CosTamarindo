# Prueba de restauracion (A.8.13 y DoD de DB-06).
#
#   powershell -File infra/scripts/restore-test.ps1 [-Dump infra/backups/x.dump]
#
# Levanta un Postgres efimero, crea los roles y el stub de auth.users que el
# dump referencia, restaura y verifica que las tablas y la funcion quedaron.
# No toca el proyecto de Supabase.

param(
    [string] $Dump = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$backupDir = Join-Path $repoRoot 'infra/backups'

if (-not $Dump) {
    $latest = Get-ChildItem $backupDir -Filter '*.dump' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) { throw "No hay respaldos en $backupDir" }
    $Dump = $latest.FullName
}
if (-not (Test-Path $Dump)) { throw "No existe $Dump" }

$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (-not $docker) {
    $candidate = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    if (Test-Path $candidate) { $docker = $candidate } else { throw 'docker.exe no disponible' }
}

$container = "ct-restore-test"
& $docker rm -f $container 2>$null | Out-Null

Write-Host '1. Levantando Postgres efimero...'
& $docker run -d --name $container -e POSTGRES_PASSWORD=restore -e POSTGRES_DB=restore postgres:17 | Out-Null

$ready = $false
$previousEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 1
    & $docker exec $container psql -U postgres -d restore -c 'select 1' -q 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
}
$ErrorActionPreference = $previousEap
if (-not $ready) { & $docker rm -f $container | Out-Null; throw 'Postgres no arranco' }

try {
    Write-Host '2. Creando roles y stub de auth.users...'
    $bootstrap = @'
create role anon nologin;
create role authenticated nologin;
create role service_role nologin bypassrls;
create role authenticator noinherit login password 'restore-only';
create role supabase_admin;
create role supabase_auth_admin;
create role supabase_storage_admin;
create role supabase_realtime_admin;
create role supabase_replication_admin;
create role supabase_read_only_user;
create role dashboard_user;
create role pgbouncer;
drop schema if exists public cascade;
create schema if not exists auth;
create table auth.users (id uuid primary key, email text);
'@
    $bootstrap | & $docker exec -i $container psql -U postgres -d restore -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 0) { throw 'bootstrap fallo' }

    Write-Host '3. Restaurando...'
    $dumpDir = Split-Path -Parent $Dump
    $dumpName = Split-Path -Leaf $Dump
    & $docker cp $Dump "$container`:/tmp/$dumpName"
    & $docker exec $container pg_restore -U postgres -d restore --no-owner --exit-on-error "/tmp/$dumpName"
    if ($LASTEXITCODE -ne 0) { throw "pg_restore fallo con codigo $LASTEXITCODE" }

    Write-Host '4. Verificando...'
    $sql = @'
select 'tablas', count(*)::text from information_schema.tables
where table_schema = 'public' and table_type = 'BASE TABLE';
select 'funciones', count(*)::text from information_schema.routines where routine_schema = 'public';
select 'migraciones', count(*)::text from private.schema_migrations;
select 'rls', count(*)::text from pg_class c join pg_namespace n on n.oid = c.relnamespace
where n.nspname = 'public' and c.relkind = 'r' and c.relrowsecurity;
'@
    $sql | & $docker exec -i $container psql -U postgres -d restore -q -t -A -F'='

    $checks = @(
        @{ Query = "select count(*) from information_schema.tables where table_schema='public' and table_type='BASE TABLE'"; Expect = '4' },
        @{ Query = "select count(*) from information_schema.routines where routine_schema='public' and routine_name='get_leaderboard'"; Expect = '1' },
        @{ Query = "select count(*) from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='public' and c.relkind='r' and c.relrowsecurity"; Expect = '4' }
    )

    $failures = 0
    foreach ($check in $checks) {
        $value = (& $docker exec $container psql -U postgres -d restore -t -A -c $check.Query).Trim()
        if ($value -eq $check.Expect) {
            Write-Host "  OK   $($check.Query) = $value"
        } else {
            Write-Host "  FALLA $($check.Query) = $value (esperado $($check.Expect))"
            $failures++
        }
    }

    if ($failures -gt 0) { throw "$failures comprobaciones fallaron" }
    Write-Host ''
    Write-Host 'Restauracion verificada.' -ForegroundColor Green
}
finally {
    & $docker rm -f $container | Out-Null
}
