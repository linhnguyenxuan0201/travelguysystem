-- Add Commission tracking fields to PostBookings table
-- Run this script to add CommissionDeducted and CommissionAmount columns

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'PaymentMethod')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [PaymentMethod] NVARCHAR(20) NOT NULL DEFAULT 'Cash';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'CommissionDeducted')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [CommissionDeducted] BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'CommissionAmount')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [CommissionAmount] DECIMAL(18,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'CommissionPaid')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [CommissionPaid] BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'CommissionPaidAt')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [CommissionPaidAt] DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostBookings]') AND name = 'CommissionPaymentRef')
BEGIN
    ALTER TABLE [dbo].[PostBookings]
    ADD [CommissionPaymentRef] NVARCHAR(120) NULL;
END
GO

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
