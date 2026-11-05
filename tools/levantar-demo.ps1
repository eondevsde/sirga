# ==========================================================================================
#  LEVANTAR LA DEMO — un solo comando, verificado
#
#  Levanta SIRGA contra las bases DEMO (nunca contra las reales) y verifica que responda.
#  Sirve después de reiniciar la máquina o si la app se cerró.
#
#  USO
#    .\tools\levantar-demo.ps1
#    .\tools\levantar-demo.ps1 -ConSigesweb        # además levanta SIGESWEB en 5300
#
#  QUÉ HACE
#    1. Baja cualquier instancia previa de la demo (para no dejar DLL bloqueados).
#    2. Publica la versión actual del código en C:\Proyectos\SIRGA_DEMO (Release).
#    3. Borra appsettings.Development.json de la publicación (apunta a la base real).
#    4. Levanta SIRGA en http://localhost:5200 contra SIRGA_DB_DEMO + SINDICATO_DEMO.
#    5. Verifica: raíz, CSS, login y que la conexión sea a la base DEMO.
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$ConSigesweb,
    [int]$PuertoSirga = 5200,
    [int]$PuertoSiges = 5300,
    [switch]$SinPublicar
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$publicacion = Join-Path $raiz "SIRGA_DEMO"
$siges = "C:\Proyectos\SIGESWEB_DEMO"

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

function BajarApp([string]$patron) {
    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object { $_.CommandLine -like $patron } |
        ForEach-Object { Write-Host "  bajando PID $($_.ProcessId)"; Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

function EsperarUrl([string]$url, [int]$segundos = 60) {
    $fin = (Get-Date).AddSeconds($segundos)
    while ((Get-Date) -lt $fin) {
        try {
            $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 10
            if ($r.StatusCode -eq 200) { return $true }
        } catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

Titulo "BAJANDO LAS INSTANCIAS ANTERIORES"
BajarApp "*SIRGA_DEMO*"
BajarApp "*SIGESWEB_DEMO*"
Start-Sleep -Seconds 4

if (-not $SinPublicar) {
    Titulo "PUBLICANDO LA VERSIÓN ACTUAL (Release)"
    $out = & dotnet publish (Join-Path $raiz "SIRGA.WebUI\SIRGA.WebUI.csproj") -c Release -o $publicacion --nologo 2>&1
    $errores = @($out | Where-Object { $_ -match ': error ' })
    if ($errores.Count -gt 0) {
        $errores | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        throw "Falló la publicación."
    }
    Write-Host "  publicación OK" -ForegroundColor Green
}

# El publish vuelve a traer el appsettings de desarrollo, que apunta a la base REAL.
$dev = Join-Path $publicacion "appsettings.Development.json"
if (Test-Path $dev) { Remove-Item $dev -Force; Write-Host "  appsettings.Development.json eliminado" -ForegroundColor Yellow }

$puertoSirga = $PuertoSirga

Titulo "ESCRIBIENDO LA CONFIGURACIÓN DEMO (el publish la pisa con la del repo)"
# El publish copia appsettings.json del repo, que apunta a SIRGA_DB (la REAL) y a Sindicato.
# Acá se deja la configuración de la demo en los DOS archivos, para que no importe cuál gane.
$configDemo = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = "Server=localhost;Database=SIRGA_DB_DEMO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
        LegacyConnection  = "Server=localhost;Database=SINDICATO_DEMO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
        Legacy            = "Server=localhost;Database=SINDICATO_DEMO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
    }
    ApiBaseUrl   = "http://localhost:$puertoSirga/"
    AllowedHosts = "*"
    Logging      = [ordered]@{ LogLevel = [ordered]@{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
}
$json = $configDemo | ConvertTo-Json -Depth 10
foreach ($nombre in @("appsettings.json", "appsettings.Production.json")) {
    Set-Content (Join-Path $publicacion $nombre) -Value $json -Encoding utf8
}
Write-Host "  las tres cadenas apuntan a SIRGA_DB_DEMO / SINDICATO_DEMO" -ForegroundColor Green

Titulo "LEVANTANDO SIRGA (demo) EN http://localhost:$PuertoSirga"
$env:ASPNETCORE_ENVIRONMENT = "Production"
Start-Process dotnet -ArgumentList "`"$(Join-Path $publicacion 'SIRGA.WebUI.dll')`" --urls `"http://localhost:$PuertoSirga`"" `
    -WorkingDirectory $publicacion -WindowStyle Hidden

if (-not (EsperarUrl "http://localhost:$PuertoSirga/" 90)) { throw "SIRGA no respondió en 90 segundos." }
Write-Host "  SIRGA responde" -ForegroundColor Green

if ($ConSigesweb) {
    Titulo "LEVANTANDO SIGESWEB (demo) EN http://localhost:$PuertoSiges"
    $clave = (Get-Content (Join-Path $raiz ".dsh-tools\.sqlpwd_local") -Raw).Trim()
    $env:ConnectionStrings__Sindicato = "Server=localhost;Database=SINDICATO_DEMO;User ID=sirga_local;Password=$clave;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False;Application Name=SIGESWEB-demo"
    Start-Process dotnet -ArgumentList "`"$(Join-Path $siges 'SIGESWEB.Web.dll')`" --urls `"http://localhost:$PuertoSiges`"" `
        -WorkingDirectory $siges -WindowStyle Hidden
    if (EsperarUrl "http://localhost:$PuertoSiges/" 90) { Write-Host "  SIGESWEB responde" -ForegroundColor Green }
    else { Write-Host "  SIGESWEB NO respondió" -ForegroundColor Red }
}

Titulo "VERIFICACIÓN"
try {
    $r = Invoke-WebRequest "http://localhost:$PuertoSirga/" -UseBasicParsing -TimeoutSec 15
    Write-Host "  raíz .......... HTTP $($r.StatusCode)"
} catch { Write-Host "  raíz .......... ERROR" -ForegroundColor Red }

try {
    $c = Invoke-WebRequest "http://localhost:$PuertoSirga/_content/MudBlazor/MudBlazor.min.css" -UseBasicParsing -TimeoutSec 15
    Write-Host "  estilos ....... HTTP $($c.StatusCode) ($([math]::Round($c.RawContentLength/1KB,0)) KB)"
} catch { Write-Host "  estilos ....... ERROR (la pantalla se vería sin formato)" -ForegroundColor Red }

try {
    $l = Invoke-WebRequest "http://localhost:$PuertoSirga/login" -UseBasicParsing -TimeoutSec 15
    Write-Host "  login ......... HTTP $($l.StatusCode)"
} catch { Write-Host "  login ......... ERROR" -ForegroundColor Red }

Write-Host ""
Write-Host "  A qué base está conectada:" -ForegroundColor Cyan
& sqlcmd -S localhost -E -W -h -1 -Q "SET NOCOUNT ON; SELECT DISTINCT '    ' + CAST(DB_NAME(database_id) AS varchar(30)) FROM sys.dm_exec_sessions WHERE is_user_process = 1 AND program_name LIKE '%EFCore%';" 2>&1

Write-Host ""
Write-Host "  DEMO LISTA: http://localhost:$PuertoSirga" -ForegroundColor Green
Write-Host "  Usuario administrador: admin@sirga.com   /  12345" -ForegroundColor Green
Write-Host "  Usuario de estudio   : cualquier email de estudio / 12345" -ForegroundColor Green
