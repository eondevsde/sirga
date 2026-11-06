# ==========================================================================================
#  10 — DESPLEGAR SIGESWEB DEMO EN EL SERVIDOR (solo ese sitio)
#
#  ⚠️ NO usar 05_pegar_publish.ps1 para esto: ese script reescribe la configuración de SIRGA
#  con `processPath="dotnet"` (framework-dependent) y SIRGA volvería al error 500.31.
#  Este script toca ÚNICAMENTE el sitio de SIGESWEB.
#
#  USO (en el servidor, PowerShell como administrador)
#    .\10_desplegar_sigesweb.ps1                     # simulacro
#    .\10_desplegar_sigesweb.ps1 -Aplicar
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$Origen = "C:\Publicaciones\SIGESWEB_DEMO",
    [string]$Destino = "C:\inetpub\sigesweb-demo",
    [string]$Pool = "sigesweb-demo",
    [string]$Dominio = "sigesweb-demo.secsantiago.com.ar",
    [string]$InstanciaSQL = ".\SQLEXPRESS",
    [string]$Base = "SINDICATO_DEMO"
)

$ErrorActionPreference = "Stop"
try { Import-Module WebAdministration -ErrorAction Stop }
catch { throw "Correr en el SERVIDOR con PowerShell ABIERTO COMO ADMINISTRADOR." }

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Paso($d, [scriptblock]$a) {
    if ($Aplicar) { & $a; Write-Host "   OK   $d" -ForegroundColor Green }
    else { Write-Host "   (simulacro) $d" -ForegroundColor DarkYellow }
}

Titulo "CONTROLES"
if (-not (Test-Path (Join-Path $Origen "SIGESWEB.Web.dll"))) { throw "No encuentro SIGESWEB.Web.dll en $Origen." }
if (-not (Test-Path $Destino)) { throw "No existe la carpeta $Destino." }
if ($Destino -notmatch "(?i)demo") { throw "La carpeta no es de demo: $Destino" }
if ($Base -notmatch "_DEMO$") { throw "La base tiene que ser _DEMO." }

Write-Host "  origen : $Origen"
Write-Host "  destino: $Destino"
Write-Host "  pool   : $Pool"
Write-Host "  base   : $Base en $InstanciaSQL"
Write-Host "  sitio  : https://$Dominio"

Titulo "ARCHIVOS"
Paso "parar el App Pool $Pool" { Stop-WebAppPool -Name $Pool -ErrorAction SilentlyContinue; Start-Sleep -Seconds 2 }
Paso "copiar $Origen -> $Destino" {
    robocopy $Origen $Destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
}

Titulo "CONFIGURACIÓN"
$cfg = [ordered]@{
    ConnectionStrings = [ordered]@{
        Sindicato = "Server=$InstanciaSQL;Database=$Base;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
    }
    AllowedHosts = "*"
    Logging      = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
}
$json = $cfg | ConvertTo-Json -Depth 10
Paso "escribir appsettings.json y appsettings.Production.json (base $Base)" {
    Set-Content (Join-Path $Destino "appsettings.json") -Value $json -Encoding utf8
    Set-Content (Join-Path $Destino "appsettings.Production.json") -Value $json -Encoding utf8
}
$wc = @"
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
Paso "escribir web.config (Production + log de arranque)" {
    Set-Content (Join-Path $Destino "web.config") -Value $wc -Encoding utf8
    New-Item -ItemType Directory -Force -Path (Join-Path $Destino "logs") | Out-Null
}
Paso "permisos para IIS APPPOOL\$Pool" {
    & icacls $Destino /grant "IIS APPPOOL\$Pool:(OI)(CI)F" /T /Q | Out-Null
}

Titulo "ARRANQUE"
Paso "arrancar el App Pool $Pool" { Start-WebAppPool -Name $Pool }

if (-not $Aplicar) {
    Write-Host ""
    Write-Host "  Fue un SIMULACRO. Para desplegar: .\10_desplegar_sigesweb.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Start-Sleep -Seconds 10
Write-Host "  estado: $((Get-WebAppPoolState $Pool).Value)"

Titulo "PRUEBA"
try {
    $r = Invoke-WebRequest "https://$Dominio/" -UseBasicParsing -TimeoutSec 30
    Write-Host "  https://$Dominio -> HTTP $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "  https://$Dominio -> $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  (si es un error de certificado, es el esperado: aceptar la advertencia)"
}
Write-Host ""
Write-Host "  Probá el scroll del Dashboard: https://$Dominio/dashboard" -ForegroundColor Cyan
