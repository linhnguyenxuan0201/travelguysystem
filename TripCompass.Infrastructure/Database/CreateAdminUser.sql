-- ============================================
-- Script: Tạo tài khoản Admin
-- Purpose: Tạo user admin với role Admin
-- Date: 2026-01-23
-- ============================================
-- LƯU Ý: Script này tạo user với password hash mẫu
-- Bạn cần sử dụng controller action để tạo admin với password hash thực sự
-- ============================================

USE [TripCompass]
GO

-- Kiểm tra và tạo Role Admin nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Admin');
    PRINT '✓ Role Admin created';
END
ELSE
BEGIN
    PRINT 'Role Admin already exists';
END
GO

-- Kiểm tra email đã tồn tại chưa
DECLARE @Email NVARCHAR(100) = 'admin@tripcompass.com';
DECLARE @UserName NVARCHAR(50) = 'admin';
DECLARE @PasswordHash NVARCHAR(255) = 'HASH_ADMIN_PLACEHOLDER'; -- ⚠️ Cần thay bằng hash thực sự

IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email OR UserName = @UserName)
BEGIN
    PRINT '⚠️ User with email/username already exists. Please use different email/username.';
    SELECT 'User already exists' AS Status;
END
ELSE
BEGIN
    -- Tạo user
    INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)
    VALUES (@UserName, @Email, @PasswordHash, 1000, 5, 0, GETUTCDATE());
    
    DECLARE @UserId BIGINT = SCOPE_IDENTITY();
    
    -- Gán role Admin
    INSERT INTO UserRoles (UserId, RoleId)
    SELECT @UserId, RoleId
    FROM Roles
    WHERE RoleName = 'Admin';
    
    -- Tạo wallet
    INSERT INTO Wallets (UserId, Balance, UpdatedAt)
    VALUES (@UserId, 1000, GETUTCDATE());
    
    PRINT '✓ Admin user created successfully';
    PRINT '⚠️ IMPORTANT: Password hash is placeholder. Use controller action to create admin with real password.';
    SELECT 
        @UserId AS UserId,
        @UserName AS UserName,
        @Email AS Email,
        'Created but password needs to be set via controller' AS Status;
END
GO
