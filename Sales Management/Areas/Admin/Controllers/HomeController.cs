using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using Microsoft.AspNetCore.Authorization;

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
            // Quick Metrics
            var today = DateOnly.FromDateTime(DateTime.Today);
            var yesterday = today.AddDays(-1);

            // Revenue Today (assuming OrderDate is DateTime)
            var revenueToday = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Today && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            var revenueYesterday = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Today.AddDays(-1) && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // Total Orders
            var totalOrdersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Today);
            
            var ordersProcessing = await _context.Orders
                .CountAsync(o => o.Status == "Processing");

            // Low Stock Alerts (Threshold < 10)
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity < 10 && p.Status == "Active")
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .ToListAsync();

            // Employee Stats
            var onlineEmployees = await _context.TimeAttendances
                .CountAsync(t => t.Date == today && t.CheckInTime != null && t.CheckOutTime == null);

            var missedCheckins = await _context.Employees
                .Where(e => !e.TimeAttendances.Any(t => t.Date == today))
                .CountAsync();

            // Chart Data: Revenue Last 7 Days
            var sevenDaysAgo = today.AddDays(-6);
            var chartData = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date >= sevenDaysAgo.ToDateTime(TimeOnly.MinValue) && o.Status != "Cancelled")
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount ?? 0) })
                .ToListAsync();

            var last7Days = Enumerable.Range(0, 7).Select(i => sevenDaysAgo.AddDays(i)).ToList();
            var revenueChartData = last7Days.Select(date => {
                var dataPoint = chartData.FirstOrDefault(d => DateOnly.FromDateTime(d.Date) == date);
                return dataPoint?.Revenue ?? 0;
            }).ToList();
            
            var revenueChartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();

            // Chart Data: Revenue by Category
            var categoryData = await _context.OrderDetails
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderDate.HasValue && od.Order.OrderDate.Value.Date >= sevenDaysAgo.ToDateTime(TimeOnly.MinValue) && od.Order.Status != "Cancelled")
                .GroupBy(od => od.Product.Category.Name)
                .Select(g => new { Label = g.Key, Value = g.Sum(od => od.Total ?? 0) })
                .ToListAsync();

            ViewBag.RevenueToday = revenueToday;
            ViewBag.RevenueGrowth = revenueYesterday > 0 ? ((revenueToday - revenueYesterday) / revenueYesterday) * 100 : 0;
            ViewBag.TotalOrdersToday = totalOrdersToday;
            ViewBag.OrdersProcessing = ordersProcessing;
            ViewBag.OnlineEmployees = onlineEmployees;
            ViewBag.MissedCheckins = missedCheckins;
            ViewBag.LowStockProducts = lowStockProducts;
            
            // Pass Chart Data
            ViewBag.RevenueChartLabels = revenueChartLabels;
            ViewBag.RevenueChartData = revenueChartData;
            ViewBag.CategoryChartLabels = categoryData.Select(c => c.Label).ToList();
            ViewBag.CategoryChartData = categoryData.Select(c => c.Value).ToList();

            return View();
        }

        public async Task<IActionResult> RevenueReport(string period = "Month", DateTime? startDate = null, DateTime? endDate = null)
        {
             // Default to this month
            if (!startDate.HasValue) startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (!endDate.HasValue) endDate = DateTime.Today;

            var viewModel = new Sales_Management.Models.ViewModels.RevenueReportViewModel
            {
                PeriodType = period,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            // Base Query
            var orders = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.Category)
                .Include(o => o.CreatedByNavigation)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate.Value.AddDays(1) && o.Status != "Cancelled");

            // Total Metrics
            viewModel.TotalRevenue = await orders.SumAsync(o => o.TotalAmount ?? 0);
            viewModel.TotalOrders = await orders.CountAsync();

            // Revenue Over Time
            var ordersList = await orders.ToListAsync(); 
            
            IEnumerable<Sales_Management.Models.ViewModels.ChartData> timelineData;

            if (period == "Year")
            {
                // Group by Month
                timelineData = ordersList
                    .Where(o => o.OrderDate.HasValue)
                    .GroupBy(o => String.Format("{0:MMM yyyy}", o.OrderDate!.Value))
                    .Select(g => new Sales_Management.Models.ViewModels.ChartData 
                    { 
                        Label = g.Key, 
                        Value = g.Sum(o => o.TotalAmount ?? 0) 
                    });
            }
            else
            {
                // Group by Day
                timelineData = ordersList
                    .Where(o => o.OrderDate.HasValue)
                    .GroupBy(o => String.Format("{0:dd/MM}", o.OrderDate!.Value))
                    .Select(g => new Sales_Management.Models.ViewModels.ChartData 
                    { 
                        Label = g.Key, 
                        Value = g.Sum(o => o.TotalAmount ?? 0) 
                    });
            }
            viewModel.RevenueOverTime = timelineData.ToList();

            // Revenue by Employee (User)
            viewModel.RevenueByEmployee = ordersList
                .GroupBy(o => o.CreatedByNavigation != null ? o.CreatedByNavigation.FullName : "Không xác định")
                .Select(g => new Sales_Management.Models.ViewModels.ChartData
                {
                    Label = g.Key,
                    Value = g.Sum(o => o.TotalAmount ?? 0)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // Revenue By Category
            viewModel.RevenueByCategory = ordersList.SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.Category != null ? od.Product.Category.Name : "Chưa phân loại")
                .Select(g => new Sales_Management.Models.ViewModels.ChartData
                {
                    Label = g.Key,
                    Value = g.Sum(od => od.Total ?? 0)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // Top Selling Products
            viewModel.TopProducts = ordersList.SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product)
                .Select(g => new Sales_Management.Models.ViewModels.ProductPerformance
                {
                    ProductName = g.Key.Name,
                    Category = g.Key.Category != null ? g.Key.Category.Name : "N/A",
                    QuantitySold = g.Sum(od => od.Quantity),
                    RevenueGenerated = g.Sum(od => od.Total ?? 0)
                })
                .OrderByDescending(p => p.RevenueGenerated)
                .Take(10)
                .ToList();

             return View(viewModel);
        }
    }
}
