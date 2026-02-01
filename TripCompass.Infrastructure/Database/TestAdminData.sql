------------------------------------------------------------
-- TRIPCOMPASS - TEST DATA FOR ADMIN FUNCTIONALITIES
-- File SQL để test các logic và luồng hoạt động của Admin
-- Sử dụng sau khi database đã được tạo
------------------------------------------------------------

USE TripCompass;
GO

PRINT '========================================';
PRINT 'Cleaning up old test data...';
PRINT '========================================';
GO

------------------------------------------------------------
-- CLEANUP: Xóa dữ liệu test cũ (theo thứ tự để tránh FK constraint)
------------------------------------------------------------

BEGIN TRANSACTION;

BEGIN TRY
    -- Delete in reverse order of dependencies
    -- 1. Comment Reactions
    DELETE FROM CommentReactions 
    WHERE CommentId IN (SELECT CommentId FROM PostComments WHERE Content LIKE 'Test Comment%');
    
    -- 2. Comments
    DELETE FROM PostComments WHERE Content LIKE 'Test Comment%';
    
    -- 3. Post Categories (only for test posts)
    DELETE FROM PostCategories 
    WHERE PostId IN (SELECT PostId FROM Posts WHERE Title LIKE 'Test Post%');
    
    -- 4. Reports
    DELETE FROM Reports WHERE Reason LIKE 'Test Report%';
    
    -- 5. Coin Transactions (only test transactions from test users)
    DELETE FROM CoinTransactions 
    WHERE UserId IN (SELECT UserId FROM Users WHERE UserName IN ('user1', 'user2', 'user3', 'user4', 'user5'))
        AND CreatedAt > DATEADD(DAY, -30, GETUTCDATE());
    
    -- 6. Partner Discount Codes (Ad Packages)
    DELETE FROM PartnerDiscountCodes 
    WHERE Code LIKE 'TEST%' OR Code LIKE 'ADPACK%';
    
    -- 7. Partners
    DELETE FROM Partners WHERE StoreName LIKE 'Test Partner%';
    
    -- 8. Posts
    DELETE FROM Posts WHERE Title LIKE 'Test Post%';
    
    -- 9. Admin Logs (only test-related logs)
    DELETE FROM AdminLogs 
    WHERE (ActionType LIKE 'Test%' OR Note LIKE 'Test%')
        AND CreatedAt > DATEADD(DAY, -30, GETUTCDATE());
    
    -- 10. Test Users (except admin and moderator)
    DELETE FROM UserRoles 
    WHERE UserId IN (SELECT UserId FROM Users WHERE UserName IN ('user1', 'user2', 'user3', 'user4', 'user5'));
    
    DELETE FROM Wallets 
    WHERE UserId IN (SELECT UserId FROM Users WHERE UserName IN ('user1', 'user2', 'user3', 'user4', 'user5'));
    
    DELETE FROM Users 
    WHERE UserName IN ('user1', 'user2', 'user3', 'user4', 'user5');
    
    COMMIT TRANSACTION;
    PRINT 'Old test data cleaned up successfully';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error during cleanup: ' + ERROR_MESSAGE();
    PRINT 'Continuing with data insertion...';
END CATCH
GO

PRINT '========================================';
PRINT 'Inserting Test Data for Admin Testing...';
PRINT '========================================';
GO

------------------------------------------------------------
-- 1. USERS - Tạo các user với roles khác nhau
------------------------------------------------------------

-- Admin User (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'admin')
BEGIN
    INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)
    VALUES ('admin', 'admin@tripcompass.com', '$2a$11$TestHashForAdmin123456789', 10000, 5, 0, GETUTCDATE());
    
    DECLARE @AdminUserId BIGINT = SCOPE_IDENTITY();
    
    -- Assign Admin role
    DECLARE @AdminRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin');
    IF @AdminRoleId IS NOT NULL
        INSERT INTO UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);
    
    -- Create wallet
    INSERT INTO Wallets (UserId, Balance, UpdatedAt) VALUES (@AdminUserId, 10000, GETUTCDATE());
    
    PRINT 'Admin user created: admin@tripcompass.com';
END
GO

