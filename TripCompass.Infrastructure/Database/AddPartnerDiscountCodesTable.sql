-- =========================================================
-- Script để tạo bảng PartnerDiscountCodes
-- =========================================================

USE TripCompass;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartnerDiscountCodes')
BEGIN
    CREATE TABLE PartnerDiscountCodes (
        PartnerDiscountCodeId BIGINT IDENTITY PRIMARY KEY,
        PartnerUserId BIGINT NOT NULL,
        Code NVARCHAR(30) NOT NULL,
        PercentOff INT NOT NULL,
        Purpose NVARCHAR(200) NOT NULL,
        ExpiryDate DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 0, -- 0 = Chờ duyệt, 1 = Đã duyệt và hoạt động
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_PartnerDiscountCodes_PartnerUserId_Code
        ON PartnerDiscountCodes(PartnerUserId, Code);

    CREATE INDEX IX_PartnerDiscountCodes_IsActive
        ON PartnerDiscountCodes(IsActive);

    PRINT '✓ Bảng PartnerDiscountCodes đã được tạo';
END
ELSE
BEGIN
    PRINT '⚠ Bảng PartnerDiscountCodes đã tồn tại';
    -- Đảm bảo IsActive có default value = 0
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PartnerDiscountCodes') AND name = 'IsActive')
    BEGIN
        -- Kiểm tra và cập nhật default constraint nếu cần
        DECLARE @ConstraintName NVARCHAR(200);
        SELECT @ConstraintName = name FROM sys.default_constraints 
        WHERE parent_object_id = OBJECT_ID('PartnerDiscountCodes') AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('PartnerDiscountCodes'), 'IsActive', 'ColumnId');
        
        IF @ConstraintName IS NULL
        BEGIN
            ALTER TABLE PartnerDiscountCodes
            ADD CONSTRAINT DF_PartnerDiscountCodes_IsActive DEFAULT 0 FOR IsActive;
            PRINT '✓ Đã thêm default constraint cho IsActive = 0';
        END
    END
END
GO

