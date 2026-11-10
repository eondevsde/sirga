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
    [string]$Proyecto = "C:\Proyectos\SIRGA\SIRGA.WebUI\SIRGA.WebUI.csproj",
    [string]$InstanciaSQL = ".\SQLEXPRESS",
    [string]$BaseSirga = "SIRGA_DB_DEMO",
    [string]$BaseLegado = "SINDICATO_DEMO",
    [string]$Dominio = "sirga-demo.secsantiago.com.ar"
)

$ErrorActionPreference = "Stop"
$inicio = Get-Date

function EscribirConfigDemo {
    param(
        [Parameter(Mandatory = $true)][string]$Carpeta,
        [Parameter(Mandatory = $true)][hashtable]$Cadenas,
        [string]$ApiBase = ""
    )
    # NO se reescribe el archivo entero: se lee el que dejó el publish (que trae las secciones del
    # repo: Email, Logging, etc.), se le cambian SOLO las cadenas y se vuelve a escribir.
    # Reescribirlo desde cero borraba secciones como `Email` y rompía el envío de correo.
    $ruta = Join-Path $Carpeta "appsettings.json"
    $cfg = if (Test-Path $ruta) { Get-Content $ruta -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }

    $cfg | Add-Member -NotePropertyName ConnectionStrings -NotePropertyValue ([pscustomobject]$Cadenas) -Force
    if ($ApiBase) { $cfg | Add-Member -NotePropertyName ApiBaseUrl -NotePropertyValue $ApiBase -Force }
    if (-not ($cfg.PSObject.Properties.Name -contains "AllowedHosts")) {
        $cfg | Add-Member -NotePropertyName AllowedHosts -NotePropertyValue "*" -Force
    }

    $json = $cfg | ConvertTo-Json -Depth 10
    Set-Content $ruta $json -Encoding utf8
    Set-Content (Join-Path $Carpeta "appsettings.Production.json") $json -Encoding utf8
    return ($cfg.PSObject.Properties.Name)
}
function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

# Se borran los appsettings de la publicación ANTERIOR: dotnet publish no pisa un archivo si el
# destino es más nuevo, así que sin esto se quedaba la configuración vieja (y se perdían secciones
# como Email que sí trae el repo).
foreach ($n in @("appsettings.json", "appsettings.Production.json", "appsettings.Development.json")) {
    $p = Join-Path $Publicacion $n
    if (Test-Path $p) { Remove-Item $p -Force; Write-Host "  (borrado $n de la publicación anterior)" }
}
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

# ---------------------------------------------------------------------------------------
# LA CONFIGURACIÓN DEMO SE ESCRIBE EN LA CARPETA PUBLICADA, NO AL DESPLEGAR.
# Motivo: `dotnet publish` copia el appsettings.json del repo, que apunta a SIRGA_DB (la base
# REAL). Si alguien copia la carpeta a mano al servidor, la demo escribiría en producción.
# Escribiéndola acá, cualquier copia a mano ya queda apuntando a las bases demo.
# ---------------------------------------------------------------------------------------
Titulo "CONFIGURACIÓN DEMO EN LA PUBLICACIÓN"
$cadenaBase = "Server=$InstanciaSQL;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
$secciones = EscribirConfigDemo -Carpeta $Publicacion -Cadenas ([ordered]@{
    DefaultConnection = ($cadenaBase -f "SIRGA_DB_DEMO")
    LegacyConnection  = ($cadenaBase -f "SINDICATO_DEMO")
    Legacy            = ($cadenaBase -f "SINDICATO_DEMO")
}) -ApiBase "https://$Dominio/"
New-Item -ItemType Directory -Force -Path (Join-Path $Publicacion "logs") | Out-Null
Write-Host "  bases DEMO escritas ($($secciones -join ', '))" -ForegroundColor Green

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
