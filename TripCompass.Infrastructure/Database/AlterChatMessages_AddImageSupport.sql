-- Add ImageUrl and MessageType columns to ChatMessages table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = 'ImageUrl')
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
    ADD [ImageUrl] NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = 'MessageType')
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
    ADD [MessageType] NVARCHAR(20) NOT NULL DEFAULT 'Text';
END
GO
