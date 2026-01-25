-- ============================================
-- Script: Tạo Admin trực tiếp trong Database
-- Purpose: Tạo admin với password hash thực sự
-- Date: 2026-01-23
-- Password: Admin@123
-- ============================================
-- HƯỚNG DẪN:
-- 1. Chạy ứng dụng và truy cập: /Setup/GenerateHash?password=Admin@123
-- 2. Copy hash được hiển thị
-- 3. Thay thế @PasswordHash bên dưới bằng hash vừa copy
-- 4. Chạy script SQL này
-- ============================================

USE [TripCompass]
GO

DECLARE @UserName NVARCHAR(50) = 'admin';
DECLARE @Email NVARCHAR(100) = 'admin@tripcompass.com';
DECLARE @Password NVARCHAR(50) = 'Admin@123';

-- ⚠️ THAY THẾ hash bên dưới bằng hash từ /Setup/GenerateHash
-- Hash mẫu (KHÔNG DÙNG - chỉ để tham khảo):
DECLARE @PasswordHash NVARCHAR(255) = 'AQAAAAIAAYagAAAAE...'; -- Thay bằng hash thực sự

-- Kiểm tra user đã tồn tại chưa
DECLARE @UserId BIGINT;
DECLARE @ExistingUserId BIGINT = (SELECT UserId FROM Users WHERE Email = @Email OR UserName = @UserName);

IF @ExistingUserId IS NOT NULL
BEGIN
    -- User đã tồn tại - cập nhật password hash
    SET @UserId = @ExistingUserId;
    UPDATE Users 
    SET PasswordHash = @PasswordHash,
        ReputationScore = 1000,
        ReputationLevel = 5,
        IsBanned = 0
    WHERE UserId = @UserId;
    
    PRINT '✓ User đã tồn tại - đã cập nhật password hash';
END
ELSE
BEGIN
    -- Tạo admin mới
    INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)
    VALUES (@UserName, @Email, @PasswordHash, 1000, 5, 0, GETUTCDATE());
    
    SET @UserId = SCOPE_IDENTITY();
    PRINT '✓ User mới đã được tạo';
END

-- Đảm bảo role Admin tồn tại
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Admin');
END

-- Gán role Admin (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin'))
BEGIN
    INSERT INTO UserRoles (UserId, RoleId)
    SELECT @UserId, RoleId
    FROM Roles
    WHERE RoleName = 'Admin';
END

-- Tạo wallet (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @UserId)
BEGIN
    INSERT INTO Wallets (UserId, Balance, UpdatedAt)
    VALUES (@UserId, 1000, GETUTCDATE());
END
ELSE
BEGIN
    UPDATE Wallets SET Balance = 1000, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId;
END

PRINT '✓ Admin user created successfully!';
PRINT 'Email: ' + @Email;
PRINT 'Password: ' + @Password;
PRINT '⚠️ IMPORTANT: Change password after first login!';

SELECT 
    @UserId AS UserId,
    @UserName AS UserName,
    @Email AS Email,
    @Password AS Password,
    'Admin created successfully' AS Status;

GO
