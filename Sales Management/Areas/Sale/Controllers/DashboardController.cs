using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class DashboardController : Controller
    {
        private readonly SalesManagementContext _context;

        public DashboardController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Calculate Total Revenues (Paid orders)
            var totalRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .SumAsync(o => o.TotalAmount);

            // 2. Count Orders today
            var today = DateTime.Today;
            var ordersToday = await _context.Orders
                .CountAsync(o => o.OrderDate >= today);

            // 3. Low Stock Products
            var lowStockCount = await _context.Products
                .CountAsync(p => p.StockQuantity < 10 && p.Status == "Active");

            // 4. Recent Orders
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 5. Top Selling Products
            // Step 1: Get stats by ProductId
            var topStats = await _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .Select(g => new 
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Total)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            // Step 2: Load Product details
            var productIds = topStats.Select(x => x.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            // Step 3: Combine
            var topProducts = topStats.Select(stat => new 
            {
                Product = products.FirstOrDefault(p => p.ProductId == stat.ProductId),
                stat.TotalSold,
                stat.Revenue
            }).ToList();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.OrdersToday = ordersToday;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.TopProducts = topProducts;

            return View();
        }
    }
}
