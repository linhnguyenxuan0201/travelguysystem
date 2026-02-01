------------------------------------------------------------
-- TRIPCOMPASS - TEST QUERIES FOR ADMIN FUNCTIONALITIES
-- Các query để kiểm tra logic và luồng hoạt động
------------------------------------------------------------

USE TripCompass;
GO

PRINT '========================================';
PRINT 'ADMIN TEST QUERIES';
PRINT '========================================';
GO

------------------------------------------------------------
-- 1. DASHBOARD STATS QUERIES
------------------------------------------------------------

PRINT '';
PRINT '1. DASHBOARD STATISTICS:';
PRINT '----------------------------------------';

-- Total Users
SELECT 'Total Users' AS Metric, COUNT(*) AS Value FROM Users;
SELECT 'New Users Today' AS Metric, COUNT(*) AS Value 
FROM Users WHERE CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE);
SELECT 'Banned Users' AS Metric, COUNT(*) AS Value FROM Users WHERE IsBanned = 1;

-- Posts Stats
SELECT 'Total Posts' AS Metric, COUNT(*) AS Value FROM Posts WHERE IsDeleted = 0;
SELECT 'Pending Posts' AS Metric, COUNT(*) AS Value 
FROM Posts WHERE Status = 1 AND IsDeleted = 0;
SELECT 'Published Posts' AS Metric, COUNT(*) AS Value 
FROM Posts WHERE Status = 2 AND IsDeleted = 0;

-- Reports Stats
SELECT 'Pending Reports' AS Metric, COUNT(*) AS Value FROM Reports WHERE Status = 0;

-- Coin Balance
SELECT 'Total Coin Balance' AS Metric, SUM(Balance) AS Value FROM Wallets;

-- Top Viewed Posts
SELECT TOP 5 
    'Top Viewed Posts' AS Metric,
    PostId,
    Title,
    ViewCount,
    (SELECT UserName FROM Users WHERE UserId = p.UserId) AS AuthorName
FROM Posts p
WHERE IsDeleted = 0
ORDER BY ViewCount DESC;

-- User Growth (Last 7 days)
SELECT 
    'User Growth' AS Metric,
    CAST(CreatedAt AS DATE) AS Date,
    COUNT(*) AS NewUsers
FROM Users
WHERE CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date;

GO

------------------------------------------------------------
-- 2. USER MANAGEMENT QUERIES
------------------------------------------------------------

PRINT '';
PRINT '2. USER MANAGEMENT:';
PRINT '----------------------------------------';

-- All Users with Roles
SELECT 
    u.UserId,
    u.UserName,
    u.Email,
    u.IsBanned,
    u.ReputationScore,
    STRING_AGG(r.RoleName, ', ') AS Roles,
    u.CreatedAt
FROM Users u
LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
GROUP BY u.UserId, u.UserName, u.Email, u.IsBanned, u.ReputationScore, u.CreatedAt
ORDER BY u.CreatedAt DESC;

-- Search Users
SELECT 
    u.UserId,
    u.UserName,
    u.Email,
    u.IsBanned,
    u.ReputationScore
FROM Users u
WHERE u.UserName LIKE '%user%' OR u.Email LIKE '%test%'
ORDER BY u.CreatedAt DESC;

-- Filter by Role
SELECT 
    u.UserId,
    u.UserName,
    u.Email,
    r.RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
WHERE r.RoleName = 'User'
ORDER BY u.CreatedAt DESC;

-- Filter by Status
SELECT 
    u.UserId,
    u.UserName,
    u.Email,
    CASE WHEN u.IsBanned = 1 THEN 'Banned' ELSE 'Active' END AS Status
FROM Users u
WHERE u.IsBanned = 0
ORDER BY u.CreatedAt DESC;

GO

------------------------------------------------------------
-- 3. CONTENT MANAGEMENT QUERIES
------------------------------------------------------------

PRINT '';
PRINT '3. CONTENT MANAGEMENT:';
PRINT '----------------------------------------';

-- Posts by Status
SELECT 
    Status,
    CASE Status
        WHEN 0 THEN 'Draft'
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Published'
        WHEN 3 THEN 'Rejected'
        WHEN 4 THEN 'Archived'
    END AS StatusName,
    COUNT(*) AS Count
