-- =========================================================
-- Script để thêm role "Partner" vào database
-- =========================================================

USE TripCompass;
GO

-- Kiểm tra và thêm role Partner nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Partner')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Partner');
    PRINT '✓ Role "Partner" đã được thêm vào database';
END
ELSE
BEGIN
    PRINT '⚠ Role "Partner" đã tồn tại trong database';
END
GO
