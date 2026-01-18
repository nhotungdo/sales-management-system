using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Models;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DiscountsController : Controller
    {
    private readonly Sales_Management.Data.SalesManagementContext _context;

    public DiscountsController(Sales_Management.Data.SalesManagementContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var promotions = await _context.Promotions.OrderByDescending(p => p.StartDate).ToListAsync();
        return View(promotions);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Promotion promotion)
    {
        if (ModelState.IsValid)
        {
            // Set default status if not provided (though form should provide it)
            if (string.IsNullOrEmpty(promotion.Status))
                promotion.Status = "Active";

            _context.Add(promotion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(promotion);
    }
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
        return View(promotion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Promotion promotion)
    {
        if (id != promotion.PromotionId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
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

    private bool PromotionExists(int id)
    {
        return _context.Promotions.Any(e => e.PromotionId == id);
    }
    }
}
