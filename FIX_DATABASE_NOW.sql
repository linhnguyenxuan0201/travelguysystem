-- ============================================================
-- SCRIPT ĐỂ FIX DATABASE NGAY BÂY GIỜ
-- Copy và chạy toàn bộ script này trong SQL Server Management Studio
-- ============================================================
USE TripCompass;
GO

PRINT 'Bắt đầu fix database...';
GO

-- 1. TẠO BẢNG UserAvatars
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
    PRINT '✓ Đã tạo bảng UserAvatars';
END
GO

-- 2. THÊM CÁC CỘT VÀO BẢNG Posts
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ReputationImpact')
    ALTER TABLE Posts ADD ReputationImpact INT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'Slug')
    ALTER TABLE Posts ADD Slug NVARCHAR(255) NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'SeoTitle')
    ALTER TABLE Posts ADD SeoTitle NVARCHAR(255) NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'MetaDescription')
    ALTER TABLE Posts ADD MetaDescription NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'CanonicalUrl')
    ALTER TABLE Posts ADD CanonicalUrl NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsIndexable')
    ALTER TABLE Posts ADD IsIndexable BIT NOT NULL DEFAULT 1;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsFeatured')
    ALTER TABLE Posts ADD IsFeatured BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsTrending')
    ALTER TABLE Posts ADD IsTrending BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'IsPinned')
    ALTER TABLE Posts ADD IsPinned BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'ModerationNote')
    ALTER TABLE Posts ADD ModerationNote NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'PublishedAt')
    ALTER TABLE Posts ADD PublishedAt DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Posts') AND name = 'DeletedAt')
    ALTER TABLE Posts ADD DeletedAt DATETIME2 NULL;
GO

PRINT '========================================';
PRINT 'HOÀN TẤT! Database đã được fix thành công!';
PRINT 'Bây giờ bạn có thể chạy ứng dụng.';
PRINT '========================================';
GO
