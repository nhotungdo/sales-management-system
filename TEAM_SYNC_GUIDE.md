# Hướng dẫn Đồng bộ Code (Sync Guide)

Tài liệu này hướng dẫn các bước cần thực hiện sau khi `pull` code từ nhánh `main` về máy cá nhân để đảm bảo dự án chạy ổn định và không gặp lỗi.

## ⚠️ CẢNH BÁO QUAN TRỌNG: MIGRATIONS ĐÃ BỊ RESET ⚠️

**Dự án đã thực hiện xóa và làm mới lại toàn bộ Migrations.**
Điều này có nghĩa là lịch sử cập nhật Database cũ đã không còn khớp với code hiện tại.

Nếu bạn `pull` code về và chạy `dotnet ef database update` trên database cũ, bạn **CHẮC CHẮN SẼ GẶP LỖI**:
> *System.Data.SqlClient.SqlException: There is already an object named 'Users' in the database.*

### ✅ CÁCH KHẮC PHỤC DUY NHẤT (Khuyên dùng):
Bạn bắt buộc phải **XÓA DATABASE CŨ** và tạo lại từ đầu để đồng bộ với bộ Migrations mới.

Thực hiện lần lượt các lệnh sau:

1. **Xóa Database cũ:**
   ```bash
   dotnet ef database drop --force
   ```
   *(Lưu ý: Hành động này sẽ xóa toàn bộ dữ liệu trong DB local của bạn via code)*

2. **Cập nhật Database mới:**
   ```bash
   dotnet ef database update
   ```

---

## Các bước đồng bộ thông thường khác

### 1. Cập nhật Nuget Packages
Chạy lệnh sau để tải về các dependency mới nhất (VD: xUnit, Moq...):
```bash
dotnet restore
```

### 2. Kiểm tra AppSettings
Kiểm tra file `appsettings.json`. Đảm bảo connection string và các cấu hình khác (VNPay, Email) vẫn đúng với môi trường local của bạn.

### 3. Chạy Test
Dự án đã tích hợp Unit Test. Hãy chạy test để đảm bảo môi trường ổn định:
```bash
dotnet test
```

## Các Vấn đề thường gặp (Troubleshooting)

### Lỗi: `Unable to resolve service for type '...'`
- **Nguyên nhân:** Có Service mới được đăng ký trong `Program.cs`.
- **Khắc phục:** `dotnet build --no-incremental`

### Lỗi: `HTTP 500` khi Login/Logout
- **Nguyên nhân:** Thiếu bảng hoặc cột mới trong DB (User, TimeAttendances...).
- **Khắc phục:** Làm theo hướng dẫn **"Xóa Database cũ"** ở trên.

### Lỗi UI bị vỡ / Thiếu CSS
- **Khắc phục:** Xóa cache trình duyệt hoặc chạy `CTRL + F5`.
