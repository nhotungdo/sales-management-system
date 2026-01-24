using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Models;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Sales_Management.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly SalesManagementContext _context;
        private readonly IConfiguration _configuration;
        private const decimal COIN_RATE = 10; // 1 Coin = 10 cents

        public WalletController(SalesManagementContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers
                .Include(c => c.Wallet)
                .ThenInclude(w => w.WalletTransactions)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                // Auto-create customer profile if missing
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return RedirectToAction("Login", "Account");

                customer = new Customer
                {
                    UserId = userId,
                    FullName = user.FullName ?? user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CreatedDate = DateTime.Now,
                    Type = "Personal",
                    CustomerLevel = "Regular"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            if (customer.Wallet == null)
            {
                // Auto-create wallet
                var wallet = new Wallet
                {
                    CustomerId = customer.CustomerId,
                    Balance = 0,
                    Status = "Active",
                    UpdatedDate = DateTime.Now
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
                customer.Wallet = wallet;
            }

            // Sort transactions desc
            customer.Wallet.WalletTransactions = customer.Wallet.WalletTransactions
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            return View(customer.Wallet);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTopUp(decimal coinAmount)
        {
            if (coinAmount <= 0)
            {
                TempData["Error"] = "Số lượng coin phải lớn hơn 0";
                return RedirectToAction(nameof(Index));
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers
                .Include(c => c.Wallet)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer?.Wallet == null) return RedirectToAction(nameof(Index));

            var moneyAmount = coinAmount * COIN_RATE;
            var transactionCode = $"TOPUP{DateTime.Now:yyMMddHHmmss}{new Random().Next(100, 999)}";

            var transaction = new WalletTransaction
            {
                WalletId = customer.Wallet.WalletId,
                Amount = coinAmount,
                AmountMoney = moneyAmount,
                TransactionType = "Deposit", // Changed from "TopUp" to match DB constraint
                Method = "System", // Changed from "VietQR" to match DB constraint ('VNPay', 'System')
                Status = "Pending",
                TransactionCode = transactionCode,
                Description = $"Nạp {coinAmount:N0} coin (VietQR)",
                CreatedDate = DateTime.Now
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Payment), new { id = transaction.TransactionId });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null || transaction.Status != "Pending")
            {
                return RedirectToAction(nameof(Index));
            }

            // VietQR Config
            var vietQrConfig = _configuration.GetSection("VietQR");
            var bankId = vietQrConfig["BankId"];
            var accountNo = vietQrConfig["AccountNo"];
            var accountName = vietQrConfig["AccountName"];
            var template = vietQrConfig["Template"];

            // Format: https://img.vietqr.io/image/{BankId}-{AccountNo}-{Template}.png?amount={Amount}&addInfo={Content}&accountName={Name}
            var qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={transaction.AmountMoney}&addInfo={transaction.TransactionCode}&accountName={Uri.EscapeDataString(accountName)}";

            ViewBag.QrUrl = qrUrl;
            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> CancelTransaction(int id)
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (transaction != null && transaction.Status == "Pending" && transaction.Wallet.CustomerId == customer.CustomerId)
            {
                transaction.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy giao dịch";
            }

            return RedirectToAction(nameof(Index));
        }
        
        // Polling endpoint for status check
        [HttpGet]
        public async Task<IActionResult> CheckStatus(int id)
        {
            var transaction = await _context.WalletTransactions.FindAsync(id);
            if (transaction == null) return NotFound();
            return Ok(new { status = transaction.Status });
        }
    }
}
