# ==========================================================================================
#  CREA LAS BASES DE LA DEMO EN EL SERVIDOR — un solo comando
#
#  Hace todo lo de 01_bases_demo.sql sin que tengas que editar nada adentro:
#    - verifica que estén los dos respaldos
#    - restaura SIRGA_DB_DEMO y SINDICATO_DEMO
#    - deja las bases sanas (SIMPLE, sin auto-close ni auto-shrink)
#    - crea los usuarios de los App Pools de IIS y les da permiso
#    - verifica los conteos
#
#  USO
#    .\crear-bases-demo.ps1                                  # simulacro (no crea nada)
#    .\crear-bases-demo.ps1 -Aplicar                          # crea las bases
#    .\crear-bases-demo.ps1 -Aplicar -Instancia ".\SQLEXPRESS"
#    .\crear-bases-demo.ps1 -Aplicar -Recrear                  # las borra y las vuelve a crear
#
#  LOS RESPALDOS tienen que estar en la carpeta (por defecto C:\Demo\bak):
#    SIRGA_DB_DEMO.bak     (61 MB)
#    SINDICATO_DEMO.bak    (655 MB)
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$CarpetaBak = "C:\Demo\bak",
    [string]$Instancia = "localhost",
    [switch]$Aplicar,
    [switch]$Recrear
)

$ErrorActionPreference = "Stop"
$sql = Join-Path $PSScriptRoot "01_bases_demo.sql"

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

Titulo "CONTROLES"
if (-not (Test-Path $sql)) { throw "No encuentro el script $sql (tiene que estar en la misma carpeta)." }
if (-not (Test-Path $CarpetaBak)) { throw "No existe la carpeta de respaldos: $CarpetaBak" }

$faltan = 0
foreach ($b in @("SIRGA_DB_DEMO.bak", "SINDICATO_DEMO.bak")) {
    $ruta = Join-Path $CarpetaBak $b
    if (Test-Path $ruta) {
        $mb = [math]::Round((Get-Item $ruta).Length / 1MB, 1)
        Write-Host ("  OK  {0,-22} {1} MB" -f $b, $mb) -ForegroundColor Green
    } else {
        Write-Host ("  FALTA {0}" -f $ruta) -ForegroundColor Red
        $faltan++
    }
}
if ($faltan -gt 0) {
    Write-Host ""
    Write-Host "  Copiá los respaldos a $CarpetaBak y volvé a intentar." -ForegroundColor Yellow
    throw "Faltan respaldos."
}

Write-Host "  instancia: $Instancia"
Write-Host "  modo     : $(if ($Aplicar) { 'REAL (va a crear las bases)' } else { 'SIMULACRO (no hace nada)' })"

# Se prepara una copia del script con los parámetros ya puestos, así no hay que editarlo.
$texto = Get-Content $sql -Raw
$texto = $texto -replace "DECLARE @Simulacro      bit = \d+;", "DECLARE @Simulacro      bit = $(if ($Aplicar) { 0 } else { 1 });"
$texto = $texto -replace "DECLARE @Recrear        bit = \d+;", "DECLARE @Recrear        bit = $(if ($Recrear) { 1 } else { 0 });"
$texto = $texto -replace "DECLARE @CarpetaBak     nvarchar\(400\) = N'[^']*';", "DECLARE @CarpetaBak     nvarchar(400) = N'$CarpetaBak';"

$temporal = Join-Path $env:TEMP ("bases-demo-" + [guid]::NewGuid().ToString("N") + ".sql")
[System.IO.File]::WriteAllText($temporal, $texto, (New-Object System.Text.UTF8Encoding($true)))

try {
    Titulo "EJECUTANDO"
    & sqlcmd -S $Instancia -E -b -I -i $temporal
    $codigo = $LASTEXITCODE
    Write-Host ""
    if ($codigo -ne 0) {
        Write-Host "  Terminó con código $codigo. Revisá el detalle de arriba." -ForegroundColor Red
        exit $codigo
    }
}
finally {
    Remove-Item $temporal -Force -ErrorAction SilentlyContinue
}

if (-not $Aplicar) {
    Write-Host ""
    Write-Host "  Fue un SIMULACRO. Para crear las bases de verdad:" -ForegroundColor DarkYellow
    Write-Host "    .\crear-bases-demo.ps1 -Aplicar" -ForegroundColor DarkYellow
}