-- Moderator User
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'moderator')
BEGIN
    INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)
    VALUES ('moderator', 'moderator@tripcompass.com', '$2a$11$TestHashForMod123456789', 5000, 4, 0, GETUTCDATE());
    
    DECLARE @ModUserId BIGINT = SCOPE_IDENTITY();
    DECLARE @ModRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Moderator');
    IF @ModRoleId IS NOT NULL
        INSERT INTO UserRoles (UserId, RoleId) VALUES (@ModUserId, @ModRoleId);
    
    INSERT INTO Wallets (UserId, Balance, UpdatedAt) VALUES (@ModUserId, 5000, GETUTCDATE());
    
    PRINT 'Moderator user created';
END
GO

-- Regular Users
IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'user1')
BEGIN
    INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)
    VALUES 
        ('user1', 'user1@test.com', '$2a$11$TestHash123456789', 500, 2, 0, DATEADD(DAY, -5, GETUTCDATE())),
        ('user2', 'user2@test.com', '$2a$11$TestHash123456789', 1200, 3, 0, DATEADD(DAY, -3, GETUTCDATE())),
        ('user3', 'user3@test.com', '$2a$11$TestHash123456789', 200, 1, 1, DATEADD(DAY, -10, GETUTCDATE())), -- Banned user
        ('user4', 'user4@test.com', '$2a$11$TestHash123456789', 800, 2, 0, DATEADD(DAY, -1, GETUTCDATE())),
        ('user5', 'user5@test.com', '$2a$11$TestHash123456789', 3000, 4, 0, GETUTCDATE()); -- New user today
    
    -- Assign User role
    DECLARE @UserRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'User');
    DECLARE @UserIds TABLE (UserId BIGINT);
    
    INSERT INTO @UserIds (UserId)
    SELECT UserId FROM Users WHERE UserName IN ('user1', 'user2', 'user3', 'user4', 'user5');
    
    IF @UserRoleId IS NOT NULL
        INSERT INTO UserRoles (UserId, RoleId)
        SELECT UserId, @UserRoleId FROM @UserIds;
    
    -- Create wallets
    INSERT INTO Wallets (UserId, Balance, UpdatedAt)
    SELECT UserId, 1000, GETUTCDATE() FROM @UserIds;
    
    PRINT 'Regular users created';
END
GO

------------------------------------------------------------
-- 2. CATEGORIES - Tạo các danh mục
------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'beach')
BEGIN
    INSERT INTO Categories (Name, Slug, Icon)
    VALUES 
        ('Bãi biển', 'beach', 'fa-water'),
        ('Núi rừng', 'mountain', 'fa-mountain'),
        ('Thành phố', 'city', 'fa-city'),
        ('Ẩm thực', 'food', 'fa-utensils'),
        ('Văn hóa', 'culture', 'fa-landmark');
    
    PRINT 'Categories created';
END
GO

------------------------------------------------------------
-- 3. POSTS - Tạo posts với các status khác nhau
------------------------------------------------------------

DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');
DECLARE @User5Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user5');

