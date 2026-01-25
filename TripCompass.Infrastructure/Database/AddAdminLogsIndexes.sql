-- ============================================
-- Migration: Add indexes for AdminLogs table
-- Purpose: Optimize queries for Activity History/Audit Log
-- Date: 2026-01-23
-- ============================================

USE [TripCompass]
GO

-- Index for ActionType (used in filtering)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdminLogs_ActionType' AND object_id = OBJECT_ID('dbo.AdminLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AdminLogs_ActionType] ON [dbo].[AdminLogs]
    (
        [ActionType] ASC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    PRINT '✓ Index IX_AdminLogs_ActionType created'
END
ELSE
BEGIN
    PRINT 'Index IX_AdminLogs_ActionType already exists'
END
GO

-- Index for TargetTable (used in filtering)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdminLogs_TargetTable' AND object_id = OBJECT_ID('dbo.AdminLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AdminLogs_TargetTable] ON [dbo].[AdminLogs]
    (
        [TargetTable] ASC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    PRINT '✓ Index IX_AdminLogs_TargetTable created'
END
ELSE
BEGIN
    PRINT 'Index IX_AdminLogs_TargetTable already exists'
END
GO

-- Composite index for common query pattern: ActionType + CreatedAt (for filtering and sorting)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdminLogs_ActionType_CreatedAt' AND object_id = OBJECT_ID('dbo.AdminLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AdminLogs_ActionType_CreatedAt] ON [dbo].[AdminLogs]
    (
        [ActionType] ASC,
        [CreatedAt] DESC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    PRINT '✓ Index IX_AdminLogs_ActionType_CreatedAt created'
END
ELSE
BEGIN
    PRINT 'Index IX_AdminLogs_ActionType_CreatedAt already exists'
END
GO

-- Composite index for TargetTable + CreatedAt
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdminLogs_TargetTable_CreatedAt' AND object_id = OBJECT_ID('dbo.AdminLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AdminLogs_TargetTable_CreatedAt] ON [dbo].[AdminLogs]
    (
        [TargetTable] ASC,
        [CreatedAt] DESC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    PRINT '✓ Index IX_AdminLogs_TargetTable_CreatedAt created'
END
ELSE
BEGIN
    PRINT 'Index IX_AdminLogs_TargetTable_CreatedAt already exists'
END
GO

PRINT 'Migration completed successfully!'
GO
