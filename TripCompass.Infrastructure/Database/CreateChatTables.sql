-- Create ChatThreads & ChatMessages tables
-- Chat is scoped per BookingId (customer <-> partner)

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatThreads]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChatThreads] (
        [ChatThreadId] BIGINT IDENTITY(1,1) NOT NULL,
        [BookingId] BIGINT NOT NULL,
        [CustomerUserId] BIGINT NOT NULL,
        [PartnerUserId] BIGINT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastMessageAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CustomerUnreadCount] INT NOT NULL DEFAULT 0,
        [PartnerUnreadCount] INT NOT NULL DEFAULT 0,

        CONSTRAINT [PK_ChatThreads] PRIMARY KEY CLUSTERED ([ChatThreadId] ASC),
        CONSTRAINT [UQ_ChatThreads_BookingId] UNIQUE ([BookingId]),
        CONSTRAINT [FK_ChatThreads_PostBookings] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[PostBookings] ([BookingId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatThreads_Customer] FOREIGN KEY ([CustomerUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatThreads_Partner] FOREIGN KEY ([PartnerUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [IX_ChatThreads_Customer_LastMessageAt]
    ON [dbo].[ChatThreads] ([CustomerUserId], [LastMessageAt] DESC);

    CREATE NONCLUSTERED INDEX [IX_ChatThreads_Partner_LastMessageAt]
    ON [dbo].[ChatThreads] ([PartnerUserId], [LastMessageAt] DESC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [ChatMessageId] BIGINT IDENTITY(1,1) NOT NULL,
        [ChatThreadId] BIGINT NOT NULL,
        [SenderUserId] BIGINT NOT NULL,
        [ReceiverUserId] BIGINT NOT NULL,
        [Content] NVARCHAR(2000) NOT NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([ChatMessageId] ASC),
        CONSTRAINT [FK_ChatMessages_ChatThreads] FOREIGN KEY ([ChatThreadId]) REFERENCES [dbo].[ChatThreads] ([ChatThreadId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatMessages_Sender] FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatMessages_Receiver] FOREIGN KEY ([ReceiverUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [IX_ChatMessages_Thread_CreatedAt]
    ON [dbo].[ChatMessages] ([ChatThreadId], [CreatedAt] ASC);

    CREATE NONCLUSTERED INDEX [IX_ChatMessages_Receiver_IsRead]
    ON [dbo].[ChatMessages] ([ReceiverUserId], [IsRead], [CreatedAt] DESC);
END
GO

