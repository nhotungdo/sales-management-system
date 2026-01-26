using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DiscountsController : Controller
    {
        private readonly Sales_Management.Data.SalesManagementContext _context;
        private readonly ILogger<DiscountsController> _logger;

        public DiscountsController(Sales_Management.Data.SalesManagementContext context, ILogger<DiscountsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Discounts
        public async Task<IActionResult> Index(string statusFilter, string searchString)
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(p => p.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Code.Contains(searchString));
            }

            var promotions = await query.OrderByDescending(p => p.StartDate).ToListAsync();
            ViewData["StatusFilter"] = statusFilter;
            ViewData["CurrentFilter"] = searchString;
            return View(promotions);
        }

        // GET: Admin/Discounts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Discounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            // 1. Setting the Validity Period: automatic StartDate
            if (!promotion.StartDate.HasValue)
            {
                promotion.StartDate = DateTime.Now;
            }

            // 3. Validation: StartDate < EndDate
            if (promotion.EndDate.HasValue && promotion.StartDate >= promotion.EndDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be greater than Start Date.");
            }

            if (ModelState.IsValid)
            {
                // Default status
                if (string.IsNullOrEmpty(promotion.Status))
                    promotion.Status = "Active";

                _context.Add(promotion);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created new voucher {promotion.Code}.");
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: Admin/Discounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            // 3. Prevent editing of expired codes
            if (promotion.Status == "Expired" || promotion.Status == "Disabled" || (promotion.EndDate.HasValue && promotion.EndDate < DateTime.Now))
            {
                 // We can show it readonly or redirect with message.
                 // For now, let's just warn or redirect. User requirement says "Prevent editing".
                 // Better UX: Allow viewing but disable save, or just show error.
                 // I will return the View but perhaps set a ViewBag so the View validates/disables inputs.
                 ViewBag.IsExpired = true;
            }

            return View(promotion);
        }

        // POST: Admin/Discounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.PromotionId)
            {
                return NotFound();
            }

            // Re-fetch original to check if it WAS expired before this edit attempted (security check)
            var original = await _context.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.PromotionId == id);
            if (original == null) return NotFound();

            bool wasExpired = original.Status == "Expired" || original.Status == "Disabled" || (original.EndDate.HasValue && original.EndDate < DateTime.Now);
            if (wasExpired)
            {
                 // Strict prevention
                 ModelState.AddModelError("", "Cannot edit an expired voucher.");
                 return View(promotion);
            }

            // Validation
            if (promotion.EndDate.HasValue && promotion.StartDate >= promotion.EndDate)
            {
                 ModelState.AddModelError("EndDate", "End Date must be greater than Start Date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Detect Status Change
                    if (original.Status != promotion.Status)
                    {
                        var logMsg = $"Status changed for voucher {promotion.Code} from {original.Status} to {promotion.Status} by Admin.";
                        _logger.LogInformation(logMsg);
                        // In a real app we might write to Audit Log table here
                    }

                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotion.PromotionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: Admin/Discounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: Admin/Discounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }

             return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionId == id);
        }
    }
}
