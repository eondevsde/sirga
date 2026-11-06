# ==========================================================================================
#  08 — DESPLEGAR EN EL SERVIDOR (el que se usa después de cada corrección)
#
#  Reemplaza el contenido de la carpeta del sitio con la publicación autocontenida, escribe la
#  configuración DEMO, deja el web.config con el .exe, arregla los permisos del App Pool,
#  reinicia el pool y hace la prueba HTTP.
#
#  USO (en el servidor)
#    .\08_desplegar_en_servidor.ps1                          # simulacro
#    .\08_desplegar_en_servidor.ps1 -Aplicar                  # despliega SIRGA demo
#    .\08_desplegar_en_servidor.ps1 -Aplicar -Backup          # además guarda la carpeta anterior
#
#  POR QUÉ AUTOCONTENIDO: la publicación lleva su propio .NET. IIS sólo ejecuta el .exe,
#  así no depende de que el servidor resuelva Microsoft.AspNetCore.App (eso daba 500.31).
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [switch]$Backup,
    [string]$Origen = "C:\Publicaciones\SIRGA_DEMO_SC",
    [string]$Destino = "C:\inetpub\sirga-demo",
    [string]$Pool = "sirga-demo",
    [string]$Dominio = "sirga-demo.secsantiago.com.ar",
    [string]$InstanciaSQL = ".\SQLEXPRESS",
    [string]$BaseSirga = "SIRGA_DB_DEMO",
    [string]$BaseLegado = "SINDICATO_DEMO"
)

$ErrorActionPreference = "Stop"
try {
    Import-Module WebAdministration -ErrorAction Stop
} catch {
    throw "Este script tiene que correr en el SERVIDOR con PowerShell ABIERTO COMO ADMINISTRADOR (el modulo de IIS necesita permisos elevados)."
}

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Paso($descripcion, [scriptblock]$accion) {
    if ($Aplicar) { & $accion; Write-Host "   OK   $descripcion" -ForegroundColor Green }
    else { Write-Host "   (simulacro) $descripcion" -ForegroundColor DarkYellow }
}

# ------------------------------------------------------------------ controles
Titulo "CONTROLES"
if (-not (Test-Path (Join-Path $Origen "SIRGA.WebUI.exe"))) {
    throw "No encuentro SIRGA.WebUI.exe en $Origen. Corré primero 07_publicar_desde_dev.ps1."
}
$cantidadOrigen = (Get-ChildItem $Origen -Recurse -File).Count
if ($cantidadOrigen -lt 500) { throw "La publicación de origen parece incompleta ($cantidadOrigen archivos)." }
if (-not (Test-Path $Destino)) { throw "No existe la carpeta destino: $Destino" }
if ($Destino -notmatch "(?i)demo") { throw "La carpeta destino no es de demo: $Destino. Se aborta." }
if ($BaseSirga -notmatch "_DEMO$" -or $BaseLegado -notmatch "_DEMO$") { throw "Las bases tienen que ser _DEMO." }

Write-Host "  origen  : $Origen ($cantidadOrigen archivos)"
Write-Host "  destino : $Destino"
Write-Host "  pool    : $Pool"
Write-Host "  bases   : $BaseSirga + $BaseLegado en $InstanciaSQL"
Write-Host "  sitio   : https://$Dominio"

# ------------------------------------------------------------------ backup
if ($Backup) {
    $copia = "C:\Demo\backup-$(Split-Path $Destino -Leaf)-$(Get-Date -Format yyyyMMdd-HHmm)"
    Paso "guardar copia de la carpeta actual en $copia" {
        robocopy $Destino $copia /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    }
}

# ------------------------------------------------------------------ copia
Titulo "ARCHIVOS"
Paso "parar el App Pool $Pool" { Stop-WebAppPool -Name $Pool -ErrorAction SilentlyContinue; Start-Sleep -Seconds 2 }
Paso "vaciar $Destino" { Get-ChildItem $Destino -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }
Paso "copiar $Origen -> $Destino" {
    robocopy $Origen $Destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
}

# ------------------------------------------------------------------ configuración
Titulo "CONFIGURACIÓN"
$cadena = "Server=$InstanciaSQL;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
$cfg = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = ($cadena -f $BaseSirga)
        LegacyConnection  = ($cadena -f $BaseLegado)
        Legacy            = ($cadena -f $BaseLegado)
    }
    ApiBaseUrl   = "https://$Dominio/"
    AllowedHosts = "*"
    Logging      = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
}
$json = $cfg | ConvertTo-Json -Depth 10

Paso "escribir appsettings.json y appsettings.Production.json (bases DEMO)" {
    Set-Content (Join-Path $Destino "appsettings.json") -Value $json -Encoding utf8
    Set-Content (Join-Path $Destino "appsettings.Production.json") -Value $json -Encoding utf8
}

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

Paso "escribir web.config (processPath=.\SIRGA.WebUI.exe)" {
    Set-Content (Join-Path $Destino "web.config") -Value $webConfig -Encoding utf8
    New-Item -ItemType Directory -Force -Path (Join-Path $Destino "logs") | Out-Null
}

# ------------------------------------------------------------------ permisos
Titulo "PERMISOS"
$identidad = "IIS APPPOOL\$Pool"
Paso "dar control total a $identidad en $Destino" {
    & icacls $Destino /grant "${identidad}:(OI)(CI)F" /T /Q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls falló con código $LASTEXITCODE" }
}

# ------------------------------------------------------------------ arranque y prueba
Titulo "ARRANQUE"
Paso "arrancar el App Pool $Pool" { Start-WebAppPool -Name $Pool }

if (-not $Aplicar) {
    Write-Host ""
    Write-Host "  Fue un SIMULACRO. Para desplegar de verdad: .\08_desplegar_en_servidor.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Start-Sleep -Seconds 10
Write-Host ""
Write-Host "  estado del pool: $((Get-WebAppPoolState $Pool).Value)"

Titulo "PRUEBA"
$ok = $false
foreach ($url in @("https://$Dominio", "http://$Dominio")) {
    try {
        $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 30
        Write-Host "  $url -> HTTP $($r.StatusCode)" -ForegroundColor Green
        if ($r.StatusCode -eq 200) { $ok = $true; break }
    } catch {
        $msg = $_.Exception.Message
        if ($msg -match "SSL|TLS|confianza") {
            # puede ser certificado: se reintenta aceptándolo para ver si la app responde
            $prev = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
            try {
                [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { param($a, $b, $c, $d) return $true }
                $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 30
                Write-Host "  $url -> HTTP $($r.StatusCode) (certificado no confiable: falta asignarlo)" -ForegroundColor Yellow
                $ok = $true; break
            } catch { Write-Host "  $url -> ERROR: $($_.Exception.Message)" -ForegroundColor Red }
            finally { [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $prev }
        } else {
            Write-Host "  $url -> ERROR: $msg" -ForegroundColor Red
        }
    }
}

if (-not $ok) {
    Write-Host ""
    Write-Host "  La app no respondió. Mirá el log de arranque:" -ForegroundColor Yellow
    Write-Host "    Get-Content (Get-ChildItem $Destino\logs\*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 40"
    exit 1
}

Titulo "LISTO"
Write-Host "  https://$Dominio  ->  admin@sirga.com / 12345" -ForegroundColor Green
Write-Host "  Control completo: .\04_verificar.ps1"
