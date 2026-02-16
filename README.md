# Sales Management System

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Status](https://img.shields.io/badge/status-active-brightgreen.svg)](#)

Hệ thống quản lý bán hàng toàn diện cho các doanh nghiệp bán lẻ và phân phối. Cung cấp các công cụ mạnh mẽ để quản lý khách hàng, sản phẩm, đơn hàng, thanh toán, nhân sự và báo cáo doanh thu.

**[English Version](#english-version)**

## 📋 Mục đích

Sales Management System được thiết kế để giúp doanh nghiệp:
- ✅ Quản lý khách hàng và thông tin liên hệ
- ✅ Quản lý sản phẩm, kho hàng và danh mục
- ✅ Tạo và theo dõi đơn hàng
- ✅ Quản lý thanh toán qua ví điện tử
- ✅ Quản lý nhân viên, lương và chấm công
- ✅ Thiết lập khuyến mãi và voucher
- ✅ Báo cáo doanh thu và phân tích kinh doanh

## 🛠️ Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server / Entity Framework Core
- **Frontend**: Razor Pages, HTML5, CSS3, JavaScript
- **Real-time**: SignalR (SystemHub)
- **API**: RESTful API
- **Middleware**: Custom session tracking

## 📁 Cấu trúc dự án

```
Sales Management/
├── Areas/                    # Các khu vực chức năng quản trị
│   ├── Admin/               # Admin dashboard
│   └── Sale/                # Bán hàng
├── Controllers/             # Xử lý yêu cầu HTTP
├── Models/                  # Các mô hình dữ liệu
├── Services/                # Lôgic kinh doanh
├── Data/                    # DbContext và cấu hình database
├── Migrations/              # Entity Framework migrations
├── Views/                   # Razor Pages
├── ViewComponents/          # Các component tái sử dụng
├── Hubs/                    # SignalR hubs (real-time)
├── Middleware/              # Custom middleware
├── wwwroot/                 # Static files (CSS, JS, images)
└── Properties/              # Configuration
```

## 🚀 Cách sử dụng

### Yêu cầu
- .NET 8.0 SDK hoặc cao hơn
- SQL Server 2019 hoặc cao hơn
- Visual Studio 2022 hoặc Visual Studio Code

### Cài đặt

1. **Clone repository**
   ```bash
   git clone https://github.com/nhotungdo/sales-management-system.git
   cd sales-management-system
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Cấu hình database**
   - Chỉnh sửa `appsettings.json` với connection string của SQL Server:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=SalesManagement;Trusted_Connection=true;"
     }
   }
   ```

4. **Chạy Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Khởi chạy ứng dụng**
   ```bash
   dotnet run
   ```

   Ứng dụng sẽ chạy tại: `https://localhost:5001`

## 📌 Tính năng chính

### Quản lý Khách hàng
- CRUD khách hàng
- Lịch sử đơn hàng
- Quản lý liên hệ

### Quản lý Sản phẩm
- Danh sách sản phẩm
- Phân loại theo danh mục
- Hình ảnh sản phẩm
- Theo dõi kho hàng

### Đơn hàng & Hóa đơn
- Tạo đơn hàng
- Quản lý chi tiết đơn hàng
- Tạo hóa đơn
- Theo dõi trạng thái

### Ví Điện tử
- Nạp tiền vào ví
- Theo dõi giao dịch
- Thanh toán qua ví

### Nhân sự & Payroll
- Quản lý nhân viên
- Chấm công
- Quản lý ca làm việc
- Tính lương (salary components)
- Yêu cầu nghỉ phép

### Khuyến mãi & Voucher
- Tạo khuyến mãi
- Quản lý mã voucher
- Theo dõi hạn sử dụng
- Gói VIP (VipPackage)

### Báo cáo & Phân tích
- Báo cáo doanh thu
- Lịch sử chuyển đổi tiền tệ
- Theo dõi giao dịch kho

## 🔐 Bảo mật

- Xác thực người dùng
- Phân quyền theo vai trò
- Session tracking
- Kiểm toán giao dịch

## 📞 Liên hệ & Support

Nếu bạn gặp vấn đề hoặc có câu hỏi:
- Mở issue trên GitHub
- Liên hệ qua email

## 📄 Giấy phép

Dự án này được cấp phép dưới MIT License - xem file [LICENSE](LICENSE) để biết chi tiết.

## 👥 Tác giả

- **Developer**: nhotungdo
- **Repository**: [github.com/nhotungdo/sales-management-system](https://github.com/nhotungdo/sales-management-system)

---

## English Version

### Project Name: Sales Management System

A comprehensive sales management system for retail and distribution businesses. Provides powerful tools to manage customers, products, orders, payments, staff and sales revenue.

### Key Features

- Customer Management
- Product & Inventory Management
- Order & Invoice Management
- Wallet & Payment System
- Employee & Payroll Management
- Promotions & Voucher System
- Attendance Tracking
- Sales Report & Analytics

### Getting Started

1. Clone the repository
2. Restore dependencies: `dotnet restore`
3. Configure `appsettings.json`
4. Run migrations: `dotnet ef database update`
5. Start the app: `dotnet run`

### Technology Stack

- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server
- SignalR for real-time features

For more details, see the Vietnamese version above.
