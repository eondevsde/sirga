# ==========================================================================================
#  11 — PUBLICAR SIGESWEB DESDE LA MÁQUINA DE DESARROLLO
#
#  Compila, publica en C:\Proyectos\SIGESWEB_DEMO y deja la CONFIGURACIÓN DEMO escrita en la
#  propia carpeta publicada (no al desplegar). Así, copiar la carpeta a mano al servidor es
#  seguro: ya apunta a SINDICATO_DEMO y no a la base real.
#
#  USO (en la máquina de desarrollo)
#    .\11_publicar_sigesweb_desde_dev.ps1
#    .\11_publicar_sigesweb_desde_dev.ps1 -Servidor "\\SERVIDOR"     # además copia al servidor
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$Servidor = "",
    [string]$DestinoRemoto = "C:\Publicaciones\SIGESWEB_DEMO",
    [string]$Publicacion = "C:\Proyectos\SIGESWEB_DEMO",
    [string]$Proyecto = "C:\Proyectos\SIGESWEB\SIGESWEB.Web\SIGESWEB.Web.csproj",
    [string]$InstanciaSQL = ".\SQLEXPRESS",
    [string]$Base = "SINDICATO_DEMO",
    [string]$Dominio = "sigesweb-demo.secsantiago.com.ar"
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
    # Reescribirlo desde cero borraba `Email` y rompía el envío de correo.
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
Titulo "PUBLICANDO SIGESWEB"
Write-Host "  proyecto : $Proyecto"
Write-Host "  salida   : $Publicacion"
$out = & dotnet publish $Proyecto -c Release -o $Publicacion --nologo 2>&1
$errores = @($out | Where-Object { $_ -match ': error ' })
if ($errores.Count -gt 0) {
    $errores | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    throw "Falló la publicación."
}
if (-not (Test-Path (Join-Path $Publicacion "SIGESWEB.Web.dll"))) { throw "No se generó SIGESWEB.Web.dll." }
Write-Host "  OK: $((Get-ChildItem $Publicacion -Recurse -File).Count) archivos | $([math]::Round((Get-ChildItem $Publicacion -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)) MB" -ForegroundColor Green

# ---------------------------------------------------------------------------------------
# CONFIGURACIÓN DEMO EN LA CARPETA PUBLICADA (no al desplegar).
# `dotnet publish` copia el appsettings.json del repo, que apunta a la base REAL (Sindicato).
# ---------------------------------------------------------------------------------------
Titulo "CONFIGURACIÓN DEMO EN LA PUBLICACIÓN"
$secciones = EscribirConfigDemo -Carpeta $Publicacion -Cadenas ([ordered]@{
    Sindicato = "Server=$InstanciaSQL;Database=$Base;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
})
Write-Host "  secciones conservadas: $($secciones -join ', ')"

$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\SIGESWEB.Web.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
Set-Content (Join-Path $Publicacion "web.config") -Value $webConfig -Encoding utf8
New-Item -ItemType Directory -Force -Path (Join-Path $Publicacion "logs") | Out-Null
Write-Host "  base ${Base} en $InstanciaSQL, entorno Production, log activo" -ForegroundColor Green

if ($Servidor) {
    Titulo "COPIANDO AL SERVIDOR"
    $destino = Join-Path $Servidor $DestinoRemoto.Replace(":", "$")
    Write-Host "  destino: $destino"
    if (-not (Test-Path $destino)) { throw "No puedo acceder a $destino." }
    robocopy $Publicacion $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
    Write-Host "  copiado: $((Get-ChildItem $destino -Recurse -File).Count) archivos" -ForegroundColor Green
}

Titulo "RESUMEN"
Write-Host ("  duración: {0:N0} s" -f ((Get-Date) - $inicio).TotalSeconds)
Write-Host ""
Write-Host "  AHORA, EN EL SERVIDOR:" -ForegroundColor Cyan
Write-Host "    cd C:\Demo\instalador"
Write-Host "    .\10_desplegar_sigesweb.ps1 -Aplicar"
Write-Host ""
Write-Host "  O a mano: copiar el CONTENIDO de $Publicacion"
Write-Host "            en C:\inetpub\sigesweb-demo y reciclar el pool."
