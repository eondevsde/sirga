# ==========================================================================================
#  05 — PEGAR EL PUBLISH EN LOS SITIOS QUE YA EXISTEN
#
#  Para cuando los sitios YA están creados en IIS (con su App Pool, su carpeta y sus permisos)
#  y lo único que falta es dejar los archivos de la aplicación adentro.
#
#  NO crea sitios, NO toca bindings, NO toca permisos: sólo copia y configura.
#
#  QUÉ HACE, POR CADA SITIO
#    1. Pregunta a IIS cuál es su carpeta física (no la inventa).
#    2. Copia la publicación ahí con robocopy (deja la carpeta igual a la publicación).
#    3. Escribe appsettings.json y appsettings.Production.json con las bases DEMO.
#    4. Escribe web.config con ASPNETCORE_ENVIRONMENT=Production y log de arranque activo.
#    5. Recicla el App Pool para que tome los archivos nuevos.
#
#  USO
#    .\05_pegar_publish.ps1                                   # simulacro: sólo muestra
#    .\05_pegar_publish.ps1 -Aplicar                           # copia y configura
#    .\05_pegar_publish.ps1 -Aplicar -InstanciaSQL ".\SQLEXPRESS"
#
#  REQUISITO: las publicaciones en -OrigenPublicaciones con estas carpetas:
#    SIRGA_DEMO   SIGESWEB_DEMO   SIGESWEB_DEMO_CARNET
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$OrigenPublicaciones = "C:\Publicaciones",
    [string]$InstanciaSQL = "localhost",
    [ValidateSet("Windows", "SQL")][string]$Autenticacion = "Windows",
    [string]$UsuarioSQL = "",
    [string]$ClaveSQL = "",

    [string]$SitioSirga = "sirga-demo",
    [string]$SitioSiges = "sigesweb-demo",
    [string]$SitioCarnet = "sigesweb-demo-carnet",

    [string]$CarpetaSirga = "SIRGA_DEMO",
    [string]$CarpetaSiges = "SIGESWEB_DEMO",
    [string]$CarpetaCarnet = "SIGESWEB_DEMO_CARNET",

    [string]$DominioSirga = "sirga-demo.secsantiago.com.ar"
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration -ErrorAction Stop

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Hacer($descripcion, [scriptblock]$accion) {
    if ($Aplicar) { & $accion; Write-Host "   OK   $descripcion" -ForegroundColor Green }
    else { Write-Host "   (simulacro) $descripcion" -ForegroundColor DarkYellow }
}

$sitios = @(
    @{ Sitio = $SitioSirga;  Carpeta = $CarpetaSirga;  Dll = "SIRGA.WebUI.dll";          EsSirga = $true },
    @{ Sitio = $SitioSiges;  Carpeta = $CarpetaSiges;  Dll = "SIGESWEB.Web.dll";         EsSirga = $false },
    @{ Sitio = $SitioCarnet; Carpeta = $CarpetaCarnet; Dll = "SIGESWEB.Carnet.Api.dll";  EsSirga = $false }
)

Titulo "MODO $(if ($Aplicar) { 'REAL' } else { 'SIMULACRO' })"
Write-Host "  publicaciones: $OrigenPublicaciones"
Write-Host "  instancia SQL: $InstanciaSQL ($Autenticacion)"

if ($Autenticacion -eq "SQL" -and ([string]::IsNullOrWhiteSpace($UsuarioSQL) -or [string]::IsNullOrWhiteSpace($ClaveSQL))) {
    throw "Con -Autenticacion SQL hay que pasar -UsuarioSQL y -ClaveSQL."
}

function Cadena([string]$base) {
    if ($Autenticacion -eq "SQL") {
        return "Server=$InstanciaSQL;Database=$base;User Id=$UsuarioSQL;Password=$ClaveSQL;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
    }
    return "Server=$InstanciaSQL;Database=$base;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
}

