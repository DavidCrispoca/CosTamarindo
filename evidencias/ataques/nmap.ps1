<#
.SYNOPSIS
    Escaneo reproducible de puertos para la evidencia A.8.20.

.DESCRIPTION
    Usa nmap en modo connect (-sT) sin privilegios. En Windows puede aparecer una
    advertencia de Npcap; el escaneo sigue funcionando en modo connect.

.EXAMPLE
    powershell -File evidencias/ataques/nmap.ps1 -Target 127.0.0.1 -Ports 443,8000,8081,11434

.EXAMPLE
    powershell -File evidencias/ataques/nmap.ps1 -Target 172.18.18.220 -Ports 22,443,1514,1515,55000,9200
#>
[CmdletBinding()]
param(
    [string]$Target = '127.0.0.1',
    [string]$Ports = '443,8000,8081,11434',
    [string]$OutFile
)

$ErrorActionPreference = 'Continue'

$nmap = Get-Command nmap -ErrorAction SilentlyContinue
if (-not $nmap) {
    throw 'nmap no encontrado. Instalarlo con: winget install Insecure.Nmap'
}

$output = @(
    "# nmap - evidencia A.8.20"
    "# fecha: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK')"
    "# comando: nmap -sT -Pn --unprivileged -p $Ports $Target"
    ''
) + (& $nmap.Source -sT -Pn --unprivileged -p $Ports $Target 2>&1)

$output | ForEach-Object { $_ }

if ($OutFile) {
    $output | Set-Content -Path $OutFile -Encoding utf8
    Write-Host "Salida guardada en $OutFile"
}
