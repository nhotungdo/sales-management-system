using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Services;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly IPayrollService _payrollService;
        private readonly SalesManagementContext _context;

        public PayrollController(IPayrollService payrollService, SalesManagementContext context)
        {
            _payrollService = payrollService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var m = month ?? DateTime.Now.Month;
            var y = year ?? DateTime.Now.Year;

            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .Where(p => p.Month == m && p.Year == y)
                .ToListAsync();

            ViewBag.Month = m;
            ViewBag.Year = y;

            return View(payrolls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int month, int year)
        {
            await _payrollService.GeneratePayrollForAllAsync(month, year);
            return RedirectToAction(nameof(Index), new { month, year });
        }

        public async Task<IActionResult> Details(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(p => p.PayrollId == id);
            
            if (payroll == null) return NotFound();

            return View(payroll);
        }
    }
}