# ------------------------------------------------------------------ validaciones previas
Titulo "CONTROLES"
$problemas = 0
foreach ($s in $sitios) {
    $sitioIis = Get-ChildItem IIS:\Sites -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $s.Sitio }
    if (-not $sitioIis) {
        Write-Host "  ERROR no existe el sitio '$($s.Sitio)' en IIS" -ForegroundColor Red
        $problemas++
        $s.Camino = $null
        continue
    }
    $s.Camino = $sitioIis.physicalPath
    $s.Pool = $sitioIis.applicationPool
    $origen = Join-Path $OrigenPublicaciones $s.Carpeta
    if (-not (Test-Path (Join-Path $origen $s.Dll))) {
        Write-Host "  ERROR falta la publicación: $(Join-Path $origen $s.Dll)" -ForegroundColor Red
        $problemas++
    }
    if (-not (Test-Path $s.Camino)) {
        Write-Host "  ERROR la carpeta del sitio no existe: $($s.Camino)" -ForegroundColor Red
        $problemas++
    }
    if ($s.Camino -and $s.Camino -notmatch "(?i)demo") {
        Write-Host "  ERROR la carpeta del sitio no parece de demo: $($s.Camino)" -ForegroundColor Red
        $problemas++
    }
    Write-Host ("  {0,-24} -> {1}" -f $s.Sitio, $s.Camino)
    Write-Host ("  {0,-24}    App Pool: {1}" -f "", $s.Pool)
}

if ($problemas -gt 0) {
    Write-Host ""
    Write-Host "  Hay $problemas problema(s). No se copia nada." -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------------ copia y configuración
foreach ($s in $sitios) {
    $origen = Join-Path $OrigenPublicaciones $s.Carpeta
    Titulo "$($s.Sitio)  ->  $($s.Camino)"

    Hacer "copiar $origen -> $($s.Camino)" {
        robocopy $origen $s.Camino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
    }

    $config = if ($s.EsSirga) {
        [ordered]@{
            ConnectionStrings = [ordered]@{
                DefaultConnection = (Cadena "SIRGA_DB_DEMO")
                LegacyConnection  = (Cadena "SINDICATO_DEMO")
                Legacy            = (Cadena "SINDICATO_DEMO")
            }
            ApiBaseUrl = "https://$DominioSirga/"
            AllowedHosts = "*"
            Logging = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
        }
    } else {
        [ordered]@{
            ConnectionStrings = [ordered]@{ Sindicato = (Cadena "SINDICATO_DEMO") }
            AllowedHosts = "*"
            Logging = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
        }
    }

    Hacer "escribir configuración (bases DEMO)" {
        $json = $config | ConvertTo-Json -Depth 10
        Set-Content (Join-Path $s.Camino "appsettings.json") -Value $json -Encoding utf8
        Set-Content (Join-Path $s.Camino "appsettings.Production.json") -Value $json -Encoding utf8

        $dll = $s.Dll
        $webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\$dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
        Set-Content (Join-Path $s.Camino "web.config") -Value $webConfig -Encoding utf8
        New-Item -ItemType Directory -Force -Path (Join-Path $s.Camino "logs") | Out-Null
    }

    Hacer "reciclar el App Pool $($s.Pool) para que tome los archivos nuevos" {
        Restart-WebAppPool -Name $s.Pool
    }
}

# ------------------------------------------------------------------ cierre
Titulo "RESUMEN"
if (-not $Aplicar) {
    Write-Host "  Fue un SIMULACRO. Para copiar de verdad: .\05_pegar_publish.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Write-Host "  Archivos copiados. Ahora probá en el navegador:" -ForegroundColor Cyan
Write-Host "    https://sirga-demo.secsantiago.com.ar"
Write-Host "    https://sigesweb-demo.secsantiago.com.ar"
Write-Host "    https://sigesweb-demo-carnet.secsantiago.com.ar"
Write-Host ""
Write-Host "  Si un sitio da error 500.30, mirá su log:" -ForegroundColor Cyan
Write-Host "    <carpeta del sitio>\logs\stdout_*.log"
Write-Host ""
Write-Host "  Control final: .\04_verificar.ps1"
