using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;

namespace Sales_Management.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class WalletManagementController : Controller
    {
        private readonly SalesManagementContext _context;

        public WalletManagementController(SalesManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.WalletTransactions
                .Include(t => t.Wallet)
                .ThenInclude(w => w.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }
            
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedDate >= fromDate.Value);
            }
                
            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedDate <= toDate.Value.AddDays(1));
            }

            var transactions = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
            
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            
            return View(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
            
            if (transaction != null && transaction.Status == "Pending")
            {
                transaction.Status = "Success";
                // Update Wallet Balance
                transaction.Wallet.Balance = (transaction.Wallet.Balance ?? 0) + transaction.Amount;
                transaction.Wallet.UpdatedDate = DateTime.Now;
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt giao dịch thành công. Coin đã được cộng vào ví khách hàng.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
             var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
            
            if (transaction != null && transaction.Status == "Pending")
            {
                transaction.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy giao dịch";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export(string status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.WalletTransactions
                .Include(t => t.Wallet)
                .ThenInclude(w => w.Customer)
                .AsQueryable();

             if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);
            
            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedDate >= fromDate.Value);
                
            if (toDate.HasValue)
                query = query.Where(t => t.CreatedDate <= toDate.Value.AddDays(1));

            var transactions = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Ma GD,Khach Hang,So DT,So Coin,So Tien,Ngay Tao,Trang Thai,Noi Dung");

            foreach (var item in transactions)
            {
                var line = $"{item.TransactionCode},{item.Wallet?.Customer?.FullName},{item.Wallet?.Customer?.PhoneNumber},{item.Amount},{item.AmountMoney},{item.CreatedDate:yyyy-MM-dd HH:mm},{item.Status},{item.Description}";
                csv.AppendLine(line);
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"GiaoDichNapXu_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        public async Task<IActionResult> Accounts(string search)
        {
            var query = _context.Wallets
                .Include(w => w.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.Customer.FullName.Contains(search) 
                                      || w.Customer.Email.Contains(search) 
                                      || w.Customer.PhoneNumber.Contains(search));
            }

            var wallets = await query.OrderByDescending(w => w.Balance).ToListAsync();
            ViewBag.Search = search;
            return View(wallets);
        }

        [HttpGet]
        public async Task<IActionResult> AdjustBalance(int id)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.WalletId == id);

            if (wallet == null) return NotFound();

            return View(wallet);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustBalance(int walletId, string type, decimal amount, string reason)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.WalletId == walletId);

            if (wallet == null) return NotFound();
            if (amount <= 0)
            {
                ModelState.AddModelError("", "Số lượng coin phải lớn hơn 0");
                return View(wallet);
            }

            decimal adjustment = (type == "add") ? amount : -amount;
            
            // Check sufficient balance if deducting
            if (type == "deduct" && (wallet.Balance ?? 0) < amount)
            {
                ModelState.AddModelError("", "Số dư hiện tại không đủ để trừ.");
                return View(wallet);
            }

            // Create Transaction
            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Amount = adjustment,
                AmountMoney = 0, // Admin adjustment, no money involved usually or manual
                TransactionType = "Adjustment",
                Method = "System",
                Status = "Success", // Auto completed
                TransactionCode = $"ADJ{DateTime.Now:yyMMddHHmmss}{new Random().Next(100,999)}",
                Description = $"Admin điều chỉnh: {reason}",
                CreatedDate = DateTime.Now
            };

            // Update Balance
            wallet.Balance = (wallet.Balance ?? 0) + adjustment;
            wallet.UpdatedDate = DateTime.Now;

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật số dư thành công!";
            return RedirectToAction(nameof(Accounts));
        }
    }
}
