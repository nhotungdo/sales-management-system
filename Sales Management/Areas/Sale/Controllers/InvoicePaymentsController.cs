using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.DAL.Data;

namespace SalesManagement.Web.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class InvoicePaymentsController : Controller
    {
        private readonly AppDbContext _context;

        public InvoicePaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Sale/InvoicePayments/Invoice/5
        public async Task<IActionResult> Invoice(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null) return NotFound();

            var transactions = await _context.WalletTransactions
                .Include(t => t.Wallet)
                    .ThenInclude(w => w.Customer)
                .Where(t => t.TransactionCode == $"INV-{invoiceId}")
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            ViewBag.Invoice = invoice;
            return View(transactions);
        }
    }
}
