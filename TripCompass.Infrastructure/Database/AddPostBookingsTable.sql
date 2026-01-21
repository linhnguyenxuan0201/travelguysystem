-- =========================================================
-- Script để tạo bảng PostBookings (đơn đặt chỗ)
-- =========================================================

USE TripCompass;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PostBookings')
BEGIN
    CREATE TABLE PostBookings (
        BookingId BIGINT IDENTITY PRIMARY KEY,
        PostId BIGINT NOT NULL,
        PartnerUserId BIGINT NOT NULL,
        CustomerUserId BIGINT NOT NULL,
        CustomerName NVARCHAR(120) NOT NULL,
        CustomerPhone NVARCHAR(30) NOT NULL,
        BookedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        VisitDate DATETIME2 NULL,
        Quantity INT NOT NULL DEFAULT 1,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Processing',
        PromoCode NVARCHAR(30) NULL,
        Note NVARCHAR(500) NULL
    );

    CREATE INDEX IX_PostBookings_PartnerUserId ON PostBookings(PartnerUserId);
    CREATE INDEX IX_PostBookings_PostId ON PostBookings(PostId);
    CREATE INDEX IX_PostBookings_CustomerUserId ON PostBookings(CustomerUserId);

    PRINT '✓ Bảng PostBookings đã được tạo';
END
ELSE
BEGIN
    PRINT '⚠ Bảng PostBookings đã tồn tại';
END
GO

