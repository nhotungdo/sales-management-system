using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.AspNetCore.Authorization;

namespace SaleManagement.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales")]
    public class HomeController : Controller
    {
        private readonly SalesManagementContext _context;

        public HomeController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            ViewBag.TotalRevenue = await _context.Invoices
                .SumAsync(i => i.Amount);

            ViewBag.TodayInvoices = await _context.Invoices
                .CountAsync(i => i.InvoiceDate >= today);

            var products = await _context.Products
                .Where(p => p.Status != "Deleted")
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();
            return View(products);
        }

    }
}
