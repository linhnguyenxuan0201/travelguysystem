# Premium Orders Table Migration

## Cách chạy migration

### Option 1: Chạy trực tiếp trong SQL Server Management Studio
1. Mở SQL Server Management Studio
2. Kết nối đến database TripCompass
3. Mở file `AddPremiumOrdersTable.sql`
4. Execute script

### Option 2: Chạy bằng sqlcmd
```bash
sqlcmd -S <server> -d TripCompass -i AddPremiumOrdersTable.sql
```

### Option 3: Chạy trong ứng dụng (nếu có setup)
Nếu bạn có setup để chạy SQL scripts tự động, thêm script này vào danh sách.

## Cấu trúc bảng

- **OrderId**: Primary key, auto increment
- **UserId**: Foreign key đến Users table
- **PlanCode**: Mã gói (Pro/Enterprise)
- **PlanType**: Loại gói (monthly/yearly)
- **Amount**: Số tiền thanh toán
- **Status**: Trạng thái (Pending/Paid/Failed/Cancelled)
- **CreatedAt**: Thời gian tạo đơn
- **PaidAt**: Thời gian thanh toán
- **ExpiresAt**: Thời gian hết hạn plan
- **PaymentRef**: Mã tham chiếu thanh toán
- **TransactionId**: Transaction ID từ webhook

## Indexes

- Index trên UserId để tìm đơn hàng của user
- Index trên Status để filter theo trạng thái
- Index trên CreatedAt để sắp xếp theo thời gian