IF @User1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Posts WHERE Title LIKE 'Test Post%')
BEGIN
    -- Pending Posts (cần duyệt)
    INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, IsDeleted)
    VALUES 
        (@User1Id, 'Test Post Pending 1', 'Nội dung bài viết đang chờ duyệt...', 'Hà Nội', 1, 0, 0, DATEADD(HOUR, -2, GETUTCDATE()), 0),
        (@User2Id, 'Test Post Pending 2', 'Bài viết về du lịch đang chờ kiểm duyệt', 'Đà Nẵng', 1, 0, 0, DATEADD(HOUR, -1, GETUTCDATE()), 0),
        (@User4Id, 'Test Post Pending 3', 'Review địa điểm mới cần duyệt', 'Hồ Chí Minh', 1, 0, 0, GETUTCDATE(), 0);
    
    -- Published Posts
    INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, PublishedAt, IsDeleted)
    VALUES 
        (@User1Id, 'Test Post Published 1', 'Bài viết đã được duyệt và xuất bản', 'Phú Quốc', 2, 150, 25, DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), 0),
        (@User2Id, 'Test Post Published 2', 'Review về bãi biển đẹp', 'Nha Trang', 2, 320, 45, DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 0),
        (@User4Id, 'Test Post Published 3', 'Hướng dẫn du lịch Sapa', 'Sapa', 2, 280, 38, DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), 0),
        (@User1Id, 'Test Post Published 4 - Top Viewed', 'Bài viết có nhiều lượt xem nhất', 'Hạ Long', 2, 1500, 120, DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), 0),
        (@User2Id, 'Test Post Published 5 - Top Viewed', 'Địa điểm hot nhất tháng này', 'Đà Lạt', 2, 1200, 95, DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), 0);
    
    -- Rejected Posts
    DECLARE @User3Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user3');
    
    INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, IsDeleted)
    VALUES 
        (@User4Id, 'Test Post Rejected 1', 'Nội dung không phù hợp', 'Đà Nẵng', 3, 0, 0, DATEADD(DAY, -2, GETUTCDATE()), 0);
    
    IF @User3Id IS NOT NULL
    BEGIN
        INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, IsDeleted)
        VALUES 
            (@User3Id, 'Test Post Rejected 2', 'Bài viết bị từ chối do vi phạm', 'Hà Nội', 3, 0, 0, DATEADD(DAY, -4, GETUTCDATE()), 0);
    END
    
    -- Draft Posts
    INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, IsDeleted)
    VALUES 
        (@User1Id, 'Test Post Draft 1', 'Bài viết đang soạn thảo', 'Huế', 0, 0, 0, DATEADD(HOUR, -5, GETUTCDATE()), 0),
        (@User2Id, 'Test Post Draft 2', 'Draft bài viết mới', 'Hội An', 0, 0, 0, DATEADD(HOUR, -3, GETUTCDATE()), 0);
    
    -- Deleted Posts
    INSERT INTO Posts (UserId, Title, Content, Location, Status, ViewCount, LikeCount, CreatedAt, IsDeleted, DeletedAt)
    VALUES 
        (@User1Id, 'Test Post Deleted 1', 'Bài viết đã bị xóa', 'Cần Thơ', 2, 50, 5, DATEADD(DAY, -10, GETUTCDATE()), 1, DATEADD(DAY, -8, GETUTCDATE()));
    
    PRINT 'Posts created with various statuses';
END
GO

-- Assign Categories to Posts
DECLARE @BeachCategoryId BIGINT = (SELECT CategoryId FROM Categories WHERE Slug = 'beach');
DECLARE @MountainCategoryId BIGINT = (SELECT CategoryId FROM Categories WHERE Slug = 'mountain');
DECLARE @CityCategoryId BIGINT = (SELECT CategoryId FROM Categories WHERE Slug = 'city');

IF @BeachCategoryId IS NOT NULL
BEGIN
    -- Assign categories based on location (only for test posts and only if not already assigned)
    INSERT INTO PostCategories (PostId, CategoryId)
    SELECT p.PostId, 
        CASE 
            WHEN p.Location IN ('Phú Quốc', 'Nha Trang', 'Hạ Long') THEN @BeachCategoryId
            WHEN p.Location IN ('Sapa', 'Đà Lạt') THEN @MountainCategoryId
            ELSE @CityCategoryId
        END AS CategoryId
    FROM Posts p
    WHERE p.Title LIKE 'Test Post%'
        AND (p.Title LIKE '%Published%' OR p.Title LIKE '%Pending%')
        AND NOT EXISTS (
            SELECT 1 FROM PostCategories pc 
            WHERE pc.PostId = p.PostId 
            AND pc.CategoryId = CASE 
                WHEN p.Location IN ('Phú Quốc', 'Nha Trang', 'Hạ Long') THEN @BeachCategoryId
                WHEN p.Location IN ('Sapa', 'Đà Lạt') THEN @MountainCategoryId
                ELSE @CityCategoryId
            END
        );
    
    PRINT 'Categories assigned to posts';
END
GO

------------------------------------------------------------
-- 4. COMMENTS - Tạo comments với các trạng thái
------------------------------------------------------------

