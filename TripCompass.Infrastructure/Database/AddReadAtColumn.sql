-- Script để thêm cột ReadAt vào bảng Notifications nếu chưa có
-- =========================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
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
ELSE
BEGIN
    PRINT '✗ Bảng Notifications chưa tồn tại. Vui lòng chạy script AddNotificationsTable.sql trước.';
END
GO
