namespace Sales_Management.Areas.Admin.ViewModels
{
    public class ReportsViewModel
    {
        // KPI Cards
        public int TotalOrders { get; set; }
        public double CompletionRate { get; set; } // Success Rate
        public decimal AverageOrderValue { get; set; }
        public double CancellationRate { get; set; } // Return/Cancel Rate

        // Performance Comparisons (mocking trends for now or calculating vs previous period if possible)
        public double OrdersGrowth { get; set; } 
        public double AovGrowth { get; set; }

        // Charts
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
        public List<int> OrdersChartData { get; set; } = new List<int>();

        // Top Lists
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal RevenueGenerated { get; set; }
    }
}
