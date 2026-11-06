# ==========================================================================================
#  09 — CERTIFICADO AUTOFIRMADO PARA LOS TRES SITIOS DEMO
#
#  Para cuando no hay certificado válido y hace falta HTTPS YA, sin depender de DNS ni de
#  Let's Encrypt. Crea UN certificado con los tres nombres (SAN) y lo asigna a los tres sitios.
#
#  IMPORTANTE: un certificado autofirmado NO es de confianza para los navegadores por defecto.
#  Dos opciones:
#    a) Aceptar la advertencia una vez en el navegador (Avanzado -> Continuar).
#    b) Instalar el .cer que genera este script como "Entidades de certificación raíz de
#       confianza" EN LA MÁQUINA QUE PROYECTA. Ahí no aparece ninguna advertencia.
#
#  USO (en el servidor, PowerShell como administrador)
#    .\09_certificado_autofirmado.ps1                     # simulacro
#    .\09_certificado_autofirmado.ps1 -Aplicar
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string[]]$Nombres = @(
        "sirga-demo.secsantiago.com.ar",
        "sigesweb-demo.secsantiago.com.ar",
        "sigesweb-demo-carnet.secsantiago.com.ar"
    ),
    [string]$Sitios = "sirga-demo,sigesweb-demo,sigesweb-demo-carnet",
    [string]$ArchivoCer = "C:\Demo\secsantiago-demo.cer",
    [int]$Anios = 2
)

$ErrorActionPreference = "Stop"
try { Import-Module WebAdministration -ErrorAction Stop }
catch { throw "Correr en el SERVIDOR con PowerShell ABIERTO COMO ADMINISTRADOR (el modulo de IIS necesita permisos elevados)." }

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

$listaSitios = $Sitios.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ }

Titulo "DATOS"
Write-Host "  nombres : $($Nombres -join ', ')"
Write-Host "  sitios  : $($listaSitios -join ', ')"
Write-Host "  vence   : $(Get-Date).AddYears($Anios).ToString('yyyy-MM-dd')"
Write-Host "  .cer    : $ArchivoCer"

if (-not $Aplicar) {
    Write-Host ""
    Write-Host "  (simulacro) New-SelfSignedCertificate con esos tres nombres"
    Write-Host "  (simulacro) asignarlo a: $($listaSitios -join ', ')"
    Write-Host "  (simulacro) exportar el .cer a $ArchivoCer e importarlo al almacen de confianza del servidor"
    Write-Host "  (simulacro) iisreset"
    Write-Host ""
    Write-Host "  Para hacerlo: .\09_certificado_autofirmado.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Titulo "CREANDO EL CERTIFICADO"
$existente = Get-ChildItem Cert:\LocalMachine\My |
             Where-Object { $_.FriendlyName -eq "SEC Santiago DEMO" -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
             Sort-Object NotAfter -Descending | Select-Object -First 1

if ($existente) {
    $cert = $existente
    Write-Host "  reutilizo el existente: $($cert.Subject) (vence $($cert.NotAfter.ToString('yyyy-MM-dd')))"
} else {
    $cert = New-SelfSignedCertificate `
        -DnsName $Nombres `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -FriendlyName "SEC Santiago DEMO" `
        -NotAfter (Get-Date).AddYears($Anios) `
        -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy Exportable
    Write-Host "  creado: $($cert.Subject) | huella $($cert.Thumbprint)" -ForegroundColor Green
}

Titulo "ASIGNANDO A LOS SITIOS"
foreach ($s in $listaSitios) {
    $sitio = Get-Website -Name $s -ErrorAction SilentlyContinue
    if (-not $sitio) { Write-Host "  AVISO: no existe el sitio $s" -ForegroundColor Yellow; continue }

    $b = Get-WebBinding -Name $s -Protocol https -ErrorAction SilentlyContinue
    if (-not $b) {
        $dominio = ($Nombres | Where-Object { $_ -like "$(($s -replace '-demo',''))*" } | Select-Object -First 1)
        if (-not $dominio) { $dominio = $Nombres[0] }
        New-WebBinding -Name $s -Protocol https -Port 443 -HostHeader $dominio -SslFlags 1 | Out-Null
        $b = Get-WebBinding -Name $s -Protocol https
        Write-Host "  binding HTTPS creado para $s ($dominio)"
    }
    $b.AddSslCertificate($cert.Thumbprint, "my")
    Write-Host "  certificado asignado a $s" -ForegroundColor Green
}

Titulo "EXPORTANDO EL .CER"
New-Item -ItemType Directory -Force -Path (Split-Path $ArchivoCer -Parent) | Out-Null
Export-Certificate -Cert $cert -FilePath $ArchivoCer -Type CERT | Out-Null
Write-Host "  guardado en $ArchivoCer" -ForegroundColor Green

# En el propio servidor conviene confiar en el certificado (para que las pruebas den sin error)
$enRaiz = Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
if (-not $enRaiz) {
    Import-Certificate -FilePath $ArchivoCer -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
    Write-Host "  importado como confiable en este servidor"
}

Titulo "REINICIANDO IIS"
iisreset | Out-Null
Start-Sleep -Seconds 5
Write-Host "  iisreset hecho"

Titulo "VERIFICACIÓN"
foreach ($n in $Nombres) {
    try {
        $r = Invoke-WebRequest "https://$n/" -UseBasicParsing -TimeoutSec 25
        Write-Host "  https://$n -> HTTP $($r.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "  https://$n -> $($_.Exception.Message)" -ForegroundColor Red
    }
}

Titulo "LISTO"
Write-Host "  Para que NO haya advertencia en la máquina que proyecta:" -ForegroundColor Cyan
Write-Host "    1. Copiar $ArchivoCer a esa máquina."
Write-Host "    2. Doble clic -> Instalar certificado -> Equipo local ->"
Write-Host "       'Colocar todos los certificados en el siguiente almacén' ->"
Write-Host "       'Entidades de certificación raíz de confianza' -> Aceptar."
Write-Host "    3. Cerrar y abrir el navegador."
Write-Host ""
Write-Host "  Si no se instala, el navegador mostrará una advertencia que se saltea con"
Write-Host "  'Avanzado' -> 'Continuar'."
