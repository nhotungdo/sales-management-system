using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Areas.Admin.ViewModels;
using Sales_Management.Data;
using Sales_Management.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index(string timeframe = "month", DateTime? startDate = null, DateTime? endDate = null)
        {   
            // Xác định time báo cáo
            var range = GetDateRange(timeframe, startDate, endDate);
            var start = range.Start;
            var end = range.End;
            
            // Lấy list orders trong khoảng thời gian
            var currentOrders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .ToListAsync();
            
            // Lấy list orders của kỳ trước để so sánh với kỳ hiện tại
            var duration = end - start;
            var prevStart = start.Subtract(duration);
            var prevEnd = start.AddSeconds(-1);
            var prevOrders = await _context.Orders
                .Where(o => o.OrderDate >= prevStart && o.OrderDate <= prevEnd)
                .ToListAsync();

            // Tính doanh thu trong kỳ hiện tại
            var currentRevenue = currentOrders
                .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
                .Sum(o => o.TotalAmount ?? 0);
            // Tính doanh thu trong kỳ trước
            var prevRevenue = prevOrders
                .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
                .Sum(o => o.TotalAmount ?? 0);
            
            // Tính doanh thu tăng trưởng
            var revenueGrowth = prevRevenue > 0 ? ((currentRevenue - prevRevenue) / prevRevenue) * 100 : 100;
            
            // Fix: tính toán OrdersGrowth yêu cầu chuyển đổi sang kiểu decimal
            decimal ordersGrowth = 100;
            if (prevOrders.Count > 0)
            {
                 ordersGrowth = (decimal)(((double)(currentOrders.Count - prevOrders.Count) / prevOrders.Count) * 100);
            }

            var model = new RevenueReportViewModel
            {
                Timeframe = timeframe,
                StartDate = start,
                EndDate = end,
                TotalOrders = currentOrders.Count,
                TotalRevenue = currentRevenue,
                AverageOrderValue = currentOrders.Count > 0 ? currentRevenue / currentOrders.Count : 0,
                CompletionRate = currentOrders.Count > 0 ? (double)currentOrders.Count(o => o.Status == "Completed" || o.PaymentStatus == "Paid") / currentOrders.Count * 100 : 0,
                CancellationRate = currentOrders.Count > 0 ? (double)currentOrders.Count(o => o.Status == "Cancelled" || o.Status == "Returned") / currentOrders.Count * 100 : 0,
                RevenueGrowth = Math.Round(revenueGrowth, 1),
                OrdersGrowth = Math.Round(ordersGrowth, 1)
            };
            // Chuẩn bị dữ liệu biểu đồ doanh thu
            PrepareRevenueChartData(model, currentOrders, timeframe, start, end);

            return View(model);
        }

        // View báo cáo sản phẩm
        public async Task<IActionResult> Products(DateTime? startDate = null, DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? new DateTime(end.Year, end.Month, 1);

            var model = new ProductReportViewModel
            {
                StartDate = start,
                EndDate = end
            };

            // Thống kê Top 10 sản phẩm bán chạy nhất
            var topSellingQuery = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate >= start && od.Order.OrderDate <= end && (od.Order.Status == "Completed" || od.Order.PaymentStatus == "Paid"))
                .ToListAsync();

            model.TopSellingProducts = topSellingQuery
                .GroupBy(od => new { od.ProductId, od.Product.Name, od.Product.Code })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.Name,
                    ProductCode = g.Key.Code,
                    UnitsSold = g.Sum(x => x.Quantity),
                    RevenueGenerated = g.Sum(x => x.Total ?? 0)
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(10)
                .ToList();

            var allProducts = await _context.Products.ToListAsync();
            
            // Thống kê sản phẩm sắp hết hàng (tồn < 10)
            model.LowStockProducts = allProducts
                .Where(p => (p.StockQuantity ?? 0) < 10)
                .Select(p => new ProductInventoryViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Code = p.Code,
                    StockQuantity = p.StockQuantity ?? 0,
                    Value = (p.StockQuantity ?? 0) * p.SellingPrice
                })
                .OrderBy(p => p.StockQuantity)
                .ToList();

            // Thống kê hàng tồn kho lâu ngày (không cập nhật > 30 ngày)
            var thresholdDate = DateTime.Now.AddDays(-30);
            model.LongInStockProducts = allProducts
                .Where(p => (p.StockQuantity ?? 0) > 0 && (p.UpdatedDate ?? DateTime.MaxValue) < thresholdDate)
                .Select(p => new ProductInventoryViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Code = p.Code,
                    StockQuantity = p.StockQuantity ?? 0,
                    DaysInStock = (DateTime.Now - (p.UpdatedDate ?? p.CreatedDate ?? DateTime.Now)).Days,
                    Value = (p.StockQuantity ?? 0) * p.SellingPrice
                })
                .OrderByDescending(p => p.DaysInStock)
                .ToList();

            model.TotalItemsInStock = allProducts.Count;
            model.TotalInventoryValue = allProducts.Sum(p => (p.StockQuantity ?? 0) * p.SellingPrice);
            
            // Tính toán tỷ lệ quay vòng hàng tồn kho
            decimal salesRevenue = model.TopSellingProducts.Sum(x => x.RevenueGenerated);
            model.InventoryTurnoverRate = model.TotalInventoryValue > 0 ? salesRevenue / model.TotalInventoryValue : 0;

            return View(model);
        }

        // Báo cáo Tài chính (Financials)
        public async Task<IActionResult> Financials(DateTime? startDate = null, DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? new DateTime(end.Year, end.Month, 1);

            var model = new FinancialReportViewModel
            {
                StartDate = start,
                EndDate = end
            };

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .ToListAsync();

            // Thống kê trạng thái hóa đơn (Đã thanh toán, Chờ, Hoàn tiền)
            model.InvoiceStats = new List<InvoiceStatViewModel>
            {
                new InvoiceStatViewModel { Status = "Paid", Count = orders.Count(o => o.Status == "Completed" || o.PaymentStatus == "Paid"), TotalAmount = orders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0) },
                new InvoiceStatViewModel { Status = "Pending", Count = orders.Count(o => o.PaymentStatus == "Pending"), TotalAmount = orders.Where(o => o.PaymentStatus == "Pending").Sum(o => o.TotalAmount ?? 0) },
                new InvoiceStatViewModel { Status = "Refunded", Count = orders.Count(o => o.Status == "Returned"), TotalAmount = orders.Where(o => o.Status == "Returned").Sum(o => o.TotalAmount ?? 0) }
            };

            var walletTrans = await _context.WalletTransactions
                .Where(w => w.CreatedDate >= start && w.CreatedDate <= end && w.Status == "Success")
                .ToListAsync();

            // Tổng tiền vào/ra ví (chỉ tính giao dịch thành công)
            model.TotalCashIn = walletTrans.Where(w => w.Amount > 0).Sum(w => w.Amount);
            model.TotalCashOut = walletTrans.Where(w => w.Amount < 0).Sum(w => Math.Abs(w.Amount));

            var revenue = model.InvoiceStats.FirstOrDefault(x => x.Status == "Paid")?.TotalAmount ?? 0;
            model.NetRevenue = revenue;

            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                model.ChartLabels.Add(day.ToString("dd/MM"));
                
                var dailyIn = walletTrans
                    .Where(w => w.CreatedDate.HasValue && w.CreatedDate.Value.Date == day && w.Amount > 0)
                    .Sum(w => w.Amount);
                    
                var dailyOut = walletTrans
                    .Where(w => w.CreatedDate.HasValue && w.CreatedDate.Value.Date == day && w.Amount < 0)
                    .Sum(w => w.Amount);

                model.CashInChartData.Add(dailyIn);
                model.CashOutChartData.Add(Math.Abs(dailyOut));
            }

            return View(model);
        }

        // Xuất báo cáo Doanh thu ra CSV
        public async Task<IActionResult> ExportRevenue(string timeframe)
        {
            var range = GetDateRange(timeframe, null, null);
            var start = range.Start; 
            var end = range.End;
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .Select(o => new { o.OrderId, o.OrderDate, o.TotalAmount, o.Status })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("OrderId,Date,Amount,Status");
            foreach (var o in orders)
            {
                csv.AppendLine($"{o.OrderId},{o.OrderDate},{o.TotalAmount},{o.Status}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"RevenueReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Xuất báo cáo Tồn kho ra CSV
        public async Task<IActionResult> ExportProducts()
        {
            var products = await _context.Products.ToListAsync();
             var csv = new StringBuilder();
            csv.AppendLine("Id,Code,Name,Stock,Price,Value");
            foreach (var p in products)
            {
                csv.AppendLine($"{p.ProductId},{p.Code},{p.Name},{p.StockQuantity},{p.SellingPrice},{p.StockQuantity * p.SellingPrice}");
            }
             return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"InventoryReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Helper: Xác định khoảng thời gian báo cáo
        private (DateTime Start, DateTime End) GetDateRange(string timeframe, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue) return (start.Value, end.Value);

             var now = DateTime.Now;
             if (timeframe == "week") return (now.AddDays(-7), now);
             if (timeframe == "year") return (new DateTime(now.Year, 1, 1), new DateTime(now.Year, 12, 31));
             if (timeframe == "all") return (DateTime.MinValue, DateTime.MaxValue);
             
             // Default: Month
             return (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1));
        }

        private void PrepareRevenueChartData(RevenueReportViewModel model, List<Order> orders, string timeframe, DateTime start, DateTime end)
        {
             if (timeframe == "year")
            {
                for (int i = 1; i <= 12; i++)
                {
                    model.ChartLabels.Add($"T{i}");
                    var monthOrders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == i).ToList();
                    model.OrdersChartData.Add(monthOrders.Count);
                    model.RevenueChartData.Add(monthOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0));
                }
            }
             else if (timeframe == "all") 
            {
                 // Group by Month/Year for "All Time" to avoid overcrowding
                 // Assuming data spans multiple months
                 var groupedOrders = orders
                    .Where(o => o.OrderDate.HasValue)
                    .GroupBy(o => new { o.OrderDate.Value.Year, o.OrderDate.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToList();
                
                 foreach (var group in groupedOrders)
                 {
                     model.ChartLabels.Add($"{group.Key.Month}/{group.Key.Year}");
                     model.OrdersChartData.Add(group.Count());
                     model.RevenueChartData.Add(group.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0));
                 }
                 
                 // Handle empty case
                 if (!model.ChartLabels.Any())
                 {
                     model.ChartLabels.Add("No Data");
                     model.OrdersChartData.Add(0);
                     model.RevenueChartData.Add(0);
                 }
            }
            else
            {
                 for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
                {
                    model.ChartLabels.Add(day.ToString("dd/MM"));
                    var dayOrders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == day).ToList();
                    model.OrdersChartData.Add(dayOrders.Count);
                    model.RevenueChartData.Add(dayOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0));
                }
            }
        }
    }
}
