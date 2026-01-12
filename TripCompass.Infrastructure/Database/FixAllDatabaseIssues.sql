-- ============================================================
-- Script tổng hợp để fix tất cả các vấn đề database
-- Chạy script này để đảm bảo database có đầy đủ các bảng và cột
-- ============================================================
USE TripCompass;
GO

PRINT '========================================';
PRINT 'Bắt đầu fix database...';
PRINT '========================================';
GO

-- ============================================================
-- 1. TẠO BẢNG UserAvatars (nếu chưa có)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAvatars')
BEGIN
    CREATE TABLE UserAvatars (
        UserAvatarId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        AvatarUrl NVARCHAR(255) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserAvatars_UserId ON UserAvatars(UserId);
    PRINT '✓ Bảng UserAvatars đã được tạo';
END
ELSE
BEGIN
    PRINT '✓ Bảng UserAvatars đã tồn tại';
END
GO

-- ============================================================
-- 2. THÊM CÁC CỘT CÒN THIẾU VÀO BẢNG Posts
-- ============================================================

-- ReputationImpact
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ReputationImpact')
BEGIN
    ALTER TABLE Posts ADD ReputationImpact INT NOT NULL DEFAULT 0;
    PRINT '✓ Cột ReputationImpact đã được thêm';
END

-- SEO Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'Slug')
BEGIN
    ALTER TABLE Posts ADD Slug NVARCHAR(255) NULL;
    PRINT '✓ Cột Slug đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'SeoTitle')
BEGIN
    ALTER TABLE Posts ADD SeoTitle NVARCHAR(255) NULL;
    PRINT '✓ Cột SeoTitle đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'MetaDescription')
BEGIN
    ALTER TABLE Posts ADD MetaDescription NVARCHAR(500) NULL;
    PRINT '✓ Cột MetaDescription đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'CanonicalUrl')
BEGIN
    ALTER TABLE Posts ADD CanonicalUrl NVARCHAR(500) NULL;
    PRINT '✓ Cột CanonicalUrl đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsIndexable')
BEGIN
    ALTER TABLE Posts ADD IsIndexable BIT NOT NULL DEFAULT 1;
    PRINT '✓ Cột IsIndexable đã được thêm';
END

-- Flag Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsFeatured')
BEGIN
    ALTER TABLE Posts ADD IsFeatured BIT NOT NULL DEFAULT 0;
    PRINT '✓ Cột IsFeatured đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsTrending')
BEGIN
    ALTER TABLE Posts ADD IsTrending BIT NOT NULL DEFAULT 0;
    PRINT '✓ Cột IsTrending đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsPinned')
BEGIN
    ALTER TABLE Posts ADD IsPinned BIT NOT NULL DEFAULT 0;
    PRINT '✓ Cột IsPinned đã được thêm';
END

-- Moderation Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ModerationNote')
BEGIN
    ALTER TABLE Posts ADD ModerationNote NVARCHAR(MAX) NULL;
    PRINT '✓ Cột ModerationNote đã được thêm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'PublishedAt')
BEGIN
    ALTER TABLE Posts ADD PublishedAt DATETIME2 NULL;
    PRINT '✓ Cột PublishedAt đã được thêm';
END

-- Soft Delete Columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'DeletedAt')
BEGIN
    ALTER TABLE Posts ADD DeletedAt DATETIME2 NULL;
    PRINT '✓ Cột DeletedAt đã được thêm';
END

GO

PRINT '========================================';
PRINT 'Hoàn tất! Tất cả các vấn đề database đã được fix.';
PRINT '========================================';
GO
