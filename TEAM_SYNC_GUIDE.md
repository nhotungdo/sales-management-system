# Hướng dẫn Đồng bộ Code (Sync Guide)

Tài liệu này hướng dẫn các bước cần thực hiện sau khi `pull` code từ nhánh `main` về máy cá nhân để đảm bảo dự án chạy ổn định và không gặp lỗi.

## 1. Cập nhật Nuget Packages
Dự án có thể đã thêm các thư viện mới (ví dụ: `BCrypt.Net`, `xUnit`, `Moq`...). Hãy chạy lệnh sau để tải về các dependency mới nhất:

```bash
dotnet restore
```

## 2. Cập nhật Cơ sở dữ liệu (QUAN TRỌNG)
Gần đây dự án có thay đổi lớn về cấu trúc Database (Schema), bao gồm các bảng:
- `Shifts` (Ca làm việc)
- `TimeAttendances` (Chấm công)
- `Payrolls` (Lương)
- Cập nhật logic `Employees` và `Users`

Nếu không cập nhật DB, bạn sẽ gặp lỗi `SqlException: Invalid column name` hoặc `Foreign key constraint failed`.

### Bước 1: Build lại project để đảm bảo Migrations được nhận diện
```bash
dotnet build
```

### Bước 2: Cập nhật Database
```bash
dotnet ef database update
```

**⚠️ Lưu ý khi gặp lỗi Conflict Dữ liệu (Primary Key / Seed Data):**
Vì trong code có sử dụng `HasData` để seed dữ liệu mẫu (User Admin, Sale, Shift mẫu...), nếu DB của bạn đã có dữ liệu trùng ID nhưng khác nội dung, lệnh update có thể thất bại.

**Cách giải quyết:**
1. **Cách an toàn:** Xóa các dòng dữ liệu bị trùng ID trong DB local của bạn.
2. **Cách triệt để (Khuyên dùng môi trường Dev):** Xóa database cũ và tạo lại mới để đồng bộ hoàn toàn với cấu trúc mới.
   ```bash
   dotnet ef database drop --force
   dotnet ef database update
   ```

## 3. Kiểm tra AppSettings
Kiểm tra file `appsettings.json`. Nếu team có thêm các cấu hình mới (ví dụ: VNPay config, Email config), hãy đảm bảo file local của bạn đã có đủ các key đó.

## 4. Chạy Test
Dự án hiện đã tích hợp Unit Test. Hãy chạy test để đảm bảo môi trường của bạn đã sẵn sàng và code mới không gây lỗi logic nền tảng.
```bash
dotnet test
```

## 5. Các Vấn đề thường gặp (Troubleshooting)

### Lỗi: `Unable to resolve service for type '...'`
- **Nguyên nhân:** Có Service mới được đăng ký trong `Program.cs` nhưng máy bạn chưa build lại.
- **Khắc phục:** `dotnet build --no-incremental`

### Lỗi: `HTTP 500` khi Login/Logout
- **Nguyên nhân:** Logic Authentication thay đổi hoặc thiếu bảng `TimeAttendances` cho logic Auto-Checkout.
- **Khắc phục:** Chạy lại `dotnet ef database update`.

### Lỗi UI bị vỡ / Thiếu CSS
- **Khắc phục:** Xóa cache trình duyệt hoặc chạy `CTRL + F5`.

---
*Vui lòng tuân thủ các bước trên mỗi khi pull code mới!*