DECLARE @PublishedPostId BIGINT = (SELECT TOP 1 PostId FROM Posts WHERE Status = 2 AND IsDeleted = 0 ORDER BY CreatedAt DESC);
DECLARE @PendingPostId BIGINT = (SELECT TOP 1 PostId FROM Posts WHERE Status = 1 AND IsDeleted = 0 ORDER BY CreatedAt DESC);

IF @PublishedPostId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PostComments WHERE Content LIKE 'Test Comment%')
BEGIN
    DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
    DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
    DECLARE @User3Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user3');
    DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');
    DECLARE @User5Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user5');
    
    -- Active Comments
    INSERT INTO PostComments (PostId, UserId, Content, IsDeleted, CreatedAt)
    VALUES 
        (@PublishedPostId, @User2Id, 'Test Comment Active 1 - Bài viết rất hay!', 0, DATEADD(HOUR, -10, GETUTCDATE())),
        (@PublishedPostId, @User4Id, 'Test Comment Active 2 - Cảm ơn bạn đã chia sẻ', 0, DATEADD(HOUR, -8, GETUTCDATE())),
        (@PublishedPostId, @User5Id, 'Test Comment Active 3 - Tôi cũng muốn đến đây', 0, DATEADD(HOUR, -5, GETUTCDATE())),
        (@PendingPostId, @User1Id, 'Test Comment Active 4 - Chờ bài viết được duyệt', 0, DATEADD(HOUR, -2, GETUTCDATE()));
    
    -- Deleted Comments (only if user3 exists)
    IF @User3Id IS NOT NULL
    BEGIN
        INSERT INTO PostComments (PostId, UserId, Content, IsDeleted, CreatedAt)
        VALUES 
            (@PublishedPostId, @User3Id, 'Test Comment Deleted 1 - Comment đã bị xóa', 1, DATEADD(DAY, -3, GETUTCDATE()));
    END
    
    INSERT INTO PostComments (PostId, UserId, Content, IsDeleted, CreatedAt)
    VALUES 
        (@PublishedPostId, @User1Id, 'Test Comment Deleted 2 - Nội dung không phù hợp', 1, DATEADD(DAY, -2, GETUTCDATE()));
    
    PRINT 'Comments created';
END
GO

------------------------------------------------------------
-- 5. REPORTS - Tạo reports với các status
------------------------------------------------------------

DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');
DECLARE @AdminUserId BIGINT = (SELECT UserId FROM Users WHERE UserName = 'admin');
DECLARE @ReportPostId BIGINT = (SELECT TOP 1 PostId FROM Posts WHERE Status = 2 ORDER BY CreatedAt DESC);
DECLARE @ReportCommentId BIGINT = (SELECT TOP 1 CommentId FROM PostComments WHERE IsDeleted = 0 ORDER BY CreatedAt DESC);

IF @User1Id IS NOT NULL AND @ReportPostId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reports WHERE Reason LIKE 'Test Report%')
BEGIN
    -- Pending Reports
    INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason, Description, Status, CreatedAt)
    VALUES 
        (@User1Id, @ReportPostId, 'POST', 'Test Report Pending 1', 'Nội dung bài viết có vấn đề', 0, DATEADD(HOUR, -3, GETUTCDATE())),
        (@User2Id, @ReportPostId, 'POST', 'Test Report Pending 2', 'Thông tin không chính xác', 0, DATEADD(HOUR, -2, GETUTCDATE())),
        (@User4Id, @ReportCommentId, 'COMMENT', 'Test Report Pending 3', 'Bình luận vi phạm quy tắc', 0, DATEADD(HOUR, -1, GETUTCDATE()));
    
    -- User report (only if user3 exists)
    DECLARE @User3IdForReport BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user3');
    IF @User3IdForReport IS NOT NULL
    BEGIN
        INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason, Description, Status, CreatedAt)
        VALUES 
            (@User1Id, @User3IdForReport, 'USER', 'Test Report Pending 4', 'Người dùng có hành vi không phù hợp', 0, GETUTCDATE());
    END
    
    -- Resolved Reports
    INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason, Description, Status, ResolvedBy, ResolvedAt, CreatedAt)
    VALUES 
        (@User2Id, @ReportPostId, 'POST', 'Test Report Resolved 1', 'Đã xử lý xong', 1, @AdminUserId, DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE())),
        (@User4Id, @ReportPostId, 'POST', 'Test Report Resolved 2', 'Không có vấn đề', 1, @AdminUserId, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()));
    
    -- Rejected Reports
    INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason, Description, Status, ResolvedBy, ResolvedAt, CreatedAt)
    VALUES 
        (@User1Id, @ReportPostId, 'POST', 'Test Report Rejected 1', 'Báo cáo không hợp lệ', 2, @AdminUserId, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()));
    
    PRINT 'Reports created with various statuses';
