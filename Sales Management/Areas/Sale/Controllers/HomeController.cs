using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;

namespace SaleManagement.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class HomeController : Controller
    {
        private readonly SalesManagementContext _context;

        public HomeController(SalesManagementContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products
            .Include(p => p.ProductImages)
            .ToList();
            return View(products);


        }
    }
}
