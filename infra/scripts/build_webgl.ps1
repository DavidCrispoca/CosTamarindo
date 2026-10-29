# Build WebGL reproducible (Windows / PowerShell).
# Uso:  powershell -File infra/scripts/build_webgl.ps1
# Salida: builds/webgl  (excluida de git)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$unity = Join-Path $env:ProgramFiles 'Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
$log = Join-Path $env:TEMP 'webgl-build.log'

if (-not (Test-Path $unity)) { throw "Unity 6000.3.27f1 no encontrado en $unity" }

# Unity.exe es una app de subsistema GUI: con `&` PowerShell no espera y
# $LASTEXITCODE queda vacío. Hay que usar Start-Process -Wait.
$proc = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  '-batchmode', '-quit',
  '-projectPath', (Join-Path $root 'unity'),
  '-buildTarget', 'WebGL',
  '-executeMethod', 'CosTamarindo.EditorTools.BuildScript.BuildWebGL',
  '-logFile', $log
)

if ($proc.ExitCode -ne 0) { throw "Build WebGL falló (exit $($proc.ExitCode)). Log: $log" }

$out = Join-Path $root 'builds\webgl'

# La CSP depende del hash del <script> inline que genera Unity: se recalcula.
& node (Join-Path $root 'tools\dev-server\csp.mjs') `
  (Join-Path $out 'index.html') `
  (Join-Path $root 'infra\nginx\snippets\csp.conf')

$size = (Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
"Build OK -> $out ($([math]::Round($size, 1)) MB)"
