# Test Data và Queries cho Admin Functionalities

## Mô tả

File này chứa các SQL scripts để tạo dữ liệu test và các query để kiểm tra logic và luồng hoạt động của hệ thống Admin.

## Files

### 1. `TestAdminData.sql`
File này chèn dữ liệu test vào database để test các tính năng admin:

**Dữ liệu được tạo:**
- **Users**: Admin, Moderator, và 5 regular users (1 user bị banned)
- **Categories**: 5 categories (Bãi biển, Núi rừng, Thành phố, Ẩm thực, Văn hóa)
- **Posts**: 
  - 3 Pending posts (cần duyệt)
  - 5 Published posts (2 top viewed)
  - 2 Rejected posts
  - 2 Draft posts
  - 1 Deleted post
- **Comments**: 
  - 4 Active comments
  - 2 Deleted comments
- **Reports**:
  - 4 Pending reports
  - 2 Resolved reports
  - 1 Rejected report
- **Partners**:
  - 2 Approved partners
  - 2 Pending partners
- **Ad Packages (PartnerDiscountCodes)**:
  - 2 Active packages
  - 2 Pending packages
- **Coin Transactions**:
  - Earned, Purchased, Spent, Bonus transactions
- **Admin Logs**: Các log hoạt động admin
- **Comment Reactions**: Reactions cho comments

### 2. `TestAdminQueries.sql`
File này chứa các query để kiểm tra và test các tính năng:

**Các nhóm query:**
1. **Dashboard Statistics**: Thống kê tổng quan
2. **User Management**: Quản lý users, filter, search
3. **Content Management**: Quản lý posts, filter theo status
4. **Comment Management**: Quản lý comments
5. **Report Management**: Quản lý reports
6. **Partner Management**: Quản lý partners
7. **Ad Packages**: Quản lý ad packages
8. **Coin Transactions**: Quản lý giao dịch coin
9. **Locations**: Thống kê locations từ posts
10. **Categories**: Quản lý categories
11. **Activity History / Audit Log**: Lịch sử hoạt động admin
12. **Revenue Statistics**: Thống kê doanh thu
13. **Test Workflow Scenarios**: Các scenario test workflow

## Cách sử dụng

### Bước 1: Tạo database và schema
Chạy file `TripCompass_Complete_Schema.sql` để tạo database và các bảng.

### Bước 2: Insert test data
Chạy file `TestAdminData.sql` để chèn dữ liệu test:
```sql
USE TripCompass;
GO
-- Chạy toàn bộ file TestAdminData.sql
```

### Bước 3: Chạy test queries
Chạy file `TestAdminQueries.sql` để kiểm tra các query:
```sql
USE TripCompass;
GO
-- Chạy toàn bộ file TestAdminQueries.sql
-- Hoặc chạy từng section để test từng tính năng
```

## Test Scenarios

### 1. Dashboard Statistics
- Kiểm tra số liệu thống kê tổng quan
- Kiểm tra top viewed posts
- Kiểm tra user growth chart

### 2. User Management
- Test search users
- Test filter by role (Admin, Moderator, User)
- Test filter by status (Active, Banned)
- Test change user role
- Test ban/unban user

### 3. Content Management
- Test filter posts by status (Draft, Pending, Published, Rejected)
- Test search posts
- Test approve/reject posts
- Test change post status workflow
- Test delete/restore posts

### 4. Comment Management
- Test view comments
- Test filter by active/deleted
- Test delete comments
- Test view comment reactions

### 5. Report Management
- Test view pending reports
- Test resolve reports
- Test reject reports
- Test filter by status and target type

### 6. Partner Management
- Test view partners
- Test filter by approval status
- Test approve/reject partners

### 7. Ad Packages
- Test view ad packages
- Test filter by active/pending
- Test approve ad packages

### 8. Coin Transactions
- Test view transactions
- Test filter by type (Earned, Spent, Purchased, etc.)
- Test revenue statistics

### 9. Locations
- Test view locations from posts
- Test location statistics

### 10. Categories
- Test view categories
- Test create/update/delete categories

### 11. Activity History
- Test view admin logs
- Test filter by action type
- Test filter by target table
- Test filter by date range

### 12. Revenue Statistics
- Test daily revenue chart
- Test monthly revenue chart
- Test total revenue summary

## Lưu ý

1. **PostComment**: Trong database column là `CommentId`, nhưng trong C# entity sử dụng `Id` (mapped trong AppDbContext)
2. **Post Status**: 
   - 0 = Draft
   - 1 = Pending
   - 2 = Published
   - 3 = Rejected
   - 4 = Archived
3. **Report Status**:
   - 0 = Pending
   - 1 = Resolved
   - 2 = Rejected
4. Tất cả timestamps sử dụng UTC time

## Cleanup

Để xóa test data và bắt đầu lại:
```sql
-- Xóa dữ liệu test (giữ lại schema)
DELETE FROM AdminLogs;
DELETE FROM CommentReactions;
DELETE FROM PostComments;
DELETE FROM PostCategories;
DELETE FROM Posts;
DELETE FROM PartnerDiscountCodes;
DELETE FROM Partners;
DELETE FROM CoinTransactions;
DELETE FROM Reports;
DELETE FROM Categories;
DELETE FROM Wallets;
DELETE FROM UserRoles;
DELETE FROM Users WHERE UserName LIKE 'user%' OR UserName = 'moderator';
-- Giữ lại admin user
```
