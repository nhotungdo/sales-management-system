using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;

namespace Sales_Management.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class InvoicesController : Controller
    {
        private readonly SalesManagementContext _context;

        public InvoicesController(SalesManagementContext context)
        {
            _context = context;
        }

        // GET: Sale/Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(invoices);
        }
        // GET: Sale/Invoices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            var payments = await _context.WalletTransactions
                .Where(t => t.TransactionCode == $"INV-{id}")
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            ViewBag.Payments = payments;

            return View(invoice);
        }
    }
}