END
GO

------------------------------------------------------------
-- 6. PARTNERS - Tạo partners với các trạng thái
------------------------------------------------------------

DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');

IF @User1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Partners WHERE StoreName LIKE 'Test Partner%')
BEGIN
    -- Approved Partners (only if user doesn't already have a partner)
    IF NOT EXISTS (SELECT 1 FROM Partners WHERE UserId = @User1Id)
    BEGIN
        INSERT INTO Partners (UserId, StoreName, BusinessType, RepresentativeName, PhoneNumber, BusinessAddress, 
                             BankName, AccountNumber, AccountHolderName, IdNumber, TaxId, ServiceDescription, 
                             IsApproved, CreatedAt, UpdatedAt)
        VALUES 
            (@User1Id, 'Test Partner Approved 1', 'Khách sạn', 'Nguyễn Văn A', '0901234567', '123 Đường ABC, Quận 1, TP.HCM',
             'Vietcombank', '1234567890', 'Nguyễn Văn A', '123456789012', '9876543210', 'Dịch vụ khách sạn cao cấp',
             1, DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -9, GETUTCDATE()));
    END
    
    IF @User2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Partners WHERE UserId = @User2Id)
    BEGIN
        INSERT INTO Partners (UserId, StoreName, BusinessType, RepresentativeName, PhoneNumber, BusinessAddress, 
                             BankName, AccountNumber, AccountHolderName, IdNumber, TaxId, ServiceDescription, 
                             IsApproved, CreatedAt, UpdatedAt)
        VALUES 
            (@User2Id, 'Test Partner Approved 2', 'Nhà hàng', 'Trần Thị B', '0907654321', '456 Đường XYZ, Quận 3, TP.HCM',
             'BIDV', '0987654321', 'Trần Thị B', '987654321098', '1234567890', 'Nhà hàng ẩm thực Việt Nam',
             1, DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()));
    END
    
    -- Pending Partners
    IF @User4Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Partners WHERE UserId = @User4Id)
    BEGIN
        INSERT INTO Partners (UserId, StoreName, BusinessType, RepresentativeName, PhoneNumber, BusinessAddress,
                             BankName, AccountNumber, AccountHolderName, IdNumber, TaxId, ServiceDescription,
                             IsApproved, CreatedAt)
        VALUES 
            (@User4Id, 'Test Partner Pending 1', 'Tour du lịch', 'Lê Văn C', '0912345678', '789 Đường DEF, Quận 5, TP.HCM',
             'Techcombank', '1122334455', 'Lê Văn C', '112233445566', NULL, 'Tổ chức tour du lịch trong nước',
             0, DATEADD(DAY, -2, GETUTCDATE()));
    END
    
    -- Use a different user for second pending partner if user1 already has one
    DECLARE @PendingPartnerUserId BIGINT = @User1Id;
    IF EXISTS (SELECT 1 FROM Partners WHERE UserId = @User1Id)
    BEGIN
        SET @PendingPartnerUserId = @User4Id;
    END
    
    IF @PendingPartnerUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Partners WHERE UserId = @PendingPartnerUserId AND StoreName = 'Test Partner Pending 2')
    BEGIN
        INSERT INTO Partners (UserId, StoreName, BusinessType, RepresentativeName, PhoneNumber, BusinessAddress,
                             BankName, AccountNumber, AccountHolderName, IdNumber, TaxId, ServiceDescription,
                             IsApproved, CreatedAt)
        VALUES 
            (@PendingPartnerUserId, 'Test Partner Pending 2', 'Vận chuyển', 'Phạm Thị D', '0923456789', '321 Đường GHI, Quận 7, TP.HCM',
             'Agribank', '5566778899', 'Phạm Thị D', '556677889900', '1122334455', 'Dịch vụ vận chuyển du lịch',
             0, DATEADD(HOUR, -5, GETUTCDATE()));
    END
    
    PRINT 'Partners created with various statuses';
