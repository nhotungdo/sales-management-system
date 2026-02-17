using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Sales_Management.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Authorization;

namespace Sales_Management.Controllers
{
    // [Authorize(Roles = "Admin")] - Removed to allow public/sales access as regular users
    public class ProductsController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(SalesManagementContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Products
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var products = from s in _context.Products.Include(p => p.Category).Include(p => p.ProductImages)
                           where s.Status == "Active" // Ensure only active products are shown like regular users
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Name.Contains(searchString) || s.Code.Contains(searchString));
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
            int pageIndex = (pageNumber ?? 1);
            int count = await products.CountAsync();
            
            if (pageIndex < 1) pageIndex = 1;
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            if (pageIndex > totalPages && totalPages > 0) pageIndex = totalPages;

            var items = await products.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new Sales_Management.ViewModels.HomeProductViewModel
            {
                Products = items,
                CurrentPage = pageIndex,
                TotalPages = totalPages,
                SearchString = searchString,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null || product.Status == "Deleted")
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
