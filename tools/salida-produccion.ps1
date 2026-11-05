# ==========================================================================================
#  SALIDA A PRODUCCIÓN / PUESTA AL DÍA DE SIRGA
#
#  Corre TODO el proceso en orden y falla si el verificador no dice CONSISTENTE.
#  Se puede correr las veces que haga falta: todos los pasos son incrementales e idempotentes.
#
#  EJEMPLOS
#    .\tools\salida-produccion.ps1                                  # simulacro completo
#    .\tools\salida-produccion.ps1 -Aplicar                          # de verdad
#    .\tools\salida-produccion.ps1 -Origen SINDICATO_DEMO -Destino SIRGA_DB_DEMO -Aplicar
#    .\tools\salida-produccion.ps1 -Csv "C:\...\estudios.csv" -Aplicar
#
#  ORDEN Y POR QUÉ
#    1. companies  : crea las empresas que faltan (las que ya están, las saltea)
#    2. employees  : crea los empleados que faltan
#    3. assignments: crea las relaciones socio-empresa que faltan
#    4. align      : alinea ALTAS y BAJAS con el legado (sólo mientras el viejo es el operativo)
#    5. sync       : trae las DDJJ que faltan, repara renglones incompletos y refresca pagos/rectificaciones
#    6. sync (2da) : por si el paso 5 creó empresas que sólo estaban en las DDJJ
#    7. check      : tiene que decir CONSISTENTE
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$Origen = "Sindicato",
    [string]$Destino = "SIRGA_DB",
    [switch]$Aplicar,
    [string]$Csv = "",
    [switch]$SaltarEstudios
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raiz "SIRGA.Migration\SIRGA.Migration.csproj"
$modo = if ($Aplicar) { "REAL" } else { "SIMULACRO" }
$extra = if ($Aplicar) { @() } else { @("--simulacro") }
$inicio = Get-Date

function Titulo($texto) {
    Write-Host ""
    Write-Host ("=" * 78) -ForegroundColor DarkGray
    Write-Host "  $texto" -ForegroundColor Cyan
    Write-Host ("=" * 78) -ForegroundColor DarkGray
}

function CorrerPaso([string]$paso, [string[]]$argumentosExtra = @()) {
    Write-Host ""
    Write-Host ">> $paso" -ForegroundColor Yellow
    $comando = @("run", "--project", $proyecto, "--", $paso, "--origen", $Origen, "--destino", $Destino) + $argumentosExtra + $extra
    $salida = & dotnet @comando 2>&1
    $codigo = $LASTEXITCODE
    $salida | Where-Object { $_ -match '^\[' -or $_ -match 'ERROR|CONSISTENTE|PROBLEMAS|Informativo' } | ForEach-Object { "   $_" }
    if ($codigo -ne 0) {
        throw "El paso '$paso' terminó con código $codigo. Se detiene todo."
    }
    return $salida
}

Titulo "PUESTA AL DÍA DE SIRGA — modo $modo"
Write-Host "  origen : $Origen"
Write-Host "  destino: $Destino"
if (-not $Aplicar) { Write-Host "  (no escribe nada; para aplicar: -Aplicar)" -ForegroundColor DarkYellow }

try {
    CorrerPaso "companies"   | Out-Null
    CorrerPaso "employees"   | Out-Null
    CorrerPaso "assignments" | Out-Null
    CorrerPaso "align"       | Out-Null
    CorrerPaso "sync"        | Out-Null

    # Segunda pasada: el sync puede haber creado empresas que sólo estaban en las DDJJ,
    # y sus declaraciones recién se pueden cargar en la pasada siguiente.
    CorrerPaso "sync"        | Out-Null

    if ($Csv -and -not $SaltarEstudios) {
        Titulo "ESTUDIOS CONTABLES"
        $argsCsv = @("run", (Join-Path $raiz "tools\importar-estudios.cs"), "--", $Csv, $Destino)
        if ($Aplicar) { $argsCsv += @("--aplicar", "--bloquear") }
        & dotnet @argsCsv 2>&1 | Where-Object { $_ -match 'Estudios|Usuarios|Localidades|APLICADO|SIMULACRO|ERROR' } | ForEach-Object { "   $_" }
        if ($LASTEXITCODE -ne 0) { throw "La importación de estudios falló." }
    }

    Titulo "VERIFICACIÓN"
    $salidaCheck = CorrerPaso "check" | Out-String

    $consistente = $salidaCheck -match "CONSISTENTE"

    Titulo "RESULTADO"
    Write-Host ("  duración: {0:N0} s" -f ((Get-Date) - $inicio).TotalSeconds)
    if ($consistente) {
        Write-Host "  CONSISTENTE: SIRGA y el legado coinciden." -ForegroundColor Green
        if (-not $Aplicar) { Write-Host "  (fue un simulacro: para aplicar los cambios, agregar -Aplicar)" -ForegroundColor DarkYellow }
        exit 0
    }

    Write-Host "  NO CONSISTENTE: revisar el detalle de arriba." -ForegroundColor Red
    exit 1
}
catch {
    Titulo "SE DETUVO"
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