END
GO

------------------------------------------------------------
-- 7. PARTNER DISCOUNT CODES (Ad Packages)
------------------------------------------------------------

DECLARE @PartnerUserId BIGINT = (SELECT TOP 1 UserId FROM Partners WHERE IsApproved = 1 ORDER BY CreatedAt DESC);
DECLARE @PendingPartnerUserId BIGINT = (SELECT TOP 1 UserId FROM Partners WHERE IsApproved = 0 ORDER BY CreatedAt DESC);

IF @PartnerUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PartnerDiscountCodes WHERE Code LIKE 'TEST%')
BEGIN
    -- Active Ad Packages
    INSERT INTO PartnerDiscountCodes (PartnerUserId, Code, PercentOff, Purpose, ExpiryDate, IsActive, CreatedAt)
    VALUES 
        (@PartnerUserId, 'TESTCODE1', 10, 'Khuyến mãi mùa hè', DATEADD(MONTH, 1, GETUTCDATE()), 1, DATEADD(DAY, -5, GETUTCDATE())),
        (@PartnerUserId, 'TESTCODE2', 20, 'Giảm giá đặc biệt', DATEADD(MONTH, 2, GETUTCDATE()), 1, DATEADD(DAY, -3, GETUTCDATE()));
    
    -- Pending Ad Packages
    IF @PendingPartnerUserId IS NOT NULL
    BEGIN
        INSERT INTO PartnerDiscountCodes (PartnerUserId, Code, PercentOff, Purpose, ExpiryDate, IsActive, CreatedAt)
        VALUES 
            (@PendingPartnerUserId, 'TESTCODE3', 15, 'Mã giảm giá mới', DATEADD(MONTH, 1, GETUTCDATE()), 0, DATEADD(DAY, -2, GETUTCDATE())),
            (@PendingPartnerUserId, 'TESTCODE4', 25, 'Khuyến mãi lớn', DATEADD(MONTH, 3, GETUTCDATE()), 0, DATEADD(HOUR, -3, GETUTCDATE()));
    END
    
    PRINT 'Partner discount codes (Ad Packages) created';
END
GO

------------------------------------------------------------
-- 8. COIN TRANSACTIONS - Tạo giao dịch coin
------------------------------------------------------------

DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');
DECLARE @User5Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user5');
DECLARE @PublishedPostId BIGINT = (SELECT TOP 1 PostId FROM Posts WHERE Status = 2 ORDER BY CreatedAt DESC);

IF @User1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CoinTransactions WHERE Type = 'EARNED')
BEGIN
    -- Earned Coins (from posts)
    INSERT INTO CoinTransactions (UserId, Amount, Type, ReferenceId, CreatedAt)
    VALUES 
        (@User1Id, 150, 'EARNED', @PublishedPostId, DATEADD(DAY, -4, GETUTCDATE())),
        (@User2Id, 200, 'EARNED', @PublishedPostId, DATEADD(DAY, -2, GETUTCDATE())),
        (@User4Id, 180, 'EARNED', @PublishedPostId, DATEADD(DAY, -1, GETUTCDATE()));
    
    -- Purchased Coins
    INSERT INTO CoinTransactions (UserId, Amount, Type, ReferenceId, CreatedAt)
    VALUES 
        (@User1Id, 1000, 'PURCHASED', NULL, DATEADD(DAY, -10, GETUTCDATE())),
        (@User2Id, 2000, 'PURCHASED', NULL, DATEADD(DAY, -8, GETUTCDATE())),
        (@User4Id, 500, 'PURCHASED', NULL, DATEADD(DAY, -5, GETUTCDATE())),
        (@User5Id, 1500, 'PURCHASED', NULL, DATEADD(HOUR, -2, GETUTCDATE()));
    
    -- Spent Coins
    INSERT INTO CoinTransactions (UserId, Amount, Type, ReferenceId, CreatedAt)
    VALUES 
        (@User1Id, -100, 'SPENT', NULL, DATEADD(DAY, -3, GETUTCDATE())),
        (@User2Id, -200, 'SPENT', NULL, DATEADD(DAY, -1, GETUTCDATE())),
        (@User4Id, -50, 'SPENT', NULL, DATEADD(HOUR, -5, GETUTCDATE()));
    
    -- Bonus Coins
    INSERT INTO CoinTransactions (UserId, Amount, Type, ReferenceId, CreatedAt)
    VALUES 
        (@User1Id, 50, 'BONUS', NULL, DATEADD(DAY, -7, GETUTCDATE())),
        (@User2Id, 100, 'BONUS', NULL, DATEADD(DAY, -5, GETUTCDATE()));
    
    PRINT 'Coin transactions created';
