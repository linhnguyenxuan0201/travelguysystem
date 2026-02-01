# Hướng dẫn sử dụng file SQL TripCompass_Complete_Schema.sql

## ✅ File SQL đã được sửa đầy đủ

File `TripCompass_Complete_Schema.sql` đã được tạo với các đặc điểm:

### ✨ Đã sửa các vấn đề:

1. **✅ Kiểu dữ liệu đúng:**
   - Sử dụng `BIGINT IDENTITY` thay vì `UNIQUEIDENTIFIER`
   - Khớp 100% với entity C# (long trong C#)

2. **✅ Đầy đủ các bảng:**
   - Users (có ReputationScore, ReputationLevel, IsBanned)
   - Roles, UserRoles
   - Wallets, UserAvatars, UserPlans, UserFollows
   - Categories, Posts, PostCategories, PostImages
   - PostComments, PostReactions, CommentReactions
   - Partners, PartnerDiscountCodes, PartnerAgreements
   - PostBookings (đầy đủ các cột payment, commission, refund)
   - PremiumOrders
   - ChatThreads, ChatMessages (có ImageUrl và MessageType)
   - Notifications
   - Reports, AdminLogs
   - CoinTransactions, EmailOtps

3. **✅ Indexes và Foreign Keys:**
   - Tất cả foreign keys đã được thiết lập
   - Indexes tối ưu cho performance
   - Unique constraints đúng

## 📋 Cách sử dụng

### Bước 1: Chạy file SQL

1. Mở **SQL Server Management Studio (SSMS)** hoặc **Azure Data Studio**
2. Kết nối đến SQL Server của bạn
3. Mở file `TripCompass_Complete_Schema.sql`
4. Chạy toàn bộ script (F5)

### Bước 2: Kiểm tra kết nối

Đảm bảo connection string trong `appsettings.json` đúng:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TripCompass;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### Bước 3: Chạy ứng dụng

1. Mở dự án trong Visual Studio
2. Build solution (Ctrl+Shift+B)
3. Chạy ứng dụng (F5)

Ứng dụng sẽ tự động:
- Kết nối đến database
- Chạy `DbSeeder` để tạo dữ liệu mẫu
- Tạo admin user mặc định

### Bước 4: Đăng nhập

Sau khi chạy ứng dụng, bạn có thể đăng nhập với:

- **Email:** `admin@tripcompass.com`
- **Password:** `Admin123!`

## ⚠️ Lưu ý

1. **Nếu database đã tồn tại:**
   - Script sẽ DROP database cũ và tạo mới
   - **CẢNH BÁO:** Tất cả dữ liệu cũ sẽ bị mất!

2. **Nếu muốn giữ dữ liệu cũ:**
   - Backup database trước khi chạy
   - Hoặc comment phần DROP DATABASE trong script

3. **SQL Server Authentication:**
   - Nếu dùng SQL Authentication thay vì Windows Authentication
   - Sửa connection string:
   ```json
   "DefaultConnection": "Server=localhost;Database=TripCompass;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
   ```

## 🔍 Kiểm tra sau khi chạy

Chạy query sau để kiểm tra:

```sql
USE TripCompass;
GO

-- Kiểm tra số lượng bảng
SELECT COUNT(*) AS TableCount FROM sys.tables;
-- Kết quả mong đợi: 26 bảng

-- Kiểm tra Users table
SELECT TOP 5 * FROM Users;

-- Kiểm tra Roles
SELECT * FROM Roles;

-- Kiểm tra admin user
SELECT u.*, r.RoleName 
FROM Users u
JOIN UserRoles ur ON u.UserId = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = 'admin@tripcompass.com';
```

## ✅ Kết quả mong đợi

Sau khi chạy thành công:

- ✅ Database `TripCompass` được tạo
- ✅ 26 bảng được tạo với đầy đủ cấu trúc
- ✅ 4 roles được seed: Admin, Moderator, User, Partner
- ✅ Ứng dụng có thể kết nối và chạy bình thường

## 🆘 Xử lý lỗi

### Lỗi: "Cannot connect to SQL Server"
- Kiểm tra SQL Server đang chạy
- Kiểm tra SQL Server Browser service
- Kiểm tra firewall

### Lỗi: "Database already exists"
- Script sẽ tự động DROP database cũ
- Nếu vẫn lỗi, chạy thủ công:
  ```sql
  ALTER DATABASE TripCompass SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE TripCompass;
  ```

### Lỗi: "Login failed"
- Kiểm tra Windows Authentication hoặc SQL Authentication
- Sửa connection string nếu cần

## 📞 Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. SQL Server version (nên dùng SQL Server 2019 trở lên)
2. Connection string trong appsettings.json
3. Logs trong Visual Studio Output window
