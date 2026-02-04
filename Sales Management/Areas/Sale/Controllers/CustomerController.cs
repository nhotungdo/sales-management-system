using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Sales_Management.Areas.Sale.Models;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    [Authorize(Roles = "Sales, Admin")]
    public class CustomerController : Controller
    {
        private readonly SalesManagementContext _context;

        public CustomerController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Sales/Customer
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            var customers = _context.Customers
                .Include(c => c.Orders)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c => c.FullName.Contains(searchString) || c.PhoneNumber.Contains(searchString) || c.Email.Contains(searchString));
            }
            ViewData["CurrentFilter"] = searchString;

            // Sort by most recent customers
            customers = customers.OrderByDescending(c => c.CreatedDate);

            int pageSize = 10;
            return View(await PaginatedList<Customer>.CreateAsync(customers.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Sales/Customer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Wallet)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }
    }
}
