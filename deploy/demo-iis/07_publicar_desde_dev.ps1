# ==========================================================================================
#  07 — PUBLICAR DESDE LA MÁQUINA DE DESARROLLO
#
#  Compila, publica AUTOCONTENIDO (win-x64, con su propio .NET) y lo copia al servidor.
#  Después, en el servidor, se corre 08_desplegar_en_servidor.ps1.
#
#  USO
#    .\07_publicar_desde_dev.ps1 -Servidor "\\SERVIDOR"
#    .\07_publicar_desde_dev.ps1 -Servidor "\\192.168.1.240" -DestinoRemoto "C:\Publicaciones\SIRGA_DEMO_SC"
# ==========================================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Servidor,
    [string]$DestinoRemoto = "C:\Publicaciones\SIRGA_DEMO_SC",
    [string]$Publicacion = "C:\Proyectos\SIRGA_DEMO_SC",
    [string]$Proyecto = "C:\Proyectos\SIRGA\SIRGA.WebUI\SIRGA.WebUI.csproj"
)

$ErrorActionPreference = "Stop"
$inicio = Get-Date

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

Titulo "PUBLICANDO AUTOCONTENIDO"
Write-Host "  proyecto   : $Proyecto"
Write-Host "  salida     : $Publicacion"
$out = & dotnet publish $Proyecto -c Release -r win-x64 --self-contained true -o $Publicacion --nologo 2>&1
$errores = @($out | Where-Object { $_ -match ': error ' })
if ($errores.Count -gt 0) {
    $errores | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    throw "Falló la publicación."
}
if (-not (Test-Path (Join-Path $Publicacion "SIRGA.WebUI.exe"))) { throw "No se generó SIRGA.WebUI.exe." }

$archivos = (Get-ChildItem $Publicacion -Recurse -File).Count
$mb = [math]::Round((Get-ChildItem $Publicacion -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "  OK: $archivos archivos | $mb MB" -ForegroundColor Green

Titulo "COPIANDO AL SERVIDOR"
$destino = Join-Path $Servidor $DestinoRemoto.Replace(":", "$")
$destino = $destino -replace '\\\\', '\'
Write-Host "  destino: $destino"

if (-not (Test-Path $destino)) { throw "No puedo acceder a $destino. Verificá la ruta administrativa (C$) y los permisos." }

robocopy $Publicacion $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
$copiados = (Get-ChildItem $destino -Recurse -File).Count

Titulo "RESUMEN"
Write-Host ("  duración: {0:N0} s" -f ((Get-Date) - $inicio).TotalSeconds)
Write-Host "  en el servidor hay $copiados archivos" -ForegroundColor Green
if ($copiados -ne $archivos) { Write-Host "  AVISO: el conteo no coincide ($archivos vs $copiados)" -ForegroundColor Yellow }

Write-Host ""
Write-Host "  AHORA, EN EL SERVIDOR:" -ForegroundColor Cyan
Write-Host "    cd C:\Demo\instalador"
Write-Host "    .\08_desplegar_en_servidor.ps1 -Aplicar"
