# Phân tích Database Schema - TripCompass

## ⚠️ VẤN ĐỀ PHÁT HIỆN

### 1. **Lỗi nghiêm trọng: Kiểu dữ liệu không khớp**

File SQL bạn cung cấp (`TripCompass4 (2).sql`) sử dụng:
- `UNIQUEIDENTIFIER` (GUID) cho UserId và các ID khác
- `INT IDENTITY` cho một số ID

Nhưng dự án C# sử dụng:
- `BIGINT IDENTITY` (long trong C#) cho tất cả ID

**Hậu quả:** Entity Framework sẽ không thể map được với database, dự án sẽ **KHÔNG CHẠY ĐƯỢC**.

### 2. **Thiếu các bảng**

File SQL thiếu các bảng sau mà DbContext yêu cầu:
- ❌ `UserAvatars` 
- ❌ `UserPlans`
- ❌ `UserFollows`
- ❌ `CommentReactions`

### 3. **Khác biệt về cấu trúc**

Một số bảng có cấu trúc khác với entity C#:
- `Users`: Thiếu `ReputationScore`, `ReputationLevel`, `IsBanned`
- `Posts`: Thiếu nhiều cột như `Location`, `OpeningHours`, `Phone`, `ParkingInfo`, `IsPartner`, `IsFeatured`, etc.
- `PostBookings`: Cấu trúc khác (thiếu nhiều cột như `CustomerName`, `CustomerPhone`, `PaymentMethod`, etc.)

## ✅ GIẢI PHÁP

### **Cách 1: Sử dụng file SQL có sẵn trong dự án (KHUYẾN NGHỊ)**

Dự án đã có file `TripCompass.Infrastructure/Database/CompleteDatabase.sql` nhưng cũng chưa đầy đủ. 

**Các bước:**
1. Chạy `CompleteDatabase.sql` để tạo database cơ bản
2. Chạy các migration script còn thiếu theo thứ tự:
   - `AddUserAvatarsTable.sql`
   - `AddUserFollowTable.sql` (nếu có)
   - `AddCommentReactionsTable.sql`
   - `AddPartnersTable.sql`
   - `AddPartnerDiscountCodesTable.sql`
   - `AddPartnerAgreementsTable.sql`
   - `AddPostBookingsTable.sql`
   - `AddNotificationsTable.sql`
   - `CreateChatTables.sql`
   - `AddPremiumOrdersTable.sql`

### **Cách 2: Sửa file SQL của bạn**

Tôi sẽ tạo một file SQL đã được sửa để khớp với dự án. File này sẽ:
- ✅ Sử dụng `BIGINT IDENTITY` thay vì `UNIQUEIDENTIFIER`
- ✅ Bao gồm tất cả các bảng cần thiết
- ✅ Khớp với cấu trúc entity trong C#

## 📋 CHECKLIST TRƯỚC KHI CHẠY

- [ ] Database đã được tạo với đúng schema (BIGINT cho ID)
- [ ] Connection string trong `appsettings.json` đúng
- [ ] SQL Server đang chạy và có thể kết nối
- [ ] Tất cả các bảng đã được tạo
- [ ] Foreign keys đã được thiết lập đúng

## 🔍 KIỂM TRA KẾT NỐI

Connection string hiện tại:
```json
"DefaultConnection": "Server=localhost;Database=TripCompass;Trusted_Connection=True;TrustServerCertificate=True"
```

Đảm bảo:
- SQL Server đang chạy
- Database `TripCompass` đã được tạo
- Windows Authentication hoặc SQL Authentication đúng

## 📝 GHI CHÚ

Nếu bạn muốn tôi tạo file SQL đã sửa hoàn chỉnh, hãy cho tôi biết. Tôi sẽ tạo một file mới với:
- Tất cả các bảng cần thiết
- Đúng kiểu dữ liệu (BIGINT)
- Đầy đủ indexes và foreign keys
- Seed data cơ bản
