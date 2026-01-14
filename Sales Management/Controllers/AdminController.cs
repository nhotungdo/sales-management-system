using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sales_Management.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SalesManagementContext _context;

        public AdminController(SalesManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
             return RedirectToAction("Index", "Home");
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
            var ordersList = await orders.ToListAsync(); // Fetch to memory for easier grouping if Date translation fails, 
                                                         // optimized approach would use groupBy in SQL.
                                                         // For "Month" view, we group by Day. For "Year" view, group by Month.
            
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
            else // Month or Day
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
                .GroupBy(o => o.CreatedByNavigation?.FullName ?? "Không xác định")
                .Select(g => new Sales_Management.Models.ViewModels.ChartData
                {
                    Label = g.Key,
                    Value = g.Sum(o => o.TotalAmount ?? 0)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // Detailed Product/Category Analysis
            // Flatten OrderDetails
            var orderDetails = ordersList.SelectMany(o => o.OrderDetails);

            // Revenue By Category
            viewModel.RevenueByCategory = orderDetails
                .GroupBy(od => od.Product.Category != null ? od.Product.Category.Name : "Chưa phân loại")
                .Select(g => new Sales_Management.Models.ViewModels.ChartData
                {
                    Label = g.Key,
                    Value = g.Sum(od => od.Total ?? 0)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // Top Selling Products
            viewModel.TopProducts = orderDetails
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
