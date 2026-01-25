-- ============================================
-- Script: Sửa Password Hash cho Admin
-- Purpose: Cập nhật password hash thực sự cho user admin
-- Date: 2026-01-23
-- ============================================
-- HƯỚNG DẪN NHANH:
-- 1. Chạy ứng dụng: dotnet run --project TripCompass.WebUI
-- 2. Truy cập: http://localhost:5000/Setup/GenerateHash?password=Admin@123
-- 3. Copy dòng "Hash: ..." (chỉ phần hash, không copy chữ "Hash: ")
-- 4. Thay thế 'PASTE_HASH_HERE' bên dưới bằng hash vừa copy
-- 5. Chạy script SQL này
-- ============================================

USE [TripCompass]
GO

-- ⚠️ THAY THẾ hash bên dưới bằng hash từ /Setup/GenerateHash?password=Admin@123
-- Hash thường bắt đầu bằng "AQAAAA" và dài khoảng 100 ký tự
DECLARE @PasswordHash NVARCHAR(255) = 'PASTE_HASH_HERE'; -- ⚠️ Thay bằng hash thực sự

-- Cập nhật password hash cho user admin
UPDATE Users 
SET PasswordHash = @PasswordHash
WHERE (Email = 'admin@tripcompass.com' OR UserName = 'admin')
  AND (PasswordHash = 'HASH_ADMIN_PLACEHOLDER' 
       OR PasswordHash = 'HASH_ADMIN' 
       OR PasswordHash IS NULL
       OR LEN(PasswordHash) < 50); -- Cập nhật nếu hash không hợp lệ

IF @@ROWCOUNT > 0
BEGIN
    PRINT '✓ Password hash đã được cập nhật thành công!';
    PRINT 'Bạn có thể đăng nhập với:';
    PRINT '  Email: admin@tripcompass.com';
    PRINT '  Password: Admin@123';
    
    SELECT 
        UserId,
        UserName,
        Email,
        LEFT(PasswordHash, 20) + '...' AS PasswordHashPreview,
        'Password updated successfully' AS Status
    FROM Users
    WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';
END
ELSE
BEGIN
    PRINT '⚠️ Không tìm thấy user admin hoặc password hash đã hợp lệ.';
    PRINT 'Kiểm tra lại:';
    SELECT 
        UserId,
        UserName,
        Email,
        CASE 
            WHEN PasswordHash IS NULL THEN 'NULL'
            WHEN PasswordHash = 'HASH_ADMIN_PLACEHOLDER' THEN 'PLACEHOLDER'
            WHEN PasswordHash = 'HASH_ADMIN' THEN 'PLACEHOLDER'
            WHEN LEN(PasswordHash) < 50 THEN 'INVALID'
            ELSE 'VALID'
        END AS HashStatus,
        LEFT(PasswordHash, 20) + '...' AS PasswordHashPreview
    FROM Users
    WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';
END

GO
