-- =========================================================
-- Alter PostBookings: thêm thông tin đặt chỗ (tên/sđt/mã ưu đãi)
-- =========================================================

USE TripCompass;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PostBookings')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'CustomerName')
    BEGIN
        ALTER TABLE PostBookings ADD CustomerName NVARCHAR(120) NOT NULL CONSTRAINT DF_PostBookings_CustomerName DEFAULT N'';
        PRINT '✓ Đã thêm cột CustomerName';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'CustomerPhone')
    BEGIN
        ALTER TABLE PostBookings ADD CustomerPhone NVARCHAR(30) NOT NULL CONSTRAINT DF_PostBookings_CustomerPhone DEFAULT N'';
        PRINT '✓ Đã thêm cột CustomerPhone';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'PromoCode')
    BEGIN
        ALTER TABLE PostBookings ADD PromoCode NVARCHAR(30) NULL;
        PRINT '✓ Đã thêm cột PromoCode';
    END
END
ELSE
BEGIN
    PRINT '⚠ Bảng PostBookings chưa tồn tại';
END
GO

