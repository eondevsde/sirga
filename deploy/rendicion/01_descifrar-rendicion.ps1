# ==========================================================================================
#  DESCIFRAR LA RENDICIÓN DEL BANCO (GnuPG) Y DEJARLA LISTA PARA SIRGA
#
#  El banco manda tres archivos cifrados con OpenPGP (RSA): uno de PAGOS (.bse / .txt) y dos
#  reportes en PDF para leer. Este script los descifra y los reparte:
#
#     cifrados\*.pgp  ──gpg──►  entrada\        (*.bse, *.txt)  ← SIRGA asienta los pagos
#                        └──►  comprobantes\   (*.pdf)          ← para adjuntar / archivar
#                        └──►  cifrados-procesados\  (los .pgp originales, nunca se borran)
#
#  LA CLAVE: nunca se escribe acá. Se importa UNA vez en un almacén propio (--homedir), que es
#  una carpeta con permisos restringidos. El script sólo la usa.
#
#  PREPARACIÓN (una sola vez, en el servidor, como Administrador)
#    1. Crear el almacén:      gpg --homedir C:\Demo\rendicion\gnupg --list-keys
#    2. Importar la privada:   gpg --homedir C:\Demo\rendicion\gnupg --import mi-clave-privada.asc
#       (o exportarla desde Kleopatra: "Exportar claves secretas")
#    3. Si la clave tiene contraseña, dejarla en un archivo protegido:
#       C:\Demo\rendicion\gnupg\passphrase.txt   (permisos: solo SYSTEM y Administradores)
#    4. Probar con un archivo real antes de programarlo.
#
#  USO
#    .\01_descifrar-rendicion.ps1                    # muestra qué haría (no descifra)
#    .\01_descifrar-rendicion.ps1 -Procesar           # descifra y reparte
#    .\01_descifrar-rendicion.ps1 -Procesar -ProgramarTarea   # además lo programa cada 5 minutos
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$Base = "C:\Demo\rendicion",
    [string]$GpgHomedir = "",
    [string]$GpgExe = "C:\Program Files\GnuPG\bin\gpg.exe",
    [switch]$Procesar,
    [switch]$ProgramarTarea,
    [int]$Minutos = 5
)

$ErrorActionPreference = "Stop"

$cifrados    = Join-Path $Base "cifrados"
$entrada     = Join-Path $Base "entrada"
$comprobantes = Join-Path $Base "comprobantes"
$procesados  = Join-Path $Base "cifrados-procesados"
$errores     = Join-Path $Base "errores"
$log         = Join-Path $Base "descifrado.log"

if (-not $GpgHomedir) { $GpgHomedir = Join-Path $Base "gnupg" }
$passphraseFile = Join-Path $GpgHomedir "passphrase.txt"

function EscribirLog($mensaje) {
    $linea = "{0}  {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $mensaje
    Write-Host "  $mensaje"
    Add-Content -Path $log -Value $linea -Encoding utf8
}

Write-Host ""
Write-Host "== DESCIFRADO DE LA RENDICIÓN DEL BANCO" -ForegroundColor Cyan
Write-Host "  modo          : $(if ($Procesar) { 'PROCESA' } else { 'SIMULACRO (no descifra)' })"
Write-Host "  cifrados      : $cifrados"
Write-Host "  pagos van a  : $entrada"
Write-Host "  PDFs van a   : $comprobantes"
Write-Host "  almacén gpg   : $GpgHomedir"
Write-Host ""

# ------------------------------------------------------------------ controles
if (-not (Test-Path $GpgExe)) {
    throw "No encuentro gpg.exe en $GpgExe. Instalá Gpg4win (o pasá -GpgExe)."
}
foreach ($d in @($cifrados, $entrada, $comprobantes, $procesados, $errores)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
}
if (-not (Test-Path $GpgHomedir)) {
    Write-Host "  AVISO: no existe el almacén $GpgHomedir. Creándolo (después hay que importar la clave)." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $GpgHomedir | Out-Null
}

