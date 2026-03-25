using System;
using System.Collections.Generic;
using SalesManagement.DAL.Entities;

namespace SalesManagement.BLL.Models
{
    public class RevenueReportDTO
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<Order>? CurrentOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PrevRevenue { get; set; }
        public int PrevOrdersCount { get; set; }
    }

    public class ProductReportDTO
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<OrderDetail>? TopSellingRaw { get; set; }
        public List<Product>? AllProducts { get; set; }
    }

    public class FinancialReportDTO
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<Order>? Orders { get; set; }
        public List<WalletTransaction>? WalletTransactions { get; set; }
    }
}
