-- =========================================================
-- Patch: thêm cột ExpiryDate vào PartnerDiscountCodes (nếu bảng đã tồn tại)
-- =========================================================

USE TripCompass;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PartnerDiscountCodes')
    AND COL_LENGTH('PartnerDiscountCodes', 'ExpiryDate') IS NULL
BEGIN
    ALTER TABLE PartnerDiscountCodes ADD ExpiryDate DATETIME2 NULL;
    PRINT '✓ Đã thêm cột ExpiryDate vào PartnerDiscountCodes';
END
ELSE
BEGIN
    PRINT '⚠ Bảng PartnerDiscountCodes chưa tồn tại hoặc đã có cột ExpiryDate';
END
GO