END
GO

------------------------------------------------------------
-- 9. ADMIN LOGS - Tạo log hoạt động admin
------------------------------------------------------------

DECLARE @AdminUserId BIGINT = (SELECT UserId FROM Users WHERE UserName = 'admin');
DECLARE @TestPostId BIGINT = (SELECT TOP 1 PostId FROM Posts WHERE Status = 1 ORDER BY CreatedAt DESC);
DECLARE @TestUserId BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user3');

IF @AdminUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM AdminLogs WHERE ActionType = 'APPROVE_POST')
BEGIN
    -- Post Actions
    INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId, Note, CreatedAt)
    VALUES 
        (@AdminUserId, 'APPROVE_POST', 'Posts', @TestPostId, 'Approved test post', DATEADD(DAY, -5, GETUTCDATE())),
        (@AdminUserId, 'REJECT_POST', 'Posts', @TestPostId, 'Rejected test post', DATEADD(DAY, -4, GETUTCDATE())),
        (@AdminUserId, 'CHANGE_POST_STATUS', 'Posts', @TestPostId, 'Changed post status', DATEADD(DAY, -3, GETUTCDATE()));
    
    -- User Actions
    IF @TestUserId IS NOT NULL
    BEGIN
        INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId, Note, CreatedAt)
        VALUES 
            (@AdminUserId, 'BAN_USER', 'Users', @TestUserId, 'Banned test user', DATEADD(DAY, -10, GETUTCDATE())),
            (@AdminUserId, 'CHANGE_USER_ROLE', 'Users', @TestUserId, 'Changed user role', DATEADD(DAY, -8, GETUTCDATE()));
    END
    
    -- Report Actions
    DECLARE @TestReportId BIGINT = (SELECT TOP 1 ReportId FROM Reports WHERE Status = 1 ORDER BY CreatedAt DESC);
    IF @TestReportId IS NOT NULL
    BEGIN
        INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId, Note, CreatedAt)
        VALUES 
            (@AdminUserId, 'RESOLVE_REPORT', 'Reports', @TestReportId, 'Resolved test report', DATEADD(DAY, -2, GETUTCDATE()));
    END
    
    -- Category Actions
    DECLARE @TestCategoryId BIGINT = (SELECT TOP 1 CategoryId FROM Categories ORDER BY CategoryId DESC);
    IF @TestCategoryId IS NOT NULL
    BEGIN
        INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId, Note, CreatedAt)
        VALUES 
            (@AdminUserId, 'CREATE_CATEGORY', 'Categories', @TestCategoryId, 'Created test category', DATEADD(DAY, -15, GETUTCDATE())),
            (@AdminUserId, 'UPDATE_CATEGORY', 'Categories', @TestCategoryId, 'Updated test category', DATEADD(DAY, -12, GETUTCDATE()));
    END
    
    PRINT 'Admin logs created';
END
GO

------------------------------------------------------------
-- 10. COMMENT REACTIONS - Tạo reactions cho comments
------------------------------------------------------------

DECLARE @CommentId BIGINT = (SELECT TOP 1 CommentId FROM PostComments WHERE IsDeleted = 0 ORDER BY CreatedAt DESC);
DECLARE @User1Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user1');
DECLARE @User2Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user2');
DECLARE @User4Id BIGINT = (SELECT UserId FROM Users WHERE UserName = 'user4');

