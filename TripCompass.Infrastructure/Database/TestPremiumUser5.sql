-- Test: Thêm Premium plan cho UserId = 5
-- Chạy script này để test tính năng Premium với user có ID = 5

USE TripCompass;
GO

-- Kiểm tra user có tồn tại không
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = 5)
BEGIN
    PRINT 'User ID = 5 không tồn tại. Vui lòng tạo user trước.';
    RETURN;
END
GO

-- Xóa các plan cũ của user 5 (nếu có) để tránh conflict
DELETE FROM UserPlans WHERE UserId = 5;
GO

-- Thêm Premium plan Pro (hàng tháng) - hết hạn sau 1 tháng
INSERT INTO UserPlans (UserId, PlanCode, StartedAt, ExpiredAt)
VALUES (
    5,                              -- UserId
    'Pro',                          -- PlanCode (Pro hoặc Enterprise)
    GETUTCDATE(),                   -- StartedAt (bắt đầu từ bây giờ)
    DATEADD(MONTH, 1, GETUTCDATE()) -- ExpiredAt (hết hạn sau 1 tháng)
);
GO

-- Hoặc nếu muốn Premium Enterprise (hàng năm) - hết hạn sau 1 năm
-- Uncomment dòng dưới và comment dòng INSERT trên:
/*
INSERT INTO UserPlans (UserId, PlanCode, StartedAt, ExpiredAt)
VALUES (
    5,                              -- UserId
    'Enterprise',                   -- PlanCode
    GETUTCDATE(),                   -- StartedAt
    DATEADD(YEAR, 1, GETUTCDATE())  -- ExpiredAt (hết hạn sau 1 năm)
);
GO
*/

-- Hoặc nếu muốn Premium không bao giờ hết hạn (để test lâu dài):
/*
INSERT INTO UserPlans (UserId, PlanCode, StartedAt, ExpiredAt)
VALUES (
    5,                              -- UserId
    'Pro',                          -- PlanCode
    GETUTCDATE(),                   -- StartedAt
    NULL                            -- ExpiredAt = NULL (không bao giờ hết hạn)
);
GO
*/

-- Kiểm tra kết quả
SELECT 
    up.UserPlanId,
    up.UserId,
    u.UserName,
    u.Email,
    up.PlanCode,
    up.StartedAt,
    up.ExpiredAt,
    CASE 
        WHEN up.ExpiredAt IS NULL THEN 'Không bao giờ hết hạn'
        WHEN up.ExpiredAt > GETUTCDATE() THEN 'Còn hiệu lực'
        ELSE 'Đã hết hạn'
    END AS Status
FROM UserPlans up
INNER JOIN Users u ON up.UserId = u.UserId
WHERE up.UserId = 5;
GO

PRINT 'Đã thêm Premium plan cho User ID = 5 thành công!';
PRINT 'User này giờ sẽ thấy mascot AI thay vì banner nâng cấp.';
GO
