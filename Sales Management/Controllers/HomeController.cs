using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using Sales_Management.ViewModels;

namespace Sales_Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SalesManagementContext _context;

        public HomeController(ILogger<HomeController> logger, SalesManagementContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var products = from s in _context.Products
                                     .Include(p => p.ProductImages)
                                     .Include(p => p.Category)
                           where s.Status == "Active"
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Name.Contains(searchString) || (s.Description != null && s.Description.Contains(searchString)));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(s => s.Name);
                    break;
                case "Price":
                    products = products.OrderBy(s => s.SellingPrice);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(s => s.SellingPrice);
                    break;
                case "Date":
                    products = products.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    products = products.OrderByDescending(s => s.CreatedDate);
                    break;
                default:
                    products = products.OrderByDescending(s => s.CreatedDate);
                    break;
            }

            int pageSize = 12;
            int pageNumber = (page ?? 1);
            int count = await products.CountAsync();
            
            // Ensure page number is valid
            if (pageNumber < 1) pageNumber = 1;
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var items = await products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new HomeProductViewModel
            {
                Products = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                SearchString = searchString,
                SortOrder = sortOrder
            };

            return View(viewModel);
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
