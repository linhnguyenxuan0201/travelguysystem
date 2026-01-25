-- ============================================
-- Script: Cập nhật Password Hash cho Admin
-- Purpose: Cập nhật password hash thực sự cho user admin hiện có
-- Date: 2026-01-23
-- ============================================
-- HƯỚNG DẪN:
-- 1. Chạy ứng dụng và truy cập: /Setup/GenerateHash?password=Admin@123
-- 2. Copy hash được hiển thị (dòng "Hash: ...")
-- 3. Thay thế @PasswordHash bên dưới bằng hash vừa copy
-- 4. Chạy script SQL này
-- ============================================

USE [TripCompass]
GO

DECLARE @Email NVARCHAR(100) = 'admin@tripcompass.com';
DECLARE @UserName NVARCHAR(50) = 'admin';
DECLARE @Password NVARCHAR(50) = 'Admin@123';

-- ⚠️ THAY THẾ hash bên dưới bằng hash từ /Setup/GenerateHash?password=Admin@123
-- Hash mẫu (KHÔNG DÙNG - chỉ để tham khảo format):
-- Hash thường bắt đầu bằng "AQAAAA" và dài khoảng 100 ký tự
DECLARE @PasswordHash NVARCHAR(255) = 'PASTE_HASH_HERE'; -- ⚠️ Thay bằng hash thực sự

-- Kiểm tra user có tồn tại không
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @Email OR UserName = @UserName)
BEGIN
    PRINT '❌ User admin không tồn tại. Vui lòng tạo user trước.';
    SELECT 'User not found' AS Status;
    RETURN;
END

-- Cập nhật password hash
UPDATE Users 
SET PasswordHash = @PasswordHash
WHERE Email = @Email OR UserName = @UserName;

IF @@ROWCOUNT > 0
BEGIN
    PRINT '✓ Password hash đã được cập nhật thành công!';
    PRINT 'Email: ' + @Email;
    PRINT 'Password: ' + @Password;
    PRINT '⚠️ Bạn có thể đăng nhập ngay bây giờ!';
    
    SELECT 
        UserId,
        UserName,
        Email,
        'Password updated successfully' AS Status
    FROM Users
    WHERE Email = @Email OR UserName = @UserName;
END
ELSE
BEGIN
    PRINT '❌ Không thể cập nhật password hash.';
    SELECT 'Update failed' AS Status;
END

GO