FROM Posts
WHERE IsDeleted = 0
GROUP BY Status
ORDER BY Status;

-- Posts with Details
SELECT 
    p.PostId,
    p.Title,
    u.UserName AS AuthorName,
    c.Name AS CategoryName,
    CASE p.Status
        WHEN 0 THEN 'Draft'
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Published'
        WHEN 3 THEN 'Rejected'
        WHEN 4 THEN 'Archived'
    END AS Status,
    p.ViewCount,
    p.LikeCount,
    p.CreatedAt,
    p.PublishedAt
FROM Posts p
INNER JOIN Users u ON p.UserId = u.UserId
LEFT JOIN PostCategories pc ON p.PostId = pc.PostId
LEFT JOIN Categories c ON pc.CategoryId = c.CategoryId
WHERE p.IsDeleted = 0
ORDER BY p.CreatedAt DESC;

-- Search Posts
SELECT 
    p.PostId,
    p.Title,
    u.UserName AS AuthorName,
    p.Status,
    p.ViewCount
FROM Posts p
INNER JOIN Users u ON p.UserId = u.UserId
WHERE (p.Title LIKE '%Test%' OR p.PostId = 1)
    AND p.IsDeleted = 0
ORDER BY p.CreatedAt DESC;

GO

------------------------------------------------------------
-- 4. COMMENT MANAGEMENT QUERIES
------------------------------------------------------------

PRINT '';
PRINT '4. COMMENT MANAGEMENT:';
PRINT '----------------------------------------';

-- Comments with Details
SELECT 
    c.CommentId,
    c.PostId,
    p.Title AS PostTitle,
    u.UserName,
    u.Email,
    c.Content,
    c.IsDeleted,
    c.CreatedAt,
    (SELECT COUNT(*) FROM CommentReactions WHERE CommentId = c.CommentId) AS ReactionCount
FROM PostComments c
INNER JOIN Posts p ON c.PostId = p.PostId
INNER JOIN Users u ON c.UserId = u.UserId
ORDER BY c.CreatedAt DESC;

-- Active vs Deleted Comments
SELECT 
    CASE WHEN IsDeleted = 1 THEN 'Deleted' ELSE 'Active' END AS Status,
    COUNT(*) AS Count
FROM PostComments
GROUP BY IsDeleted;

GO

------------------------------------------------------------
-- 5. REPORT MANAGEMENT QUERIES
------------------------------------------------------------

PRINT '';
PRINT '5. REPORT MANAGEMENT:';
PRINT '----------------------------------------';

-- Reports by Status
SELECT 
    Status,
    CASE Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Resolved'
        WHEN 2 THEN 'Rejected'
    END AS StatusName,
    COUNT(*) AS Count
FROM Reports
GROUP BY Status
ORDER BY Status;

-- Reports with Details
SELECT 
    r.ReportId,
    reporter.UserName AS ReporterName,
    r.TargetType,
    r.TargetId,
    r.Reason,
    r.Description,
    CASE r.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Resolved'
        WHEN 2 THEN 'Rejected'
    END AS Status,
    resolver.UserName AS ResolverName,
    r.ResolvedAt,
    r.CreatedAt
FROM Reports r
INNER JOIN Users reporter ON r.ReporterId = reporter.UserId
LEFT JOIN Users resolver ON r.ResolvedBy = resolver.UserId
ORDER BY r.CreatedAt DESC;

-- Reports by Target Type
SELECT 
    TargetType,
    COUNT(*) AS Count
FROM Reports
GROUP BY TargetType;

GO

------------------------------------------------------------
-- 6. PARTNER MANAGEMENT QUERIES
------------------------------------------------------------

PRINT '';
PRINT '6. PARTNER MANAGEMENT:';
PRINT '----------------------------------------';

-- Partners by Status
SELECT 
    CASE WHEN IsApproved = 1 THEN 'Approved' ELSE 'Pending' END AS Status,
    COUNT(*) AS Count
FROM Partners
GROUP BY IsApproved;

-- Partners with Details
SELECT 
    p.PartnerId,
    u.UserName,
    u.Email,
    p.StoreName,
    p.BusinessType,
    p.RepresentativeName,
    p.PhoneNumber,
    CASE WHEN p.IsApproved = 1 THEN 'Approved' ELSE 'Pending' END AS Status,
    p.CreatedAt,
    p.UpdatedAt
