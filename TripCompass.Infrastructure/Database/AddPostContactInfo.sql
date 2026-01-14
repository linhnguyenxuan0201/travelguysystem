-- Migration: Add contact info fields to Posts table
-- Run this script to add OpeningHours, Phone, ParkingInfo, Price columns

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = 'OpeningHours')
BEGIN
    ALTER TABLE Posts
    ADD OpeningHours NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = 'Phone')
BEGIN
    ALTER TABLE Posts
    ADD Phone NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = 'ParkingInfo')
BEGIN
    ALTER TABLE Posts
    ADD ParkingInfo NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND name = 'Price')
BEGIN
    ALTER TABLE Posts
    ADD Price DECIMAL(18,2) NULL;
END
GO

PRINT 'Migration completed: Contact info fields added to Posts table';
