using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Areas.Admin.ViewModels;
using Sales_Management.Data;
using Sales_Management.Models;
using System.Globalization;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly SalesManagementContext _context;

        public HomeController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy Số Liệu Thống Kê Chính
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
                .SumAsync(o => o.TotalAmount) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending" || o.Status == "Processing");

            // Người dùng mới (ví dụ: đăng ký trong tháng này)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedDate.Month == currentMonth && u.CreatedDate.Year == currentYear);

            // 2. Lấy Các Giao Dịch Gần Đây
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 3. Chuẩn Bị Dữ Liệu Biểu Đồ (Doanh thu theo tháng cho năm hiện tại)
            var monthlyRevenues = await _context.Orders
                .Where(o => o.OrderDate.HasValue && 
                            o.OrderDate.Value.Year == currentYear && 
                            (o.Status == "Completed" || o.PaymentStatus == "Paid"))
                .GroupBy(o => o.OrderDate.Value.Month)
                .Select(g => new { 
                    Month = g.Key, 
                    Total = g.Sum(o => o.TotalAmount) ?? 0 
                })
                .ToListAsync();

            var revenueData = Enumerable.Range(1, 12).Select(month => {
                var monthData = monthlyRevenues.FirstOrDefault(m => m.Month == month);
                return monthData != null ? (int)monthData.Total : 0;
            }).ToList();

            // 4. Chuẩn Bị Dữ Liệu Biểu Đồ (Doanh số theo Danh mục)
            // Cần join OrderDetail -> Product -> Category
            // Lọc bỏ các Danh mục null để tránh lỗi runtime
            var categoryStats = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Product.Category != null)
                .GroupBy(od => od.Product.Category.Name)
                .Select(g => new { 
                    CategoryName = g.Key, 
                    Count = g.Sum(od => od.Quantity) 
                }) 
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var categoryLabels = categoryStats.Select(x => x.CategoryName ?? "Unknown").ToList();
            var categoryData = categoryStats.Select(x => x.Count).ToList();

            var viewModel = new DashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                NewUsers = newUsers,
                RecentOrders = recentOrders ?? new List<Order>(),
                RevenueData = revenueData,
                CategoryLabels = categoryLabels,
                CategoryData = categoryData
            };

            return View(viewModel);
        }
    }
}