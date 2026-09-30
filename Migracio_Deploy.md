# Notas de migración a producción (deploy)

Fecha de análisis: 2026-08-14

## Estado actual
- Toda la operación es local (SQL Server de DIEGOHP): SIRGA_DB (destino) y Sindicato (legado).
- App Blazor en http://localhost:5122. Sin producción ni remoto.

## Cómo se despliega (plan acordado)
1. Restore del backup local de SIRGA_DB en el server de producción (ya migrada y con datos).
2. Ajustar connection strings en prod:
   - SIRGA.WebUI/appsettings.json -> DefaultConnection
   - SIRGA.Infrastructure/Persistence/SIRG_DbContextFactory.cs:11 (design-time)
   - SIRGA.Migration/Db.cs:9-10 (ETL: LegacyCs=Sindicato, TargetCs=SIRGA_DB)
   - tools/create-studio-users.cs:11 y tools/reset-passwords.cs:9
3. Correr la app.
4. Migraciones futuras: dotnet ef database update (ver advertencia abajo).
5. ETL solo si cambian datos del legado (idempotente: limpia y recarga).

## ADVERTENCIA: cadena de migraciones NO reproducible desde cero
- 7 migraciones agregan AdminEmail a Unions: 1 legítima (20260513224613)
  y 6 duplicadas idénticas (FixedPendingChanges, SyncModelV3/V4/V5,
  ConsolidateAllChanges, AddSubscriptionsStructure) -> solo AddColumn.
- En una DB vacía, dotnet ef database update FALLA en la 2da con Error 2705
  ("'AdminEmail' ... se especifica más de una vez").
- La DB local quedó arreglada a mano: las 6 duplicadas marcadas como aplicadas
  en __EFMigrationsHistory (historial = 27) y la migración
  20260815004910_AddAssignmentActiveFields (IsActive + TerminationDate) aplicada.
- ANTES de crear cualquier DB nueva (dev/test/DR/prod sin restore): eliminar
  las 6 migraciones no-op (.cs + .Designer.cs), borrar sus filas del historial
  y verificar la cadena completa en una DB scratch.

## Deuda de migraciones
- Los snapshots del modelo estuvieron desincronizados con el historial
  (migraciones "sync" caóticas que nunca se aplicaron pero la DB ya las reflejaba).
- Se recomienda limpiar la cadena antes del primer deploy a prod.

## Volúmenes migrados (corrida 2026-08-14)
- 25 provincias, 2.596 ciudades, 4.507 empresas, 27.934 empleados,
  36.011 asignaciones (12.896 socias), 844 estudios, 248.151 DDJJ,
  1.269.977 detalles; montos cuadrados 7.514.730.620,20.
- Asignaciones: 9.593 activas / 26.418 de baja (IsActive, con TerminationDate).
- Empresas: 2.576 activas / 1.931 inactivas (Company.IsActive derivado: ≥1 asignación activa o DDJJ 2026).

## Usuarios
- 844 estudios con usuario Identity generado (password 12345),
  listado en tools/estudios_usuarios.csv. Tool: tools/create-studio-users.cs.
- Admin/auditor/sindicato: admin@/auditor@/sindicato@sirga.com.