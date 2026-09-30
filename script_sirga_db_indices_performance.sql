/* ============================================================================
   SIRGA - Índices de rendimiento sobre Declarations, AuditLogs y BalanceHistories
   ----------------------------------------------------------------------------
   IDEMPOTENTE: se puede ejecutar las veces que haga falta, incluso si quedó a
   medias. Cada paso consulta sys.indexes antes de crear o borrar, así que una
   segunda corrida no hace nada y no falla.

   Qué hace:
     - Crea 6 índices que faltaban (Declarations tiene 248.151 filas y sólo
       estaba indexada por sus claves foráneas).
     - Reemplaza los índices sueltos de FK IX_Declarations_CompanyId e
       IX_Declarations_UnionId por los compuestos (CompanyId, Period) y
       (UnionId, SubmissionDate), que cubren las mismas búsquedas porque son su
       primera columna. Los compuestos se crean ANTES de borrar los sueltos, así
       la clave foránea nunca queda sin índice.
     - Registra la migración en __EFMigrationsHistory para que
       `dotnet ef database update` no intente aplicarla de nuevo.

   Medido con SET STATISTICS IO:
     - deudores por período:            2.709 -> 3 lecturas lógicas
     - página por sindicato ordenando:  2.845 -> 3 lecturas lógicas

   Equivale a la migración EF 20260930165352_AddPerformanceIndexes.
   Uso: sqlcmd -S <servidor> -E -d SIRGA_DB -i script_sirga_db_indices_performance.sql
   ============================================================================ */

SET NOCOUNT ON;
GO

PRINT '=== SIRGA: índices de rendimiento (idempotente) ===';
GO

/* ---------------------------------------------------------------------------
   1) Índices nuevos. Se crean primero para no dejar la FK sin índice.
   --------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_Period' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    CREATE INDEX [IX_Declarations_Period] ON [dbo].[Declarations] ([Period]);
    PRINT 'CREADO   IX_Declarations_Period';
END
ELSE PRINT 'YA ESTA  IX_Declarations_Period';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_Status_SubmissionDate' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    CREATE INDEX [IX_Declarations_Status_SubmissionDate] ON [dbo].[Declarations] ([Status], [SubmissionDate]);
    PRINT 'CREADO   IX_Declarations_Status_SubmissionDate';
END
ELSE PRINT 'YA ESTA  IX_Declarations_Status_SubmissionDate';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_CompanyId_Period' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    CREATE INDEX [IX_Declarations_CompanyId_Period] ON [dbo].[Declarations] ([CompanyId], [Period]);
    PRINT 'CREADO   IX_Declarations_CompanyId_Period';
END
ELSE PRINT 'YA ESTA  IX_Declarations_CompanyId_Period';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_UnionId_SubmissionDate' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    CREATE INDEX [IX_Declarations_UnionId_SubmissionDate] ON [dbo].[Declarations] ([UnionId], [SubmissionDate]);
    PRINT 'CREADO   IX_Declarations_UnionId_SubmissionDate';
END
ELSE PRINT 'YA ESTA  IX_Declarations_UnionId_SubmissionDate';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp' AND object_id = OBJECT_ID('dbo.AuditLogs'))
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [dbo].[AuditLogs] ([Timestamp]);
    PRINT 'CREADO   IX_AuditLogs_Timestamp';
END
ELSE PRINT 'YA ESTA  IX_AuditLogs_Timestamp';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BalanceHistories_CompanyId' AND object_id = OBJECT_ID('dbo.BalanceHistories'))
BEGIN
    CREATE INDEX [IX_BalanceHistories_CompanyId] ON [dbo].[BalanceHistories] ([CompanyId]);
    PRINT 'CREADO   IX_BalanceHistories_CompanyId';
END
ELSE PRINT 'YA ESTA  IX_BalanceHistories_CompanyId';
GO

/* ---------------------------------------------------------------------------
   2) Los índices sueltos de FK quedan cubiertos por los compuestos del paso 1.
      Solo se borran si existen y si su reemplazo ya está creado.
   --------------------------------------------------------------------------- */

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_CompanyId' AND object_id = OBJECT_ID('dbo.Declarations'))
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_CompanyId_Period' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    DROP INDEX [IX_Declarations_CompanyId] ON [dbo].[Declarations];
    PRINT 'BORRADO  IX_Declarations_CompanyId (lo cubre IX_Declarations_CompanyId_Period)';
END
ELSE PRINT 'NADA     IX_Declarations_CompanyId (ya no está o falta su reemplazo)';
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_UnionId' AND object_id = OBJECT_ID('dbo.Declarations'))
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Declarations_UnionId_SubmissionDate' AND object_id = OBJECT_ID('dbo.Declarations'))
BEGIN
    DROP INDEX [IX_Declarations_UnionId] ON [dbo].[Declarations];
    PRINT 'BORRADO  IX_Declarations_UnionId (lo cubre IX_Declarations_UnionId_SubmissionDate)';
END
ELSE PRINT 'NADA     IX_Declarations_UnionId (ya no está o falta su reemplazo)';
GO

/* ---------------------------------------------------------------------------
   3) Registrar la migración equivalente para que EF no la repita.
   --------------------------------------------------------------------------- */

IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930165352_AddPerformanceIndexes', N'10.0.8');
    PRINT 'REGISTRADA migración 20260930165352_AddPerformanceIndexes';
END
ELSE PRINT 'NADA     migración ya registrada (o no hay historial de EF)';
GO

/* ---------------------------------------------------------------------------
   4) Resumen del estado final.
   --------------------------------------------------------------------------- */

SELECT t.name AS tabla, i.name AS indice,
       STUFF((SELECT ',' + c.name
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
              ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 1, '') AS columnas
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.name IN ('IX_Declarations_Period', 'IX_Declarations_Status_SubmissionDate',
                 'IX_Declarations_CompanyId_Period', 'IX_Declarations_UnionId_SubmissionDate',
                 'IX_AuditLogs_Timestamp', 'IX_BalanceHistories_CompanyId')
ORDER BY t.name, i.name;
GO
