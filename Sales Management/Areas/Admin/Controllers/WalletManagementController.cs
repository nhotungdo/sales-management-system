using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Entities;

namespace SalesManagement.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class WalletManagementController : Controller
    {
        private readonly IWalletService _walletService;

        public WalletManagementController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        // Xem danh sách giao dịch ví (có lọc theo trạng thái, ngày tháng)
        public async Task<IActionResult> Index(string status, DateTime? fromDate, DateTime? toDate)
        {
            var transactions = await _walletService.GetTransactionsAsync(status, fromDate, toDate);
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(transactions);
        }

        [HttpPost]
        // Duyệt giao dịch nạp tiền: Cập nhật trạng thái và cộng tiền vào ví
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _walletService.ApproveTransactionAsync(id);
            if (result)
            {
                TempData["Success"] = "Đã duyệt giao dịch thành công. Coin đã được cộng vào ví khách hàng.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        // Từ chối/Hủy giao dịch
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _walletService.RejectTransactionAsync(id);
            if (result)
            {
                TempData["Success"] = "Đã hủy giao dịch.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Xuất báo cáo giao dịch ra file CSV
        public async Task<IActionResult> Export(string status, DateTime? fromDate, DateTime? toDate)
        {
            var transactions = await _walletService.GetTransactionsAsync(status, fromDate, toDate);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Mã GD,Khách Hàng,Số ĐT,Số Coin,Số Tiền,Ngày Tạo,Trạng Thái,Nội Dung");

            foreach (var item in transactions)
            {
                var line = $"{item.TransactionCode},{item.Wallet?.Customer?.FullName},{item.Wallet?.Customer?.PhoneNumber},{item.Amount},{item.AmountMoney},{item.CreatedDate:yyyy-MM-dd HH:mm},{item.Status},{item.Description}";
                csv.AppendLine(line);
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"GiaoDichNapXu_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        // Quản lý danh sách tài khoản ví của khách hàng
        public async Task<IActionResult> Accounts(string search)
        {
            var wallets = await _walletService.GetAllWalletsAsync(search);
            ViewBag.Search = search;
            return View(wallets);
        }

        [HttpGet]
        // Form điều chỉnh số dư thủ công (Admin can thiệp)
        public async Task<IActionResult> AdjustBalance(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null) return NotFound();
            return View(wallet);
        }

        [HttpPost]
        // Xử lý điều chỉnh số dư (Cộng hoặc Trừ)
        public async Task<IActionResult> AdjustBalance(int walletId, string type, decimal amount, string reason)
        {
            var wallet = await _walletService.GetWalletByIdAsync(walletId);
            if (wallet == null) return NotFound();

            if (amount <= 0)
            {
                ModelState.AddModelError("", "Số lượng coin phải lớn hơn 0");
                return View(wallet);
            }

            var result = await _walletService.AdjustBalanceAsync(walletId, type, amount, reason);
            if (!result)
            {
                ModelState.AddModelError("", "Số dư hiện tại không đủ để trừ hoặc có lỗi hệ thống.");
                return View(wallet);
            }

            TempData["Success"] = "Đã cập nhật số dư thành công!";
            return RedirectToAction(nameof(Accounts));
        }
    }
}
