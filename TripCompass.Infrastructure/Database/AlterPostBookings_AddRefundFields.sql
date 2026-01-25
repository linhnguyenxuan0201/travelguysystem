-- Add Refund tracking fields to PostBookings table
-- Run this script if you only need to add refund-related columns

-- Refund tracking fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'Refunded')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [Refunded] BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'RefundAmount')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [RefundAmount] DECIMAL(18,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'RefundedAt')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [RefundedAt] DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'RefundReason')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [RefundReason] NVARCHAR(500) NULL;
END
GO
