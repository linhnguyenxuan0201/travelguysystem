-- Script để tạo bảng Notifications hoặc thêm cột ReferenceId nếu chưa có
-- =========================================================

-- Tạo bảng Notifications
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        NotificationId BIGINT IDENTITY PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        Link NVARCHAR(500) NULL,
        ReferenceId BIGINT NULL,
        IsRead BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ReadAt DATETIME2 NULL,
        
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
    
    -- Indexes for performance
    CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
    CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);
    
    PRINT '✓ Bảng Notifications đã được tạo';
END
ELSE
BEGIN
    PRINT '⚠ Bảng Notifications đã tồn tại';
    
    -- Thêm cột ReferenceId nếu chưa có
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'ReferenceId')
    BEGIN
        ALTER TABLE Notifications ADD ReferenceId BIGINT NULL;
        PRINT '✓ Đã thêm cột ReferenceId vào bảng Notifications';
    END
    ELSE
    BEGIN
        PRINT '⚠ Cột ReferenceId đã tồn tại';
    END
    
    -- Thêm cột ReadAt nếu chưa có
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'ReadAt')
    BEGIN
        ALTER TABLE Notifications ADD ReadAt DATETIME2 NULL;
        PRINT '✓ Đã thêm cột ReadAt vào bảng Notifications';
    END
    ELSE
    BEGIN
        PRINT '⚠ Cột ReadAt đã tồn tại';
    END
END
GO