# ¿está la clave privada en el almacén?
$claves = & $GpgExe --homedir $GpgHomedir --list-secret-keys 2>&1
$tienePrivada = ($LASTEXITCODE -eq 0) -and (($claves -join "`n") -match "\bsec\b|sec#")
if (-not $tienePrivada) {
    Write-Host "  AVISO: el almacén no tiene ninguna clave SECRETA. El descifrado va a fallar." -ForegroundColor Yellow
    Write-Host "         Importarla con: gpg --homedir `"$GpgHomedir`" --import <archivo-de-clave>" -ForegroundColor Yellow
}

# ------------------------------------------------------------------ archivos a procesar
$archivos = @(Get-ChildItem $cifrados -File -ErrorAction SilentlyContinue |
              Where-Object { $_.Extension -match "(?i)^\.(pgp|gpg|asc)$" })

Write-Host "  archivos cifrados pendientes: $($archivos.Count)"
if ($archivos.Count -eq 0) {
    Write-Host "  Nada para hacer."
    exit 0
}

$ok = 0; $fallo = 0
foreach ($archivo in $archivos) {
    $nombrePlano = $archivo.Name -replace "(?i)\.(pgp|gpg|asc)$", ""
    $destinoDir = if ($nombrePlano -match "(?i)\.(bse|txt)$") { $entrada } else { $comprobantes }
    $destino = Join-Path $destinoDir $nombrePlano

    Write-Host ""
    Write-Host "  > $($archivo.Name)" -ForegroundColor Cyan
    Write-Host "      salida: $destino"

    if (-not $Procesar) {
        Write-Host "      (simulacro) se descifraría acá" -ForegroundColor DarkYellow
        continue
    }

    $args = @("--homedir", $GpgHomedir, "--batch", "--yes", "--decrypt", "--output", $destino)
    if (Test-Path $passphraseFile) {
        $args = @("--homedir", $GpgHomedir, "--batch", "--yes", "--pinentry-mode", "loopback",
                  "--passphrase-file", $passphraseFile, "--decrypt", "--output", $destino)
    }
    $args += $archivo.FullName

    $salida = & $GpgExe @args 2>&1
    if ($LASTEXITCODE -eq 0 -and (Test-Path $destino)) {
        $kb = [math]::Round((Get-Item $destino).Length / 1KB, 1)
        Write-Host "      OK -> $([System.IO.Path]::GetFileName($destino)) ($kb KB)" -ForegroundColor Green
        EscribirLog "OK   $($archivo.Name) -> $destino"
        Move-Item $archivo.FullName (Join-Path $procesados $archivo.Name) -Force
        $ok++
    } else {
        $detalle = ($salida | Where-Object { $_ -match "gpg:|error" } | Select-Object -First 2) -join " | "
        Write-Host "      FALLÓ: $detalle" -ForegroundColor Red
        EscribirLog "ERROR $($archivo.Name): $detalle"
        Move-Item $archivo.FullName (Join-Path $errores $archivo.Name) -Force -ErrorAction SilentlyContinue
        $fallo++
    }
}

Write-Host ""
Write-Host "== RESUMEN: $ok descifrado(s), $fallo con error" -ForegroundColor $(if ($fallo -gt 0) { "Yellow" } else { "Green" })

# ------------------------------------------------------------------ tarea programada
if ($ProgramarTarea -and $Procesar) {
    $nombre = "SIRGA - Descifrar rendicion del banco"
    $accion = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Procesar -Base `"$Base`""
    schtasks /Create /TN $nombre /TR $accion /SC MINUTE /MO $Minutos /RU "SYSTEM" /RL HIGHEST /F | Out-Null
    Write-Host "  Tarea programada creada: '$nombre' cada $Minutos minutos (como SYSTEM)." -ForegroundColor Green
    Write-Host "  La clave tiene que estar importada en el almacén $GpgHomedir para la cuenta SYSTEM." -ForegroundColor Yellow
}
