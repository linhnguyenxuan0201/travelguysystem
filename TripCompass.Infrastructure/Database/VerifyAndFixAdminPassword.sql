-- ============================================
-- Script: Verify và Fix Password Hash cho Admin
-- Purpose: Kiểm tra và cập nhật password hash cho user admin
-- Date: 2026-01-24
-- ============================================
-- HƯỚNG DẪN:
-- 1. Chạy ứng dụng: dotnet run --project TripCompass.WebUI
-- 2. Truy cập: http://localhost:5000/Setup/GenerateHash?password=Admin@123
-- 3. Copy dòng "Hash: ..." (chỉ phần hash, không copy chữ "Hash: ")
-- 4. Thay thế 'PASTE_HASH_HERE' bên dưới bằng hash vừa copy
-- 5. Chạy script SQL này
-- ============================================

USE [TripCompass]
GO

-- Kiểm tra user admin hiện tại
SELECT 
    UserId,
    UserName,
    Email,
    CASE 
        WHEN PasswordHash IS NULL THEN 'NULL'
        WHEN PasswordHash = 'HASH_ADMIN_PLACEHOLDER' THEN 'PLACEHOLDER'
        WHEN PasswordHash = 'HASH_ADMIN' THEN 'PLACEHOLDER'
        WHEN LEN(PasswordHash) < 80 THEN 'INVALID (too short)'
        WHEN PasswordHash NOT LIKE 'AQAAAA%' THEN 'INVALID (wrong format)'
        ELSE 'VALID'
    END AS HashStatus,
    LEN(PasswordHash) AS HashLength,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview
FROM Users
WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';

PRINT '========================================';
PRINT 'Nếu HashStatus = VALID và HashLength >= 80, hash đã đúng.';
PRINT 'Nếu không, cần cập nhật hash bên dưới.';
PRINT '========================================';
PRINT '';

-- ⚠️ THAY THẾ hash bên dưới bằng hash từ /Setup/GenerateHash?password=Admin@123
-- Hash thường bắt đầu bằng "AQAAAA" và dài khoảng 100 ký tự
DECLARE @NewPasswordHash NVARCHAR(255) = 'PASTE_HASH_HERE'; -- ⚠️ Thay bằng hash thực sự

-- Chỉ update nếu hash mới hợp lệ
IF LEN(@NewPasswordHash) >= 80 AND @NewPasswordHash LIKE 'AQAAAA%'
BEGIN
    UPDATE Users 
    SET PasswordHash = @NewPasswordHash
    WHERE (Email = 'admin@tripcompass.com' OR UserName = 'admin')
      AND (PasswordHash IS NULL 
           OR PasswordHash = 'HASH_ADMIN_PLACEHOLDER' 
           OR PasswordHash = 'HASH_ADMIN'
           OR LEN(PasswordHash) < 80
           OR PasswordHash NOT LIKE 'AQAAAA%');

    IF @@ROWCOUNT > 0
    BEGIN
        PRINT '✓ Password hash đã được cập nhật thành công!';
        PRINT 'Bạn có thể đăng nhập với:';
        PRINT '  Email: admin@tripcompass.com';
        PRINT '  Password: Admin@123';
    END
    ELSE
    BEGIN
        PRINT '⚠️ Hash hiện tại đã hợp lệ hoặc không tìm thấy user admin.';
    END
END
ELSE
BEGIN
    PRINT '❌ Hash không hợp lệ! Hash phải:';
    PRINT '  - Bắt đầu bằng "AQAAAA"';
    PRINT '  - Dài ít nhất 80 ký tự';
    PRINT '  - Lấy từ: /Setup/GenerateHash?password=Admin@123';
END

-- Kiểm tra lại sau khi update
SELECT 
    UserId,
    UserName,
    Email,
    CASE 
        WHEN PasswordHash IS NULL THEN 'NULL'
        WHEN PasswordHash = 'HASH_ADMIN_PLACEHOLDER' THEN 'PLACEHOLDER'
        WHEN PasswordHash = 'HASH_ADMIN' THEN 'PLACEHOLDER'
        WHEN LEN(PasswordHash) < 80 THEN 'INVALID (too short)'
        WHEN PasswordHash NOT LIKE 'AQAAAA%' THEN 'INVALID (wrong format)'
        ELSE 'VALID'
    END AS HashStatus,
    LEN(PasswordHash) AS HashLength,
    'Hash updated' AS Status
FROM Users
WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';

GO
