using Sales_Management.Models;

namespace Sales_Management.ViewModels
{
    public class HomeProductViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchString { get; set; }
        public string? SortOrder { get; set; }
        public int? CategoryId { get; set; }
    }
}