FROM Partners p
INNER JOIN Users u ON p.UserId = u.UserId
ORDER BY p.CreatedAt DESC;

GO

------------------------------------------------------------
-- 7. AD PACKAGES QUERIES
------------------------------------------------------------

PRINT '';
PRINT '7. AD PACKAGES:';
PRINT '----------------------------------------';

-- Ad Packages by Status
SELECT 
    CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Pending' END AS Status,
    COUNT(*) AS Count
FROM PartnerDiscountCodes
GROUP BY IsActive;

-- Ad Packages with Partner Info
SELECT 
    pd.PartnerDiscountCodeId,
    p.StoreName AS PartnerName,
    u.UserName,
    pd.Code,
    pd.PercentOff,
    pd.Purpose,
    pd.ExpiryDate,
    CASE WHEN pd.IsActive = 1 THEN 'Active' ELSE 'Pending' END AS Status,
    pd.CreatedAt
FROM PartnerDiscountCodes pd
INNER JOIN Partners p ON pd.PartnerUserId = p.UserId
INNER JOIN Users u ON p.UserId = u.UserId
ORDER BY pd.CreatedAt DESC;

GO

------------------------------------------------------------
-- 8. COIN TRANSACTIONS QUERIES
------------------------------------------------------------

PRINT '';
PRINT '8. COIN TRANSACTIONS:';
PRINT '----------------------------------------';

-- Transactions by Type
SELECT 
    Type,
    COUNT(*) AS Count,
    SUM(Amount) AS TotalAmount
FROM CoinTransactions
GROUP BY Type
ORDER BY Type;

-- Recent Transactions
SELECT 
    t.TransactionId,
    u.UserName,
    u.Email,
    t.Amount,
    t.Type,
    t.ReferenceId,
    t.CreatedAt
FROM CoinTransactions t
INNER JOIN Users u ON t.UserId = u.UserId
ORDER BY t.CreatedAt DESC;

-- Purchased Coins (Revenue)
SELECT 
    'Total Purchased Coins' AS Metric,
    COUNT(*) AS TransactionCount,
    SUM(Amount) AS TotalAmount
FROM CoinTransactions
WHERE Type = 'PURCHASED' AND Amount > 0;

GO

------------------------------------------------------------
-- 9. LOCATIONS QUERIES
------------------------------------------------------------

PRINT '';
PRINT '9. LOCATIONS:';
PRINT '----------------------------------------';

-- Locations from Posts
SELECT 
    Location,
    COUNT(*) AS PostCount,
    SUM(ViewCount) AS TotalViews,
    SUM(LikeCount) AS TotalLikes,
    MAX(CreatedAt) AS LastPostDate
FROM Posts
WHERE IsDeleted = 0 AND Location IS NOT NULL AND Location != ''
GROUP BY Location
ORDER BY PostCount DESC, LastPostDate DESC;

GO

------------------------------------------------------------
-- 10. CATEGORIES QUERIES
------------------------------------------------------------

PRINT '';
PRINT '10. CATEGORIES:';
PRINT '----------------------------------------';

-- Categories with Post Count
SELECT 
    c.CategoryId,
    c.Name,
    c.Slug,
    c.Icon,
    COUNT(pc.PostId) AS PostCount
FROM Categories c
LEFT JOIN PostCategories pc ON c.CategoryId = pc.CategoryId
LEFT JOIN Posts p ON pc.PostId = p.PostId AND p.IsDeleted = 0
GROUP BY c.CategoryId, c.Name, c.Slug, c.Icon
ORDER BY c.Name;

GO

------------------------------------------------------------
-- 11. ACTIVITY HISTORY / AUDIT LOG QUERIES
------------------------------------------------------------

PRINT '';
PRINT '11. ACTIVITY HISTORY / AUDIT LOG:';
PRINT '----------------------------------------';

-- Recent Admin Actions
SELECT TOP 20
    al.LogId,
    admin.UserName AS AdminName,
    admin.Email AS AdminEmail,
    al.ActionType,
    al.TargetTable,
    al.TargetId,
    al.Note,
    al.CreatedAt
FROM AdminLogs al
INNER JOIN Users admin ON al.AdminId = admin.UserId
ORDER BY al.CreatedAt DESC;

