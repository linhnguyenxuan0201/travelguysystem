-- Create PremiumOrders table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PremiumOrders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PremiumOrders] (
        [OrderId] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [PlanCode] NVARCHAR(50) NOT NULL,
        [PlanType] NVARCHAR(50) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [PaidAt] DATETIME2 NULL,
        [ExpiresAt] DATETIME2 NULL,
        [PaymentRef] NVARCHAR(500) NULL,
        [TransactionId] NVARCHAR(500) NULL,
        CONSTRAINT [PK_PremiumOrders] PRIMARY KEY CLUSTERED ([OrderId] ASC),
        CONSTRAINT [FK_PremiumOrders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );

    -- Create indexes
    CREATE INDEX [IX_PremiumOrders_UserId] ON [dbo].[PremiumOrders] ([UserId]);
    CREATE INDEX [IX_PremiumOrders_Status] ON [dbo].[PremiumOrders] ([Status]);
    CREATE INDEX [IX_PremiumOrders_CreatedAt] ON [dbo].[PremiumOrders] ([CreatedAt]);
    
    PRINT 'PremiumOrders table created successfully';
END
ELSE
BEGIN
    PRINT 'PremiumOrders table already exists';
END
GO
