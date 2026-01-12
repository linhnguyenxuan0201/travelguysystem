USE master;
GO

ALTER DATABASE TripCompass SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

DROP DATABASE TripCompass;
GO


CREATE DATABASE TripCompass;
GO
USE TripCompass;
GO


CREATE TABLE Users (
    UserId BIGINT IDENTITY PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255),
    ReputationScore INT DEFAULT 0,
    ReputationLevel INT DEFAULT 1,
    IsBanned BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME()
);

CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE UserRoles (
    UserId BIGINT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
);

CREATE TABLE Wallets (
    WalletId BIGINT IDENTITY PRIMARY KEY,
    UserId BIGINT NOT NULL UNIQUE,
    Balance INT DEFAULT 0,
    UpdatedAt DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE UserAvatars (
    UserAvatarId BIGINT IDENTITY PRIMARY KEY,
    UserId BIGINT NOT NULL,
    AvatarUrl NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE Categories (
    CategoryId BIGINT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL UNIQUE,
    Icon NVARCHAR(50),
    CreatedAt DATETIME2 DEFAULT SYSDATETIME()
);

CREATE TABLE Posts (
    PostId BIGINT IDENTITY PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Location NVARCHAR(200),
    ViewCount INT DEFAULT 0,
    LikeCount INT DEFAULT 0,
    DislikeCount INT DEFAULT 0,
    ReputationImpact INT DEFAULT 0,
    IsPartner BIT DEFAULT 0,
    Status INT DEFAULT 1,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    -- SEO & Metadata
    Slug NVARCHAR(255) NULL,
    SeoTitle NVARCHAR(255) NULL,
    MetaDescription NVARCHAR(500) NULL,
    CanonicalUrl NVARCHAR(500) NULL,
    IsIndexable BIT DEFAULT 1,
    -- Flags
    IsFeatured BIT DEFAULT 0,
    IsTrending BIT DEFAULT 0,
    IsPinned BIT DEFAULT 0,
    -- Moderation
    ModerationNote NVARCHAR(MAX) NULL,
    PublishedAt DATETIME2 NULL,
    -- Soft Delete
    DeletedAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE PostCategories (
    PostId BIGINT NOT NULL,
    CategoryId BIGINT NOT NULL,
    PRIMARY KEY (PostId, CategoryId),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId) ON DELETE CASCADE
);

CREATE TABLE PostComments (
    CommentId BIGINT IDENTITY PRIMARY KEY,
    PostId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Reports (
    ReportId BIGINT IDENTITY PRIMARY KEY,
    ReporterId BIGINT NOT NULL,
    TargetId BIGINT NOT NULL,
    TargetType NVARCHAR(20),
    Reason NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (ReporterId) REFERENCES Users(UserId)
);

CREATE TABLE AdminLogs (
    LogId BIGINT IDENTITY PRIMARY KEY,
    AdminId BIGINT NOT NULL,
    ActionType NVARCHAR(50),
    TargetTable NVARCHAR(50),
    TargetId BIGINT,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    FOREIGN KEY (AdminId) REFERENCES Users(UserId)
);

/* =========================================================
   SEED DATA
========================================================= */

-- ROLES
INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Moderator'), ('User');

-- USERS
INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel)
VALUES
('admin','admin@system.com','HASH_ADMIN',1000,5),
('moderator','mod@system.com','HASH_MOD',500,3),
('user1','user1@mail.com','HASH_USER1',100,2),
('user2','user2@mail.com','HASH_USER2',20,1);

-- USER ROLES
INSERT INTO UserRoles
SELECT u.UserId, r.RoleId
FROM Users u JOIN Roles r ON
    (u.UserName='admin' AND r.RoleName='Admin')
 OR (u.UserName='moderator' AND r.RoleName='Moderator')
 OR (u.UserName IN ('user1','user2') AND r.RoleName='User');

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

-- CATEGORIES
INSERT INTO Categories (Name, Slug, Icon)
VALUES
(N'Du lịch','du-lich','fa-plane'),
(N'Ẩm thực','am-thuc','fa-utensils'),
(N'Khách sạn','khach-san','fa-hotel');

-- POSTS
INSERT INTO Posts (UserId, Title, Content, Location)
SELECT UserId, N'Khám phá Đà Nẵng', N'Lịch trình 3 ngày 2 đêm', N'Đà Nẵng'
FROM Users WHERE UserName='user1';

INSERT INTO Posts (UserId, Title, Content, Location, IsPartner)
SELECT UserId, N'Review khách sạn Mỹ Khê', N'View đẹp – giá tốt', N'Đà Nẵng', 1
FROM Users WHERE UserName='user2';

-- POST CATEGORY
INSERT INTO PostCategories
SELECT p.PostId, c.CategoryId
FROM Posts p JOIN Categories c ON c.Slug='du-lich';

-- COMMENTS
INSERT INTO PostComments (PostId, UserId, Content)
SELECT p.PostId, u.UserId, N'Bài viết rất hữu ích'
FROM Posts p CROSS JOIN Users u
WHERE u.UserName='moderator';

-- REPORT
INSERT INTO Reports (ReporterId, TargetId, TargetType, Reason)
SELECT u.UserId, p.PostId, 'POST', N'Nội dung cần xem xét'
FROM Users u CROSS JOIN Posts p
WHERE u.UserName='user2';

-- ADMIN LOG
INSERT INTO AdminLogs (AdminId, ActionType, TargetTable, TargetId)
SELECT u.UserId, 'APPROVE', 'Posts', p.PostId
FROM Users u CROSS JOIN Posts p
WHERE u.UserName='admin';
