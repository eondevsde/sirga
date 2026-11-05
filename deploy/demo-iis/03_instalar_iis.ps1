# ==========================================================================================
#  03 — INSTALA LAS TRES APLICACIONES EN IIS (versión DEMO, con dominios y HTTPS)
#
#  Deja la demo "como si fuera producción": sitios en IIS, HTTPS, dominios propios y las bases
#  DEMO. No toca los sitios de producción.
#
#  LOS TRES SITIOS (los nombres se pueden cambiar por parámetro)
#    SIRGA_DEMO              -> https://sirga-demo.secsantiago.com.ar
#    SIGESWEB_DEMO           -> https://sigesweb-demo.secsantiago.com.ar
#    SIGESWEB_DEMO_CARNET    -> https://carnet-demo.secsantiago.com.ar   (o el dominio que uses)
#
#  QUÉ HACE
#    1. Copia las tres publicaciones a la carpeta de sitios (robocopy).
#    2. Escribe la configuración de cada sitio (cadenas a las bases DEMO + ApiBaseUrl).
#    3. Crea los App Pools y los sitios con binding HTTP y HTTPS (certificado por dominio).
#    4. Pone ASPNETCORE_ENVIRONMENT=Production y activa el log de stdout (para diagnosticar).
#    5. Da permisos de carpeta a la identidad del App Pool.
#
#  USO (siempre primero en simulacro)
#    .\03_instalar_iis.ps1
#    .\03_instalar_iis.ps1 -Aplicar
#
#  SI LOS SITIOS YA LOS CREASTE CON OTROS NOMBRES, se los indicás:
#    .\03_instalar_iis.ps1 -Aplicar `
#        -SitioSirga "SIRGA_DEMO" -SitioSiges "SIGESWEB_DEMO" -SitioCarnet "sigesweb-demo-carnet" `
#        -DominioCarnet "sigesweb-demo-carnet.secsantiago.com.ar"
#
#  SE PUEDE CORRER DOS VECES: si el sitio ya existe, lo actualiza (no lo duplica).
# ==========================================================================================

