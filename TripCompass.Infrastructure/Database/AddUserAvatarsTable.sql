-- Add UserAvatars table
USE TripCompass;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAvatars')
BEGIN
    CREATE TABLE UserAvatars (
        UserAvatarId BIGINT IDENTITY PRIMARY KEY,
        UserId BIGINT NOT NULL,
        AvatarUrl NVARCHAR(255) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );

    -- Create index on UserId for better query performance
    CREATE INDEX IX_UserAvatars_UserId ON UserAvatars(UserId);
    
    PRINT 'UserAvatars table created successfully';
END
ELSE
BEGIN
    PRINT 'UserAvatars table already exists';
END
GO
