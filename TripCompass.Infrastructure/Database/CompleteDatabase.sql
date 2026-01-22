-- ============================================================
-- TRIPCOMPASS - COMPLETE DATABASE SCHEMA
-- Tổng hợp toàn bộ database schema và seed data
-- ============================================================

USE master;
GO

-- Drop existing database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'TripCompass')
BEGIN
    ALTER DATABASE TripCompass SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TripCompass;
    PRINT 'Database TripCompass dropped';
END
GO

-- Create database
CREATE DATABASE TripCompass;
GO

USE TripCompass;
GO

PRINT '========================================';
PRINT 'Creating TripCompass Database...';
PRINT '========================================';
GO

-- ============================================================
-- CORE TABLES
-- ============================================================

-- Users Table
CREATE TABLE Users (
    UserId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255),
    ReputationScore INT NOT NULL DEFAULT 0,
    ReputationLevel INT NOT NULL DEFAULT 1,
    IsBanned BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_UserName ON Users(UserName);
GO

-- Roles Table
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- UserRoles Table (Many-to-Many)
CREATE TABLE UserRoles (
    UserId BIGINT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
);
GO

-- Wallets Table
CREATE TABLE Wallets (
    WalletId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL UNIQUE,
    Balance INT NOT NULL DEFAULT 0,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- UserAvatars Table
CREATE TABLE UserAvatars (
    UserAvatarId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    AvatarUrl NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_UserAvatars_UserId ON UserAvatars(UserId);
GO

-- UserPlans Table
CREATE TABLE UserPlans (
    UserPlanId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    PlanCode NVARCHAR(50) NOT NULL, -- Free, Pro, Enterprise
    StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiredAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_UserPlans_UserId ON UserPlans(UserId);
GO

-- Categories Table
CREATE TABLE Categories (
    CategoryId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL UNIQUE,
    Icon NVARCHAR(50),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- POSTS & CONTENT
-- ============================================================

-- Posts Table (Complete with all columns)
CREATE TABLE Posts (
    PostId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Location NVARCHAR(200) NULL,
    
    -- Statistics
    ViewCount INT NOT NULL DEFAULT 0,
    LikeCount INT NOT NULL DEFAULT 0,
    DislikeCount INT NOT NULL DEFAULT 0,
    ReputationImpact INT NOT NULL DEFAULT 0,
    
    -- Flags
    IsPartner BIT NOT NULL DEFAULT 0,
    IsFeatured BIT NOT NULL DEFAULT 0,
    IsTrending BIT NOT NULL DEFAULT 0,
    IsPinned BIT NOT NULL DEFAULT 0,
    
    -- Status & Moderation
    Status INT NOT NULL DEFAULT 1, -- 0=Draft, 1=Pending, 2=Published, 3=Rejected, 4=Archived
    ModerationNote NVARCHAR(MAX) NULL,
    PublishedAt DATETIME2 NULL,
    
    -- SEO & Metadata
    Slug NVARCHAR(255) NULL,
    SeoTitle NVARCHAR(255) NULL,
    MetaDescription NVARCHAR(500) NULL,
    CanonicalUrl NVARCHAR(500) NULL,
    IsIndexable BIT NOT NULL DEFAULT 1,
    
    -- Soft Delete
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    
    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_Posts_UserId ON Posts(UserId);
CREATE INDEX IX_Posts_Status ON Posts(Status);
CREATE INDEX IX_Posts_CreatedAt ON Posts(CreatedAt);
CREATE INDEX IX_Posts_Slug ON Posts(Slug) WHERE Slug IS NOT NULL;
GO

-- PostCategories Table (Many-to-Many)
CREATE TABLE PostCategories (
    PostId BIGINT NOT NULL,
    CategoryId BIGINT NOT NULL,
    PRIMARY KEY (PostId, CategoryId),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId) ON DELETE CASCADE
);
GO

-- PostImages Table
CREATE TABLE PostImages (
    PostImageId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId BIGINT NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    IsCover BIT NOT NULL DEFAULT 0,
    SortOrder INT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE
);

CREATE INDEX IX_PostImages_PostId ON PostImages(PostId);
GO

-- PostComments Table
CREATE TABLE PostComments (
    CommentId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    ParentCommentId BIGINT NULL, -- For nested comments
    Content NVARCHAR(MAX) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (ParentCommentId) REFERENCES PostComments(CommentId) ON DELETE NO ACTION
);

CREATE INDEX IX_PostComments_PostId ON PostComments(PostId);
CREATE INDEX IX_PostComments_UserId ON PostComments(UserId);
CREATE INDEX IX_PostComments_ParentCommentId ON PostComments(ParentCommentId);
GO

-- PostReactions Table
CREATE TABLE PostReactions (
    ReactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    ReactionType NVARCHAR(20) NOT NULL, -- LIKE, DISLIKE
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    UNIQUE (PostId, UserId) -- One reaction per user per post
);

CREATE INDEX IX_PostReactions_PostId ON PostReactions(PostId);
CREATE INDEX IX_PostReactions_UserId ON PostReactions(UserId);
GO

-- ============================================================
-- REPORTS & MODERATION
-- ============================================================

-- Reports Table
CREATE TABLE Reports (
    ReportId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReporterId BIGINT NOT NULL,
    TargetId BIGINT NOT NULL,
    TargetType NVARCHAR(20) NOT NULL, -- POST, COMMENT, USER
    Reason NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Resolved, 2=Rejected
    ResolvedBy BIGINT NULL,
    ResolvedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ReporterId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (ResolvedBy) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_Reports_ReporterId ON Reports(ReporterId);
CREATE INDEX IX_Reports_TargetId ON Reports(TargetId);
CREATE INDEX IX_Reports_Status ON Reports(Status);
GO

-- AdminLogs Table
CREATE TABLE AdminLogs (
    LogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    AdminId BIGINT NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    TargetTable NVARCHAR(50) NOT NULL,
    TargetId BIGINT NOT NULL,
    Note NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (AdminId) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_AdminLogs_AdminId ON AdminLogs(AdminId);
CREATE INDEX IX_AdminLogs_CreatedAt ON AdminLogs(CreatedAt);
GO

-- ============================================================
-- TRANSACTIONS & OTPS
-- ============================================================

-- EmailOtps Table
CREATE TABLE EmailOtps (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL,
    OtpCode NVARCHAR(10) NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    ExpiredAt DATETIME2 NOT NULL
);

CREATE INDEX IX_EmailOtps_Email ON EmailOtps(Email);
CREATE INDEX IX_EmailOtps_OtpCode ON EmailOtps(OtpCode);
GO

-- CoinTransactions Table
CREATE TABLE CoinTransactions (
    TransactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Amount INT NOT NULL, -- Positive for credit, negative for debit
    Type NVARCHAR(50) NOT NULL, -- EARN, SPEND, REFUND, etc.
    ReferenceId BIGINT NULL, -- Reference to PostId, CommentId, etc.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_CoinTransactions_UserId ON CoinTransactions(UserId);
CREATE INDEX IX_CoinTransactions_CreatedAt ON CoinTransactions(CreatedAt);
GO

PRINT '========================================';
PRINT 'All tables created successfully!';
PRINT '========================================';
GO

-- ============================================================
-- SEED DATA
-- ============================================================

PRINT '========================================';
PRINT 'Seeding initial data...';
PRINT '========================================';
GO

-- ROLES
INSERT INTO Roles (RoleName) VALUES 
('Admin'), 
('Moderator'), 
('User'),
('Partner');
PRINT '✓ Roles seeded';
GO

-- USERS
INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel)
VALUES
('admin', 'admin@system.com', 'HASH_ADMIN', 1000, 5),
('moderator', 'mod@system.com', 'HASH_MOD', 500, 3),
('user1', 'user1@mail.com', 'HASH_USER1', 100, 2),
('user2', 'user2@mail.com', 'HASH_USER2', 20, 1);
PRINT '✓ Users seeded';
GO

-- USER ROLES
INSERT INTO UserRoles
SELECT u.UserId, r.RoleId
FROM Users u 
CROSS JOIN Roles r
WHERE (u.UserName = 'admin' AND r.RoleName = 'Admin')
   OR (u.UserName = 'moderator' AND r.RoleName = 'Moderator')
   OR (u.UserName IN ('user1', 'user2') AND r.RoleName = 'User');
PRINT '✓ UserRoles seeded';
GO

-- WALLETS
INSERT INTO Wallets (UserId, Balance)
SELECT UserId,
       CASE UserName
            WHEN 'admin' THEN 1000
            WHEN 'moderator' THEN 500
            WHEN 'user1' THEN 200
            ELSE 50
       END
FROM Users;
PRINT '✓ Wallets seeded';
GO

-- CATEGORIES
INSERT INTO Categories (Name, Slug, Icon)
VALUES
(N'Du lịch', 'du-lich', 'fa-plane'),
(N'Ẩm thực', 'am-thuc', 'fa-utensils'),
(N'Khách sạn', 'khach-san', 'fa-hotel');
PRINT '✓ Categories seeded';
GO

-- POSTS
INSERT INTO Posts (UserId, Title, Content, Location, Status)
SELECT UserId, N'Khám phá Đà Nẵng', N'Lịch trình 3 ngày 2 đêm', N'Đà Nẵng', 1
FROM Users WHERE UserName = 'user1';

INSERT INTO Posts (UserId, Title, Content, Location, IsPartner, Status)
SELECT UserId, N'Review khách sạn Mỹ Khê', N'View đẹp – giá tốt', N'Đà Nẵng', 1, 1
FROM Users WHERE UserName = 'user2';
PRINT '✓ Posts seeded';
GO

-- POST CATEGORIES
INSERT INTO PostCategories
SELECT p.PostId, c.CategoryId
FROM Posts p 
CROSS JOIN Categories c 
WHERE c.Slug = 'du-lich';
PRINT '✓ PostCategories seeded';
GO

-- COMMENTS
INSERT INTO PostComments (PostId, UserId, Content)
SELECT p.PostId, u.UserId, N'Bài viết rất hữu ích'
FROM Posts p 
CROSS JOIN Users u
WHERE u.UserName = 'moderator';
PRINT '✓ PostComments seeded';
GO

-- REPORTS
INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason, Status)
SELECT u.UserId, p.PostId, 'POST', N'Nội dung cần xem xét', 0
FROM Users u 
CROSS JOIN Posts p
WHERE u.UserName = 'user2';
PRINT '✓ Reports seeded';
GO

-- ADMIN LOGS
INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId)
SELECT u.UserId, 'APPROVE', 'Posts', p.PostId
FROM Users u 
CROSS JOIN Posts p
WHERE u.UserName = 'admin';
PRINT '✓ AdminLogs seeded';
GO

PRINT '========================================';
PRINT 'Database setup completed successfully!';
PRINT '========================================';
GO
