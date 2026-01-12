-- Add missing columns to Posts table
USE TripCompass;
GO

-- Add ReputationImpact column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ReputationImpact')
BEGIN
    ALTER TABLE Posts ADD ReputationImpact INT NOT NULL DEFAULT 0;
    PRINT 'Column ReputationImpact added to Posts table';
END
GO

-- Add SEO Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'Slug')
BEGIN
    ALTER TABLE Posts ADD Slug NVARCHAR(255) NULL;
    PRINT 'Column Slug added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'SeoTitle')
BEGIN
    ALTER TABLE Posts ADD SeoTitle NVARCHAR(255) NULL;
    PRINT 'Column SeoTitle added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'MetaDescription')
BEGIN
    ALTER TABLE Posts ADD MetaDescription NVARCHAR(500) NULL;
    PRINT 'Column MetaDescription added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'CanonicalUrl')
BEGIN
    ALTER TABLE Posts ADD CanonicalUrl NVARCHAR(500) NULL;
    PRINT 'Column CanonicalUrl added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsIndexable')
BEGIN
    ALTER TABLE Posts ADD IsIndexable BIT NOT NULL DEFAULT 1;
    PRINT 'Column IsIndexable added to Posts table';
END
GO

-- Add Flag Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsFeatured')
BEGIN
    ALTER TABLE Posts ADD IsFeatured BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsFeatured added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsTrending')
BEGIN
    ALTER TABLE Posts ADD IsTrending BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsTrending added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsPinned')
BEGIN
    ALTER TABLE Posts ADD IsPinned BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsPinned added to Posts table';
END
GO

-- Add Moderation Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ModerationNote')
BEGIN
    ALTER TABLE Posts ADD ModerationNote NVARCHAR(MAX) NULL;
    PRINT 'Column ModerationNote added to Posts table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'PublishedAt')
BEGIN
    ALTER TABLE Posts ADD PublishedAt DATETIME2 NULL;
    PRINT 'Column PublishedAt added to Posts table';
END
GO

-- Add Soft Delete Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'DeletedAt')
BEGIN
    ALTER TABLE Posts ADD DeletedAt DATETIME2 NULL;
    PRINT 'Column DeletedAt added to Posts table';
END
GO

-- Update PostStatus Enum check constraint if exists, or just note that values changed
-- 0=Draft, 1=Pending, 2=Published, 3=Rejected, 4=Archived
PRINT 'All columns added successfully!';
GO