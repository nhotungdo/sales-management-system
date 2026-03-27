using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.BLL.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SalesManagement.Web.Controllers
{
    /// <summary>
    /// CoinController – Trang Rewards: hiển thị tổng xu và lịch sử giao dịch.
    /// Chỉ dành cho Customer đã đăng nhập.
    /// </summary>
    [Authorize(Roles = "Customer")]
    public class CoinController : Controller
    {
        private readonly ICoinService _coinService;

        public CoinController(ICoinService coinService)
        {
            _coinService = coinService;
        }

        /// <summary>Trang chính: tổng xu + lịch sử giao dịch</summary>
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            ViewBag.TotalCoins = await _coinService.GetUserCoins(userId);
            ViewBag.Transactions = await _coinService.GetCoinTransactionHistory(userId);

            return View();
        }

        // ─── Private helpers ─────────────────────────────────────────────────────────

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
