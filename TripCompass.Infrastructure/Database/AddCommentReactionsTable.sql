-- Migration: Create CommentReactions table
-- Run this script to create the CommentReactions table for like/dislike comments

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CommentReactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE CommentReactions (
        ReactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
        CommentId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        ReactionType NVARCHAR(20) NOT NULL, -- LIKE, DISLIKE
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        FOREIGN KEY (CommentId) REFERENCES PostComments(CommentId) ON DELETE CASCADE,
        FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
        
        -- Unique constraint: một user chỉ react một comment một lần
        CONSTRAINT UQ_CommentReaction_Comment_User UNIQUE (CommentId, UserId)
    );
    
    CREATE INDEX IX_CommentReactions_CommentId ON CommentReactions(CommentId);
    CREATE INDEX IX_CommentReactions_UserId ON CommentReactions(UserId);
    
    PRINT 'Table CommentReactions created successfully';
END
ELSE
BEGIN
    PRINT 'Table CommentReactions already exists';
END
GO
