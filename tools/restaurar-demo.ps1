# ==========================================================================================
#  RESTAURAR LA DEMO AL ESTADO DEL RESPALDO (botón de emergencia)
#
#  Vuelve las bases demo al estado del respaldo del 2026-11-05 y levanta la aplicación.
#  Sirve si durante una práctica se ensució la demo o algo quedó raro.
#  Tarda ~2 minutos.
#
#  USO
#    .\tools\restaurar-demo.ps1
#    .\tools\restaurar-demo.ps1 -SoloBases      # sin volver a publicar/levantar
# ==========================================================================================

[CmdletBinding()]
param(
    [string]$CarpetaRespaldos = "C:\Proyectos\_RESPALDOS\demo-2026-11-05",
    [switch]$SoloBases
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$dir = "C:\Program Files\Microsoft SQL Server\MSSQL16.DIEGONBHP\MSSQL\DATA"

function Titulo($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }

foreach ($b in @(
    @{ Bak = "SIRGA_DB_DEMO.bak";  Base = "SIRGA_DB_DEMO";  Logico = "SIRGA_DB";  LogicoLog = "SIRGA_DB_log" },
    @{ Bak = "SINDICATO_DEMO.bak"; Base = "SINDICATO_DEMO"; Logico = "sindicato"; LogicoLog = "sindicato_log" }
)) {
    $archivo = Join-Path $CarpetaRespaldos $b.Bak
    Titulo "RESTAURANDO $($b.Base) desde $($b.Bak)"
    if (-not (Test-Path $archivo)) { throw "No existe el respaldo $archivo" }

    $sql = @"
SET NOCOUNT ON;
IF DB_ID('$($b.Base)') IS NOT NULL
BEGIN
  ALTER DATABASE [$($b.Base)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [$($b.Base)];
END
RESTORE DATABASE [$($b.Base)] FROM DISK = N'$archivo'
  WITH MOVE '$($b.Logico)' TO N'$dir\$($b.Base).mdf',
       MOVE '$($b.LogicoLog)' TO N'$dir\$($b.Base)_log.ldf',
       RECOVERY, STATS = 25;
ALTER DATABASE [$($b.Base)] SET RECOVERY SIMPLE WITH NO_WAIT;
"@
    $sql | & sqlcmd -S localhost -E -b 2>&1 | Where-Object { $_ -match 'RESTORE DATABASE procesó|ERROR|Msg' } | ForEach-Object { "  $_" }
    Write-Host "  $($b.Base) restaurada" -ForegroundColor Green
}

# La restauración borra los usuarios de la base: SIGESWEB usa un login SQL local, así que se recrea.
Titulo "RECREANDO EL USUARIO SQL DE SIGESWEB"
$clave = (Get-Content (Join-Path $raiz ".dsh-tools\.sqlpwd_local") -Raw).Trim()
$sqlUsuario = @"
SET NOCOUNT ON;
USE [SINDICATO_DEMO];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sirga_local')
BEGIN
  CREATE USER [sirga_local] FOR LOGIN [sirga_local];
  ALTER ROLE db_datareader ADD MEMBER [sirga_local];
  ALTER ROLE db_datawriter ADD MEMBER [sirga_local];
END
"@
$sqlUsuario | & sqlcmd -S localhost -E -b 2>&1 | Select-Object -First 1
Write-Host "  usuario sirga_local listo en SINDICATO_DEMO" -ForegroundColor Green

Titulo "CONTEOS"
& sqlcmd -S localhost -E -W -s "|" -Q @"
SET NOCOUNT ON;
SELECT 'SIRGA_DB_DEMO' AS base, COUNT(*) AS ddjj, (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Companies) AS empresas, (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Employees) AS empleados FROM SIRGA_DB_DEMO.dbo.Declarations
UNION ALL
SELECT 'SINDICATO_DEMO', COUNT(*), (SELECT COUNT(*) FROM SINDICATO_DEMO.fotos_.maesoc), (SELECT COUNT(*) FROM SINDICATO_DEMO.dbo.ddjjt) FROM SINDICATO_DEMO.dbo.ddjj;
"@ 2>&1 | Where-Object { $_ -match '\S' }

if (-not $SoloBases) {
    Titulo "LEVANTANDO LA DEMO"
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot "levantar-demo.ps1") -ConSigesweb
}
