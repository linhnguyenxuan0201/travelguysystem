-- Create Notifications table
-- Run this script to create the Notifications table for the notification system

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [Type] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [Link] NVARCHAR(500) NULL,
        [ReferenceId] BIGINT NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    
    -- Create index for faster queries
    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_IsRead] 
    ON [dbo].[Notifications] ([UserId], [IsRead], [CreatedAt] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAt] 
    ON [dbo].[Notifications] ([CreatedAt] DESC);
END
GO
