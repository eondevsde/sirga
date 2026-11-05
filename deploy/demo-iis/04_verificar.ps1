# ==========================================================================================
#  04 — VERIFICA LA DEMO YA INSTALADA EN IIS
#
#  Comprueba, en el servidor: sitios de IIS, bindings, HTTPS, las bases DEMO y que las
#  aplicaciones respondan. Sirve como última puerta antes de presentar.
#
#  USO
#    .\04_verificar.ps1
#    .\04_verificar.ps1 -Raiz "C:\Sitios\Demo"
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$Raiz = "C:\Sitios\Demo",
    [string]$SitioSirga = "sirga-demo",
    [string]$SitioSiges = "sigesweb-demo",
    [string]$SitioCarnet = "sigesweb-demo-carnet",
    [string]$CarpetaSirga = "SIRGA_DEMO",
    [string]$CarpetaSiges = "SIGESWEB_DEMO",
    [string]$CarpetaCarnet = "SIGESWEB_DEMO_CARNET",
    [string]$DominioSirga = "sirga-demo.secsantiago.com.ar",
    [string]$DominioSiges = "sigesweb-demo.secsantiago.com.ar",
    [string]$DominioCarnet = "sigesweb-demo-carnet.secsantiago.com.ar",
    [switch]$SinHttps
)

$ErrorActionPreference = "Continue"
$errores = New-Object System.Collections.Generic.List[string]
$avisos = New-Object System.Collections.Generic.List[string]
$bien = New-Object System.Collections.Generic.List[string]

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

Import-Module WebAdministration -ErrorAction SilentlyContinue

# ---------------------------------------------------------------- IIS
Titulo "IIS"
foreach ($nombre in @($SitioSirga, $SitioSiges, $SitioCarnet)) {
    $sitio = Get-ChildItem IIS:\Sites -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $nombre }
    if (-not $sitio) { $errores.Add("No existe el sitio $nombre en IIS."); continue }
    $bien.Add("Sitio $nombre existe")
    if ($sitio.State -ne "Started") {
        $avisos.Add("El sitio $nombre está $($sitio.State): iniciarlo con Start-Website -Name $nombre")
    } else { $bien.Add("Sitio $nombre iniciado") }
    $bindings = ($sitio.Bindings.Collection | ForEach-Object { $_.bindingInformation }) -join "  "
    $bien.Add("Bindings: $bindings")
}

# ---------------------------------------------------------------- certificados y HTTPS
if (-not $SinHttps) {
    Titulo "HTTPS"
    foreach ($par in @(@{n = $SitioSirga; d = $DominioSirga }, @{n = $SitioSiges; d = $DominioSiges }, @{n = $SitioCarnet; d = $DominioCarnet })) {
        $https = Get-WebBinding -Name $par.n -Protocol https -ErrorAction SilentlyContinue
        if (-not $https) { $errores.Add("El sitio $($par.n) no tiene binding HTTPS para $($par.d).") }
        else {
            $bien.Add("HTTPS configurado para $($par.d)")
            $thumb = (Get-ItemProperty "IIS:\Sites\$($par.n)" -ErrorAction SilentlyContinue).sslFlags
            $cert = Get-ChildItem Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
                    Where-Object { $_.Subject -match [regex]::Escape($par.d) -or $_.Subject -match [regex]::Escape("*." + ($par.d.Split('.')[1..10] -join '.')) } |
                    Sort-Object NotAfter -Descending | Select-Object -First 1
            if (-not $cert) {
                $todos = Get-ChildItem Cert:\LocalMachine\My -ErrorAction SilentlyContinue
                if ($todos.Count -eq 0) { $avisos.Add("No hay NINGÚN certificado en el almacén del servidor (Cert:\LocalMachine\My).") }
                else { $avisos.Add("No hay certificado que cubra $($par.d). Hay $($todos.Count) en el almacén: $(($todos | ForEach-Object { $_.Subject }) -join ' | ')") }
            }
            elseif ($cert.NotAfter -lt (Get-Date).AddDays(15)) { $avisos.Add("El certificado de $($par.d) vence el $($cert.NotAfter.ToString('yyyy-MM-dd')).") }
            else { $bien.Add("Certificado de $($par.d) vence $($cert.NotAfter.ToString('yyyy-MM-dd'))") }
        }
    }
}

