-- ============================================
-- Script: Cập nhật Password Hash cho Admin (UserId: 28)
-- Purpose: Cập nhật password hash thực sự cho user admin hiện có
-- Date: 2026-01-24
-- UserId: 28
-- Email: admin@tripcompass.com
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

-- Kiểm tra user hiện tại
SELECT 
    UserId,
    UserName,
    Email,
    LEN(PasswordHash) AS HashLength,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview,
    CASE 
        WHEN PasswordHash IS NULL THEN 'NULL'
        WHEN PasswordHash = 'HASH_ADMIN_PLACEHOLDER' THEN 'PLACEHOLDER'
        WHEN PasswordHash = 'HASH_ADMIN' THEN 'PLACEHOLDER'
        WHEN LEN(PasswordHash) < 80 THEN 'INVALID (too short)'
        WHEN PasswordHash NOT LIKE 'AQAAAA%' THEN 'INVALID (wrong format)'
        ELSE 'VALID'
    END AS HashStatus
FROM Users
WHERE UserId = 28 OR Email = 'admin@tripcompass.com' OR UserName = 'admin';

PRINT '========================================';
PRINT 'Nếu HashStatus = VALID và HashLength >= 80, hash đã đúng.';
PRINT 'Nếu không, cần cập nhật hash bên dưới.';
PRINT '========================================';
PRINT '';

-- ⚠️ THAY THẾ hash bên dưới bằng hash từ /Setup/GenerateHash?password=Admin@123
-- Hash thường bắt đầu bằng "AQAAAA" và dài khoảng 100 ký tự
DECLARE @NewPasswordHash NVARCHAR(255) = 'PASTE_HASH_HERE'; -- ⚠️ Thay bằng hash thực sự

-- Validate hash format
IF LEN(@NewPasswordHash) >= 80 AND @NewPasswordHash LIKE 'AQAAAA%'
BEGIN
    -- Cập nhật password hash
    UPDATE Users 
    SET PasswordHash = @NewPasswordHash
    WHERE UserId = 28 OR (Email = 'admin@tripcompass.com' OR UserName = 'admin');

    IF @@ROWCOUNT > 0
    BEGIN
        PRINT '✓ Password hash đã được cập nhật thành công!';
        PRINT 'Bạn có thể đăng nhập với:';
        PRINT '  Email: admin@tripcompass.com';
        PRINT '  Password: Admin@123';
        PRINT '';
        PRINT 'Hoặc test password verification tại:';
        PRINT '  http://localhost:5000/Account/TestPassword?email=admin@tripcompass.com&password=Admin@123';
    END
    ELSE
    BEGIN
        PRINT '⚠️ Không tìm thấy user admin (UserId: 28).';
    END
END
ELSE
BEGIN
    PRINT '❌ Hash không hợp lệ! Hash phải:';
    PRINT '  - Bắt đầu bằng "AQAAAA"';
    PRINT '  - Dài ít nhất 80 ký tự';
    PRINT '  - Lấy từ: /Setup/GenerateHash?password=Admin@123';
    PRINT '';
    PRINT 'Hash bạn đã nhập:';
    PRINT '  Length: ' + CAST(LEN(@NewPasswordHash) AS NVARCHAR(10));
    PRINT '  Format: ' + CASE WHEN @NewPasswordHash LIKE 'AQAAAA%' THEN 'CORRECT' ELSE 'WRONG' END;
END

-- Kiểm tra lại sau khi update
SELECT 
    UserId,
    UserName,
    Email,
    LEN(PasswordHash) AS HashLength,
    CASE 
        WHEN PasswordHash IS NULL THEN 'NULL'
        WHEN PasswordHash = 'HASH_ADMIN_PLACEHOLDER' THEN 'PLACEHOLDER'
        WHEN PasswordHash = 'HASH_ADMIN' THEN 'PLACEHOLDER'
        WHEN LEN(PasswordHash) < 80 THEN 'INVALID (too short)'
        WHEN PasswordHash NOT LIKE 'AQAAAA%' THEN 'INVALID (wrong format)'
        ELSE 'VALID ✓'
    END AS HashStatus,
    'Ready to login' AS Status
FROM Users
WHERE UserId = 28 OR Email = 'admin@tripcompass.com' OR UserName = 'admin';

GO
