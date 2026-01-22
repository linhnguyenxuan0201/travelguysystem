-- =========================================================
-- Script để tạo bảng PartnerAgreements
-- =========================================================

USE TripCompass;
GO

-- Tạo bảng PartnerAgreements
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartnerAgreements')
BEGIN
    CREATE TABLE PartnerAgreements (
        AgreementId BIGINT IDENTITY PRIMARY KEY,
        UserId BIGINT NOT NULL,
        AgreementVersion NVARCHAR(20) NOT NULL, -- v1.0, v1.1, etc.
        AgreedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
    
    -- Index để tìm kiếm nhanh theo UserId
    CREATE INDEX IX_PartnerAgreements_UserId ON PartnerAgreements(UserId);
    
    -- Index để tìm kiếm theo version
    CREATE INDEX IX_PartnerAgreements_Version ON PartnerAgreements(AgreementVersion);
    
    PRINT '✓ Bảng PartnerAgreements đã được tạo';
END
ELSE
BEGIN
    PRINT '⚠ Bảng PartnerAgreements đã tồn tại';
END
GO
