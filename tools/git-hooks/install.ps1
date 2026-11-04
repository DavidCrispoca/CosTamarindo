# Instala el hook de Gitleaks en este clon de Git.
#
#   powershell -File tools/git-hooks/install.ps1
#
# Por qué existe este script y no se copia el hook a mano:
#   1. .git/hooks no se versiona, así que el hook se perdería en cada clon.
#   2. Git para Windows ejecuta los hooks con sh (MSYS), donde "gitleaks" no
#      está en PATH aunque Windows ya lo tenga: se resuelve aquí la ruta real y
#      se "hornea" en el hook instalado. Sin esto el hook avisa y deja pasar el
#      commit, que es justo el fallo silencioso que hay que evitar.

$ErrorActionPreference = 'Stop'

$repoRoot = git rev-parse --show-toplevel
if (-not $repoRoot) { throw 'No se encontró la raíz del repositorio Git.' }

$template = Join-Path $repoRoot 'tools/git-hooks/pre-commit'
if (-not (Test-Path $template)) { throw "No existe el hook de origen: $template" }

# --- 1. Resolver el binario de gitleaks -------------------------------------
$gitleaks = (Get-Command gitleaks -ErrorAction SilentlyContinue).Source

if (-not $gitleaks) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Microsoft/WinGet/Packages'),
        (Join-Path $env:USERPROFILE 'AppData/Local/Microsoft/WinGet/Packages')
    )
    foreach ($base in $candidates) {
        if (-not (Test-Path $base)) { continue }
        $found = Get-ChildItem $base -Recurse -Filter gitleaks.exe -ErrorAction SilentlyContinue |
                 Select-Object -First 1
        if ($found) { $gitleaks = $found.FullName; break }
    }
}

if (-not $gitleaks) {
    throw 'gitleaks no está instalado. Ejecutar: winget install Gitleaks.Gitleaks'
}

# --- 2. Convertir la ruta de Windows a formato POSIX para sh ---------------
#   C:\ruta\gitleaks.exe  ->  /c/ruta/gitleaks.exe
$posix = $gitleaks -replace '\\', '/'
if ($posix -match '^([A-Za-z]):/(.*)$') {
    $drive = $Matches[1].ToLower()
    $posix = "/$drive/$($Matches[2])"
}

# --- 3. Generar el hook instalado con la ruta horneada ---------------------
$body = Get-Content $template -Raw

# Se reemplaza la línea de descubrimiento por una ruta absoluta y explícita.
$body = $body -replace '(?m)^GITLEAKS="\$\{GITLEAKS_BIN:-gitleaks\}"$',
                        "GITLEAKS=`"$posix`""

# Git para Windows exige LF; se normaliza antes de escribir bytes.
$body = $body -replace "`r`n", "`n"

$hooksDir = Join-Path $repoRoot '.git/hooks'
if (-not (Test-Path $hooksDir)) { New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null }

$target = Join-Path $hooksDir 'pre-commit'
[System.IO.File]::WriteAllBytes($target, [System.Text.Encoding]::UTF8.GetBytes($body))

Write-Host "[ok] Hook instalado en .git/hooks/pre-commit" -ForegroundColor Green
Write-Host "     gitleaks: $gitleaks" -ForegroundColor DarkGray
Write-Host '     Verificar: la próxima vez que hagas commit debe decir "[pre-commit] gitleaks: sin hallazgos."' -ForegroundColor DarkGray