-- Actions by Type
SELECT 
    ActionType,
    COUNT(*) AS Count
FROM AdminLogs
GROUP BY ActionType
ORDER BY Count DESC;

-- Actions by Target Table
SELECT 
    TargetTable,
    COUNT(*) AS Count
FROM AdminLogs
GROUP BY TargetTable
ORDER BY Count DESC;

-- Actions by Date Range
SELECT 
    CAST(CreatedAt AS DATE) AS Date,
    COUNT(*) AS ActionCount
FROM AdminLogs
WHERE CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date DESC;

GO

------------------------------------------------------------
-- 12. REVENUE STATISTICS QUERIES
------------------------------------------------------------

PRINT '';
PRINT '12. REVENUE STATISTICS:';
PRINT '----------------------------------------';

-- Daily Revenue (Last 30 days)
SELECT 
    CAST(CreatedAt AS DATE) AS Date,
    SUM(CASE WHEN Type = 'PURCHASED' AND Amount > 0 THEN Amount ELSE 0 END) AS CoinRevenue
FROM CoinTransactions
WHERE CreatedAt >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY CAST(CreatedAt AS DATE)
ORDER BY Date DESC;

-- Monthly Revenue (Last 12 months)
SELECT 
    FORMAT(CreatedAt, 'yyyy-MM') AS Month,
    SUM(CASE WHEN Type = 'PURCHASED' AND Amount > 0 THEN Amount ELSE 0 END) AS CoinRevenue
FROM CoinTransactions
WHERE CreatedAt >= DATEADD(MONTH, -12, GETUTCDATE())
GROUP BY FORMAT(CreatedAt, 'yyyy-MM')
ORDER BY Month DESC;

-- Total Revenue Summary
SELECT 
    'Total Coin Revenue' AS Metric,
    SUM(Amount) AS TotalAmount,
    COUNT(*) AS TransactionCount
FROM CoinTransactions
WHERE Type = 'PURCHASED' AND Amount > 0;

GO

------------------------------------------------------------
-- 13. TEST WORKFLOW QUERIES
------------------------------------------------------------

PRINT '';
PRINT '13. TEST WORKFLOW SCENARIOS:';
PRINT '----------------------------------------';

-- Scenario 1: Approve a Pending Post
PRINT 'Scenario 1: Post Status Workflow';
SELECT 
    PostId,
    Title,
    Status,
    CASE Status
        WHEN 0 THEN 'Draft'
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Published'
        WHEN 3 THEN 'Rejected'
    END AS StatusName,
    CreatedAt,
    PublishedAt
FROM Posts
WHERE Status = 1 AND IsDeleted = 0
ORDER BY CreatedAt;

-- Scenario 2: Resolve a Pending Report
PRINT 'Scenario 2: Report Resolution Workflow';
SELECT 
    ReportId,
    TargetType,
    TargetId,
    Reason,
    Status,
    CASE Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Resolved'
        WHEN 2 THEN 'Rejected'
    END AS StatusName,
    CreatedAt
FROM Reports
WHERE Status = 0
ORDER BY CreatedAt;

-- Scenario 3: Approve a Pending Partner
PRINT 'Scenario 3: Partner Approval Workflow';
SELECT 
    PartnerId,
    StoreName,
    BusinessType,
    IsApproved,
    CASE WHEN IsApproved = 1 THEN 'Approved' ELSE 'Pending' END AS Status,
    CreatedAt
FROM Partners
WHERE IsApproved = 0
ORDER BY CreatedAt;

-- Scenario 4: Approve a Pending Ad Package
PRINT 'Scenario 4: Ad Package Approval Workflow';
SELECT 
    pd.PartnerDiscountCodeId,
    p.StoreName AS PartnerName,
    pd.Code,
    pd.PercentOff,
    pd.IsActive,
    CASE WHEN pd.IsActive = 1 THEN 'Active' ELSE 'Pending' END AS Status,
    pd.CreatedAt
FROM PartnerDiscountCodes pd
INNER JOIN Partners p ON pd.PartnerUserId = p.UserId
WHERE pd.IsActive = 0
ORDER BY pd.CreatedAt;

GO

PRINT '';
PRINT '========================================';
PRINT 'All test queries completed!';
PRINT '========================================';
GO
