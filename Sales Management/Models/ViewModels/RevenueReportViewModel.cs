using System;
using System.Collections.Generic;

namespace Sales_Management.Models.ViewModels
{
    public class RevenueReportViewModel
    {
        public string PeriodType { get; set; } // "Day", "Month", "Year"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }

        public List<ChartData> RevenueOverTime { get; set; } = new List<ChartData>();
        public List<ChartData> RevenueByEmployee { get; set; } = new List<ChartData>();
        public List<ChartData> RevenueByCategory { get; set; } = new List<ChartData>();
        public List<ProductPerformance> TopProducts { get; set; } = new List<ProductPerformance>();
    }

    public class ChartData
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class ProductPerformance
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int QuantitySold { get; set; }
        public decimal RevenueGenerated { get; set; }
    }
}
