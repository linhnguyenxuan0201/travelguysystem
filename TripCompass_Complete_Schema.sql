------------------------------------------------------------
-- TRIPCOMPASS - COMPLETE DATABASE SCHEMA
-- File SQL đã sửa đầy đủ và khớp với dự án C#
-- Sử dụng BIGINT IDENTITY thay vì UNIQUEIDENTIFIER
-- Tạo ngày: 2024
------------------------------------------------------------

USE master;
GO

-- Drop existing database if exists
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'TripCompass')
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

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Database settings
ALTER DATABASE TripCompass SET RECOVERY SIMPLE;
ALTER DATABASE TripCompass SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE TripCompass SET ALLOW_SNAPSHOT_ISOLATION ON;
GO

PRINT '========================================';
PRINT 'Creating TripCompass Database Schema...';
PRINT '========================================';
GO

------------------------------------------------------------
-- 1. CORE AUTH TABLES
------------------------------------------------------------

-- Users Table
CREATE TABLE Users (
    UserId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    ReputationScore INT NOT NULL DEFAULT 0,
    ReputationLevel INT NOT NULL DEFAULT 1,
    IsBanned BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_UserName ON Users(UserName);
CREATE INDEX IX_Users_IsBanned ON Users(IsBanned);
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

------------------------------------------------------------
-- 2. USER PROFILE TABLES
------------------------------------------------------------

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
    AvatarUrl NVARCHAR(500) NOT NULL,
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

-- UserFollows Table
CREATE TABLE UserFollows (
    FollowId BIGINT IDENTITY(1,1) PRIMARY KEY,
    FollowerId BIGINT NOT NULL, -- Người theo dõi
    FollowingId BIGINT NOT NULL, -- Người được theo dõi
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (FollowerId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (FollowingId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_UserFollows_Follower_Following UNIQUE (FollowerId, FollowingId),
    CONSTRAINT CK_UserFollows_NotSelf CHECK (FollowerId <> FollowingId)
);

CREATE INDEX IX_UserFollows_FollowerId ON UserFollows(FollowerId);
CREATE INDEX IX_UserFollows_FollowingId ON UserFollows(FollowingId);
GO

------------------------------------------------------------
-- 3. CATEGORIES
------------------------------------------------------------

CREATE TABLE Categories (
    CategoryId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(150) NOT NULL UNIQUE,
    Icon NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Categories_Slug ON Categories(Slug);
GO

------------------------------------------------------------
-- 4. POSTS & CONTENT
------------------------------------------------------------

-- Posts Table
CREATE TABLE Posts (
    PostId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Location NVARCHAR(200) NULL,
    
    -- Contact Info
    OpeningHours NVARCHAR(200) NULL,
    Phone NVARCHAR(50) NULL,
    ParkingInfo NVARCHAR(500) NULL,
    Price DECIMAL(18,2) NULL,
    
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
    Slug NVARCHAR(350) NULL,
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
CREATE INDEX IX_Posts_CreatedAt ON Posts(CreatedAt DESC);
CREATE INDEX IX_Posts_Slug ON Posts(Slug) WHERE Slug IS NOT NULL;
CREATE INDEX IX_Posts_IsDeleted_Status ON Posts(IsDeleted, Status) WHERE IsDeleted = 0 AND Status = 2;
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
CREATE INDEX IX_PostImages_Cover ON PostImages(PostId) WHERE IsCover = 1 AND IsDeleted = 0;
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
CREATE INDEX IX_PostComments_Post_CreatedAt ON PostComments(PostId, CreatedAt) WHERE IsDeleted = 0;
GO

-- PostReactions Table
CREATE TABLE PostReactions (
    ReactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    ReactionType NVARCHAR(50) NOT NULL, -- LIKE, DISLIKE
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_PostReactions_Post_User_Type UNIQUE (PostId, UserId, ReactionType)
);

CREATE INDEX IX_PostReactions_PostId ON PostReactions(PostId);
CREATE INDEX IX_PostReactions_UserId ON PostReactions(UserId);
GO

-- CommentReactions Table
CREATE TABLE CommentReactions (
    ReactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    CommentId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    ReactionType NVARCHAR(20) NOT NULL, -- LIKE, DISLIKE
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (CommentId) REFERENCES PostComments(CommentId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_CommentReactions_Comment_User UNIQUE (CommentId, UserId)
);

CREATE INDEX IX_CommentReactions_CommentId ON CommentReactions(CommentId);
CREATE INDEX IX_CommentReactions_UserId ON CommentReactions(UserId);
GO

------------------------------------------------------------
-- 5. PARTNERS + DISCOUNTS
------------------------------------------------------------

-- Partners Table
CREATE TABLE Partners (
    PartnerId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL UNIQUE,
    
    -- Thông tin đăng ký
    StoreName NVARCHAR(300) NOT NULL,
    BusinessType NVARCHAR(100) NOT NULL,
    RepresentativeName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    BusinessAddress NVARCHAR(500) NOT NULL,
    
    -- Thông tin tài khoản ngân hàng
    BankName NVARCHAR(100) NOT NULL,
    AccountNumber NVARCHAR(50) NOT NULL,
    AccountHolderName NVARCHAR(100) NOT NULL,
    
    -- Giấy tờ pháp lý
    IdNumber NVARCHAR(20) NOT NULL,
    TaxId NVARCHAR(20) NULL,
    
    -- Mô tả dịch vụ
    ServiceDescription NVARCHAR(2000) NULL,
    
    -- Trạng thái
    IsApproved BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_Partners_UserId ON Partners(UserId);
CREATE INDEX IX_Partners_IsApproved ON Partners(IsApproved);
GO

-- PartnerDiscountCodes Table
CREATE TABLE PartnerDiscountCodes (
    PartnerDiscountCodeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PartnerUserId BIGINT NOT NULL,
    Code NVARCHAR(100) NOT NULL,
    PercentOff INT NOT NULL, -- 1-100
    Purpose NVARCHAR(200) NOT NULL,
    ExpiryDate DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 0, -- false = Chờ duyệt, true = Đã duyệt và hoạt động
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PartnerUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT UQ_PartnerDiscountCodes_Partner_Code UNIQUE (PartnerUserId, Code)
);

CREATE INDEX IX_PartnerDiscountCodes_PartnerUserId ON PartnerDiscountCodes(PartnerUserId);
CREATE INDEX IX_PartnerDiscountCodes_IsActive ON PartnerDiscountCodes(IsActive);
GO

-- PartnerAgreements Table
CREATE TABLE PartnerAgreements (
    AgreementId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    AgreementVersion NVARCHAR(20) NOT NULL, -- v1.0, v1.1, etc.
    AgreedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_PartnerAgreements_UserId ON PartnerAgreements(UserId);
GO

------------------------------------------------------------
-- 6. BOOKINGS + PAYMENTS
------------------------------------------------------------

-- PostBookings Table
CREATE TABLE PostBookings (
    BookingId BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId BIGINT NOT NULL,
    PartnerUserId BIGINT NOT NULL,
    CustomerUserId BIGINT NOT NULL,
    
    -- Thông tin liên hệ người đặt
    CustomerName NVARCHAR(120) NOT NULL,
    CustomerPhone NVARCHAR(30) NOT NULL,
    
    -- Thông tin đặt chỗ
    BookedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    VisitDate DATETIME2 NULL,
    Quantity INT NOT NULL DEFAULT 1,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Processing', -- Processing/Completed/Cancelled
    
    -- Ưu đãi
    PromoCode NVARCHAR(30) NULL,
    Note NVARCHAR(500) NULL,
    
    -- Payment tracking
    PaymentMethod NVARCHAR(20) NOT NULL DEFAULT 'Cash', -- Cash/Online
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending/Paid/Failed
    PaidAt DATETIME2 NULL,
    AmountPaid DECIMAL(18,2) NULL,
    PaymentRef NVARCHAR(120) NULL,
    VerifiedBy BIGINT NULL,
    VerifiedAt DATETIME2 NULL,
    
    -- Commission tracking
    CommissionDeducted BIT NOT NULL DEFAULT 0,
    CommissionAmount DECIMAL(18,2) NULL,
    CommissionPaid BIT NOT NULL DEFAULT 0,
    CommissionPaidAt DATETIME2 NULL,
    CommissionPaymentRef NVARCHAR(120) NULL,
    
    -- Refund tracking
    Refunded BIT NOT NULL DEFAULT 0,
    RefundAmount DECIMAL(18,2) NULL,
    RefundedAt DATETIME2 NULL,
    RefundReason NVARCHAR(500) NULL,
    
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE NO ACTION,
    FOREIGN KEY (PartnerUserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (CustomerUserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (VerifiedBy) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_PostBookings_PostId ON PostBookings(PostId);
CREATE INDEX IX_PostBookings_PartnerUserId ON PostBookings(PartnerUserId);
CREATE INDEX IX_PostBookings_CustomerUserId ON PostBookings(CustomerUserId);
CREATE INDEX IX_PostBookings_PaymentStatus ON PostBookings(PaymentStatus);
CREATE INDEX IX_PostBookings_Customer_CreatedAt ON PostBookings(CustomerUserId, BookedAt DESC);
GO

-- PremiumOrders Table
CREATE TABLE PremiumOrders (
    OrderId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    PlanCode NVARCHAR(50) NOT NULL, -- Pro / Enterprise
    PlanType NVARCHAR(50) NOT NULL, -- monthly / yearly
    Amount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending / Paid / Failed / Cancelled
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PaidAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NULL,
    PaymentRef NVARCHAR(500) NULL,
    TransactionId NVARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_PremiumOrders_UserId ON PremiumOrders(UserId);
CREATE INDEX IX_PremiumOrders_Status ON PremiumOrders(Status);
CREATE INDEX IX_PremiumOrders_CreatedAt ON PremiumOrders(CreatedAt);
GO

------------------------------------------------------------
-- 7. CHAT SYSTEM
------------------------------------------------------------

-- ChatThreads Table
CREATE TABLE ChatThreads (
    ChatThreadId BIGINT IDENTITY(1,1) PRIMARY KEY,
    BookingId BIGINT NOT NULL UNIQUE,
    CustomerUserId BIGINT NOT NULL,
    PartnerUserId BIGINT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastMessageAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CustomerUnreadCount INT NOT NULL DEFAULT 0,
    PartnerUnreadCount INT NOT NULL DEFAULT 0,
    FOREIGN KEY (BookingId) REFERENCES PostBookings(BookingId) ON DELETE CASCADE,
    FOREIGN KEY (CustomerUserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (PartnerUserId) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_ChatThreads_Customer_LastMessageAt ON ChatThreads(CustomerUserId, LastMessageAt DESC);
CREATE INDEX IX_ChatThreads_Partner_LastMessageAt ON ChatThreads(PartnerUserId, LastMessageAt DESC);
GO

-- ChatMessages Table
CREATE TABLE ChatMessages (
    ChatMessageId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ChatThreadId BIGINT NOT NULL,
    SenderUserId BIGINT NOT NULL,
    ReceiverUserId BIGINT NOT NULL,
    Content NVARCHAR(2000) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    MessageType NVARCHAR(20) NOT NULL DEFAULT 'Text', -- Text, Image, File
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ChatThreadId) REFERENCES ChatThreads(ChatThreadId) ON DELETE CASCADE,
    FOREIGN KEY (SenderUserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (ReceiverUserId) REFERENCES Users(UserId) ON DELETE NO ACTION
);

CREATE INDEX IX_ChatMessages_Thread_CreatedAt ON ChatMessages(ChatThreadId, CreatedAt);
CREATE INDEX IX_ChatMessages_Receiver_IsRead ON ChatMessages(ReceiverUserId, IsRead, CreatedAt DESC);
GO

------------------------------------------------------------
-- 8. NOTIFICATIONS
------------------------------------------------------------

CREATE TABLE Notifications (
    NotificationId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- NEW_ORDER, ORDER_APPROVED, etc.
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Link NVARCHAR(500) NULL,
    ReferenceId BIGINT NULL, -- ID của đối tượng liên quan
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ReadAt DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead, CreatedAt DESC);
GO

------------------------------------------------------------
-- 9. REPORTS & MODERATION
------------------------------------------------------------

-- Reports Table
CREATE TABLE Reports (
    ReportId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReporterId BIGINT NOT NULL,
    TargetId BIGINT NOT NULL,
    TargetType NVARCHAR(50) NOT NULL, -- POST, COMMENT, USER
    Reason NVARCHAR(500) NOT NULL,
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

------------------------------------------------------------
-- 10. TRANSACTIONS & OTPS
------------------------------------------------------------

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

-- EmailOtps Table
CREATE TABLE EmailOtps (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL,
    OtpCode NVARCHAR(20) NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    ExpiredAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_EmailOtps_Email ON EmailOtps(Email);
CREATE INDEX IX_EmailOtps_OtpCode ON EmailOtps(OtpCode);
CREATE INDEX IX_EmailOtps_ExpiredAt ON EmailOtps(ExpiredAt);
GO

------------------------------------------------------------
-- 11. CONCURRENCY (ROWVERSION) - Optional
------------------------------------------------------------

-- Uncomment if you need optimistic concurrency control
-- ALTER TABLE Wallets ADD RowVer ROWVERSION;
-- ALTER TABLE PostBookings ADD RowVer ROWVERSION;
-- GO

------------------------------------------------------------
-- 12. SEED DATA
------------------------------------------------------------

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

-- Note: Users will be created by DbSeeder in Program.cs
-- The admin user will be created with password: Admin123!

PRINT '========================================';
PRINT 'Database schema created successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Run the application to seed initial data';
PRINT '2. Default admin credentials:';
PRINT '   - Email: admin@tripcompass.com';
PRINT '   - Password: Admin123!';
PRINT '========================================';
GO