IF @CommentId IS NOT NULL AND @User1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CommentReactions WHERE CommentId = @CommentId)
BEGIN
    INSERT INTO CommentReactions (CommentId, UserId, ReactionType, CreatedAt)
    VALUES 
        (@CommentId, @User1Id, 'LIKE', DATEADD(HOUR, -8, GETUTCDATE())),
        (@CommentId, @User2Id, 'LIKE', DATEADD(HOUR, -7, GETUTCDATE())),
        (@CommentId, @User4Id, 'LIKE', DATEADD(HOUR, -6, GETUTCDATE()));
    
    PRINT 'Comment reactions created';
END
GO

------------------------------------------------------------
-- SUMMARY
------------------------------------------------------------

PRINT '';
PRINT '========================================';
PRINT 'Test Data Summary:';
PRINT '========================================';

DECLARE @UserCount INT = (SELECT COUNT(*) FROM Users);
DECLARE @PostCount INT = (SELECT COUNT(*) FROM Posts);
DECLARE @PendingPostCount INT = (SELECT COUNT(*) FROM Posts WHERE Status = 1);
DECLARE @PublishedPostCount INT = (SELECT COUNT(*) FROM Posts WHERE Status = 2);
DECLARE @RejectedPostCount INT = (SELECT COUNT(*) FROM Posts WHERE Status = 3);
DECLARE @CommentCount INT = (SELECT COUNT(*) FROM PostComments);
DECLARE @ReportCount INT = (SELECT COUNT(*) FROM Reports);
DECLARE @PendingReportCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 0);
DECLARE @ResolvedReportCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 1);
DECLARE @PartnerCount INT = (SELECT COUNT(*) FROM Partners);
DECLARE @ApprovedPartnerCount INT = (SELECT COUNT(*) FROM Partners WHERE IsApproved = 1);
DECLARE @PendingPartnerCount INT = (SELECT COUNT(*) FROM Partners WHERE IsApproved = 0);
DECLARE @AdPackageCount INT = (SELECT COUNT(*) FROM PartnerDiscountCodes);
DECLARE @CoinTransactionCount INT = (SELECT COUNT(*) FROM CoinTransactions);
DECLARE @CategoryCount INT = (SELECT COUNT(*) FROM Categories);
DECLARE @AdminLogCount INT = (SELECT COUNT(*) FROM AdminLogs);

PRINT 'Users: ' + CAST(@UserCount AS NVARCHAR(10));
PRINT 'Posts: ' + CAST(@PostCount AS NVARCHAR(10));
PRINT '  - Pending: ' + CAST(@PendingPostCount AS NVARCHAR(10));
PRINT '  - Published: ' + CAST(@PublishedPostCount AS NVARCHAR(10));
PRINT '  - Rejected: ' + CAST(@RejectedPostCount AS NVARCHAR(10));
PRINT 'Comments: ' + CAST(@CommentCount AS NVARCHAR(10));
PRINT 'Reports: ' + CAST(@ReportCount AS NVARCHAR(10));
PRINT '  - Pending: ' + CAST(@PendingReportCount AS NVARCHAR(10));
PRINT '  - Resolved: ' + CAST(@ResolvedReportCount AS NVARCHAR(10));
PRINT 'Partners: ' + CAST(@PartnerCount AS NVARCHAR(10));
PRINT '  - Approved: ' + CAST(@ApprovedPartnerCount AS NVARCHAR(10));
PRINT '  - Pending: ' + CAST(@PendingPartnerCount AS NVARCHAR(10));
PRINT 'Ad Packages: ' + CAST(@AdPackageCount AS NVARCHAR(10));
PRINT 'Coin Transactions: ' + CAST(@CoinTransactionCount AS NVARCHAR(10));
PRINT 'Categories: ' + CAST(@CategoryCount AS NVARCHAR(10));
PRINT 'Admin Logs: ' + CAST(@AdminLogCount AS NVARCHAR(10));
PRINT '========================================';
PRINT 'Test data insertion completed!';
PRINT '========================================';
GO
