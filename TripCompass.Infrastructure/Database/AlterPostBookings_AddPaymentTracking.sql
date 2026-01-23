-- =========================================================
-- Alter PostBookings: thêm cột tracking thanh toán
-- =========================================================

USE TripCompass;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PostBookings')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'PaymentStatus')
    BEGIN
        ALTER TABLE PostBookings ADD PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_PostBookings_PaymentStatus DEFAULT N'Pending';
        PRINT '✓ Added PaymentStatus';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'PaidAt')
    BEGIN
        ALTER TABLE PostBookings ADD PaidAt DATETIME2 NULL;
        PRINT '✓ Added PaidAt';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'AmountPaid')
    BEGIN
        ALTER TABLE PostBookings ADD AmountPaid DECIMAL(18,2) NULL;
        PRINT '✓ Added AmountPaid';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'PaymentRef')
    BEGIN
        ALTER TABLE PostBookings ADD PaymentRef NVARCHAR(120) NULL;
        PRINT '✓ Added PaymentRef';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'VerifiedBy')
    BEGIN
        ALTER TABLE PostBookings ADD VerifiedBy BIGINT NULL;
        PRINT '✓ Added VerifiedBy';
    END

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PostBookings') AND name = 'VerifiedAt')
    BEGIN
        ALTER TABLE PostBookings ADD VerifiedAt DATETIME2 NULL;
        PRINT '✓ Added VerifiedAt';
    END
END
ELSE
BEGIN
    PRINT '⚠ Bảng PostBookings chưa tồn tại';
END
GO

