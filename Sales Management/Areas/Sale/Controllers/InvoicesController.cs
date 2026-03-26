using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManagement.Web.Areas.Sale.Controllers
{
    [Area("Sale")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IWalletService _walletService;

        public InvoicesController(IInvoiceService invoiceService, IWalletService walletService)
        {
            _invoiceService = invoiceService;
            _walletService = walletService;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);
            if (invoice == null) return NotFound();

            var payments = await _walletService.GetTransactionsAsync(null, null, null);
            var invoicePayments = payments.Where(t => t.InvoiceId == id).ToList();

            ViewBag.Payments = invoicePayments;
            return View(invoice);
        }
    }
}
