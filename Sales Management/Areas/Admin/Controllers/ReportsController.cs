using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Web.Areas.Admin.ViewModels;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;
using SalesManagement.BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index(string timeframe = "month", DateTime? startDate = null, DateTime? endDate = null)
        {
            var data = await _reportService.GetRevenueReportAsync(timeframe, startDate, endDate);
            
            DateTime start = data.Start;
            DateTime end = data.End;
            List<Order> currentOrders = data.CurrentOrders ?? new List<Order>();
            decimal currentRevenue = data.TotalRevenue;
            decimal prevRevenue = data.PrevRevenue;
            int prevOrdersCount = data.PrevOrdersCount;

            var revenueGrowth = prevRevenue > 0 ? ((currentRevenue - prevRevenue) / prevRevenue) * 100 : 100;
            decimal ordersGrowth = prevOrdersCount > 0 ? (decimal)(((double)(currentOrders.Count - prevOrdersCount) / prevOrdersCount) * 100) : 100;

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

            PrepareRevenueChartData(model, currentOrders, timeframe, start, end);
            return View(model);
        }

        public async Task<IActionResult> Products(DateTime? startDate = null, DateTime? endDate = null)
        {
            var data = await _reportService.GetProductReportAsync(startDate, endDate);
            
            DateTime start = data.Start;
            DateTime end = data.End;
            List<OrderDetail> topSellingQuery = data.TopSellingRaw ?? new List<OrderDetail>();
            List<Product> allProducts = data.AllProducts ?? new List<Product>();

            var model = new ProductReportViewModel
            {
                StartDate = start,
                EndDate = end,
                TopSellingProducts = topSellingQuery
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
                    .ToList(),
                LowStockProducts = allProducts.Where(p => (p.StockQuantity ?? 0) < 10)
                    .Select(p => new ProductInventoryViewModel { ProductId = p.ProductId, Name = p.Name, Code = p.Code, StockQuantity = p.StockQuantity ?? 0, Value = (p.StockQuantity ?? 0) * p.SellingPrice })
                    .OrderBy(p => p.StockQuantity).ToList(),
                TotalItemsInStock = allProducts.Count,
                TotalInventoryValue = allProducts.Sum(p => (p.StockQuantity ?? 0) * p.SellingPrice)
            };

            return View(model);
        }

        public async Task<IActionResult> Financials(DateTime? startDate = null, DateTime? endDate = null)
        {
            var data = await _reportService.GetFinancialReportAsync(startDate, endDate);
            DateTime start = data.Start;
            DateTime end = data.End;
            List<Order> orders = data.Orders ?? new List<Order>();
            List<WalletTransaction> walletTrans = data.WalletTransactions ?? new List<WalletTransaction>();

            var model = new FinancialReportViewModel
            {
                StartDate = start,
                EndDate = end,
                InvoiceStats = new List<InvoiceStatViewModel>
                {
                    new InvoiceStatViewModel { Status = "Paid", Count = orders.Count(o => o.Status == "Completed" || o.PaymentStatus == "Paid"), TotalAmount = orders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0) },
                    new InvoiceStatViewModel { Status = "Pending", Count = orders.Count(o => o.PaymentStatus == "Pending"), TotalAmount = orders.Where(o => o.PaymentStatus == "Pending").Sum(o => o.TotalAmount ?? 0) },
                    new InvoiceStatViewModel { Status = "Refunded", Count = orders.Count(o => o.Status == "Returned"), TotalAmount = orders.Where(o => o.Status == "Returned").Sum(o => o.TotalAmount ?? 0) }
                },
                TotalCashIn = walletTrans.Where(w => w.Amount > 0).Sum(w => w.Amount),
                TotalCashOut = walletTrans.Where(w => w.Amount < 0).Sum(w => Math.Abs(w.Amount))
            };
            model.NetRevenue = model.InvoiceStats.FirstOrDefault(x => x.Status == "Paid")?.TotalAmount ?? 0;

            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                model.ChartLabels.Add(day.ToString("dd/MM"));
                model.CashInChartData.Add(walletTrans.Where(w => w.CreatedDate.HasValue && w.CreatedDate.Value.Date == day && w.Amount > 0).Sum(w => w.Amount));
                model.CashOutChartData.Add(Math.Abs(walletTrans.Where(w => w.CreatedDate.HasValue && w.CreatedDate.Value.Date == day && w.Amount < 0).Sum(w => w.Amount)));
            }

            return View(model);
        }

        public async Task<IActionResult> ExportRevenue(string timeframe)
        {
            var data = await _reportService.GetRevenueReportAsync(timeframe, null, null);
            List<Order> orders = data.CurrentOrders ?? new List<Order>();

            var csv = new StringBuilder();
            csv.AppendLine("OrderId,Date,Amount,Status");
            foreach (var o in orders) csv.AppendLine($"{o.OrderId},{o.OrderDate},{o.TotalAmount},{o.Status}");

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"RevenueReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ExportProducts()
        {
            var data = await _reportService.GetProductReportAsync(null, null);
            List<Product> allProducts = data.AllProducts ?? new List<Product>();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Code,Name,Stock,Price,Value");
            foreach (var p in allProducts) csv.AppendLine($"{p.ProductId},{p.Code},{p.Name},{p.StockQuantity},{p.SellingPrice},{p.StockQuantity * p.SellingPrice}");
            
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"InventoryReport_{DateTime.Now:yyyyMMdd}.csv");
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
