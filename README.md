# 🛒 Sales Management System - ASP.NET Core MVC

![Banner](https://img.shields.io/badge/ASP.NET%20Core%208.0-MVC-blue?style=for-the-badge&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Developing-orange?style=for-the-badge)

Hệ thống Quản lý Bán hàng đa năng được xây dựng trên nền tảng .NET 8, áp dụng kiến trúc 3 lớp (3-Layer Architecture) chuẩn chỉnh, tối ưu cho việc mở rộng và bảo trì.

---

## 🚀 Tính năng nổi bật

### 📦 Quản lý Sản phẩm & Kho hàng
- **Danh mục & Sản phẩm:** Quản lý đa cấp danh mục, hình ảnh sản phẩm chất lượng cao (hỗ trợ WebP).
- **Kho hàng:** Theo dõi lịch sử nhập/xuất kho tự động.
- **Smart Alert:** Cảnh báo khi tồn kho xuống mức thấp.

### 💰 Bán hàng & Thanh toán
- **POS & Cart:** Giỏ hàng thông minh, tích hợp mã giảm giá (Vouchers) và hệ thống tích điểm (Coins).
- **Thanh toán:** Hỗ trợ Ví điện tử (Wallet), thanh toán tiền mặt và đang tích hợp VNPAY/MoMo.
- **Hóa đơn:** Tự động tạo hóa đơn PDF chuyên nghiệp.

### 👥 Quản trị Nhân sự (HRM)
- **Hồ sơ:** Quản lý thông tin nhân viên, vị trí công việc.
- **Chấm công & Lương:** Hệ thống chấm công theo ca (Shifts) và tự động tính bảng lương (Payroll) hàng tháng.
- **Nghỉ phép:** Quy trình nộp và duyệt đơn xin nghỉ phép trực tuyến.

### 🧠 Tính năng Thông minh (Smart Features)
- **SignalR Realtime:** Thông báo tức thời khi có đơn hàng hoặc sự kiện quan trọng.
- **Wishlist & Review:** Khách hàng có thể lưu sản phẩm yêu thích và đánh giá (Verified Purchase).
- **Dashboard:** Thống kê doanh thu, sản phẩm bán chạy qua biểu đồ trực quan (Chart.js).

---

## 🏗️ Kiến trúc dự án (Architecture)

Dự án được chia làm 3 tầng tách biệt:

1. **SalesManagement.DAL (Data Access Layer):** 
   - Quản lý database sử dụng **Entity Framework Core**.
   - Repository Pattern giúp tách biệt logic truy vấn dữ liệu.
2. **SalesManagement.BLL (Business Logic Layer):** 
   - Xử lý toàn bộ logic nghiệp vụ, tính toán lương, kiểm tra tồn kho.
   - Sử dụng DTOs để trao đổi dữ liệu an toàn.
3. **Sales Management (Web Layer):** 
   - Giao diện người dùng (Razor Pages), API Controllers và SignalR Hubs.
   - Tích hợp Authentication & Google OAuth.

---

## 🛠️ Công nghệ sử dụng

- **Backend:** .NET 8 (C#), ASP.NET Core MVC.
- **Database:** SQL Server / Azure SQL.
- **Frontend:** HTML5, Vanilla CSS, JavaScript (ES6+), Bootstrap 5.
- **Thư viện chính:** 
  - Entity Framework Core.
  - SignalR (Realtime notifications).
  - iTextSharp (Export PDF).
  - AutoMapper (Mapping DTOs).
  - SweetAlert2 (Interactive UI).

---

## 💻 Hướng dẫn cài đặt

1. **Clone repository:**
   ```bash
   git clone https://github.com/nhotungdo/sales-management-system.git
   ```
2. **Cấu hình Database:**
   - Mở file `appsettings.json` trong project `Sales Management`.
   - Cập nhật chuỗi kết nối `ConnectionStrings:DBDefault` phù hợp với SQL Server của bạn.
3. **Chạy Migration (Nếu cần):**
   ```bash
   dotnet ef database update --project SalesManagement.DAL
   ```
4. **Chạy ứng dụng:**
   ```bash
   dotnet run --project "Sales Management"
   ```

---

## 🤝 Đóng góp

1. Fork dự án.
2. Tạo nhánh tính năng (`git checkout -b feature/AmazingFeature`).
3. Commit thay đổi (`git commit -m 'Add some AmazingFeature'`).
4. Push lên nhánh (`git push origin feature/AmazingFeature`).
5. Mở một Pull Request.

---

## 📄 Giấy phép

Phân phối theo giấy phép MIT. Xem `LICENSE` để biết thêm thông tin.

---
⭐ **Nếu bạn thấy dự án hữu ích, hãy tặng chúng mình 1 sao nhé!**
