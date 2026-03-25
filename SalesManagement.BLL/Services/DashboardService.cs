using Microsoft.EntityFrameworkCore;
using SalesManagement.BLL.Interfaces;
using SalesManagement.BLL.Models;
using SalesManagement.DAL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.BLL.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> GetDashboardStatsAsync()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // 1. Basic Stats
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
                .SumAsync(o => o.TotalAmount) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending" || o.Status == "Processing");

            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedDate.Month == currentMonth && u.CreatedDate.Year == currentYear);

            // 2. Recent Orders
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 3. Revenue over months
            var monthlyRevenues = await _context.Orders
                .Where(o => o.OrderDate.HasValue && 
                            o.OrderDate.Value.Year == currentYear && 
                            (o.Status == "Completed" || o.PaymentStatus == "Paid"))
                .GroupBy(o => o.OrderDate!.Value.Month)
                .Select(g => new { 
                    Month = g.Key, 
                    Total = g.Sum(o => o.TotalAmount) ?? 0 
                })
                .ToListAsync();

            var revenueData = Enumerable.Range(1, 12).Select(month => {
                var monthData = monthlyRevenues.FirstOrDefault(m => m.Month == month);
                return (int)(monthData?.Total ?? 0);
            }).ToList();

            // 4. Category statistics
            var categoryStats = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Product != null && od.Product.Category != null)
                .GroupBy(od => od.Product.Category!.Name)
                .Select(g => new { 
                    CategoryName = g.Key, 
                    Count = g.Sum(od => od.Quantity) 
                }) 
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var categoryLabels = categoryStats.Select(x => x.CategoryName ?? "Unknown").ToList();
            var categoryData = categoryStats.Select(x => x.Count).ToList();

            return new DashboardDTO
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                NewUsers = newUsers,
                RecentOrders = recentOrders,
                RevenueData = revenueData,
                CategoryLabels = categoryLabels,
                CategoryData = categoryData
            };
        }
    }
}
