# Actualizar SIRGA: detener -> build -> test -> relanzar
# Uso:   .\actualizar.ps1
# Flags: .\actualizar.ps1 -NoTest   (omite los tests)
#        .\actualizar.ps1 -NoRun    (no relanza la app)

param(
    [switch]$NoTest,
    [switch]$NoRun
)

$ErrorActionPreference = "Stop"
$root = "C:\Proyectos\SIRGA"
$url = "http://localhost:5122"
$log = Join-Path $root "actualizar.log"

function Log($msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -LiteralPath $log -Value $line
}

Write-Host "=== Actualizar SIRGA ===" -ForegroundColor Cyan
Start-Sleep -Seconds 1

# 1) Detener la app si esta corriendo (evita MSB3021)
$p = Get-Process -Name SIRGA.WebUI -ErrorAction SilentlyContinue
if ($p) {
    Log "Deteniendo SIRGA.WebUI (PID $($p.Id -join ','))..."
    $p | Stop-Process -Force
    Start-Sleep -Seconds 2
} else {
    Log "SIRGA.WebUI no estaba corriendo."
}

# 2) Build solucion + WebUI (WebUI no esta en SIRGA.slnx)
Log "Compilando SIRGA.slnx..."
& dotnet build (Join-Path $root "SIRGA.slnx") --nologo -v q | Tee-Object -Variable outBuild | Out-Host
if ($LASTEXITCODE -ne 0) { Log "ERROR: build de solucion fallo."; exit 1 }

Log "Compilando SIRGA.WebUI..."
& dotnet build (Join-Path $root "SIRGA.WebUI\SIRGA.WebUI.csproj") --nologo -v q | Tee-Object -Variable outWeb | Out-Host
if ($LASTEXITCODE -ne 0) { Log "ERROR: build de WebUI fallo."; exit 1 }

# 3) Tests (opcional)
if (-not $NoTest) {
    Log "Ejecutando tests..."
    & dotnet test (Join-Path $root "SIRGA.Tests\SIRGA.Tests.csproj") --nologo -v q | Tee-Object -Variable outTest | Out-Host
    if ($LASTEXITCODE -ne 0) { Log "ERROR: tests fallaron."; exit 1 }
} else {
    Log "Tests omitidos (-NoTest)."
}

# 4) Relanzar la app
if (-not $NoRun) {
    Log "Lanzando SIRGA.WebUI en $url ..."
    Start-Process dotnet -ArgumentList "run --project `"$root\SIRGA.WebUI\SIRGA.WebUI.csproj`" --launch-profile http" -WorkingDirectory $root -WindowStyle Hidden

    # Esperar hasta que responda (max 60s)
    $ok = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 1
        try {
            $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($r.StatusCode -eq 200) { $ok = $true; break }
        } catch { }
    }
    if ($ok) {
        Log "OK: app respondiendo en $url"
        Write-Host "`nActualizacion completa. App en $url" -ForegroundColor Green
    } else {
        Log "AVISO: la app no respondio en $url tras 60s. Revisa $log"
        Write-Host "`nLa app no respondio. Revisa el log: $log" -ForegroundColor Yellow
    }
} else {
    Log "App no relanzada (-NoRun)."
}

Write-Host "Log completo: $log" -ForegroundColor DarkGray