[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$Raiz = "C:\Sitios\Demo",
    [string]$OrigenPublicaciones = "",            # si se pasa, copia las carpetas desde acá
    [string]$InstanciaSQL = "localhost",
    [ValidateSet("Windows", "SQL")][string]$Autenticacion = "Windows",
    [string]$UsuarioSQL = "",
    [string]$ClaveSQL = "",

    # Nombres de los sitios en IIS
    [string]$SitioSirga = "SIRGA_DEMO",
    [string]$SitioSiges = "SIGESWEB_DEMO",
    [string]$SitioCarnet = "SIGESWEB_DEMO_CARNET",

    # Carpetas de las publicaciones (si difieren del nombre del sitio)
    [string]$CarpetaSirga = "SIRGA_DEMO",
    [string]$CarpetaSiges = "SIGESWEB_DEMO",
    [string]$CarpetaCarnet = "SIGESWEB_DEMO_CARNET",

    # Dominios
    [string]$DominioSirga = "sirga-demo.secsantiago.com.ar",
    [string]$DominioSiges = "sigesweb-demo.secsantiago.com.ar",
    [string]$DominioCarnet = "carnet-demo.secsantiago.com.ar",

    # Certificados (vacío = se busca por dominio en el almacén)
    [string]$CertificadoSirga = "",
    [string]$CertificadoSiges = "",
    [string]$CertificadoCarnet = "",
    [switch]$SinHttps
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration -ErrorAction Stop

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Hacer($descripcion, [scriptblock]$accion) {
    if ($Aplicar) { & $accion; Write-Host "   OK   $descripcion" -ForegroundColor Green }
    else { Write-Host "   (simulacro) $descripcion" -ForegroundColor DarkYellow }
}

$sitios = @(
    @{ Sitio = $SitioSirga;  Carpeta = $CarpetaSirga;  Dominio = $DominioSirga;  Cert = $CertificadoSirga;  Dll = "SIRGA.WebUI.dll" },
    @{ Sitio = $SitioSiges;  Carpeta = $CarpetaSiges;  Dominio = $DominioSiges;  Cert = $CertificadoSiges;  Dll = "SIGESWEB.Web.dll" },
    @{ Sitio = $SitioCarnet; Carpeta = $CarpetaCarnet; Dominio = $DominioCarnet; Cert = $CertificadoCarnet; Dll = "SIGESWEB.Carnet.Api.dll" }
)

# ============================== GUARDA: SOLO DEMO =========================================
# Este paquete es SÓLO para la demo. Se niega a tocar nada que no sea de demo:
#   - el nombre de cada sitio tiene que contener "demo"
#   - el dominio tiene que contener "demo"
#   - se aborta si alguna carpeta destino ya es la ruta física de un sitio que NO es demo
#     (el robocopy /MIR borra lo que sobra en el destino: así no puede pisar producción)
foreach ($s in $sitios) {
    if ($s.Sitio -notmatch "(?i)demo") { throw "El sitio '$($s.Sitio)' no contiene 'demo': este paquete es sólo para la demo." }
    if ($s.Dominio -notmatch "(?i)demo") { throw "El dominio '$($s.Dominio)' no contiene 'demo': este paquete es sólo para la demo." }
}

foreach ($s in $sitios) {
    $destino = Join-Path $Raiz $s.Sitio
    $sitioExistente = Get-ChildItem IIS:\Sites -ErrorAction SilentlyContinue |
                      Where-Object { $_.physicalPath -and ($_.physicalPath.TrimEnd('\') -eq $destino.TrimEnd('\')) }
    foreach ($se in $sitioExistente) {
        if ($se.Name -notmatch "(?i)demo") {
            throw "La carpeta $destino es la ruta del sitio '$($se.Name)', que NO es demo. Se aborta para no tocar producción."
        }
    }
}

Titulo "MODO $(if ($Aplicar) { 'REAL' } else { 'SIMULACRO' })"
Write-Host "  carpeta de sitios : $Raiz"
Write-Host "  instancia SQL     : $InstanciaSQL  ($Autenticacion)"
foreach ($s in $sitios) { Write-Host ("  {0,-24} -> https://{1}" -f $s.Sitio, $s.Dominio) }
Write-Host "  guarda: sólo sitios y bases de demo" -ForegroundColor Green

if ($Autenticacion -eq "SQL" -and ([string]::IsNullOrWhiteSpace($UsuarioSQL) -or [string]::IsNullOrWhiteSpace($ClaveSQL))) {
    throw "Con -Autenticacion SQL hay que pasar -UsuarioSQL y -ClaveSQL."
}

# ------------------------------------------------------------------ certificados
function BuscarCertificado([string]$dominio, [string]$thumbprint) {
    if ($thumbprint) {
        $c = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $thumbprint }
        if (-not $c) { throw "No encuentro el certificado con huella $thumbprint." }
        return $c
    }
    # *.dominio.com.ar cubre los subdominios con guion
    $partes = $dominio.Split('.')
    $comodin = "*." + ($partes[1..($partes.Count - 1)] -join '.')
    return Get-ChildItem Cert:\LocalMachine\My |
           Where-Object { $_.Subject -match [regex]::Escape($dominio) -or $_.Subject -match [regex]::Escape($comodin) } |
           Sort-Object NotAfter -Descending | Select-Object -First 1
}

# ------------------------------------------------------------------ cadenas
function Cadena([string]$base) {
    if ($Autenticacion -eq "SQL") {
        return "Server=$InstanciaSQL;Database=$base;User Id=$UsuarioSQL;Password=$ClaveSQL;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
    }
    return "Server=$InstanciaSQL;Database=$base;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
}

# ------------------------------------------------------------------ copia y configuración
Titulo "ARCHIVOS Y CONFIGURACIÓN"
foreach ($s in $sitios) {
    $destino = Join-Path $Raiz $s.Sitio
    if ($OrigenPublicaciones) {
        $origen = Join-Path $OrigenPublicaciones $s.Carpeta
        if (-not (Test-Path $origen)) { throw "No existe la publicación de origen: $origen" }
        Hacer "copiar $origen -> $destino" {
            robocopy $origen $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
        }
    } elseif (-not (Test-Path (Join-Path $destino $s.Dll))) {
        throw "No encuentro $($s.Dll) en $destino. Copiá la publicación o pasá -OrigenPublicaciones."
    }

    $esSirga = ($s.Sitio -eq $SitioSirga)
    $config = if ($esSirga) {
        [ordered]@{
            ConnectionStrings = [ordered]@{
                DefaultConnection = (Cadena "SIRGA_DB_DEMO")
                LegacyConnection  = (Cadena "SINDICATO_DEMO")
                Legacy            = (Cadena "SINDICATO_DEMO")
            }
            ApiBaseUrl = "https://$($s.Dominio)/"
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

    Hacer "escribir appsettings.Production.json de $($s.Sitio) (bases DEMO)" {
        $json = $config | ConvertTo-Json -Depth 10
        Set-Content (Join-Path $destino "appsettings.Production.json") -Value $json -Encoding utf8
        Set-Content (Join-Path $destino "appsettings.json") -Value $json -Encoding utf8

        # web.config con entorno explícito y log de arranque: si algo falla, se ve el motivo.
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
        Set-Content (Join-Path $destino "web.config") -Value $webConfig -Encoding utf8
        New-Item -ItemType Directory -Force -Path (Join-Path $destino "logs") | Out-Null
    }
}

# ------------------------------------------------------------------ app pools
Titulo "APP POOLS"
foreach ($s in $sitios) {
    $pool = $s.Sitio
    $existe = (Get-ChildItem IIS:\AppPools -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $pool })
    if (-not $existe) {
        Hacer "crear App Pool $pool (sin código administrado, siempre iniciado)" { New-WebAppPool -Name $pool | Out-Null }
    } else {
        Write-Host "   ya existe: $pool"
    }
    Hacer "configurar App Pool $pool" {
        Set-ItemProperty "IIS:\AppPools\$pool" managedRuntimeVersion ""
        Set-ItemProperty "IIS:\AppPools\$pool" startMode "AlwaysRunning"
        Set-ItemProperty "IIS:\AppPools\$pool" processModel.idleTimeout "00:00:00"
        Set-ItemProperty "IIS:\AppPools\$pool" recycling.periodicRestart.time "00:00:00"
    }
}

# ------------------------------------------------------------------ sitios y bindings
Titulo "SITIOS Y BINDINGS"
foreach ($s in $sitios) {
    $camino = Join-Path $Raiz $s.Sitio
    $existe = (Get-ChildItem IIS:\Sites -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $s.Sitio })
    if (-not $existe) {
        Hacer "crear sitio $($s.Sitio) -> $camino (App Pool $($s.Sitio), puerto 80)" {
            New-Website -Name $s.Sitio -PhysicalPath $camino -ApplicationPool $s.Sitio -Port 80 -HostHeader $s.Dominio | Out-Null
        }
    } else {
        Write-Host "   ya existe: $($s.Sitio)"
        Hacer "actualizar $($s.Sitio) (ruta, App Pool, binding HTTP)" {
            Set-ItemProperty "IIS:\Sites\$($s.Sitio)" physicalPath $camino
            Set-ItemProperty "IIS:\Sites\$($s.Sitio)" applicationPool $s.Sitio
            $b = Get-WebBinding -Name $s.Sitio -Protocol http -ErrorAction SilentlyContinue
            if (-not $b) { New-WebBinding -Name $s.Sitio -Protocol http -Port 80 -HostHeader $s.Dominio | Out-Null }
            else { $b.bindings.Collection[0].bindingInformation = "*:80:$($s.Dominio)" }
        }
    }

    if (-not $SinHttps) {
        $cert = $null
        try { $cert = BuscarCertificado $s.Dominio $s.Cert } catch { Write-Host "   AVISO: $($_.Exception.Message)" -ForegroundColor Yellow }
        if (-not $cert) {
            Write-Host "   AVISO: no encontré certificado para $($s.Dominio): queda sólo HTTP." -ForegroundColor Yellow
        } else {
            Write-Host "   certificado: $($cert.Subject) (vence $($cert.NotAfter.ToString('yyyy-MM-dd')))"
            Hacer "binding HTTPS de $($s.Sitio) en 443" {
                $b = Get-WebBinding -Name $s.Sitio -Protocol https -ErrorAction SilentlyContinue
                if (-not $b) { New-WebBinding -Name $s.Sitio -Protocol https -Port 443 -HostHeader $s.Dominio -SslFlags 1 | Out-Null }
                else { $b.bindings.Collection[0].bindingInformation = "*:443:$($s.Dominio)"; $b.bindings.Collection[0].sslFlags = 1 }
                $b = Get-WebBinding -Name $s.Sitio -Protocol https
                $b.AddSslCertificate($cert.Thumbprint, "my")
            }
        }
    }
}

# ------------------------------------------------------------------ permisos de carpeta
Titulo "PERMISOS DE CARPETA"
foreach ($s in $sitios) {
    $camino = Join-Path $Raiz $s.Sitio
    $identidad = "IIS APPPOOL\$($s.Sitio)"
    Hacer "dar control total a $identidad en $camino" {
        $acl = Get-Acl $camino
        $regla = New-Object System.Security.AccessControl.FileSystemAccessRule($identidad, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($regla)
        Set-Acl $camino $acl
    }
}

# ------------------------------------------------------------------ cierre
Titulo "RESUMEN"
if (-not $Aplicar) {
    Write-Host "  Fue un SIMULACRO. Para instalar de verdad: .\03_instalar_iis.ps1 -Aplicar" -ForegroundColor DarkYellow
    exit 0
}

Write-Host "  Sitios de demo:" -ForegroundColor Cyan
Get-ChildItem IIS:\Sites | Where-Object { $_.Name -match "(?i)demo" } | ForEach-Object {
    $bindings = ($_.Bindings.Collection | ForEach-Object { $_.bindingInformation }) -join "  "
    Write-Host ("   {0,-24} {1}" -f $_.Name, $bindings)
}

Write-Host ""
Write-Host "  Probá en el servidor:" -ForegroundColor Cyan
foreach ($s in $sitios) { Write-Host "   https://$($s.Dominio)" }
Write-Host ""
Write-Host "  Si no responde, el log de arranque está en: $Raiz\<sitio>\logs\stdout_*.log" -ForegroundColor Cyan
Write-Host "  Siguiente paso: 04_verificar.ps1"
