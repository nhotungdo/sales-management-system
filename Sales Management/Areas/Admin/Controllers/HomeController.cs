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
            // 1. Fetch Key Metrics
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
                .SumAsync(o => o.TotalAmount) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending" || o.Status == "Processing");

            // New Users (e.g., created this month)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedDate.Month == currentMonth && u.CreatedDate.Year == currentYear);

            // 2. Fetch Recent Transactions
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 3. Prepare Chart Data (Revenue per Month for current year)
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

            // 4. Prepare Chart Data (Sales by Category)
            // Need to join OrderDetail -> Product -> Category
            // Filtering out null Categories to prevent runtime errors
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