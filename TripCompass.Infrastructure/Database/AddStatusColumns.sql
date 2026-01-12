-- Add Status column to Posts table if it doesn't exist
USE TripCompass;
GO

-- Check if column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'Status')
BEGIN
    ALTER TABLE Posts
    ADD Status INT NOT NULL DEFAULT 1; -- 1 = Pending (default)
    
    PRINT 'Column Status added to Posts table';
END
ELSE
BEGIN
    PRINT 'Column Status already exists in Posts table';
END
GO

-- Also add Status column to Reports table if needed
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'Status')
BEGIN
    ALTER TABLE Reports
    ADD Status INT NOT NULL DEFAULT 0; -- 0 = Pending
    
    PRINT 'Column Status added to Reports table';
END
ELSE
BEGIN
    PRINT 'Column Status already exists in Reports table';
END
GO
