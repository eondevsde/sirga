# ==========================================================================================
#  03 — INSTALA LAS TRES APLICACIONES EN IIS (versión DEMO, con dominios y HTTPS)
#
#  Deja la demo "como si fuera producción": sitios en IIS, HTTPS, dominios propios y las bases
#  DEMO. No toca los sitios de producción.
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
#    .\03_instalar_iis.ps1 -Aplicar -InstanciaSQL ".\SQLEXPRESS" -Autenticacion SQL -UsuarioSQL sqladmin -ClaveSQL "..."
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
    [string]$DominioSirga = "sirga-demo.secsantiago.com.ar",
    [string]$DominioSiges = "sigesweb-demo.secsantiago.com.ar",
    [string]$DominioCarnet = "carnet-demo.secsantiago.com.ar",
    [string]$CertificadoSirga = "",               # thumbprint; si está vacío se busca por dominio
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
    @{ Nombre = "SIRGA_DEMO";              App = "SIRGA_DEMO";              Dominio = $DominioSirga;  Cert = $CertificadoSirga;  Dll = "SIRGA.WebUI.dll" },
    @{ Nombre = "SIGESWEB_DEMO";           App = "SIGESWEB_DEMO";           Dominio = $DominioSiges;  Cert = $CertificadoSiges;  Dll = "SIGESWEB.Web.dll" },
    @{ Nombre = "SIGESWEB_DEMO_CARNET";    App = "SIGESWEB_DEMO_CARNET";    Dominio = $DominioCarnet; Cert = $CertificadoCarnet; Dll = "SIGESWEB.Carnet.Api.dll" }
)

Titulo "MODO $(if ($Aplicar) { 'REAL' } else { 'SIMULACRO' })"
Write-Host "  carpeta de sitios : $Raiz"
Write-Host "  instancia SQL     : $InstanciaSQL  ($Autenticacion)"
Write-Host "  dominios          : $DominioSirga | $DominioSiges | $DominioCarnet"

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
    $c = Get-ChildItem Cert:\LocalMachine\My |
         Where-Object { $_.Subject -match [regex]::Escape($dominio) -or $_.Subject -match [regex]::Escape($comodin) } |
         Sort-Object NotAfter -Descending | Select-Object -First 1
    return $c
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
    $destino = Join-Path $Raiz $s.Nombre
    if ($OrigenPublicaciones) {
        $origen = Join-Path $OrigenPublicaciones $s.Nombre
        if (-not (Test-Path $origen)) { throw "No existe la publicación de origen: $origen" }
        Hacer "copiar $origen -> $destino" {
            robocopy $origen $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }
        }
    } elseif (-not (Test-Path (Join-Path $destino $s.Dll))) {
        throw "No encuentro $($s.Dll) en $destino. Copiá la publicación o pasá -OrigenPublicaciones."
    }

    # Configuración según el sitio
    $config = if ($s.Nombre -eq "SIRGA_DEMO") {
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

    Hacer "escribir appsettings.Production.json de $($s.Nombre) (bases DEMO)" {
        $json = $config | ConvertTo-Json -Depth 10
        Set-Content (Join-Path $destino "appsettings.Production.json") -Value $json -Encoding utf8
        Set-Content (Join-Path $destino "appsettings.json") -Value $json -Encoding utf8

        # Un web.config con entorno explícito y log de arranque: si algo falla, se ve el motivo.
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
    $existe = (Get-ChildItem IIS:\AppPools -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $s.App })
    if (-not $existe) {
        Hacer "crear App Pool $($s.App) (sin código administrado, siempre iniciado)" {
            New-WebAppPool -Name $s.App | Out-Null
        }
    } else {
        Write-Host "   ya existe: $($s.App)"
    }
    Hacer "configurar App Pool $($s.App)" {
        Set-ItemProperty "IIS:\AppPools\$($s.App)" managedRuntimeVersion ""
        Set-ItemProperty "IIS:\AppPools\$($s.App)" startMode "AlwaysRunning"
        Set-ItemProperty "IIS:\AppPools\$($s.App)" processModel.idleTimeout "00:00:00"
        Set-ItemProperty "IIS:\AppPools\$($s.App)" recycling.periodicRestart.time "00:00:00"
    }
}

# ------------------------------------------------------------------ sitios y bindings
Titulo "SITIOS Y BINDINGS"
foreach ($s in $sitios) {
    $camino = Join-Path $Raiz $s.Nombre
    $existe = (Get-ChildItem IIS:\Sites -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $s.Nombre })
    if (-not $existe) {
        Hacer "crear sitio $($s.Nombre) -> $camino (App Pool $($s.App), puerto 80)" {
            New-Website -Name $s.Nombre -PhysicalPath $camino -ApplicationPool $s.App -Port 80 -HostHeader $s.Dominio | Out-Null
        }
    } else {
        Write-Host "   ya existe: $($s.Nombre)"
        Hacer "actualizar $($s.Nombre) (ruta, App Pool, binding HTTP)" {
            Set-ItemProperty "IIS:\Sites\$($s.Nombre)" physicalPath $camino
            Set-ItemProperty "IIS:\Sites\$($s.Nombre)" applicationPool $s.App
            $b = Get-WebBinding -Name $s.Nombre -Protocol http -ErrorAction SilentlyContinue
            if (-not $b) { New-WebBinding -Name $s.Nombre -Protocol http -Port 80 -HostHeader $s.Dominio | Out-Null }
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
            Hacer "binding HTTPS de $($s.Nombre) en 443" {
                $b = Get-WebBinding -Name $s.Nombre -Protocol https -ErrorAction SilentlyContinue
                if (-not $b) { New-WebBinding -Name $s.Nombre -Protocol https -Port 443 -HostHeader $s.Dominio -SslFlags 1 | Out-Null }
                else { $b.bindings.Collection[0].bindingInformation = "*:443:$($s.Dominio)"; $b.bindings.Collection[0].sslFlags = 1 }
                $b = Get-WebBinding -Name $s.Nombre -Protocol https
                $b.AddSslCertificate($cert.Thumbprint, "my")
            }
        }
    }
}

# ------------------------------------------------------------------ permisos de carpeta
Titulo "PERMISOS DE CARPETA"
foreach ($s in $sitios) {
    $camino = Join-Path $Raiz $s.Nombre
    $identidad = "IIS APPPOOL\$($s.App)"
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

Write-Host "  Sitios:" -ForegroundColor Cyan
Get-ChildItem IIS:\Sites | Where-Object { $_.Name -like "*DEMO*" } | ForEach-Object {
    $bindings = ($_.Bindings.Collection | ForEach-Object { $_.bindingInformation }) -join "  "
    Write-Host ("   {0,-24} {1}" -f $_.Name, $bindings)
}

Write-Host ""
Write-Host "  Probá en el servidor:" -ForegroundColor Cyan
Write-Host "   https://$DominioSirga"
Write-Host "   https://$DominioSiges"
Write-Host "   https://$DominioCarnet"
Write-Host ""
Write-Host "  Si no responde, el log de arranque está en: $Raiz\<sitio>\logs\stdout_*.log" -ForegroundColor Cyan
Write-Host "  Siguiente paso: 04_verificar.ps1"
