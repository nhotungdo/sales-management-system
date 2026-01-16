using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Areas.Admin.ViewModels;
using Sales_Management.Data;
using System.Globalization;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly SalesManagementContext _context;

        public ReportsController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string timeframe = "month")
        {
            // Default to current month data
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            if (timeframe == "week")
            {
                startDate = now.AddDays(-7);
                endDate = now;
            }
            else if (timeframe == "year")
            {
                startDate = new DateTime(now.Year, 1, 1);
                endDate = new DateTime(now.Year, 12, 31);
            }

            // 1. Fetch Orders in range
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.Status == "Completed" || o.PaymentStatus == "Paid");
            var cancelledOrders = orders.Count(o => o.Status == "Cancelled" || o.Status == "Returned"); // Assuming 'Returned' status exists or 'Cancelled' implies generic failure
            
            // 2. Calculate KPIs
            var totalRevenue = orders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0);
            
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            var completionRate = totalOrders > 0 ? (double)completedOrders / totalOrders * 100 : 0;
            var cancellationRate = totalOrders > 0 ? (double)cancelledOrders / totalOrders * 100 : 0;

            // 3. Chart Data (Daily for month/week, Monthly for year)
            var chartLabels = new List<string>();
            var revenueData = new List<decimal>();
            var ordersData = new List<int>();

            if (timeframe == "year")
            {
                for (int i = 1; i <= 12; i++)
                {
                    chartLabels.Add($"Th {i}");
                    var monthOrders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == i).ToList();
                    ordersData.Add(monthOrders.Count);
                    revenueData.Add(monthOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0));
                }
            }
            else // Daily
            {
                for (var day = startDate; day <= (endDate > now ? now : endDate); day = day.AddDays(1))
                {
                    chartLabels.Add(day.ToString("dd/MM"));
                    var dayOrders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == day.Date).ToList();
                    ordersData.Add(dayOrders.Count);
                    revenueData.Add(dayOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0));
                }
            }

            // 4. Top Products (All time or in range? Let's do in range for consistency)
            // Need to query OrderDetails -> filtered by Orders in range
            var topProductsQuery = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                .GroupBy(od => od.Product.Name)
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(od => od.Quantity),
                    RevenueGenerated = g.Sum(od => od.Total ?? 0) // Or UnitPrice * Quantity
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(5)
                .ToListAsync();

            var viewModel = new ReportsViewModel
            {
                TotalOrders = totalOrders,
                CompletionRate = Math.Round(completionRate, 1),
                AverageOrderValue = Math.Round(avgOrderValue, 0),
                CancellationRate = Math.Round(cancellationRate, 1),
                
                ChartLabels = chartLabels,
                RevenueChartData = revenueData,
                OrdersChartData = ordersData,
                
                TopProducts = topProductsQuery
            };

            ViewData["CurrentTimeframe"] = timeframe;

            return View(viewModel);
        }
    }
}
