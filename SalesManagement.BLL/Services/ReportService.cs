using Microsoft.EntityFrameworkCore;
using SalesManagement.BLL.Interfaces;
using SalesManagement.BLL.Models;
using SalesManagement.DAL.Data;
using SalesManagement.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportDTO> GetRevenueReportAsync(string timeframe, DateTime? startDate, DateTime? endDate)
        {
            var range = GetDateRange(timeframe, startDate, endDate);
            var start = range.Start;
            var end = range.End;

            var currentOrders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .ToListAsync();

            var duration = end - start;
            var prevStart = start.Subtract(duration);
            var prevEnd = start.AddSeconds(-1);
            var prevOrders = await _context.Orders
                .Where(o => o.OrderDate >= prevStart && o.OrderDate <= prevEnd)
                .ToListAsync();

            var currentRevenue = currentOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0);
            var prevRevenue = prevOrders.Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid").Sum(o => o.TotalAmount ?? 0);

            return new RevenueReportDTO
            {
                Start = start, End = end,
                CurrentOrders = currentOrders,
                TotalRevenue = currentRevenue,
                PrevRevenue = prevRevenue,
                PrevOrdersCount = prevOrders.Count
            };
        }

        public async Task<ProductReportDTO> GetProductReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? new DateTime(end.Year, end.Month, 1);

            var topSelling = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.OrderDate >= start && od.Order.OrderDate <= end && (od.Order.Status == "Completed" || od.Order.PaymentStatus == "Paid"))
                .ToListAsync();

            var allProducts = await _context.Products.ToListAsync();

            return new ProductReportDTO
            {
                Start = start, End = end,
                TopSellingRaw = topSelling,
                AllProducts = allProducts
            };
        }

        public async Task<FinancialReportDTO> GetFinancialReportAsync(DateTime? startDate, DateTime? endDate)
        {
             var end = endDate ?? DateTime.Now;
             var start = startDate ?? new DateTime(end.Year, end.Month, 1);

             var orders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .ToListAsync();

             var walletTrans = await _context.WalletTransactions
                .Where(w => w.CreatedDate >= start && w.CreatedDate <= end && w.Status == "Success")
                .ToListAsync();

             return new FinancialReportDTO
             {
                 Start = start, End = end,
                 Orders = orders,
                 WalletTransactions = walletTrans
             };
        }

        private (DateTime Start, DateTime End) GetDateRange(string timeframe, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue) return (start.Value, end.Value);
            var now = DateTime.Now;
            if (timeframe == "week") return (now.AddDays(-7), now);
            if (timeframe == "year") return (new DateTime(now.Year, 1, 1), new DateTime(now.Year, 12, 31));
            if (timeframe == "all") return (DateTime.MinValue, DateTime.MaxValue);
            return (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1));
        }
    }
}
