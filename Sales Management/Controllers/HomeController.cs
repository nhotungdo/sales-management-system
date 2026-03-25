using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Web.ViewModels;
using SalesManagement.BLL.Interfaces;
using SalesManagement.Web.Models;
using Microsoft.Extensions.Logging;

namespace SalesManagement.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            int pageSize = 12;
            int pageNumber = page ?? 1;

            if (searchString != null) pageNumber = 1;
            else searchString = currentFilter;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var products = await _productService.GetPagedProductsAsync(pageNumber, pageSize, searchString, sortOrder);
            var totalCount = await _productService.GetTotalProductCountAsync(searchString);

            var viewModel = new HomeProductViewModel
            {
                Products = products.Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name,
                    SellingPrice = p.SellingPrice,
                    CoinPrice = p.CoinPrice,
                    StockQuantity = p.StockQuantity,
                    Status = p.Status,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl 
                                    ?? p.ProductImages.FirstOrDefault()?.ImageUrl
                }).ToList(),
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                SearchString = searchString,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productService.GetProductByIdAsync(id.Value);

            if (product == null || product.Status == "Deleted")
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
