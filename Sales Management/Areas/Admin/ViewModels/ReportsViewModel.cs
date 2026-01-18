using System;
using System.Collections.Generic;

namespace Sales_Management.Areas.Admin.ViewModels
{
    public class RevenueReportViewModel
    {
        // KPIs
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public double CompletionRate { get; set; }
        public double CancellationRate { get; set; }
        
        // Comparison (vs Previous Period)
        public decimal RevenueGrowth { get; set; } // Percentage
        public decimal OrdersGrowth { get; set; }

        // Charts
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
        public List<int> OrdersChartData { get; set; } = new List<int>();
        
        // Current Filter Context
        public string Timeframe { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ProductReportViewModel
    {
        public List<TopProductViewModel> TopSellingProducts { get; set; } = new();
        public List<ProductInventoryViewModel> LongInStockProducts { get; set; } = new(); // > 30/60 days
        public List<ProductInventoryViewModel> LowStockProducts { get; set; } = new();
        
        // Stats
        public decimal InventoryTurnoverRate { get; set; } 
        public int TotalItemsInStock { get; set; }
        public decimal TotalInventoryValue { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class FinancialReportViewModel
    {
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal NetRevenue { get; set; } // Approximate
        
        public List<InvoiceStatViewModel> InvoiceStats { get; set; } = new();
        
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> CashInChartData { get; set; } = new();
        public List<decimal> CashOutChartData { get; set; } = new();

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int UnitsSold { get; set; }
        public decimal RevenueGenerated { get; set; }
    }

    public class ProductInventoryViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int StockQuantity { get; set; }
        public int DaysInStock { get; set; } // Approximation
        public decimal Value { get; set; }
    }

    public class InvoiceStatViewModel
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
