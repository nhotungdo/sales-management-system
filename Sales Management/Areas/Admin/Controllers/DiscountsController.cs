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

        // GET: Admin/Discounts (Danh sách mã giảm giá)
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

        // POST: Admin/Discounts/Create (Tạo mã giảm giá mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            // 1. Thiết lập thời gian hiệu lực: tự động gán StartDate nếu chưa có
            if (!promotion.StartDate.HasValue)
            {
                promotion.StartDate = DateTime.Now;
            }

            // 3. Validate: Ngày kết thúc phải sau ngày bắt đầu
            if (promotion.EndDate.HasValue && promotion.StartDate >= promotion.EndDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be greater than Start Date.");
            }

            if (ModelState.IsValid)
            {
                // Trạng thái mặc định
                if (string.IsNullOrEmpty(promotion.Status))
                    promotion.Status = "Active";

                _context.Add(promotion);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created new voucher {promotion.Code}.");
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        // GET: Admin/Discounts/Edit/5 (Chỉnh sửa mã giảm giá)
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

            // 3. Ngăn chặn chỉnh sửa mã đã hết hạn
            if (promotion.Status == "Expired" || promotion.Status == "Disabled" || (promotion.EndDate.HasValue && promotion.EndDate < DateTime.Now))
            {
                 // UX: Có thể chỉ xem (readonly) hoặc báo lỗi. Ở đây cảnh báo qua ViewBag.
                 ViewBag.IsExpired = true;
            }

            return View(promotion);
        }

        // POST: Admin/Discounts/Edit/5 (Lưu thay đổi mã giảm giá)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.PromotionId)
            {
                return NotFound();
            }

            // Lấy lại dữ liệu gốc để kiểm tra xem nó CÓ TỪNG hết hạn trước khi sửa không (check bảo mật)
            var original = await _context.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.PromotionId == id);
            if (original == null) return NotFound();

            bool wasExpired = original.Status == "Expired" || original.Status == "Disabled" || (original.EndDate.HasValue && original.EndDate < DateTime.Now);
            if (wasExpired)
            {
                 // Ngăn chặn nghiêm ngặt
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
                    // Phát hiện thay đổi trạng thái
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

        // GET: Admin/Discounts/Delete/5 (Xác nhận xóa)
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

        // POST: Admin/Discounts/Delete/5 (Xóa mã giảm giá)
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
