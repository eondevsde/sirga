# ==========================================================================================
#  CHEQUEO PRE-VUELO — valida una publicación y sus bases ANTES de dar el OK
#
#  Detecta las trampas que ya nos costaron caro:
#    - appsettings.Development.json en la carpeta publicada (apunta a la base de desarrollo)
#    - cadenas de conexión faltantes o que apuntan a las bases REALES
#    - esquema incompleto (faltan tablas o migraciones)
#    - legado sin las tablas del carnet
#
#  USO
#    .\tools\pre-vuelo.ps1 -Publicacion "C:\Proyectos\SIRGA_DEMO" -Base SIRGA_DB_DEMO -Legado SINDICATO_DEMO
#    .\tools\pre-vuelo.ps1 -Publicacion "C:\Sitios\SIRGA" -Base SIRGA_DB -Legado Sindicato -EsProduccion
#
#  Sale con código 1 si hay algún ERROR (las advertencias no cortan).
# ==========================================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Publicacion,
    [Parameter(Mandatory = $true)][string]$Base,
    [string]$Legado = "",
    [string]$Instancia = "localhost",
    [switch]$EsProduccion
)

$ErrorActionPreference = "Stop"
$errores = New-Object System.Collections.Generic.List[string]
$advertencias = New-Object System.Collections.Generic.List[string]
$ok = New-Object System.Collections.Generic.List[string]

function Sql([string]$consulta, [string]$baseDatos = "master") {
    $salida = & sqlcmd -S $Instancia -E -b -h -1 -W -Q "SET NOCOUNT ON; $consulta" -d $baseDatos 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($salida -join " ") }
    return ($salida | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() })
}

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

# ---------------------------------------------------------------- 1. publicación
Titulo "PUBLICACIÓN: $Publicacion"

if (-not (Test-Path $Publicacion)) {
    $errores.Add("No existe la carpeta de publicación.")
} else {
    if (Test-Path (Join-Path $Publicacion "SIRGA.WebUI.dll")) { $ok.Add("SIRGA.WebUI.dll presente") }
    else { $errores.Add("Falta SIRGA.WebUI.dll en la publicación.") }

    if (Test-Path (Join-Path $Publicacion "appsettings.Development.json")) {
        $errores.Add("Está appsettings.Development.json: apunta a la base de DESARROLLO. Borrarlo.")
    } else { $ok.Add("Sin appsettings.Development.json") }

    if (Test-Path (Join-Path $Publicacion "wwwroot\_content")) { $ok.Add("Assets estáticos publicados (_content)") }
    else { $advertencias.Add("No encuentro wwwroot\_content: revisar que los estilos se sirvan.") }

    $cfgProd = Join-Path $Publicacion "appsettings.Production.json"
    if (-not (Test-Path $cfgProd)) {
        $errores.Add("Falta appsettings.Production.json.")
    } else {
        $cfg = Get-Content $cfgProd -Raw | ConvertFrom-Json
        $cs = $cfg.ConnectionStrings
        foreach ($clave in @("DefaultConnection", "LegacyConnection", "Legacy")) {
            if ($null -eq $cs -or -not ($cs.PSObject.Properties.Name -contains $clave)) {
                $errores.Add("Falta ConnectionStrings:$clave en appsettings.Production.json")
            } else { $ok.Add("ConnectionStrings:$clave definida") }
        }
        if ([string]::IsNullOrWhiteSpace($cfg.ApiBaseUrl)) { $advertencias.Add("ApiBaseUrl vacía o ausente (se usa para armar links).") }
        else { $ok.Add("ApiBaseUrl = $($cfg.ApiBaseUrl)") }

        # ¿alguna cadena apunta a una base de desarrollo?
        $basesProhibidas = @("SIRGA_DB", "Sindicato", "SindicatoProd")
        if (-not $EsProduccion) {
            foreach ($p in @($cs.PSObject.Properties)) {
                foreach ($b in $basesProhibidas) {
                    if ($p.Value -match "(?i)(Database|Initial Catalog)\s*=\s*$b(;|$)") {
                        $errores.Add("ConnectionStrings:$($p.Name) apunta a la base de DESARROLLO '$b'.")
                    }
                }
            }
        }
    }
}

# ---------------------------------------------------------------- 2. base de SIRGA
Titulo "BASE DE SIRGA: $Base"

try {
    $tablas = [int](Sql "SELECT COUNT(*) FROM sys.tables;" $Base)
    if ($tablas -ge 59) { $ok.Add("Tablas: $tablas") } else { $errores.Add("Tablas: $tablas (se esperaban 59). Falta el esquema.") }

    $mig = [int](Sql "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory;" $Base)
    if ($mig -ge 57) { $ok.Add("Migraciones registradas: $mig") } else { $errores.Add("Migraciones: $mig (se esperaban 57).") }

    $ddjj = [int](Sql "SELECT COUNT(*) FROM dbo.Declarations;" $Base)
    $emp  = [int](Sql "SELECT COUNT(*) FROM dbo.Employees;" $Base)
    $empresas = [int](Sql "SELECT COUNT(*) FROM dbo.Companies;" $Base)
    $ok.Add("Datos: $empresas empresas, $emp empleados, $ddjj declaraciones")
    if ($ddjj -eq 0 -and $EsProduccion) { $advertencias.Add("No hay declaraciones cargadas: ¿faltó correr el ETL?") }
}
catch { $errores.Add("No se pudo consultar la base ${Base}: $($_.Exception.Message)") }

# ---------------------------------------------------------------- 3. legado
if ($Legado) {
    Titulo "LEGADO (sistema viejo): $Legado"
    try {
        foreach ($t in @("fotos_.maesoc", "fotos_.socemp", "dbo.ddjj", "dbo.ddjjt")) {
            $n = [int](Sql "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID('$t');" $Legado)
            if ($n -eq 1) { $ok.Add("Tabla $t presente") } else { $errores.Add("Falta la tabla $t en el legado.") }
        }
        $carnet = [int](Sql "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE s.name = 'carnet';" $Legado)
        if ($carnet -ge 14) { $ok.Add("Tablas del carnet: $carnet") }
        else { $errores.Add("Tablas del carnet: $carnet de 14. Falta aplicar SIGESWEB_migraciones_idempotente.sql.") }

        $socios = [int](Sql "SELECT COUNT(*) FROM fotos_.maesoc;" $Legado)
        $ok.Add("Socios en el padrón: $socios")
    }
    catch { $errores.Add("No se pudo consultar el legado ${Legado}: $($_.Exception.Message)") }
}

# ---------------------------------------------------------------- resumen
Titulo "RESUMEN"
foreach ($o in $ok) { Write-Host "  OK    $o" -ForegroundColor Green }
foreach ($a in $advertencias) { Write-Host "  AVISO $a" -ForegroundColor Yellow }
foreach ($e in $errores) { Write-Host "  ERROR $e" -ForegroundColor Red }

Write-Host ""
if ($errores.Count -eq 0) {
    Write-Host "  PRE-VUELO OK: $($ok.Count) controles pasados, $($advertencias.Count) advertencias." -ForegroundColor Green
    exit 0
}
Write-Host "  PRE-VUELO FALLÓ: $($errores.Count) error(es) y $($advertencias.Count) advertencia(s)." -ForegroundColor Red
exit 1
