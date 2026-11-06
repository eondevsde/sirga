# ==========================================================================================
#  06 — DESPLIEGA LA PUBLICACIÓN AUTOCONTENIDA (la que NO depende del runtime del servidor)
#
#  Para cuando el sitio da 500.31 ("Failed to load ASP.NET Core runtime") aunque el runtime
#  esté instalado. La publicación autocontenida lleva su propio .NET: el módulo de IIS sólo
#  tiene que lanzar SIRGA.WebUI.exe.
#
#  QUÉ HACE
#    1. Comprueba que la publicación autocontenida tenga SIRGA.WebUI.exe.
#    2. Pide a IIS la carpeta del sitio.
#    3. Vacía esa carpeta y copia la publicación autocontenida.
#    4. Escribe appsettings.json / appsettings.Production.json con las bases DEMO.
#    5. Escribe web.config con processPath=".\SIRGA.WebUI.exe" y log de arranque.
#    6. Reinicia el App Pool.
#
#  USO (en el servidor)
#    .\06_pegar_autocontenido.ps1                                        # simulacro
#    .\06_pegar_autocontenido.ps1 -Aplicar -Origen "C:\Publicaciones\SIRGA_DEMO_SC"
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$Origen = "C:\Publicaciones\SIRGA_DEMO_SC",
    [string]$Sitio = "sirga-demo",
    [string]$InstanciaSQL = ".\SQLEXPRESS",
    [string]$DominioSirga = "sirga-demo.secsantiago.com.ar"
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration -ErrorAction Stop

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

Titulo "CONTROLES"
if (-not (Test-Path (Join-Path $Origen "SIRGA.WebUI.exe"))) {
    throw "No encuentro SIRGA.WebUI.exe en $Origen. Copiá primero la publicación autocontenida."
}
$sitio = Get-Website -Name $Sitio -ErrorAction SilentlyContinue
if (-not $sitio) { throw "No existe el sitio '$Sitio' en IIS." }
$destino = $sitio.physicalPath
if ($destino -notmatch "(?i)demo") { throw "La carpeta del sitio no parece de demo: $destino. Se aborta." }

Write-Host "  origen  : $Origen"
Write-Host "  destino : $destino"
Write-Host "  App Pool: $($sitio.applicationPool)"
Write-Host "  SQL     : $InstanciaSQL"

if (-not $Aplicar) {
    Write-Host ""
    Write-Host "  (simulacro) vaciar $destino y copiar $Origen"
    Write-Host "  (simulacro) escribir configuración con SIRGA_DB_DEMO y SINDICATO_DEMO"
    Write-Host "  (simulacro) web.config con processPath=.\SIRGA.WebUI.exe"
    Write-Host "  (simulacro) reiniciar el App Pool $($sitio.applicationPool)"
    Write-Host ""
    Write-Host "  Para hacerlo de verdad: .\06_pegar_autocontenido.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Titulo "COPIANDO"
Stop-WebAppPool -Name $sitio.applicationPool -ErrorAction SilentlyContinue
Get-ChildItem $destino -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
robocopy $Origen $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
Write-Host "  archivos copiados: $((Get-ChildItem $destino -Recurse -File).Count)"

Titulo "CONFIGURACIÓN"
$cadena = {
    param($base)
    "Server=$InstanciaSQL;Database=$base;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
}

$cfg = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = (& $cadena "SIRGA_DB_DEMO")
        LegacyConnection  = (& $cadena "SINDICATO_DEMO")
        Legacy            = (& $cadena "SINDICATO_DEMO")
    }
    ApiBaseUrl   = "https://$DominioSirga/"
    AllowedHosts = "*"
    Logging      = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
}
$json = $cfg | ConvertTo-Json -Depth 10
Set-Content (Join-Path $destino "appsettings.json") -Value $json -Encoding utf8
Set-Content (Join-Path $destino "appsettings.Production.json") -Value $json -Encoding utf8
Write-Host "  cadenas -> SIRGA_DB_DEMO y SINDICATO_DEMO" -ForegroundColor Green

$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\SIRGA.WebUI.exe" arguments="" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
Set-Content (Join-Path $destino "web.config") -Value $webConfig -Encoding utf8
New-Item -ItemType Directory -Force -Path (Join-Path $destino "logs") | Out-Null
Write-Host "  web.config -> processPath=.\SIRGA.WebUI.exe (usa el runtime propio)" -ForegroundColor Green

Titulo "ARRANCANDO"
Start-WebAppPool -Name $sitio.applicationPool
Start-Sleep -Seconds 8
Get-WebAppPoolState $sitio.applicationPool

Titulo "LISTO"
Write-Host "  Probá: http://$DominioSirga" -ForegroundColor Cyan
Write-Host "  Si falla, el log: $destino\logs\stdout_*.log" -ForegroundColor Cyan
Write-Host "  Control: .\04_verificar.ps1"
