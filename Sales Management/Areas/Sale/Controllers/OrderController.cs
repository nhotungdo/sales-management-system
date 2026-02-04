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
    public class OrderController : Controller
    {
        private readonly SalesManagementContext _context;

        public OrderController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Sales/Order
        public async Task<IActionResult> Index(string status, string searchString, int? pageNumber)
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }
            ViewData["CurrentStatus"] = status;

            if (!string.IsNullOrEmpty(searchString))
            {
                // Search by Order ID or Customer Name
                if (int.TryParse(searchString, out int orderId))
                {
                    orders = orders.Where(o => o.OrderId == orderId);
                }
                else
                {
                    orders = orders.Where(o => o.Customer.FullName.Contains(searchString));
                }
            }
            ViewData["CurrentFilter"] = searchString;

            orders = orders.OrderByDescending(o => o.OrderDate);

            int pageSize = 10;
            return View(await PaginatedList<Order>.CreateAsync(orders.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Sales/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Sales/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Simple validation of status flow could go here
            order.Status = newStatus;
            
            if (newStatus == "Paid" && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
            }

            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