# ---------------------------------------------------------------- configuración
Titulo "CONFIGURACIÓN DE CADA SITIO"
$cadenas = @{}
foreach ($nombre in @($SitioSirga, $SitioSiges, $SitioCarnet)) {
    # La carpeta se toma del propio sitio en IIS (no se supone): así funciona con cualquier
    # ubicación, por ejemplo C:\inetpub\<sitio>.
    $sitioIis = Get-WebSite -Name $nombre -ErrorAction SilentlyContinue
    $carpeta = if ($sitioIis -and $sitioIis.physicalPath) { $sitioIis.physicalPath } else { Join-Path $Raiz $nombre }
    $cfgPath = Join-Path $carpeta "appsettings.Production.json"
    if (-not (Test-Path $cfgPath)) { $errores.Add("Falta $cfgPath"); continue }
    $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
    foreach ($p in $cfg.ConnectionStrings.PSObject.Properties) {
        $cadenas["$nombre/$($p.Name)"] = $p.Value
        if ($p.Value -match "(?i)(Database|Initial Catalog)\s*=\s*([^;]+)") {
            $base = $Matches[2]
            if ($base -notmatch "_DEMO$") { $errores.Add("$nombre/$($p.Name) apunta a la base '$base' (no es DEMO).") }
            else { $bien.Add("$nombre/$($p.Name) -> $base") }
        }
    }
    $dev = Join-Path $carpeta "appsettings.Development.json"
    if (Test-Path $dev) { $avisos.Add("$nombre tiene appsettings.Development.json (no debería).") }
    $wc = Join-Path $carpeta "web.config"
    if (Test-Path $wc) {
        $texto = Get-Content $wc -Raw
        if ($texto -match 'ASPNETCORE_ENVIRONMENT" value="Production"') { $bien.Add("$nombre corre en Production") }
        else { $avisos.Add("$nombre no fija ASPNETCORE_ENVIRONMENT=Production en web.config.") }
    } else { $errores.Add("$nombre no tiene web.config.") }
}

# ---------------------------------------------------------------- bases
Titulo "BASES DE DATO"
foreach ($clave in $cadenas.Keys) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($cadenas[$clave])
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT DB_NAME()"
        $base = $cmd.ExecuteScalar()
        $bien.Add("$clave conecta a $base")
        $conn.Close()
    } catch {
        $mensaje = $_.Exception.Message
        $pista = if ($mensaje -match "error: 40|No se encontró el servidor|No se pudo abrir una conexión") {
            "  >>> PISTA: 'localhost' no es la instancia correcta. Averiguá el nombre (por ejemplo .\SQLEXPRESS) y volvé a escribir la configuración con -InstanciaSQL."
        } else { "" }
        $errores.Add("$clave NO conecta: $mensaje$pista")
    }
}

# contar tablas y migraciones en SIRGA_DB_DEMO
# OJO: hay que tomar la cadena DefaultConnection (SIRGA_DB_DEMO). Tomar "la primera del sitio"
# podía devolver LegacyConnection (SINDICATO_DEMO) y entonces se contaban las 227 tablas del
# sindicato y se buscaba dbo.Declarations donde no existe.
$cadenaSirga = $cadenas["$SitioSirga/DefaultConnection"]
if ($cadenaSirga) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($cadenaSirga)
        $conn.Open()
        foreach ($consulta in @(
            @{ q = "SELECT COUNT(*) FROM sys.tables"; esperado = 59; que = "tablas" },
            @{ q = "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory"; esperado = 57; que = "migraciones" },
            @{ q = "SELECT COUNT(*) FROM dbo.Declarations"; esperado = 252165; que = "declaraciones" }
        )) {
            $cmd = $conn.CreateCommand(); $cmd.CommandText = $consulta.q
            $valor = [int]$cmd.ExecuteScalar()
            if ($valor -ge $consulta.esperado) { $bien.Add("SIRGA_DB_DEMO: $($consulta.que) = $valor") }
            else { $errores.Add("SIRGA_DB_DEMO: $($consulta.que) = $valor (se esperaban $($consulta.esperado))") }
        }
        $conn.Close()
    } catch { $errores.Add("No pude consultar SIRGA_DB_DEMO: $($_.Exception.Message)") }
}

# comprobar las tablas del carnet en SINDICATO_DEMO
$cadenaLegado = $cadenas["$SitioSiges/Sindicato"]
if ($cadenaLegado) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($cadenaLegado)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE s.name = 'carnet'"
        $carnet = [int]$cmd.ExecuteScalar()
        if ($carnet -ge 14) { $bien.Add("SINDICATO_DEMO: $carnet tablas del carnet") }
        else { $errores.Add("SINDICATO_DEMO: $carnet tablas del carnet (se esperaban 14).") }
        $conn.Close()
    } catch { $errores.Add("No pude contar las tablas del carnet: $($_.Exception.Message)") }
}

# ---------------------------------------------------------------- respuesta HTTP
Titulo "RESPUESTA DE LAS APLICACIONES"
foreach ($par in @(
    @{ n = "SIRGA";  s = $SitioSirga;  d = $DominioSirga },
    @{ n = "SIGESWEB"; s = $SitioSiges; d = $DominioSiges },
    @{ n = "CARNET"; s = $SitioCarnet; d = $DominioCarnet }
)) {
    # Se prueba sólo el protocolo que el sitio tiene configurado (si es sólo HTTPS, no tiene
    # sentido reclamar por HTTP: el 404 lo devuelve otro sitio).
    $protocolos = @()
    if (Get-WebBinding -Name $par.s -Protocol https -ErrorAction SilentlyContinue) { $protocolos += "https" }
    if (Get-WebBinding -Name $par.s -Protocol http  -ErrorAction SilentlyContinue) { $protocolos += "http" }
    if ($protocolos.Count -eq 0) { $errores.Add("$($par.s) no tiene ningún binding (ni HTTP ni HTTPS)."); continue }

    foreach ($proto in $protocolos) {
        $url = "${proto}://$($par.d)/"

        # 1) Primero estricto: si el certificado es válido, mejor (y se informa así).
        try {
            $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 25
            if ($r.StatusCode -eq 200) { $bien.Add("$($par.n) responde en $url (certificado válido)") }
            else { $avisos.Add("$($par.n) respondió HTTP $($r.StatusCode) en $url") }
            continue
        } catch {
            $mensajeEstricto = $_.Exception.Message
        }

        # 2) Distinguir "certificado no confiable" de "el sitio no responde":
        #    se reintenta aceptando cualquier certificado. Si así responde, el problema es SÓLO el
        #    certificado (en el navegador se puede continuar); si tampoco, el sitio está caído.
        $esTls = $mensajeEstricto -match "SSL|TLS|confianza|certificado|certificate"
        if (-not $esTls) { $errores.Add("$($par.n) NO responde en ${url}: $mensajeEstricto"); continue }

        $callbackPrevio = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
        try {
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { param($a, $b, $d, $e) return $true }
            $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 25
            if ($r.StatusCode -eq 200) {
                $avisos.Add("$($par.n) RESPONDE en $url pero el certificado NO es de confianza (el navegador pedirá aceptarlo). Instalar el certificado correcto.")
            } else {
                $avisos.Add("$($par.n) responde HTTP $($r.StatusCode) en $url (certificado no confiable)")
            }
        } catch {
            $errores.Add("$($par.n) NO responde en ${url}: $($_.Exception.Message)")
        }
        finally {
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $callbackPrevio
        }
    }
}

# ---------------------------------------------------------------- resumen
Titulo "RESUMEN"
foreach ($b in $bien) { Write-Host "  OK    $b" -ForegroundColor Green }
foreach ($a in $avisos) { Write-Host "  AVISO $a" -ForegroundColor Yellow }
foreach ($e in $errores) { Write-Host "  ERROR $e" -ForegroundColor Red }

Write-Host ""
if ($errores.Count -eq 0) {
    Write-Host "  DEMO VERIFICADA: $($bien.Count) controles OK, $($avisos.Count) advertencias." -ForegroundColor Green
    exit 0
}
Write-Host "  HAY $($errores.Count) PROBLEMA(S). Revisar arriba antes de presentar." -ForegroundColor Red
exit 1
